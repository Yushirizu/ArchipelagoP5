using System.Collections.Frozen;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class GameTaskListener
{
    private readonly IReloadedHooks _hooks;

    [Function(CallingConventions.Fastcall)]
    public unsafe delegate IntPtr GameTaskOnUpdate(GameTask* gameTask, float arg2, IntPtr arg3, float arg4);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate void GameTaskOnDestroy(GameTask* eventInfo);

    [Function(CallingConventions.Fastcall)]
    private unsafe delegate IntPtr CreateGameTaskType(IntPtr param_1, char* objName, byte param_3, byte param_4,
        int param_5, uint param_6, GameTaskOnUpdate* runtimeFunc, long param_8, GameTaskOnDestroy* onDestroyFunc,
        IntPtr args, IntPtr param_11);


    private readonly Dictionary<int, IReverseWrapper<GameTaskOnDestroy>> _temporaryHooks = new();

    private readonly Dictionary<IntPtr, Action> _onCreateListeners = new();
    private readonly Dictionary<IntPtr, Action> _onDestroyListeners = new();

    private readonly Dictionary<IntPtr, GameTaskOnDestroy> originalOnDestroyFuncPtr = new();
    private readonly Dictionary<IntPtr, IntPtr> originalOnDestroyHooks = new();

    private FrozenDictionary<IntPtr, Action> _frozenCreateListeners;
    private FrozenDictionary<IntPtr, Action> _frozenDestroyListeners;

    private IHook<CreateGameTaskType> _createGameTask;
    private readonly IReverseWrapper<GameTaskOnDestroy> _onDestroyWrapperHook;

    public unsafe GameTaskListener(IReloadedHooks hooks)
    {
        _hooks = hooks;

        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 56 41 57 48 83 EC 20 48 8B F1 41 8B E9",
            address => _createGameTask =
                hooks.CreateHook<CreateGameTaskType>(CreateGameTaskImpl, address).Activate());

        _onDestroyWrapperHook = hooks.CreateReverseWrapper<GameTaskOnDestroy>(OnDestroyWrapper);
    }

    /**
    * Freezes the listeners for a performance benefit.
    */
    public void FreezeListeners()
    {
        _frozenCreateListeners = _onCreateListeners.ToFrozenDictionary();
        _frozenDestroyListeners = _onDestroyListeners.ToFrozenDictionary();
    }

    public void ListenForTaskCreate(IntPtr runtimeFunc, Action callback)
    {
        if (_frozenCreateListeners is not null)
        {
            throw new InvalidOperationException("Tried to add a listener to the task after the listeners were frozen.");
        }

        if (_onCreateListeners.ContainsKey(runtimeFunc))
        {
            _onCreateListeners[runtimeFunc] += callback;
        }
        else
        {
            _onCreateListeners.Add(runtimeFunc, callback);
        }
    }

    public void ListenForTaskDestroy(IntPtr runtimeFunc, Action callback)
    {
        if (_frozenDestroyListeners is not null)
        {
            throw new InvalidOperationException("Tried to add a listener to the task after the listeners were frozen.");
        }

        if (_onDestroyListeners.ContainsKey(runtimeFunc))
        {
            _onDestroyListeners[runtimeFunc] += callback;
        }
        else
        {
            _onDestroyListeners.Add(runtimeFunc, callback);
        }
    }

    private static readonly List<object> _pinnedWrapperDelegates = new();

    private unsafe void OnDestroyWrapper(GameTask* gameTask)
    {
        if (gameTask == null || (ulong)gameTask < 0x10000 || (ulong)gameTask > 0x7FFFFFFFFFFF) return;

        IntPtr funcPtr = gameTask->runtimeFunc;

        if (_frozenDestroyListeners != null && _frozenDestroyListeners.ContainsKey(funcPtr))
        {
            _frozenDestroyListeners[funcPtr].Invoke();
        }
        else if (_onDestroyListeners.ContainsKey(funcPtr))
        {
            _onDestroyListeners[funcPtr].Invoke();
        }

        if (originalOnDestroyFuncPtr.TryGetValue(funcPtr, out var origFunc))
        {
            origFunc.Invoke(gameTask);
        }
    }

    private unsafe IntPtr CreateGameTaskImpl(IntPtr param_1, char* taskName, byte param_3, byte param_4,
        int param_5, uint param_6, GameTaskOnUpdate* runtimeFunc, long param_8, GameTaskOnDestroy* onDestroyFunc,
        IntPtr args, IntPtr param_11)
    {
        if (_createGameTask == null) return IntPtr.Zero;

        IntPtr funcPtr = (IntPtr)runtimeFunc;

        bool hasCreateListener = _frozenCreateListeners != null 
            ? _frozenCreateListeners.ContainsKey(funcPtr) 
            : _onCreateListeners.ContainsKey(funcPtr);

        if (hasCreateListener)
        {
            if (_frozenCreateListeners != null) _frozenCreateListeners[funcPtr].Invoke();
            else _onCreateListeners[funcPtr].Invoke();
        }

        GameTaskOnDestroy* myOnDestroy;
        bool hasDestroyListener = _frozenDestroyListeners != null 
            ? _frozenDestroyListeners.ContainsKey(funcPtr) 
            : _onDestroyListeners.ContainsKey(funcPtr);

        if (hasDestroyListener)
        {
            if (!originalOnDestroyFuncPtr.ContainsKey(funcPtr) && onDestroyFunc != null)
            {
                var wrapper = _hooks.CreateWrapper<GameTaskOnDestroy>((IntPtr)onDestroyFunc, out var addr);
                _pinnedWrapperDelegates.Add(wrapper);
                originalOnDestroyFuncPtr[funcPtr] = wrapper;
                originalOnDestroyHooks.TryAdd(funcPtr, addr);
            }
            myOnDestroy = (GameTaskOnDestroy*)_onDestroyWrapperHook.WrapperPointer;
        }
        else
        {
            myOnDestroy = onDestroyFunc;
        }

        return _createGameTask.OriginalFunction(param_1, taskName, param_3, param_4, param_5, param_6, runtimeFunc,
            param_8, myOnDestroy, args, param_11);
    }
}