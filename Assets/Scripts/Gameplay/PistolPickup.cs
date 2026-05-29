using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Script especializado para a coleta da pistola no tutorial.
/// Herda de Interactable para usar o sistema de "Olhar e apertar E".
/// </summary>
public class PistolPickup : Interactable {

    #region SERIALIZED FIELDS

    [Header("Pistol Settings")]
    [Tooltip("Data da Pistola (ID 1) que será desbloqueada.")]
    [SerializeField] private ShopItemDataSO pistolData;

    [Header("Visual Effects")]
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float floatAmplitude = 0.25f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Tutorial Activation")]
    [Tooltip("O objeto pai do HUD de munição para ativar na coleta.")]
    [SerializeField] private GameObject ammoHUDObject;
    
    [Tooltip("O Mesh dos braços do jogador para ativar na coleta.")]
    [SerializeField] private GameObject playerArmsMesh;

    [Header("Audio")]
    [Tooltip("Efeito sonoro ao coletar a pistola.")]
    [SerializeField] private AudioClip pickupSFX;

    [Header("Tutorial Progression")]
    [Tooltip("Paredes invisíveis ou objetos a serem desativados ao coletar a pistola.")]
    [SerializeField] private List<GameObject> barriersToDeactivate;

    private Vector3 startPosition;

    #endregion

    #region UNITY

    private void Start() {
        startPosition = transform.position;
    }

    private void Update() {
        // Efeito de Rotação 360 no eixo Y
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // Efeito de Flutuar (Seno)
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Implementação da interação: desbloqueia a pistola, ativa visuais e a equipa.
    /// </summary>
    public override void Interact() {
        if (PlayerProgress.Instance == null) return;
        if (pistolData == null) {
            Debug.LogWarning("[PistolPickup] PistolData não atribuída!");
            return;
        }

        // 1. Ativa o HUD, os Braços e Desativa as Barreiras
        if (ammoHUDObject != null) ammoHUDObject.SetActive(true);
        if (playerArmsMesh != null) playerArmsMesh.SetActive(true);
        
        if (barriersToDeactivate != null) {
            foreach (GameObject barrier in barriersToDeactivate) {
                if (barrier != null) barrier.SetActive(false);
            }
        }

        // 2. Desbloqueia a pistola no progresso do jogador
        PlayerProgress.Instance.UnlockItem(pistolData);
        Debug.Log("[PistolPickup] Pistola desbloqueada!");

        // 3. Toca o efeito sonoro de coleta
        if (pickupSFX != null) {
            IAudioManagerService audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            if (audioService != null) {
                audioService.PlaySFX2D(pickupSFX);
            }
        }

        // 4. Tenta encontrar o inventário no jogador para equipar a pistola na hora
        Character player = Object.FindFirstObjectByType<Character>();
        if (player != null) {
            Inventory inventory = player.GetComponentInChildren<Inventory>();
            if (inventory != null) {
                inventory.SelectItem(0);
                Debug.Log("[PistolPickup] Pistola equipada automaticamente.");
            }
        }

        // 5. Destrói o objeto de pickup do mundo
        Destroy(gameObject);
    }

    #endregion
}
