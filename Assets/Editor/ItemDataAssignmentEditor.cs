using UnityEngine;
using UnityEditor;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Editor script that auto-assigns ScriptableObject data to all items in the Inventory.
/// Run this once after creating item GameObjects to populate all medkitData, grenadeData, etc.
/// </summary>
public class ItemDataAssignmentEditor : EditorWindow {

    [MenuItem("Tools/Deadzone/Assign Item Data to Scene Items")]
    public static void AssignItemData() {
        Debug.Log("[ItemDataAssignmentEditor] Starting automatic item data assignment...");

        // Find Inventory in scene
        Inventory inventory = FindObjectOfType<Inventory>();
        if (inventory == null) {
            EditorUtility.DisplayDialog("Error", "Inventory not found in scene!", "OK");
            return;
        }

        // Get all ItemBehaviour children
        ItemBehaviour[] items = inventory.GetComponentsInChildren<ItemBehaviour>(includeInactive: true);
        Debug.Log($"[ItemDataAssignmentEditor] Found {items.Length} ItemBehaviour components");

        int successCount = 0;

        foreach (ItemBehaviour item in items) {
            // Skip weapons (they use itemID field, not SO data)
            if (item is WeaponBehaviour) {
                Debug.Log($"[ItemDataAssignmentEditor] Skipping WeaponBehaviour: {item.gameObject.name}");
                continue;
            }

            // Medkit
            if (item is Medkit medkit) {
                if (AssignMedkitData(medkit)) successCount++;
            }
            // Grenade
            else if (item is Grenade grenade) {
                if (AssignGrenadeData(grenade)) successCount++;
            }
            // Barricade
            else if (item is Barricade barricade) {
                if (AssignBarricadeData(barricade)) successCount++;
            }
            // ExplosiveBarrel
            else if (item is ExplosiveBarrel barrel) {
                if (AssignExplosiveBarrelData(barrel)) successCount++;
            }
            // BearTrap
            else if (item is BearTrap bearTrap) {
                if (AssignBearTrapData(bearTrap)) successCount++;
            }
            // Vest
            else if (item is Vest vest) {
                if (AssignVestData(vest)) successCount++;
            }
            else {
                Debug.LogWarning($"[ItemDataAssignmentEditor] Unknown item type: {item.GetType().Name} on {item.gameObject.name}");
            }
        }

        Debug.Log($"[ItemDataAssignmentEditor] Successfully assigned data to {successCount}/{items.Length} items");
        EditorUtility.DisplayDialog("Success", $"Assigned item data to {successCount} items!", "OK");
    }

    private static bool AssignMedkitData(Medkit medkit) {
        // Use reflection to set private field
        var fieldInfo = medkit.GetType().GetField("medkitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] medkitData field not found on {medkit.gameObject.name}");
            return false;
        }

        MedkitDataSO data = Resources.Load<MedkitDataSO>("Items/MedkitData") 
            ?? AssetDatabase.LoadAssetAtPath<MedkitDataSO>("Assets/Data/Items/MedkitData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] MedkitData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(medkit, data);
        EditorUtility.SetDirty(medkit);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned MedkitData to {medkit.gameObject.name}");
        return true;
    }

    private static bool AssignGrenadeData(Grenade grenade) {
        var fieldInfo = grenade.GetType().GetField("grenadeData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] grenadeData field not found on {grenade.gameObject.name}");
            return false;
        }

        GrenadeDataSO data = Resources.Load<GrenadeDataSO>("Items/GrenadeData")
            ?? AssetDatabase.LoadAssetAtPath<GrenadeDataSO>("Assets/Data/Items/GrenadeData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] GrenadeData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(grenade, data);
        EditorUtility.SetDirty(grenade);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned GrenadeData to {grenade.gameObject.name}");
        return true;
    }

    private static bool AssignBarricadeData(Barricade barricade) {
        var fieldInfo = barricade.GetType().GetField("barricadeData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] barricadeData field not found on {barricade.gameObject.name}");
            return false;
        }

        BuildableDataSO data = Resources.Load<BuildableDataSO>("Items/BarricadeData")
            ?? AssetDatabase.LoadAssetAtPath<BuildableDataSO>("Assets/Data/Items/BarricadeData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] BarricadeData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(barricade, data);
        EditorUtility.SetDirty(barricade);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned BarricadeData to {barricade.gameObject.name}");
        return true;
    }

    private static bool AssignExplosiveBarrelData(ExplosiveBarrel barrel) {
        var fieldInfo = barrel.GetType().GetField("barrelData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] barrelData field not found on {barrel.gameObject.name}");
            return false;
        }

        BuildableDataSO data = Resources.Load<BuildableDataSO>("Items/ExplosiveBarrelData")
            ?? AssetDatabase.LoadAssetAtPath<BuildableDataSO>("Assets/Data/Items/ExplosiveBarrelData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] ExplosiveBarrelData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(barrel, data);
        EditorUtility.SetDirty(barrel);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned ExplosiveBarrelData to {barrel.gameObject.name}");
        return true;
    }

    private static bool AssignBearTrapData(BearTrap bearTrap) {
        var fieldInfo = bearTrap.GetType().GetField("bearTrapData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] bearTrapData field not found on {bearTrap.gameObject.name}");
            return false;
        }

        BuildableDataSO data = Resources.Load<BuildableDataSO>("Items/BearTrapData")
            ?? AssetDatabase.LoadAssetAtPath<BuildableDataSO>("Assets/Data/Items/BearTrapData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] BearTrapData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(bearTrap, data);
        EditorUtility.SetDirty(bearTrap);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned BearTrapData to {bearTrap.gameObject.name}");
        return true;
    }

    private static bool AssignVestData(Vest vest) {
        var fieldInfo = vest.GetType().GetField("vestData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldInfo == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] vestData field not found on {vest.gameObject.name}");
            return false;
        }

        VestDataSO data = Resources.Load<VestDataSO>("Items/VestData")
            ?? AssetDatabase.LoadAssetAtPath<VestDataSO>("Assets/Data/Items/VestData.asset");
        
        if (data == null) {
            Debug.LogWarning($"[ItemDataAssignmentEditor] VestData.asset not found!");
            return false;
        }

        fieldInfo.SetValue(vest, data);
        EditorUtility.SetDirty(vest);
        Debug.Log($"[ItemDataAssignmentEditor] Assigned VestData to {vest.gameObject.name}");
        return true;
    }
}
