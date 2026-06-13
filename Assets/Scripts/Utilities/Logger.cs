using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Custom logging utility that can be disabled in production builds.
/// Uses [Conditional] attribute so calls are COMPLETELY REMOVED from release builds,
/// meaning zero performance cost when not in the editor.
/// CONCEITO: Atributos [Conditional("UNITY_EDITOR")] fazem o compilador REMOVER
/// as chamadas deste método do build final. É como se o código nunca existisse
/// no jogo compilado — zero overhead de performance.
/// O Debug.Log normal continua sendo compilado mesmo no build final,
/// só não aparece no Console por causa do logEnabled.
/// </summary>
public static class Logger {

    /// <summary>
    /// Logs a message only in Unity Editor / Development builds.
    /// In release builds, this call is stripped by the compiler.
    /// CONCEITO: Use este método NO LUGAR de Debug.Log em métodos chamados
    /// todo frame (Update, corrotinas frequentes). O custo de chamar Debug.Log
    /// é pequeno, mas centenas de chamadas por frame acumulam.
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
    /// CONCEITO: Erros SÃO registrados mesmo em produção porque indicam
    /// problemas reais que precisam ser diagnosticados.
    /// </summary>
    public static void LogError(object message, UnityEngine.Object context = null) {
        if (context != null)
            UnityEngine.Debug.LogError(message, context);
        else
            UnityEngine.Debug.LogError(message);
    }
}
