using System.Collections.Generic;
using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Handles the pistol pickup interaction in the tutorial. Unlocks the pistol,
/// activates the ammo HUD and player arms, removes barriers, and equips the weapon.
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

    #endregion

    #region FIELDS

    private Vector3 startPosition;

    #endregion

    #region UNITY

    private void Start() {
        startPosition = transform.position;
    }

    private void Update() {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    #endregion

    #region METHODS

    /// <summary>
    /// Unlocks the pistol, activates the ammo HUD and player arms, removes tutorial barriers,
    /// plays the pickup sound, equips the pistol, and destroys the pickup object.
    /// </summary>
    public override void Interact() {
        if (PlayerProgress.Instance == null) return;
        if (pistolData == null) {
            Debug.LogWarning("[PistolPickup] PistolData não atribuída!");
            return;
        }

        if (ammoHUDObject != null) ammoHUDObject.SetActive(true);
        if (playerArmsMesh != null) playerArmsMesh.SetActive(true);
        
        if (barriersToDeactivate != null) {
            foreach (GameObject barrier in barriersToDeactivate) {
                if (barrier != null) barrier.SetActive(false);
            }
        }

        PlayerProgress.Instance.UnlockItem(pistolData);

        if (pickupSFX != null) {
            IAudioManagerService audioService = ServiceLocator.Current.Get<IAudioManagerService>();
            if (audioService != null) {
                audioService.PlaySFX2D(pickupSFX);
            }
        }

        Character player = Object.FindFirstObjectByType<Character>();
        if (player != null) {
            Inventory inventory = player.GetComponentInChildren<Inventory>();
            if (inventory != null) {
                inventory.SelectItem(0);
            }
        }

        Destroy(gameObject);
    }

    #endregion
}
