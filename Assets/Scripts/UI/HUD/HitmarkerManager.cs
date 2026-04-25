using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Manages the hitmarker system for the game. Displays visual feedback and plays audio
/// when the player successfully hits an enemy.
/// 
/// Agora usa o IAudioManagerService ao invés do singleton AudioManager para garantir que funcione
/// em qualquer cena, independente da ordem de carregamento.
/// </summary>
public class HitmarkerManager : MonoBehaviour {

    /// <summary>
    /// Static event that is invoked when an enemy is hit by a projectile.
    /// The Projectile script invokes this event through the TriggerHitmarker() method,
    /// and HitmarkerManager subscribes to it in OnEnable().
    /// 
    /// Events são o padrão Observer: permite que vários objetos "observem" e reajam
    /// quando algo acontece, sem acoplamento direto entre eles.
    /// </summary>
    private static event Action OnEnemyHit;

    [Header("UI References")]
    [Tooltip("Image component that displays the hitmarker visual.")]
    [SerializeField] private Image hitmarkerImage;

    [Header("Audio References")]
    [Tooltip("Audio clip to play when an enemy is hit.")]
    [SerializeField] private AudioClip hitmarkerSound;

    [Header("Settings")]
    [Tooltip("How long the hitmarker remains visible on screen (in seconds).")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float displayDuration = 0.1f;

    /// <summary>
    /// Referência ao serviço de áudio obtida através do Service Locator.
    /// Service Locator é um padrão de design que permite acessar serviços globais
    /// sem depender de singletons, facilitando testes e manutenção.
    /// </summary>
    private IAudioManagerService audioService;

    private WaitForSeconds hitmarkerDelay;

    private void Awake() {
        hitmarkerDelay = new WaitForSeconds(displayDuration);
        // Obtém o serviço de áudio do Service Locator
        // ServiceLocator.Current.Get<T>() busca o serviço registrado do tipo T
        audioService = ServiceLocator.Current.Get<IAudioManagerService>();
    }

    private void OnEnable() {
        // += adiciona um método ao evento (subscribe)
        // Agora quando OnEnemyHit for invocado, ShowHitmarker será chamado
        OnEnemyHit += ShowHitmarker;
    }

    private void OnDisable() {
        // -= remove um método do evento (unsubscribe)
        // Importante fazer cleanup para evitar memory leaks
        OnEnemyHit -= ShowHitmarker;
    }

    /// <summary>
    /// Static method that allows external scripts (like Projectile) to trigger the hitmarker.
    /// This is called from Projectile.cs when an enemy is successfully hit.
    /// 
    /// Método estático pode ser chamado sem instância da classe (sem criar objeto).
    /// </summary>
    public static void TriggerHitmarker() {
        // ?. é null-conditional operator: só invoca se OnEnemyHit não for null
        // Evita NullReferenceException caso ninguém esteja inscrito no evento
        OnEnemyHit?.Invoke();
    }

    /// <summary>
    /// Shows the hitmarker by enabling its UI element and playing the sound effect.
    /// Called by the OnEnemyHit event when a projectile hits an enemy.
    /// 
    /// Agora usa PlaySFX2D do serviço unificado ao invés do singleton.
    /// </summary>
    private void ShowHitmarker() {
        if (hitmarkerImage == null) {
            return;
        }

        hitmarkerImage.enabled = true;

        // Usa o serviço de áudio para tocar som 2D (UI)
        // PlaySFX2D é adequado para sons de UI que não têm posição espacial
        if (audioService != null && hitmarkerSound != null) {
            audioService.PlaySFX2D(hitmarkerSound, 1f);
        }

        // StopAllCoroutines para o hitmarker anterior caso um novo hit aconteça
        // antes do anterior desaparecer
        StopAllCoroutines();

        StartCoroutine(HideHitmarkerAfterDelay());
    }

    /// <summary>
    /// Coroutine that waits for the display duration, then hides the hitmarker.
    /// 
    /// Coroutines permitem pausar a execução do código e retomar depois,
    /// útil para temporizadores e animações.
    /// </summary>
    private IEnumerator HideHitmarkerAfterDelay() {
        // yield return pausa a coroutine até a condição ser satisfeita
        // WaitForSeconds espera o tempo real passar
        yield return hitmarkerDelay;

        if (hitmarkerImage != null) {
            hitmarkerImage.enabled = false;
        }
    }
}
