# 1. Overview

## 1.1 Game Concept

A first-person shooter (FPS) with survival and tower defense elements. The player is trapped inside a house that serves as the last safe refuge in the Deadzone, facing zombie hordes that attack in endless waves. The goal is to survive as many waves as possible through a loop of combat, resource management, and base fortification.

## 1.2 Target Audience

- **Primary:** Fans of zombie survival shooters (such as _Call of Duty: Zombies_ and _Left 4 Dead_).
- **Secondary:** Players who enjoy strategy and base-building in a survival context.
- **Age Rating:** Teens and adults (16+), due to violence and zombie themes.

## 1.3 Genre and Platform

- **Genre:** FPS (First-Person Shooter), Survival, Tower Defense.
- **Platform:** WebGL.
- **Audience:** Single-player only.

## 1.4 Core Loop

**Shopping Phase (safe inside house) → Start Wave → Combat Phase → Survive → Earn Credits → Shopping Phase**

---

# 2. Mechanics

## 2.1 Gameplay

Gameplay is divided into two cyclical phases: Shopping Phase and Wave Phase.

- **Shopping Phase:** Occurs between waves. The player is safe inside the house and can spend Credits to buy, upgrade, or restock items at the Merchant NPC. This is the time to place traps and barricades, and plan defense for the next wave.
- **Wave Phase:** The player starts the next wave by interacting with the **Wave Button**. Enemies spawn from points outside the house and try to reach and attack the player. The objective is to eliminate all enemies in the wave to survive.

### 2.1.1 Rules

- The player loses if health reaches zero.
- A wave is cleared when all designated enemies are eliminated.
- Weapons require ammo. If ammo runs out, the weapon cannot be used until reloaded or more ammo is purchased.
- The game is endless — waves continue indefinitely until the player dies.

## 2.2 Player

### 2.2.1 Movement and Controls

- **Move:** WASD.
- **Look:** Mouse movement.
- **Jump:** Spacebar.
- **Crouch:** C.
- **Shoot:** Left Mouse Button.
- **Aim Down Sights:** Right Mouse Button.
- **Reload:** R.
- **Melee Attack (Knife):** F.
- **Throw / Use Item:** Q.
- **Switch Item Slot:** Number keys 1–8.
- **Interact:** E (Wave Button, Merchant, etc.).
- **Open Shop:** Tab.
- **Open Map Select:** M.
- **Pause:** Esc.

### 2.2.2 Player Stats

- **Health:** Maximum health with damage feedback (screen effects, sounds).
- **Armor:** Provided by the Vest item. Absorbs a percentage of incoming damage. Depletes over time and can be recharged in the shop.
- **Stamina:** Used for actions; regenerates when idle.
- **Movement Speed:** Base movement speed; reduced while crouching or aiming.
- **Crouch:** Lowers the player's height, reducing visibility and improving accuracy.

## 2.3 Enemies

### 2.3.1 Enemy Types

| Enemy                      | Description                                                                              | Behavior                                                |
| -------------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------- |
| **ZombieDefault (Walker)** | Basic slow zombie. Standard melee attacker, moves in groups.                             | Walks toward the player, attacks on contact.            |
| **ZombieCop**              | Tougher than Walker. Higher health and damage.                                           | Same base behavior, but more resilient.                 |
| **ZombieDoctor**           | Faster and more aggressive. Moderate health.                                             | Charges at the player with higher speed.                |
| **ZombieBoss**             | Boss enemy with very high health and damage. Spawns every 5 waves.                       | Slow but devastating. High priority target.             |
| **TutorialDummyZombie**    | Harmless dummy used in the tutorial.                                                     | Does not attack or move.                                |
| **PenguinEnemy**           | Secret easter egg enemy. Unlocked by shooting a painting multiple times with the Pistol. | Unique behavior, spawns during a wave after activation. |

### 2.3.2 Wave Scaling

- Enemy health, damage, and credit reward scale with the current wave number.
- Scaling follows a **quadratic curve capped at wave 20** — difficulty increases quickly early on and plateaus at wave 20.
- Each enemy type has a **different credit reward value** (e.g., ZombieCop gives more Credits than ZombieDefault).

### 2.3.3 Spawning

- Enemies spawn from designated spawn points around the map.
- Spawning uses an **Object Pooling** system to avoid runtime instantiation overhead.
- Enemy types are chosen via **weighted random selection**, configurable per wave.

## 2.4 Economy and Progression

### 2.4.1 Currency

- **Credits** are the in-game currency.
- Earned by eliminating enemies and surviving waves.
- Used in the shop to unlock, upgrade, and restock items.

### 2.4.2 Item Progression

Each item has three stages in the shop:

1. **Unlock:** Purchase the item for the first time to add it to the inventory.
2. **Upgrade:** Each item can be upgraded up to 10 levels. Upgrades improve stats (damage, magazine size, heal amount, etc.). Upgrade cost increases per level.
3. **Ammo / Supply Purchase:** After unlocking, buy additional ammo or charges for the item. Ammo is tracked separately for the magazine (current) and reserve (total).

### 2.4.3 UpgradeManager

- Central system that applies upgrade levels to weapons and items.
- Notifies weapons when their stats change (e.g., damage, fire rate).

### 2.4.4 AmmoManager

- Handles ammo purchasing in the shop.
- Manages per-item magazine and reserve ammo counts.

## 2.5 Weapons

### 2.5.1 Weapon List

| Weapon            | Description                                                                                         |
| ----------------- | --------------------------------------------------------------------------------------------------- |
| **Pistol**        | Starting weapon. Semi-automatic, moderate damage, low magazine.                                     |
| **AK47**          | Automatic rifle. High damage, larger magazine, higher recoil.                                       |
| **Shotgun**       | Pump-action. Fires a spread of pellets. High close-range damage, slow fire rate.                    |
| **Melee (Knife)** | Default melee weapon. Activated with F key. Quick damage at close range using OverlapBox detection. |

### 2.5.2 Weapon System

- Weapons are ScriptableObject-based data (WeaponDataSO) with configurable stats.
- **WeaponBehaviour** (abstract) defines the base class.
- Concrete implementations: Pistol, AK47, Shotgun, MeleeWeapon.
- Weapons track ammo (magazine and reserve), fire rate, damage, and reload time.
- **Reload** transfers ammo from reserve to magazine.

## 2.6 Items and Equipment

### 2.6.1 Item List

| Item        | Description                                                                                           |
| ----------- | ----------------------------------------------------------------------------------------------------- |
| **Medkit**  | Consumable. Heals the player by a fixed amount.                                                       |
| **Grenade** | Throwable explosive. Hold-to-charge, released to throw. Deals AoE damage on explosion.                |
| **Vest**    | Passive armor. Absorbs a percentage of damage. Visual armor bar on HUD. Can be recharged in the shop. |

### 2.6.2 Item System

- All items inherit from **ItemBehaviour** (abstract base class).
- Items are managed through the **Inventory** system (8 slots, keys 1–8).
- Medkit and Grenade are consumables. Vest is passive once equipped.
- Buildables (Barricade, Explosive Barrel, Bear Trap) are not inventory items — they are placed via the Building System.

## 2.7 Building System

### 2.7.1 Buildables

| Buildable            | Description                                                                |
| -------------------- | -------------------------------------------------------------------------- |
| **Barricade**        | Blocks enemy paths. Enemies must attack it to break through.               |
| **Explosive Barrel** | A trap that explodes when shot or when an enemy is near. Deals AoE damage. |
| **Bear Trap**        | A floor trap that damages and briefly immobilizes enemies.                 |

### 2.7.2 How It Works

- **BuildingController** manages placement.
- When placing, a **GhostObject** preview shows the intended position with color feedback (green = valid, red = invalid).
- Placement validation checks for overlaps, valid surfaces, and range.
- Buildables have a cooldown between placements.
- Buildables can be purchased in the shop and placed during the Shopping Phase.

## 2.8 Wave System

### 2.8.1 Wave Cycle

1. Shopping Phase begins. The player can buy, upgrade, restock, and place items.
2. Player presses the **Wave Button** to start the wave.
3. Enemies spawn in waves and attack.
4. When all enemies are eliminated, the wave ends and Credits are awarded.
5. Return to step 1.

### 2.8.2 Wave Scaling

- Number of enemies increases with wave number.
- Enemy stats (HP, damage, reward) scale quadratically with wave, capped at wave 20.
- Every 5th wave is a **Boss Wave** that spawns a ZombieBoss alongside regular enemies.
- The game is **endless** — there is no final wave.

### 2.8.3 WaveButton

- An interactable object in the safe house.
- Starts the next wave on interaction.
- Only usable during the Shopping Phase.

## 2.9 Tutorial

- The tutorial is managed by **TutorialManager** using **TutorialStepSO** (ScriptableObject) data.
- Steps are triggered by **zonal triggers** (e.g., "move to this area", "pick up the weapon").
- Covers: WASD movement, shooting, aiming, reloading, jumping, crouching, melee, item usage, and shop interaction.
- Includes a **TutorialDummyZombie** for safe combat practice.

## 2.10 Easter Egg: Penguin Mode

- **PenguinMode** is a secret easter egg.
- Activated by shooting a specific painting with the **Pistol** multiple times.
- Once activated, a **PenguinEnemy** can appear during waves.

## 2.11 Slow Motion

- **Slow Motion** triggers on certain events (e.g., explosions).
- Implemented via `GameManager.TriggerSlowMotion()`.
- Modifies `Time.timeScale` temporarily for dramatic effect.

---

# 3. Aesthetics

## 3.1 Visual Style

- **Low Poly:** 3D style with low-poly models, solid colors/gradients, simple textures.
- **Dark / Post-Apocalyptic:** Desaturated color palette with warm interior lighting.
- **Blood and Gore:** Particle effects for blood on hit, enemy death animations.
- **Post-Processing:** URP Volume Profile with bloom, color grading, and ambient occlusion.

## 3.2 Visual References

- Team Fortress 2
- Unturned
- Boxhead

## 3.3 Audio

### 3.3.1 Soundtrack

- **Shopping Phase:** Ambient, calm music with suspense and tension.
- **Wave Phase:** Dynamic, intense music that escalates with the horde.

### 3.3.2 Sound Effects (SFX)

- **Feedback:** Damage taken, item usage, building placement.
- **Weapons:** Distinct shoot, reload, and empty-click sounds per weapon.
- **Enemies:** Groans, footsteps, and attack sounds per type.
- **Environment:** Ambience, door sounds.
- **UI:** Click, purchase, confirmation sounds.

### 3.3.3 Audio System

- Managed by **AudioManagerService** (implements IAudioManagerService).
- Supports: PlaySFX2D, PlaySFX3D, BGM crossfade, PlayDialogue.
- Uses Unity AudioMixer with parameter groups (Master, SFX, BGM).

---

# 4. Narrative

Narrative is environmental. The story is told through the setting: a viral outbreak has turned the population into zombies. The player is a lone survivor holed up in a safe house within the Deadzone. An **NPC Merchant** appears between waves (inside the house) to sell supplies. The player's goal is simply to survive as long as possible.

---

# 5. Systems

## 5.1 HUD

| Element              | Position                   | Description                                               |
| -------------------- | -------------------------- | --------------------------------------------------------- |
| **Health Bar**       | Bottom left                | Player's current health.                                  |
| **Armor Bar**        | Bottom left (below health) | Current Vest armor points.                                |
| **Ammo Counter**     | Bottom right               | Magazine / Reserve ammo for the equipped weapon.          |
| **Weapon Icon**      | Bottom right               | Icon of the currently equipped weapon.                    |
| **Wave Indicator**   | Top center                 | Current wave number.                                      |
| **Credits Display**  | Top right                  | Current Credits balance.                                  |
| **Items Hotbar**     | Bottom center              | 8 slots showing equipped items with keybind labels (1–8). |
| **Crosshair**        | Center                     | Dynamic crosshair that spreads while moving/shooting.     |
| **Hitmarker**        | Center                     | Brief visual feedback on enemy hit.                       |
| **Enemy Health Bar** | Above enemy                | Shows remaining health of targeted enemy.                 |

## 5.2 Menus

### 5.2.1 Main Menu

- New Game
- Options
- Controls
- Credits
- Exit

### 5.2.2 Options Menu

- **Volume Controls:**
  - Master Volume (slider)
  - SFX Volume (slider)
  - BGM Volume (slider)
- **Mouse Sensitivity** (slider)

### 5.2.3 Map Select

- Choose between 3 available maps before starting.
- Displays map name and thumbnail.

### 5.2.4 Pause Menu

- Resume
- Options
- Controls
- Back to Main Menu

### 5.2.5 Game Over Screen

- Shows wave reached and total Credits earned.
- Buttons: Try Again, Back to Main Menu.

## 5.3 Camera

- First-person camera positioned at the player's head.
- Mouse look rotation with configurable sensitivity.
- Subtle effects: head bob and camera shake (damage, explosions).

---

# 6. Technical Details

## 6.1 Development Tools

- **Game Engine:** Unity 6000.2.10f1.
- **Language:** C#.
- **Input:** New Unity Input System.
- **3D Modeling:** Blender.
- **Project Organization:** Miro and Notion.
- **Version Control:** Git and GitHub.

## 6.2 Architecture Highlights

- **Service Locator:** Central registry for game services (AudioManagerService, GameModeService, etc.).
- **Object Pooling:** GameObjectPool and PooledObject for efficient spawning of enemies, projectiles, and shell casings.
- **Game State Machine:** Enum-driven states (Intro, Loading, MainMenu, Playing, Paused, Shopping, InWave, GameOver) with transition control via GameManager.
- **ScriptableObject Data:** Weapons, items, shop data, tutorial steps, and dialogues use ScriptableObjects for data-driven design.

---

# Appendices

## Appendix A: Shop Items

| Item             | Category   | Unlock Cost    |
| ---------------- | ---------- | -------------- |
| Pistol           | Weapon     | Free (starter) |
| AK47             | Weapon     | High           |
| Shotgun          | Weapon     | High           |
| Medkit           | Consumable | Low            |
| Grenade          | Consumable | Medium         |
| Vest             | Equipment  | Medium         |
| Barricade        | Buildable  | Low            |
| Explosive Barrel | Buildable  | Medium         |
| Bear Trap        | Buildable  | Low            |

## Appendix B: Enemy Reference

| Enemy                  | Type     | Spawn Rule                  |
| ---------------------- | -------- | --------------------------- |
| ZombieDefault (Walker) | Standard | Waves 1+                    |
| ZombieCop              | Tough    | Waves 3+                    |
| ZombieDoctor           | Fast     | Waves 5+                    |
| ZombieBoss             | Boss     | Every 5 waves               |
| TutorialDummyZombie    | Dummy    | Tutorial only               |
| PenguinEnemy           | Secret   | After easter egg activation |

## Appendix C: Maps

| Map       | Theme          | Features                                           |
| --------- | -------------- | -------------------------------------------------- |
| ForestMap | Forest / rural | Open outdoor areas, cabin safe house               |
| DesertMap | Desert / arid  | Sandy terrain, ruined buildings, bunker safe house |
| StreetMap | Urban / city   | Tight streets, alleyways, house safe zone          |

All maps include a **safe zone house** with the Merchant NPC and Wave Button.
