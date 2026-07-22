using System;

namespace ArchipelagoP5RMod;

public static class NativeSafetyGuard
{
    public static bool IsValidPointer(IntPtr ptr)
    {
        ulong val = (ulong)ptr;
        return ptr != IntPtr.Zero && val >= 0x10000 && val <= 0x7FFFFFFFFFFF;
    }

    public static unsafe bool IsValidPointer(void* ptr)
    {
        return IsValidPointer((IntPtr)ptr);
    }

    public static bool ExecuteSafe(Action action, string componentName)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            MyLogger.LogException($"[SAFETY] {componentName}", ex);
            return false;
        }
    }
}
