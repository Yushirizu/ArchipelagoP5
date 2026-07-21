using System;
using System.IO;
using System.Runtime.InteropServices;
using ArchipelagoP5RMod.Configuration;
using Reloaded.Mod.Interfaces;

namespace ArchipelagoP5RMod;

public static class MyLogger
{
    private static ILogger _logger;
    private static bool _logDebug = true;
    private static readonly string LogFilePath1 = @"C:\Users\ulyss\AppData\Roaming\Reloaded-Mod-Loader-II\Logs\AP_ALWAYS_SAVED.log";
    private static readonly string LogFilePath2 = @"C:\Users\ulyss\RiderProjects\ArchipelagoP5RMod\AP_ALWAYS_SAVED.log";
    private static readonly object _lock = new();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int UnhandledExceptionFilterDelegate(IntPtr exceptionPointers);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr SetUnhandledExceptionFilter(UnhandledExceptionFilterDelegate lpTopLevelExceptionFilter);

    private static UnhandledExceptionFilterDelegate _nativeCrashDelegate;

    public static void Setup(ILogger logger, Config configuration)
    {
        _logger = logger;
        _logDebug = true;

        try
        {
            File.WriteAllText(LogFilePath1, $"=== AP NATIVE & MANAGED LOG STARTED {DateTime.Now} ===\n");
            File.WriteAllText(LogFilePath2, $"=== AP NATIVE & MANAGED LOG STARTED {DateTime.Now} ===\n");
        }
        catch { }

        // Register Native SEH Crash Filter
        _nativeCrashDelegate = OnNativeCrash;
        try
        {
            SetUnhandledExceptionFilter(_nativeCrashDelegate);
        }
        catch { }
    }

    private static unsafe int OnNativeCrash(IntPtr exceptionPointers)
    {
        try
        {
            if (exceptionPointers != IntPtr.Zero)
            {
                IntPtr* recordPtr = *(IntPtr**)exceptionPointers;
                if (recordPtr != null)
                {
                    uint code = *(uint*)recordPtr;
                    IntPtr address = *(IntPtr*)(recordPtr + 2);
                    Log($"[NATIVE CRASH SEH] Fatal Exception Code: 0x{code:X8} at Address: 0x{address:X16}");
                }
            }
        }
        catch { }
        return 0; // EXCEPTION_CONTINUE_SEARCH
    }

    public static void Log(string message)
    {
        string text = $"[{DateTime.Now:HH:mm:ss.fff}] [AP] {message}";
        _logger?.WriteLine(text);
        WriteToDisk(text);
    }

    public static void DebugLog(string message)
    {
        if (!_logDebug)
            return;
        string text = $"[{DateTime.Now:HH:mm:ss.fff}] [AP] [DEBUG] {message}";
        _logger?.WriteLine(text);
        WriteToDisk(text);
    }

    public static void LogException(string context, Exception ex)
    {
        string text = $"[{DateTime.Now:HH:mm:ss.fff}] [AP] [EXCEPTION in {context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        _logger?.WriteLine(text);
        WriteToDisk(text);
    }

    private static void WriteToDisk(string text)
    {
        lock (_lock)
        {
            try
            {
                using var fs1 = new FileStream(LogFilePath1, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var sw1 = new StreamWriter(fs1) { AutoFlush = true };
                sw1.WriteLine(text);
            }
            catch { }

            try
            {
                using var fs2 = new FileStream(LogFilePath2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var sw2 = new StreamWriter(fs2) { AutoFlush = true };
                sw2.WriteLine(text);
            }
            catch { }
        }
    }
}