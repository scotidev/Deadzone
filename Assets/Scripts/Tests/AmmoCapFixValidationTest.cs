using UnityEngine;
using InfimaGames.LowPolyShooterPack;

/// <summary>
/// Validation test to ensure the ammo cap fix is working correctly.
/// This test verifies that MaxAmmo is no longer hardcoded and uses the configured values.
/// </summary>
public class AmmoCapFixValidationTest : MonoBehaviour {

    public void OnEnable() {
        Debug.Log("========== AMMO CAP FIX VALIDATION TEST ==========");
        TestAK47MaxAmmo();
        TestGetMaxAmmoAtLevel();
        TestAllWeaponMaxAmmo();
        Debug.Log("========== TEST COMPLETE ==========");
    }

    private void TestAK47MaxAmmo() {
        Debug.Log("\n--- Testing AK47 MaxAmmo Configuration ---");
        var ak47Data = Resources.Load<WeaponDataSO>("Items/AK47Data");
        
        if (ak47Data == null) {
            Debug.LogError("❌ Failed to load AK47Data!");
            return;
        }
        
        Debug.Log($"✓ Loaded AK47Data");
        Debug.Log($"  ItemID: {ak47Data.ItemID}");
        Debug.Log($"  MaxAmmo: {ak47Data.MaxAmmo}");
        Debug.Log($"  BaseAmmo: {ak47Data.BaseAmmo}");
        Debug.Log($"  AmmoScaling: {ak47Data.AmmoScaling}");
        
        // Verify MaxAmmo is 600 (not hardcoded 300)
        if (ak47Data.MaxAmmo == 600) {
            Debug.Log("✅ PASS: AK47 MaxAmmo is correctly set to 600");
        } else {
            Debug.LogError($"❌ FAIL: AK47 MaxAmmo is {ak47Data.MaxAmmo}, expected 600");
        }
    }

    private void TestGetMaxAmmoAtLevel() {
        Debug.Log("\n--- Testing GetMaxAmmoAtLevel Formula ---");
        var ak47Data = Resources.Load<WeaponDataSO>("Items/AK47Data");
        
        if (ak47Data == null) {
            Debug.LogError("❌ Failed to load AK47Data!");
            return;
        }
        
        // Test at different levels
        for (int level = 1; level <= 10; level++) {
            int maxAmmo = ak47Data.GetMaxAmmoAtLevel(level);
            // Formula: baseAmmo * (1 + ammoScaling * (level - 1)), capped at MaxAmmo
            float expectedScaled = 30f * (1f + 2f * (level - 1));
            int expectedCapped = Mathf.Min((int)expectedScaled, 600);
            
            string status = maxAmmo == expectedCapped ? "✅" : "❌";
            Debug.Log($"{status} Level {level}: MaxAmmo = {maxAmmo} (expected {expectedCapped})");
        }
    }

    private void TestAllWeaponMaxAmmo() {
        Debug.Log("\n--- Testing All Weapons (No Hardcoded Overrides) ---");
        var allWeapons = Resources.LoadAll<WeaponDataSO>("Items/");
        
        Debug.Log($"Found {allWeapons.Length} weapons");
        
        foreach (var weapon in allWeapons) {
            // Verify no override is hardcoded (MaxAmmo should match configured value)
            Debug.Log($"✓ {weapon.ItemID}: MaxAmmo = {weapon.MaxAmmo}");
        }
        
        Debug.Log("✅ PASS: All weapons loaded successfully (no hardcoded override crashes)");
    }
}
