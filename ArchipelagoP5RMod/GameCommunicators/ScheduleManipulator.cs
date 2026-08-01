using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public enum ScheduleState
{
    Uninitialized,
    SetupFlowStarted,
    IntroWarpCompleted
}

public class ScheduleManipulator
{
    readonly FlagManipulator _flagManipulator;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;
    public const byte SETUP_TIME = DateManipulator.SETUP_TIME;

    private readonly Action _onNewGameSetup;
    public ScheduleState CurrentState { get; private set; } = ScheduleState.Uninitialized;

    public ScheduleManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks, Action onNewGameSetup)
    {
        _flagManipulator = flagManipulator;
        _onNewGameSetup = onNewGameSetup;

        AddressScanner.DelayedScanPattern(
            "40 55 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 8B 05 ?? ?? ?? ??",
            address =>
            {
                MyLogger.DebugLog($"[SCHEDULE:INIT] Dynamically scanned RunScheduleForDay: 0x{address:X}");
                _runScheduleForDayHook =
                    hooks.CreateHook<RunScheduleForDay>(RunScheduleForDayImpl, address).Activate();
            });
    }

    [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
    private unsafe IntPtr RunScheduleForDayImpl(uint month, uint day, byte time)
    {
        try
        {
            if (month < 1 || month > 12 || day < 1 || day > 31)
            {
                MyLogger.DebugLog($"[SCHEDULE:WARN] Invalid schedule parameters received: month:{month} day:{day} time:{time}");
                return _runScheduleForDayHook?.OriginalFunction(month, day, time) ?? IntPtr.Zero;
            }

            MyLogger.DebugLog($"[SCHEDULE:CALL] RunScheduleForDayImpl month:{month} day:{day} time:{time} (State:{CurrentState})");
            uint newMonth = month;
            uint newDay = day;
            byte newTime = time;

            var typeOfDay = DateManipulator.ToTypeOfDay(month, day);
            MyLogger.DebugLog($"[SCHEDULE:CHECK] typeOfDay: {typeOfDay}");

            switch (typeOfDay)
            {
                case TypeOfDay.Setup:
                    if (CurrentState == ScheduleState.Uninitialized)
                    {
                        CurrentState = ScheduleState.SetupFlowStarted;
                        MyLogger.DebugLog("[SCHEDULE:SETUP] Initial setup day hit: executing custom setup flow function (NewGameSetupSdl).");
                        return FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NewGameSetupSdl);
                    }
                    else if (CurrentState == ScheduleState.SetupFlowStarted)
                    {
                        CurrentState = ScheduleState.IntroWarpCompleted;
                        MyLogger.DebugLog("[SCHEDULE:WARP] Setup flow completed - deferring FirstTimeSetup and advancing schedule to Day 21 (April 22).");
                        Task.Delay(500).ContinueWith(_ => _onNewGameSetup?.Invoke());

                        newMonth = 4;
                        newDay = 22;
                        newTime = 0;
                    }
                    break;
                case TypeOfDay.InfiltrationDay:
                    (newMonth, newDay) = GetInfiltrationDay(month, day, time);
                    MyLogger.DebugLog($"[SCHEDULE:INFILTRATION] Mapped infiltration day (m:{month}, d:{day}) -> (m:{newMonth}, d:{newDay})");
                    break;
                case TypeOfDay.LoopDay:
                    (newMonth, newDay) = GetBoringDay(month, day, time);
                    MyLogger.DebugLog($"[SCHEDULE:LOOP] Mapped loop day (m:{month}, d:{day}) -> (m:{newMonth}, d:{newDay})");
                    break;
                case TypeOfDay.None:
                default:
                    (newMonth, newDay) = GetBoringDay(month, day, time);
                    break;
            }

            if (_runScheduleForDayHook == null)
            {
                MyLogger.DebugLog("[SCHEDULE:ERROR] _runScheduleForDayHook is null!");
                return IntPtr.Zero;
            }

            try
            {
                if (newMonth != month || newDay != day || newTime != time)
                {
                    MyLogger.DebugLog($"[SCHEDULE:REDIRECT] Invoking OriginalFunction with redirected date: (m:{month}, d:{day}, t:{time}) -> (m:{newMonth}, d:{newDay}, t:{newTime})");
                }
                return _runScheduleForDayHook.OriginalFunction(newMonth, newDay, newTime);
            }
            catch (Exception ex)
            {
                MyLogger.LogException("RunScheduleForDay Native OriginalFunction", ex);
                return IntPtr.Zero;
            }
        }
        catch (Exception ex)
        {
            MyLogger.LogException("RunScheduleForDayImpl", ex);
            return IntPtr.Zero;
        }
    }

    private (uint month, uint day) GetBoringDay(uint month, uint day, byte time)
    {
        if (month == 4 && day < 7)
        {
            return (4, 1);
        }

        return (month, day);
    }

    private (uint month, uint day) GetInfiltrationDay(uint month, uint day, byte time)
    {
        return (4, 28);
    }
}