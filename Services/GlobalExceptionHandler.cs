/***

REMINDER: Global Error Handling Enhancements
1. Logic: Flatten AggregateExceptions

Problem: Task-based errors often wrap the real cause in an AggregateException.

Action: Update GlobalExceptionHandler.cs to use e.Exception.Flatten().

Goal: Log the specific InnerExceptions (e.g., SqliteException) instead of generic "One or more errors occurred" messages.

2. UX: Smart Error Responses

Transient Errors (Network/Logic): * Use a Modal/Popup instead of navigating away.

Include a "Try Again" button to re-trigger the failed command.

Keep the user on the current page to preserve input data.

Fatal Crashes: * Capture telemetry, then navigate to a Stable State (e.g., HomePage/SplashPage).

Avoid "zombie" app states after a platform-level crash.

3. Telemetry: Silent Reporting

Mechanism: Integrate a Serilog Sink (e.g., Sentry, Seq, or AppCenter).

Benefit: Handles "Immediate Send" vs. "Local Buffer" logic automatically when the device is offline.

Solo Dev Goal: Receive automated crash reports with stack traces and breadcrumbs without manual user intervention.

***/

using Serilog;

namespace MyVocaList.Services;

/// <summary>
/// Centralized exception handling for the entire application.
/// Hooks into unhandled exceptions and logs them via Serilog.
/// </summary>
public static class GlobalExceptionHandler
{
    private static bool _initialized;

    /// <summary>
    /// Initialize global exception handlers. Call this FIRST in MauiProgram.CreateMauiApp().
    /// </summary>
    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        // Hook AppDomain unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Hook Task unobserved exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

#if ANDROID
        // Hook Android-specific exceptions
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
#endif

        Log.Information("GlobalExceptionHandler initialized");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Log.Fatal(exception, "Unhandled exception in AppDomain. IsTerminating: {IsTerminating}", e.IsTerminating);
        }
        else
        {
            Log.Fatal("Unhandled non-exception object: {ExceptionObject}. IsTerminating: {IsTerminating}",
                e.ExceptionObject, e.IsTerminating);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");

        // Mark as observed to prevent app termination
        e.SetObserved();
    }

#if ANDROID
    private static void OnAndroidUnhandledException(object? sender, Android.Runtime.RaiseThrowableEventArgs e)
    {
        Log.Fatal(e.Exception, "Android unhandled exception");

        // Mark as handled to prevent immediate crash (allows logging to complete)
        e.Handled = true;
    }
#endif
}
