using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class ScheduleManipulator
{
    readonly FlagManipulator _flagManipulator;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;
    private const byte SETUP_TIME = DateManipulator.SETUP_TIME;

    private readonly Action _onNewGameSetup;

    public ScheduleManipulator(FlagManipulator flagManipulator, IReloadedHooks hooks, Action onNewGameSetup)
    {
        _flagManipulator = flagManipulator;
        _onNewGameSetup = onNewGameSetup;

        AddressScanner.DelayedScanPattern(
            "40 55 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 8B 05 ?? ?? ?? ??",
            address =>
            {
                MyLogger.DebugLog($"[SCHEDULE] Dynamically scanned RunScheduleForDay: 0x{address:X}");
                _runScheduleForDayHook =
                    hooks.CreateHook<RunScheduleForDay>(RunScheduleForDayImpl, address).Activate();
            });
    }

    [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
    private IntPtr RunScheduleForDayImpl(uint month, uint day, byte time)
    {
        try
        {
            MyLogger.DebugLog($"[SCHEDULE] RunScheduleForDayImpl month:{month} day:{day} time:{time}");
            uint newMonth = month;
            uint newDay = day;

            var typeOfDay = DateManipulator.ToTypeOfDay(month, day);
            MyLogger.DebugLog($"[SCHEDULE] typeOfDay: {typeOfDay}");

            switch (typeOfDay)
            {
                case TypeOfDay.Setup:
                    if (time == SETUP_TIME)
                    {
                        MyLogger.DebugLog("Trying to call custom schedule for setup day.");
                        _onNewGameSetup?.Invoke();
                        return FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NewGameSetupSdl);
                    }

                    (newMonth, newDay) = GetBoringDay(month, day, time);
                    break;
                case TypeOfDay.InfiltrationDay:
                    (newMonth, newDay) = GetInfiltrationDay(month, day, time);
                    break;
                case TypeOfDay.None:
                case TypeOfDay.LoopDay:
                default:
                    (newMonth, newDay) = GetBoringDay(month, day, time);
                    break;
            }

            if (_runScheduleForDayHook == null) return IntPtr.Zero;
            try
            {
                return _runScheduleForDayHook.OriginalFunction(newMonth, newDay, time);
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