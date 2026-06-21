using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Custom logging utility that can be disabled in production builds.
/// Uses [Conditional] attribute so calls are completely removed from release builds,
/// meaning zero performance cost when not in the editor.
/// </summary>
public static class Logger {

    /// <summary>
    /// Logs a message only in Unity Editor / Development builds.
    /// In release builds, this call is stripped by the compiler.
    /// Use in Update paths instead of Debug.Log to avoid overhead in production.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context = null) {
        if (context != null)
            UnityEngine.Debug.Log(message, context);
        else
            UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// Logs a warning only in Unity Editor / Development builds.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, UnityEngine.Object context = null) {
        if (context != null)
            UnityEngine.Debug.LogWarning(message, context);
        else
            UnityEngine.Debug.LogWarning(message);
    }

    /// <summary>
    /// Logs an error only in Unity Editor / Development builds.
    /// </summary>
    public static void LogError(object message, UnityEngine.Object context = null) {
        if (context != null)
            UnityEngine.Debug.LogError(message, context);
        else
            UnityEngine.Debug.LogError(message);
    }
}
