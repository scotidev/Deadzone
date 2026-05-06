using UnityEngine;
using InfimaGames.LowPolyShooterPack;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Grenade consumable item. Throws a grenade when used.
    /// Supports exclusive upgrade: grenade has 1.5x damage at level 9+
    /// </summary>
    public class Grenade : ItemBehaviour {

        #region SERIALIZED FIELDS

        [SerializeField] private GrenadeDataSO grenadeData;
        [SerializeField] private Sprite hudIcon;
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private float throwForce = 20f;
        [SerializeField] private float exclusiveDamageMultiplier = 1.5f;

        #endregion

        #region ITEM BEHAVIOUR IMPLEMENTATION

        public override string GetItemID() {
            if (grenadeData == null) {
                Debug.LogWarning("[Grenade] grenadeData is null!", gameObject);
                return "grenade_null";
            }
            return grenadeData.itemID;
        }

        public override string GetDisplayName() {
            if (grenadeData == null) return "Unknown";
            return grenadeData.itemName;
        }

        public override Sprite GetIcon() {
            if (hudIcon == null) {
                Debug.LogWarning("[Grenade] hudIcon is null!", gameObject);
                return null;
            }
            return hudIcon;
        }

        /// <summary>
        /// Called when player selects this item (key 5).
        /// Activate visual representation (grenade model in hand).
        /// </summary>
        public override void OnSelected() {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when player selects another item.
        /// Deactivate visual.
        /// </summary>
        public override void OnDeselected() {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// NORMAL use: Throw grenade with normal damage.
        /// CONCEITO: Instancia a granada prefab na posição da câmera do jogador
        /// e aplica velocidade inicial (throwForce) na direção da câmera.
        /// </summary>
        public override void OnUse() {
            if (!CanBeUsed() || grenadeData == null) {
                return;
            }

            ThrowGrenade(grenadeData.damage);
        }

        /// <summary>
        /// EXCLUSIVE use: Throw grenade with 1.5x damage.
        /// </summary>
        public override void OnUseExclusive() {
            if (!CanBeUsed() || !HasExclusiveUnlocked() || grenadeData == null) {
                return;
            }

            float exclusiveDamage = grenadeData.damage * exclusiveDamageMultiplier;
            ThrowGrenade(exclusiveDamage);
        }

        /// <summary>
        /// Helper method to throw grenade with specified damage.
        /// Gets camera position and direction from Character.
        /// </summary>
        private void ThrowGrenade(float damage) {
            if (grenadePrefab == null) {
                Debug.LogWarning("[Grenade] grenadePrefab is null!", gameObject);
                return;
            }

            Character character = GetComponentInParent<Character>();
            if (character == null) {
                return;
            }

            Transform cameraTransform = character.GetCameraWorld().transform;
            if (cameraTransform == null) {
                return;
            }

            // Instantiate grenade at camera position
            GameObject grenade = Instantiate(grenadePrefab, cameraTransform.position, Quaternion.identity);

            // Apply throw force
            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.linearVelocity = cameraTransform.forward * throwForce;
            }

            // TODO: Apply damage to grenade component if it has one

            // Consume 1 grenade from inventory
            if (PlayerProgress.Instance != null) {
                PlayerProgress.Instance.ConsumeItem(GetItemID(), 1);
            }
        }

        /// <summary>
        /// Check if grenade is unlocked (for selection). Quantity check happens in OnUse().
        /// </summary>
        public override bool CanBeUsed() {
            if (PlayerProgress.Instance == null) {
                Debug.LogWarning($"[Grenade] CanBeUsed: PlayerProgress.Instance is NULL!");
                return false;
            }

            // CONCEITO: CanBeUsed() é para seleção, apenas verifica se desbloqueado.
            // A checagem de quantidade é feita em OnUse(), não em CanBeUsed().
            // Isso permite selecionar qualquer item desbloqueado, mesmo sem quantidade.
            bool isUnlocked = PlayerProgress.Instance.IsItemUnlocked(GetItemID());
            Debug.Log($"[Grenade] CanBeUsed check: ID={GetItemID()}, Unlocked={isUnlocked}");
            return isUnlocked;
        }

        /// <summary>
        /// Check if exclusive upgrade is unlocked (level 9+).
        /// </summary>
        public override bool HasExclusiveUnlocked() {
            if (PlayerProgress.Instance == null) {
                return false;
            }

            int level = PlayerProgress.Instance.GetItemLevel(GetItemID());
            return level >= 9;
        }

        #endregion
    }
}
