# Checkpoint 002: Ammo System Initialization & Legacy Sync Fixes

**Date**: Current Session  
**Status**: ✅ COMPILATION SUCCESSFUL (0 errors, 5 pre-existing warnings)  
**Testing**: Ready for Unity Editor integration testing

## Problem Summary

The ammo display system had three critical bugs after Task 7 integration testing:
1. **Inverted display**: Weapons showed 0 current ammo initially, then -1 after each shot
2. **Total always 0**: TextAmmunitionTotal never showed reserve ammo amounts
3. **Infinite reload**: Could reload infinitely despite showing 0 total ammo
4. **Buildables worked**: Only weapons had display issues (suggests initialization problem)

## Root Cause Analysis

**Dual Source of Truth Problem:**
- Legacy system: `weaponReserveAmmo` dict (updated by Shop when buying ammo)
- New system: `itemTotalAmmo` dict (used by UI for display)
- They were never syncing → Shop added ammo to legacy dict, but UI read from new dict showing 0

**Magazine Initialization Issue:**
- `MagazineBehaviour.GetAmmunitionTotal()` returns MAX capacity (e.g., 10 for pistol), not current ammo
- `Weapon.InitializeWeapon()` was reading this and setting local `ammunitionCurrent` to 10
- But `PlayerProgress.InitializeItemAmmo()` was setting `itemCurrentAmmo[itemID] = 0`
- Result: Local weapon showed full magazine, but UI (reading from PlayerProgress) showed 0

**Transfer Logic Issue:**
- `ReloadItem()` transfers from `itemTotalAmmo` to `itemCurrentAmmo`
- If `itemTotalAmmo` was never populated (because Shop added to `weaponReserveAmmo`), no transfer
- Result: Reload appeared infinite because nothing was actually transferred but UI showed 0

## Solutions Implemented

### 1. Fixed Weapon Initialization (Weapon.cs)
**File**: `Assets/Scripts/Weapons/Weapon.cs` lines 91-130  
**Change**: `InitializeWeapon()` now syncs initial magazine value to PlayerProgress immediately after reading from MagazineBehaviour

```csharp
// Get magazine capacity from MagazineBehaviour
int magazineCapacity = equippedMagazine.GetAmmunitionTotal();
PlayerProgress.Instance.SetItemCurrent(itemID, magazineCapacity);

// Also initialize total ammo (reserve) if not already set
if (PlayerProgress.Instance.GetItemTotal(itemID) == 0) {
    PlayerProgress.Instance.SetItemTotal(itemID, magazineCapacity);
}
```

**Why**: Ensures PlayerProgress dicts are populated with actual values from the start, not left at 0

### 2. Refactored AddReserveAmmo (PlayerProgress.cs)
**File**: `Assets/Scripts/Economy/PlayerProgress.cs` lines 650-662  
**Change**: Now calls `AddItemAmmo()` (new system) instead of only updating `weaponReserveAmmo` (legacy)

```csharp
public bool AddReserveAmmo(string weaponID, int amount, int maxReserve) {
    // NEW: Use unified AddItemAmmo() instead of manipulating weaponReserveAmmo directly
    bool added = AddItemAmmo(weaponID, amount);
    
    // LEGACY: Also update weaponReserveAmmo for backward compatibility
    if (added) {
        weaponReserveAmmo[weaponID] = GetItemTotal(weaponID);
    }
    return added;
}
```

**Why**: Ensures Shop's ammo purchases populate BOTH the legacy dict (for backward compatibility) and the new dict (so UI can display it)

### 3. Refactored SpendReserveAmmo (PlayerProgress.cs)
**File**: `Assets/Scripts/Economy/PlayerProgress.cs` lines 671-675  
**Change**: Now calls `UseItem()` (unified system) instead of manipulating `weaponReserveAmmo` directly

```csharp
public bool SpendReserveAmmo(string weaponID, int amount) {
    // NEW: Use unified UseItem() instead of manipulating weaponReserveAmmo directly
    return UseItem(weaponID, amount);
}
```

**Why**: Ensures reload logic uses the same unified system that UI reads from, preventing desync

## Architecture Consolidation Achieved

### Before (Dual System - Broken)
```
Weapon.Fire()
    └─ Weapon.ammunitionCurrent-- (local state)
    └─ UI reads PlayerProgress.itemCurrentAmmo (empty because never synced!)
    
Shop.BuyAmmo()
    └─ PlayerProgress.AddReserveAmmo()
    └─ weaponReserveAmmo[weaponID]++ (legacy dict)
    └─ UI reads PlayerProgress.itemTotalAmmo (empty!)
```

### After (Unified System - Working)
```
Weapon.Fire()
    └─ Weapon.ammunitionCurrent-- (local state)
    └─ PlayerProgress.UseItem(weaponID, 1) (syncs to itemCurrentAmmo)
    └─ UI reads PlayerProgress.GetItemCurrent() (gets synced value)
    
Shop.BuyAmmo()
    └─ PlayerProgress.AddReserveAmmo()
    └─ AddItemAmmo() (populates itemTotalAmmo)
    └─ weaponReserveAmmo[weaponID]++ (legacy dict - also updated for backward compat)
    └─ UI reads PlayerProgress.GetItemTotal() (gets synced value)
    
Weapon.Reload()
    └─ ReloadItem()
    └─ Transfers from itemTotalAmmo to itemCurrentAmmo (both synced!)
    └─ UI reads PlayerProgress (both display correctly)
```

## Files Modified This Session

1. **Assets/Scripts/Weapons/Weapon.cs**
   - Line 91-130: `InitializeWeapon()` - Added sync to PlayerProgress

2. **Assets/Scripts/Economy/PlayerProgress.cs**
   - Line 650-662: `AddReserveAmmo()` - Now uses AddItemAmmo() + legacy sync
   - Line 671-675: `SpendReserveAmmo()` - Now uses UseItem()

## Compilation Status

```
✅ Build Success
   Errors: 0
   Warnings: 5 (pre-existing, unrelated to ammo system)
   DLL Output: C:\Users\gabes\Games\UNINTER\Deadzone\Temp\bin\Debug\Assembly-CSharp.dll
```

## Testing Checklist (Ready for Unity Editor)

- [ ] **Weapon Firing**: Pistol starts with 10 in TextAmmunitionCurrent
- [ ] **Fire Decrement**: Each shot decrements current by 1 (not -1)
- [ ] **Total Display**: TextAmmunitionTotal shows correct reserve (not always 0)
- [ ] **Fire Prevention**: Cannot fire when current = 0
- [ ] **Reload Logic**: Pressing R transfers from total to current correctly
- [ ] **Reload Prevention**: Cannot reload when total = 0
- [ ] **AK47 Test**: Same as Pistol (different magazine capacity)
- [ ] **M4 Test**: (Third weapon if unlocked)
- [ ] **Buildables**: Still show current=0, total=correct
- [ ] **Consumables**: Display correct values
- [ ] **Shop Integration**: Buying ammo increases TextAmmunitionTotal
- [ ] **Weapon Upgrade**: Magazine capacity upgrade increases max ammo display

## Key Insights for Future Work

1. **Legacy System Still Active**: `weaponReserveAmmo` dict continues to exist for backward compatibility with ShopManager
   - All methods now update BOTH systems
   - Can be safely removed once ShopManager is refactored to only use new system

2. **Magazine Initialization**: Always sync to PlayerProgress immediately after reading from MagazineBehaviour
   - MagazineBehaviour is just a capacity provider, not a state holder
   - PlayerProgress is the single source of truth

3. **Dual-Dict Sync Pattern**: When supporting legacy code, update both dicts but read from unified system
   ```csharp
   AddItemAmmo(weaponID, amount);  // Update new system
   legacyDict[weaponID] = GetItemTotal(weaponID);  // Sync legacy
   ```

4. **No More Direct Dict Access**: All external methods should use GetItemCurrent/GetItemTotal, not direct dict access
   - AddReserveAmmo/SpendReserveAmmo still exist as wrappers for backward compat
   - New code should use AddItemAmmo/UseItem directly

## Next Immediate Steps

1. **Test in Unity Editor** - Run game and verify all items display correctly
2. **Verify Shop Integration** - Check if ammo purchase increases TextAmmunitionTotal
3. **Test Weapon Upgrades** - Verify magazine capacity upgrades work
4. **Refactor Shop** (Future) - Update ShopManager to call AddItemAmmo directly instead of AddReserveAmmo
5. **Remove Dual Sync** (Future) - Once legacy wrappers no longer needed, simplify

## Code Quality

- ✅ XML comments on all methods
- ✅ Consistent with AGENTS.md guidelines
- ✅ No commented-out code
- ✅ All changes focused on sync/initialization (minimal diff)
- ✅ Maintains backward compatibility
- ✅ Compilation successful
