using ArchipelagoP5RMod.GameCommunicators;
using ArchipelagoP5RMod.Types;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;

namespace ArchipelagoP5RMod;

public class DateManipulator
{
    public const uint CALENDAR_ANIM_TOGGLE = 0x50000000 + 17;

    [Function(CallingConventions.Fastcall)]
    private delegate void DateDisplayToggle(long shouldDisplay, uint param2);

    private DateDisplayToggle _dateDisplay;

    private FlowFunctionWrapper.BasicFlowFunc _dateDisplayFlow;

    /* Fields */
    private readonly FlagManipulator _flagManipulator;

    public static unsafe bool IsDateInfoValid => _dateInfoRefAddress != null && *_dateInfoRefAddress != null && (ulong)*_dateInfoRefAddress >= 0x10000 && (ulong)*_dateInfoRefAddress <= 0x7FFFFFFFFFFF;
    public static unsafe short CurrTotalDays => IsDateInfoValid ? DateInfoAddress->currTotalDays : (short)0;
    public static unsafe DateInfo* DateInfoAddress => IsDateInfoValid ? *_dateInfoRefAddress : null;
    private static unsafe DateInfo** _dateInfoRefAddress;

    private IntPtr _dateDisplayAdr;
    private IntPtr _dateDisplayFlowAdr;

    private static int[] DAYS_IN_MONTH = [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    public const short SETUP_TOTAL_DAY = 6;
    public const byte SETUP_TIME = 4;
    private static readonly SortedSet<short> _allLoopDates = [21, 52];
    private SortedSet<short> _loopDates = [21, 52];

    public delegate void OnDateChangedHandler(short currTotalDays, byte currTime);

    public event OnDateChangedHandler OnDateChanged = delegate { };

    private bool disablingCalendarAnimation = false;

    public DateManipulator(GameTaskListener gameTaskListener, FlagManipulator flagManipulator, IReloadedHooks hooks)
    {
        _flagManipulator = flagManipulator;

        AddressScanner.DelayedScanPattern(
            "40 53 48 83 EC 60 48 8B 59 ?? 48 63 03",
            address =>
            {
                gameTaskListener.ListenForTaskCreate(address, OnTimeUpdateCreated);
                gameTaskListener.ListenForTaskDestroy(address, OnTimeUpdateDestroyed);
                unsafe
                {
                    byte* ptr = (byte*)address;
                    for (int offset = 100; offset < 120; offset++)
                    {
                        if (ptr[offset] == 0x48 && ptr[offset + 1] == 0x8B && ptr[offset + 2] == 0x0D)
                        {
                            int disp = *(int*)(ptr + offset + 3);
                            _dateInfoRefAddress = (DateInfo**)(ptr + offset + 7 + disp);
                            MyLogger.DebugLog($"Dynamically scanned _dateInfoRefAddress: 0x{(IntPtr)_dateInfoRefAddress:X}");
                            break;
                        }
                    }
                }
            });

        AddressScanner.DelayedScanPattern(
            "40 57 48 83 EC 70 48 8B 05 ?? ?? ?? ?? 48 8B 78 ??",
            address => _dateDisplay = hooks.CreateWrapper<DateDisplayToggle>(address, out _dateDisplayAdr));
        //
        // AddressScanner.DelayedScanPattern(
        //     "48 83 EC 28 31 C9 E8 ?? ?? ?? ?? 89 C1 E8 ?? ?? ?? ?? B8 01 00 00 00",
        //     address => _dateDisplayFlow =
        //         hooks.CreateWrapper<FlowFunctionWrapper.BasicFlowFunc>(address, out _dateDisplayFlowAdr));

        MyLogger.DebugLog("Created DateManipulator Hooks");
    }

    public void SetDateDisplay(bool display)
    {
        MyLogger.DebugLog($"SetDateDisplay called with display: {display}");
        _dateDisplay(display ? 100L : 0L, 1);

        // FlowFunctionWrapper.CallFlowFunctionSetup(display ? 1L : 0L);
        //
        // _dateDisplayFlow();
        //
        // FlowFunctionWrapper.CallFlowFunctionCleanup();
    }

    public static Month GetMonthFromTotalDays(int totalGameDays)
    {
        int month = 3;
        totalGameDays %= 365;
        // Looks a little odd because it mimics the decompiled C version of this method in the game.
        int i = 0;
        do
        {
            if (totalGameDays < DAYS_IN_MONTH[month % 12])
                break;
            totalGameDays -= DAYS_IN_MONTH[month % 12];
            i += 1;
            month += 1 % 12;
        } while (i < 12);

        return (Month)month;
    }

    public static int GetTotalDays(uint month, uint day)
    {
        if (month < 1 || month > 12)
            return 0;

        if (month == 4)
            return (int)Math.Max(0, (int)day - 1);

        uint prevMonth = month - 1;
        if (month - 1 == 0)
        {
            prevMonth = 12;
        }

        int totalDays = DAYS_IN_MONTH[(prevMonth - 1) % 12];
        long iVar1 = prevMonth - 1;
        long monthCounter = iVar1;
        int loopGuard = 0;
        while (prevMonth != 4 && loopGuard++ < 15)
        {
            if (monthCounter == 0)
            {
                monthCounter = 12;
            }

            totalDays += DAYS_IN_MONTH[(monthCounter - 1) % 12];
            iVar1 = monthCounter - 1;
            prevMonth = (uint)monthCounter;
            monthCounter = iVar1;
        }

        return (int)(day + -1 + totalDays);
    }

    public static TypeOfDay ToTypeOfDay(uint month, uint day)
    {
        int totalDays = GetTotalDays(month, day);
        return ToTypeOfDay(totalDays);
    }

    public static TypeOfDay ToTypeOfDay(long totalDays)
    {
        if (totalDays == SETUP_TOTAL_DAY)
        {
            return TypeOfDay.Setup;
        }

        if (_allLoopDates.Contains((short)totalDays))
        {
            return TypeOfDay.LoopDay;
        }

        if (_allLoopDates.Contains((short)(totalDays - 1)))
        {
            return TypeOfDay.InfiltrationDay;
        }

        return TypeOfDay.None;
    }

    private void OnTimeUpdateCreated()
    {
        ManipulateInGameDate();
    }

    private void OnTimeUpdateDestroyed()
    {
        if (disablingCalendarAnimation)
        {
            _flagManipulator.ToggleBit(CALENDAR_ANIM_TOGGLE);
            disablingCalendarAnimation = false;
        }

        unsafe
        {
            if (IsDateInfoValid)
            {
                OnDateChanged.Invoke(DateInfoAddress->currTotalDays, DateInfoAddress->currTime);
            }
        }
    }

    private short NextDay(short currentDay)
    {
        MyLogger.DebugLog($"NextDay called currentDay:{currentDay}");
        foreach (short date in _loopDates)
        {
            if (currentDay < date)
            {
                MyLogger.DebugLog($"Got day:{date}");
                return date;
            }
        }

        return (short)(_loopDates.First() - 1);
    }

    private unsafe void ManipulateInGameDate()
    {
        var dateInfo = DateInfoAddress;
        if (dateInfo == null) return;
        MyLogger.DebugLog($"ManipulateInGameDate called currentDay:{dateInfo->currTotalDays} time:{dateInfo->currTime}");

        // Only mess with dates at the end of the day
        if (dateInfo->currTime < 6 && dateInfo->currTotalDays >= SETUP_TOTAL_DAY)
        {
            return;
        }

        if (dateInfo->currTotalDays <= SETUP_TOTAL_DAY)
        {
            MyLogger.DebugLog("Setup day active, maintaining setup date.");
            dateInfo->nextTime = SETUP_TIME;
            dateInfo->nextTotalDays = SETUP_TOTAL_DAY;
            return;
        }

        Palaces palace = ConquestManager.TotalDaysToPalace(dateInfo->currTotalDays);
        bool isCurrentlyInfiltrating = InfiltrationManager.IsCurrentlyInfiltrating(_flagManipulator, palace);
        if (_loopDates.Contains(dateInfo->currTotalDays) && isCurrentlyInfiltrating)
        {
            dateInfo->nextTime = 0;
            dateInfo->nextTotalDays = (short)(dateInfo->currTotalDays + 1);
            return;
        }

        dateInfo->nextTime = 0;
        dateInfo->nextTotalDays = NextDay(dateInfo->currTotalDays);
        if (dateInfo->nextTotalDays < dateInfo->currTotalDays)
        {
            dateInfo->nextTime = 7;
            _flagManipulator.ToggleBit(CALENDAR_ANIM_TOGGLE);
            disablingCalendarAnimation = true;
        }
    }
}