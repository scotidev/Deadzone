using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable component that applies a scale pulse animation to any Transform.
/// Call Pulse() to briefly scale up and back down to the original size.
/// </summary>
public class TextScalePulse : MonoBehaviour {

    #region SERIALIZED FIELDS

    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private float duration = 0.2f;

    #endregion

    #region FIELDS

    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    #endregion

    #region UNITY

    private void Awake() {
        originalScale = transform.localScale;
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Triggers the scale pulse animation. If already playing, restarts it.
    /// </summary>
    public void Pulse() {
        if (!gameObject.activeInHierarchy) return;

        if (animationCoroutine != null) {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(PulseRoutine());
    }

    /// <summary>
    /// Coroutine that smoothly scales up to animationScale and back to original.
    /// </summary>
    private IEnumerator PulseRoutine() {
        float timer = 0f;
        while (timer < duration) {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float currentScale = Mathf.Lerp(scaleMultiplier, 1f, progress);
            transform.localScale = originalScale * currentScale;
            yield return null;
        }
        transform.localScale = originalScale;
        animationCoroutine = null;
    }

    #endregion
}
