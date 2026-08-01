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
    private static readonly string LogFilePath1 = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Reloaded-Mod-Loader-II", "Logs", "AP_ALWAYS_SAVED.log");
    private static readonly string LogFilePath2 = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "AP_ALWAYS_SAVED.log");
    private static readonly object _lock = new();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int UnhandledExceptionFilterDelegate(IntPtr exceptionPointers);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int VectoredExceptionHandlerDelegate(IntPtr exceptionInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr AddVectoredExceptionHandler(uint first, VectoredExceptionHandlerDelegate handler);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr SetUnhandledExceptionFilter(UnhandledExceptionFilterDelegate lpTopLevelExceptionFilter);

    private static UnhandledExceptionFilterDelegate _nativeCrashDelegate;
    private static VectoredExceptionHandlerDelegate _vehDelegate;

    public static void Setup(ILogger logger, Config configuration)
    {
        _logger = logger;
        _logDebug = true;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath1)!);
            File.WriteAllText(LogFilePath1, $"=== AP NATIVE & MANAGED LOG STARTED {DateTime.Now} ===\n");
        }
        catch { }

        try
        {
            File.WriteAllText(LogFilePath2, $"=== AP NATIVE & MANAGED LOG STARTED {DateTime.Now} ===\n");
        }
        catch { }

        // Register Native SEH & VEH Crash Filters
        _nativeCrashDelegate = OnNativeCrash;
        _vehDelegate = OnVectoredException;
        try
        {
            SetUnhandledExceptionFilter(_nativeCrashDelegate);
            AddVectoredExceptionHandler(1, _vehDelegate);
        }
        catch { }
    }

    private static unsafe int OnVectoredException(IntPtr exceptionInfo)
    {
        try
        {
            if (exceptionInfo != IntPtr.Zero)
            {
                IntPtr* pointers = (IntPtr*)exceptionInfo;
                IntPtr rec = pointers[0];
                if (rec != IntPtr.Zero)
                {
                    uint code = *(uint*)rec;
                    if (code == 0xC0000005) // EXCEPTION_ACCESS_VIOLATION
                    {
                        IntPtr address = *(IntPtr*)(rec + 16);
                        Log($"[VEH NATIVE ACCESS VIOLATION] Fault at 0x{address:X16}");
                    }
                }
            }
        }
        catch { }
        return 0; // EXCEPTION_CONTINUE_SEARCH
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

    private static string _lastMessage = string.Empty;
    private static int _repeatCount = 0;
    private const int MaxRepeatsBeforeSuppression = 5;

    private static string FormatPrefix() => $"[{DateTime.Now:HH:mm:ss.fff}] [T{Environment.CurrentManagedThreadId}] [AP]";

    public static void Log(string message)
    {
        ProcessAndWriteLog($"{FormatPrefix()} {message}", message);
    }

    public static void DebugLog(string message)
    {
        if (!_logDebug)
            return;
        ProcessAndWriteLog($"{FormatPrefix()} [DEBUG] {message}", message);
    }

    public static void LogException(string context, Exception ex)
    {
        ProcessAndWriteLog($"{FormatPrefix()} [EXCEPTION in {context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", $"{context}:{ex.GetType().Name}");
    }

    private static void ProcessAndWriteLog(string fullFormattedText, string rawKey)
    {
        lock (_lock)
        {
            if (rawKey == _lastMessage)
            {
                _repeatCount++;
                if (_repeatCount == MaxRepeatsBeforeSuppression + 1)
                {
                    string suppressText = $"{FormatPrefix()} [LOG-SUPPRESSED] Identical log message repeating, further duplicate entries suppressed...";
                    _logger?.WriteLine(suppressText);
                    WriteToDiskInternal(suppressText);
                }
                if (_repeatCount > MaxRepeatsBeforeSuppression)
                {
                    return; // Suppress duplicate log line
                }
            }
            else
            {
                if (_repeatCount > MaxRepeatsBeforeSuppression)
                {
                    string summaryText = $"{FormatPrefix()} [LOG-SUMMARY] Previous suppressed log entry repeated {_repeatCount} times total.";
                    _logger?.WriteLine(summaryText);
                    WriteToDiskInternal(summaryText);
                }
                _lastMessage = rawKey;
                _repeatCount = 1;
            }

            _logger?.WriteLine(fullFormattedText);
            WriteToDiskInternal(fullFormattedText);
        }
    }

    private static void WriteToDiskInternal(string text)
    {
        try
        {
            using var fs1 = new FileStream(LogFilePath1, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var sw1 = new StreamWriter(fs1) { AutoFlush = true };
            sw1.WriteLine(text);
            sw1.Flush();
            fs1.Flush(true);
        }
        catch { }

        try
        {
            using var fs2 = new FileStream(LogFilePath2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var sw2 = new StreamWriter(fs2) { AutoFlush = true };
            sw2.WriteLine(text);
            sw2.Flush();
            fs2.Flush(true);
        }
        catch { }
    }
}