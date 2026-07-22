using ArchipelagoP5RMod.Types;

namespace ArchipelagoP5RMod.GameCommunicators;

public static class SequenceMonitor
{
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private static unsafe SequenceObj** _sequence;

    public static unsafe SequenceType CurrentSequenceType
    {
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        get
        {
            try
            {
                if (_sequence == null || !NativeSafetyGuard.IsValidPointer((IntPtr)_sequence))
                    return SequenceType.None;

                var seqObj = *_sequence;
                if (seqObj == null || !NativeSafetyGuard.IsValidPointer((IntPtr)seqObj))
                    return SequenceType.None;

                var args = seqObj->args;
                if (args == null || !NativeSafetyGuard.IsValidPointer((IntPtr)args))
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
        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        get
        {
            try
            {
                if (_sequence == null || !NativeSafetyGuard.IsValidPointer((IntPtr)_sequence))
                    return SequenceType.None;

                var seqObj = *_sequence;
                if (seqObj == null || !NativeSafetyGuard.IsValidPointer((IntPtr)seqObj))
                    return SequenceType.None;

                var args = seqObj->args;
                if (args == null || !NativeSafetyGuard.IsValidPointer((IntPtr)args))
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