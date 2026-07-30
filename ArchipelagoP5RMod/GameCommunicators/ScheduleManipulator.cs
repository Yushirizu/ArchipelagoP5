using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod.GameCommunicators;

public class ScheduleManipulator
{
    readonly FlagManipulator _flagManipulator;

    [Function(CallingConventions.Fastcall)]
    private delegate IntPtr RunScheduleForDay(uint month, uint day, byte time);

    private IHook<RunScheduleForDay> _runScheduleForDayHook;
    public const byte SETUP_TIME = DateManipulator.SETUP_TIME;

    private readonly Action _onNewGameSetup;
    private bool _hasRunNewGameSetup = false;

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
    private unsafe IntPtr RunScheduleForDayImpl(uint month, uint day, byte time)
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
                    if (!_hasRunNewGameSetup)
                    {
                        _hasRunNewGameSetup = true;
                        MyLogger.DebugLog("Initial setup day hit: executing custom setup flow function (NewGameSetupSdl).");
                        return FlowFunctionWrapper.CallCustomFlowFunction(CustomApMethodsIndexes.NewGameSetupSdl);
                    }
                    else
                    {
                        MyLogger.DebugLog("Setup flow completed - deferring FirstTimeSetup and advancing schedule to Day 21 (April 22).");
                        Task.Delay(200).ContinueWith(_ => _onNewGameSetup?.Invoke());

                        var dateInfo = DateManipulator.DateInfoAddress;
                        if (dateInfo != null)
                        {
                            dateInfo->currTotalDays = 21;
                            dateInfo->nextTotalDays = 21;
                            dateInfo->currTime = 0;
                            dateInfo->nextTime = 0;
                        }
                        newMonth = 4;
                        newDay = 22;
                        break;
                    }
                case TypeOfDay.InfiltrationDay:
                    (newMonth, newDay) = GetInfiltrationDay(month, day, time);
                    break;
                case TypeOfDay.LoopDay:
                    (newMonth, newDay) = GetBoringDay(month, day, time);
                    break;
                case TypeOfDay.None:
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