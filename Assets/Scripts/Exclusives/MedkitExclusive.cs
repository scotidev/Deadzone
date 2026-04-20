using UnityEngine;

public class MedkitExclusive : ItemExclusive
{
    [Header("Medkit Exclusive Settings")]
    [SerializeField] private float continuousHealAmount = 5f; // Valor de cura contínua por segundo
    [SerializeField] private float healDuration = 30f; // Duração da cura em segundos

    private void Awake()
    {
        base.Awake(); // Chama o Awake da classe base primeiro
        SetupExclusive(5, "Continuous healing for 30s after use.");
    }

    protected override void ApplyExclusiveEffects()
    {
        // Lógica para aplicar a cura contínua.
        Debug.Log($"Medkit Exclusive Activated: Continuous healing ({continuousHealAmount}/sec for {healDuration}s) applied.");
    }

    /*
    private System.Collections.IEnumerator ApplyContinuousHealing()
    {
        // Implementação da lógica de cura contínua aqui
        // Ex: Obter o PlayerHealth component e aplicar cura ao longo do tempo.
        yield return new WaitForSeconds(healDuration);
    }
    */
}
