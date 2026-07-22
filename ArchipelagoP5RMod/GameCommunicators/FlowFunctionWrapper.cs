using System.Diagnostics;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

public static class FlowFunctionWrapper
{
    [Function(CallingConventions.Fastcall)]
    public delegate uint FlowFuncDelegate4();

    [Function(CallingConventions.Fastcall)]
    public delegate ulong FlowFuncDelegate8();

    [Function(CallingConventions.Fastcall)]
    public unsafe delegate long OnUpdateDelegate(GameTask* eventInfo);


    public delegate long BasicFlowFunc();

    public delegate void BitToggleType();

    private static unsafe FlowCommandData* backupCommandData;
    private static GCHandle temporaryCommandDataHandle;
    // private static int addedStack = 0;

    [Function(CallingConventions.Fastcall)]
    public delegate int GetFlowscriptInt4ArgType(byte paramIndex);

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunFlowFuncFromFileType(int param1, IntPtr file, uint fileSize, uint funcIndex);

    public static GetFlowscriptInt4ArgType GetFlowscriptInt4Arg { get; set; } = _ => 0;
    private static RunFlowFuncFromFileType RunFlowFuncFromFile { get; set; } = (_, _, _, _) => 0;

    private static IntPtr _getFlowscriptInt4ArgPtr;
    private static IntPtr _runFlowFuncFromFilePtr;

    private static IHook<OnUpdateDelegate> _onFlowUpdateDelegate;

    public static unsafe FlowCommandData* FlowCommandDataAddress
    {
        get => *_flowCommanderDataRefAddress;
        set => *_flowCommanderDataRefAddress = value;
    }

    public static unsafe bool IsAdrNullPointer => (IntPtr)_flowCommanderDataRefAddress == IntPtr.Zero;

    private static unsafe FlowCommandData** _flowCommanderDataRefAddress;

    public static void Setup(IReloadedHooks hooks)
    {
        AddressScanner.DelayedAddressHack(
            0x14A69890,
            address =>
            {
                unsafe
                {
                    int disp = *(int*)(address + 3);
                    _flowCommanderDataRefAddress = (FlowCommandData**)((byte*)address + 7 + (long)disp);
                    // Fallback to static RVA 0x2199008 if hook modified memory before scan
                    if ((nint)_flowCommanderDataRefAddress < 0x140000000 || (nint)_flowCommanderDataRefAddress > 0x143000000)
                    {
                        _flowCommanderDataRefAddress = (FlowCommandData**)(AddressScanner.BaseAddress + 0x2199008);
                    }
                    MyLogger.DebugLog($"Dynamically scanned _flowCommanderDataRefAddress: 0x{(IntPtr)_flowCommanderDataRefAddress:X}");
                }
            });

        AddressScanner.DelayedScanPattern(
            "48 8B 05 ?? ?? ?? ?? 48 85 C0 75 01 C3 8B 80 24 02 00 00 C3",
            address =>
            {
                MyLogger.DebugLog($"[FLOW] Dynamically scanned GetFlowscriptInt4Arg: 0x{address:X}");
                GetFlowscriptInt4Arg =
                    hooks.CreateWrapper<GetFlowscriptInt4ArgType>(address, out _getFlowscriptInt4ArgPtr);
            });
        AddressScanner.DelayedScanPattern(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 60 41 89 CE",
            address => RunFlowFuncFromFile =
                hooks.CreateWrapper<RunFlowFuncFromFileType>(address, out _runFlowFuncFromFilePtr));
    }

    private static unsafe long FlowOnUpdateImpl(GameTask* eventInfo)
    {
        // _logger.WriteLine("Called Flow onUpdate");
        var flowCommandData = (FlowCommandData*)eventInfo->args;
        // _logger.WriteLine($"Got the args from the event {(IntPtr)flowCommandData}");
        // _logger.WriteLine($"Func name => {(IntPtr)flowCommandData->CurrFuncName}");
        // _logger?.Write($"FlowFunctionWrapper flow function called with method \"");
        // for (int i = 0; i < 40; i++)
        // {
        //     _logger.Write(flowCommandData->CurrFuncName[i].ToString());
        // }
        // _logger?.WriteLine("\"");

        // var funcName = new string(flowCommandData->CurrFuncName);
        // _logger?.WriteLine($"FlowFunctionWrapper flow function called with method {funcName}");

        return _onFlowUpdateDelegate.OriginalFunction(eventInfo);
    }

    public static unsafe void ReplaceArgInt4(int index, int newValue)
    {
        var stackIndex = FlowCommandDataAddress->StackSize - index - 1;
        FlowCommandDataAddress->ArgData[stackIndex] = newValue;
    }

    private static readonly System.Collections.Generic.Stack<(IntPtr backup, GCHandle handle)> _commandDataStack = new();

    public static unsafe void CallFlowFunctionSetup(params long[] args)
    {
        IntPtr backup = FlowCommandDataAddress != null ? (IntPtr)FlowCommandDataAddress : IntPtr.Zero;

        FlowCommandData initialData = FlowCommandDataAddress != null ? *FlowCommandDataAddress : default;
        var handle = GCHandle.Alloc(initialData, GCHandleType.Pinned);
        _commandDataStack.Push((backup, handle));

        FlowCommandDataAddress = (FlowCommandData*)handle.AddrOfPinnedObject();

        int stackSize = args.Length;
        if (stackSize > 47)
        {
            MyLogger.DebugLog(
                $"ERROR: trying to push flow command call data stack to {stackSize} over maximum size 47");
            return;
        }

        FlowCommandDataAddress->StackSize = stackSize;
        for (int i = 0; i < args.Length; i++)
        {
            FlowCommandDataAddress->ArgData[stackSize - i - 1] = args[i];
            FlowCommandDataAddress->ArgTypes[stackSize - i - 1] = (byte)FlowParamType.Int;
        }
    }

    public static unsafe long CallFlowFunctionCleanup()
    {
        if (_commandDataStack.Count == 0) return 0;

        var (backup, handle) = _commandDataStack.Pop();
        long retVal = FlowCommandDataAddress != null ? FlowCommandDataAddress->ReturnValue : 0;

        FlowCommandDataAddress = (FlowCommandData*)backup;
        if (handle.IsAllocated)
        {
            handle.Free();
        }

        return retVal;
    }

    public static bool TestFlowscriptWrapper(int totalTests = 10)
    {
        Debug.Assert(GetFlowscriptInt4Arg != null, nameof(GetFlowscriptInt4Arg) + " != null");
        var random = new Random();
        var success = true;
        for (var testNum = 0; testNum < totalTests; testNum++)
        {
            int paramNum = random.Next() % 3 + 1;
            var parameters = new long[paramNum];
            for (var i = 0; i < paramNum; i++)
            {
                parameters[i] = random.Next();
            }

            CallFlowFunctionSetup(parameters);

            for (byte i = 0; i < paramNum; i++)
            {
                bool thisTestSuccess = parameters[i] == GetFlowscriptInt4Arg(i);
                if (!thisTestSuccess)
                {
                    success = false;
                    MyLogger.DebugLog(
                        $"Test {testNum} failed on {i}. {GetFlowscriptInt4Arg(i)} is not equal to parameters[{i}]: {parameters[i]}.");
                }
            }

            long testRetVal = random.NextInt64();
            unsafe
            {
                ((FlowCommandData*)temporaryCommandDataHandle.AddrOfPinnedObject())->ReturnValue = testRetVal;
            }

            long actualRetVal = CallFlowFunctionCleanup();

            if (testRetVal != actualRetVal)
            {
                success = false;
                MyLogger.DebugLog(
                    $"Test {testNum} failed on retVal. Expected value ({testRetVal:X}) is not equal to actual value ({actualRetVal:X}).");
            }
        }

        return success;
    }

    [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
    public static unsafe IntPtr CallCustomFlowFunction(CustomApMethodsIndexes func)
    {
        MyLogger.DebugLog($"[FLOW] Calling CustomFlowFunction: {func}");
        if (!BfLoader.IsLoaded || BfLoader.ApMethodsBfFilePointer == null)
        {
            MyLogger.DebugLog("ERROR: BfLoader is not initialized!");
            return IntPtr.Zero;
        }

        if (RunFlowFuncFromFile is null)
        {
            MyLogger.DebugLog("ERROR: RunFlowFuncFromFile is null!");
            return IntPtr.Zero;
        }

        try
        {
            var res = RunFlowFuncFromFile(8, (IntPtr)BfLoader.ApMethodsBfFilePointer, BfLoader.ApMethodsBfFileLength, (uint)func);
            MyLogger.DebugLog($"[FLOW] CustomFlowFunction {func} completed with result 0x{res:X}");
            return res;
        }
        catch (Exception ex)
        {
            MyLogger.LogException($"CallCustomFlowFunction {func}", ex);
            return IntPtr.Zero;
        }
    }
}