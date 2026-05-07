using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to automatically create and configure ShopItemDataSO assets.
/// This centralizes all shop item configuration in one place and prevents manual errors.
/// </summary>
public class ShopItemSetupEditor : EditorWindow {
    
    private Vector2 scrollPosition;

    [MenuItem("Tools/Deadzone/Setup Shop Items")]
    public static void ShowWindow() {
        GetWindow<ShopItemSetupEditor>("Shop Item Setup");
    }

    private void OnGUI() {
        GUILayout.Label("Shop Item Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Click the button below to create all 9 ShopItemDataSO assets.", EditorStyles.wordWrappedLabel);
        GUILayout.Label("This will automatically configure items for the shop.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create All Shop Items", GUILayout.Height(40))) {
            CreateAllShopItems();
        }

        GUILayout.Space(20);
        GUILayout.Label("Status:", EditorStyles.boldLabel);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        
        if (GUILayout.Button("Check Existing Assets")) {
            CheckExistingAssets();
        }

        GUILayout.EndScrollView();
    }

    private static void CreateAllShopItems() {
        // CONCEITO: Criamos uma lista de items com nome, ID, e pasta de destino.
        // Isso centraliza toda a configuração em um único lugar.
        var itemsToCreate = new[] {
            new { Name = "Pistol", ID = "1", PreviewPrefab = "Pistol", Type = "Weapon" },
            new { Name = "AK47", ID = "2", PreviewPrefab = "AK47", Type = "Weapon" },
            new { Name = "Shotgun", ID = "3", PreviewPrefab = "Shotgun", Type = "Weapon" },
            new { Name = "Medkit", ID = "4", PreviewPrefab = "Medkit", Type = "Consumable" },
            new { Name = "Grenade", ID = "5", PreviewPrefab = "Grenade", Type = "Consumable" },
            new { Name = "Barricade", ID = "6", PreviewPrefab = "Barricade", Type = "Buildable" },
            new { Name = "ExplosiveBarrel", ID = "7", PreviewPrefab = "ExplosiveBarrel", Type = "Buildable" },
            new { Name = "BearTrap", ID = "8", PreviewPrefab = "BearTrap", Type = "Buildable" },
            new { Name = "Vest", ID = "9", PreviewPrefab = "Vest", Type = "Armor" },
        };

        string shopItemsPath = "Assets/Resources/ShopItems";
        Directory.CreateDirectory(shopItemsPath);

        int created = 0;

        foreach (var item in itemsToCreate) {
            string assetPath = $"{shopItemsPath}/ShopItem_{item.Name}.asset";
            
            // CONCEITO: Se o asset já existe, pulamos para não sobrescrever configurações manuais.
            if (File.Exists(assetPath)) {
                Debug.LogWarning($"Shop item already exists: {assetPath}");
                continue;
            }

            // Cria a instância do ShopItemDataSO
            ShopItemDataSO shopItem = ScriptableObject.CreateInstance<ShopItemDataSO>();
            
            shopItem.SetItemDescription($"The {item.Name} item. Type: {item.Type}");
            shopItem.SetPreviewPrefab(Resources.Load<GameObject>($"Prefabs/Items/{item.PreviewPrefab}"));

            // Salva o asset
            AssetDatabase.CreateAsset(shopItem, assetPath);
            created++;
            Debug.Log($"Created shop item: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Shop item setup complete! Created {created} items.");
        EditorUtility.DisplayDialog("Success", $"Created {created} shop item assets.\n\nNow configure each item in the inspector.", "OK");
    }

    private static void CheckExistingAssets() {
        string shopItemsPath = "Assets/Resources/ShopItems";
        
        if (!Directory.Exists(shopItemsPath)) {
            Debug.LogWarning($"Shop items directory not found: {shopItemsPath}");
            EditorUtility.DisplayDialog("Info", "No shop items directory found.\n\nClick 'Create All Shop Items' first.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("ShopItem_", new[] { shopItemsPath });
        Debug.Log($"Found {guids.Length} shop item assets");

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShopItemDataSO item = AssetDatabase.LoadAssetAtPath<ShopItemDataSO>(path);
            if (item != null) {
                Debug.Log($"  - {item.ItemName} (ID: {item.ItemID}) at {path}");
            }
        }

        EditorUtility.DisplayDialog("Info", $"Found {guids.Length} shop items.\n\nCheck console for details.", "OK");
    }
}
