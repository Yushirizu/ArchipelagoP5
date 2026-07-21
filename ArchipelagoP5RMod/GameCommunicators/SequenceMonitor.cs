using ArchipelagoP5RMod.Types;

namespace ArchipelagoP5RMod.GameCommunicators;

public static class SequenceMonitor
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static unsafe SequenceObj** _sequence;

    public static unsafe SequenceType CurrentSequenceType
    {
        get
        {
            try
            {
                if (_sequence == null || _sequence == (SequenceObj**)0x0)
                    return SequenceType.None;

                var seqObj = *_sequence;
                if (seqObj == (SequenceObj*)0x0 || (ulong)seqObj < 0x10000 || (ulong)seqObj > 0x7FFFFFFFFFFF)
                    return SequenceType.None;

                var args = seqObj->args;
                if (args == (SequenceInfo*)0x0 || (ulong)args < 0x10000 || (ulong)args > 0x7FFFFFFFFFFF)
                    return SequenceType.None;

                return args->CurrentSequence;
            }
            catch
            {
                return SequenceType.None;
            }
        }
    }

    public static unsafe SequenceType LastSequenceType
    {
        get
        {
            try
            {
                if (_sequence == null || _sequence == (SequenceObj**)0x0)
                    return SequenceType.None;

                var seqObj = *_sequence;
                if (seqObj == (SequenceObj*)0x0 || (ulong)seqObj < 0x10000 || (ulong)seqObj > 0x7FFFFFFFFFFF)
                    return SequenceType.None;

                var args = seqObj->args;
                if (args == (SequenceInfo*)0x0 || (ulong)args < 0x10000 || (ulong)args > 0x7FFFFFFFFFFF)
                    return SequenceType.None;

                return args->LastSequence;
            }
            catch
            {
                return SequenceType.None;
            }
        }
    }

    public static bool SequenceCanShowMessage => CurrentSequenceType is SequenceType.Battle or SequenceType.Field;

    public static unsafe void Setup()
    {
        AddressScanner.DelayedScanPattern(
            "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 ?? 48 8B 40 48 48 85 C0",
            address =>
            {
                int disp = *(int*)(address + 3);
                _sequence = (SequenceObj**)(address + 7 + disp);
                MyLogger.DebugLog($"Dynamically scanned _sequence: 0x{(IntPtr)_sequence:X}");
            });
    }
}