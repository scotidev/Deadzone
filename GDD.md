# 1. Overview

## 1.1 Game Concept

A first-person shooter (FPS) game with Tower Defense and survival elements. The player is trapped inside a house that serves as the last safe refuge in the Deadzone, facing zombie hordes that attack in waves. The goal is to survive as many waves as possible through a loop of combat, resource gathering, and defense building. Outside the house, the Deadzone (a toxic fog that contaminates everything) limits exploration time, creating a risk-reward dynamic.

## 1.2 Target Audience

- **Primary:** Fans of cooperative shooters and zombie survival games (such as *Call of Duty: Zombies* and *Left 4 Dead*).
- **Secondary:** Players who enjoy strategy and Tower Defense games, and who appreciate the freedom to build and fortify a base.
- **Age Rating:** Teens and adults (16+), due to violence and survival themes.

## 1.3 Genre and Platform

- **Genre:** FPS (First-Person Shooter), Tower Defense, Survival.
- **Platform:** PC (Windows) and WebGL.

## 1.4 Core Loop

- Fight -> Earn Rewards -> Spend Rewards to Grow Stronger -> Fight a Bigger Challenge

---

# 2. Mechanics

## 2.1 Gameplay

Gameplay is divided into two cyclical phases: Preparation Phase and Combat Phase.

- **Preparation Phase:** Occurs between enemy waves. The player is safe inside the house and can spend earned points to buy weapons, ammo, upgrades, and defensive items in a shop. This is the time to place traps, barricades, and plan the defense for the next wave. The player may venture into the toxic fog to collect bonus resources, but will take continuous damage.
- **Combat Phase:** The player starts the next wave by interacting with the **Horde Button**. Enemies spawn from points outside the house and try to invade and attack the player. The objective is to eliminate all enemies in the wave.

### 2.1.1 Rules

- The player loses if their health reaches zero.
- The player clears a wave by eliminating all designated enemies.
- The area outside the house deals constant damage. Health lost in the fog does not regenerate automatically.
- Weapons require ammo. If ammo runs out, the weapon cannot be used until reloaded or until more ammo is purchased.

## 2.2 Player

### 2.2.1 Movement and Controls

- **Move:** WASD.
- **Look:** Mouse movement.
- **Sprint:** Hold Shift.
- **Jump:** Spacebar.
- **Shoot:** Left Mouse Button.
- **Aim:** Right Mouse Button.
- **Reload:** `R` key.
- **Switch Weapon:** Number keys (`1`, `2`, `3`, ...).
- **Interact / Use:** `E` key (for the Horde Button, shop, etc.).

## 2.3 Enemies

- **Walker Zombie:** Standard unit. Slow, melee attacker, moves in large groups.
- **Runner Zombie:** More fragile than the basic unit, but much faster.
- **Tanker Zombie:** Large, slow, and high-health unit. Absorbs heavy damage and can destroy barricades more easily.

## 2.4 Progression

### 2.4.1 Scoring

- **Points** are the in-game currency.
- Earned by eliminating enemies and defeating hordes.
- Used in the shop to buy everything: weapons, upgrades, ammo, traps, and medkits.

### 2.4.2 Maps

- The game will take place on one selectable map out of three possible options, and all maps must include a safe zone house. The house layout will be designed to provide multiple routes, choke points, and open areas, encouraging movement and varied defense strategies.

## 2.5 AI (Artificial Intelligence)

- Enemy AI will be based on pathfinding (NavMesh).
- The enemies' primary goal is to find the shortest path to the player.
- Enemies will attack barricades blocking their path, attempting to open a new route.

## 2.6 Game Elements

- **Shop (Merchant):** A fixed interaction point inside the house where the player spends points by interacting with the NPC.
- **Horde Button:** An interactive object that, when activated, ends the Preparation Phase and starts the next Combat Phase.
- **Placeable Traps:** Purchasable items (explosive barrels, proximity mines, barricades) that the player can place in the environment during the Preparation Phase using a grid system.

## 2.7 Combat

- Combat is first-person and firearm-based.
- Hit feedback (hitmarker) is crucial to inform the player that they are dealing damage.
- Different weapons will have different attributes (damage, fire rate, accuracy, magazine size).

---

# 3. Aesthetics

## 3.1 Visual Style

- **Low Poly:** A 3D style with low-poly models, solid colors or gradients, and simple textures.

## 3.2 Visual References

- Team Fortress 2
- Unturned
- Boxhead

## 3.3 Audio

### 3.3.1 Soundtrack

- **Preparation Phase:** Ambient, calm music with suspense and tension, suggesting the peace is temporary.
- **Combat Phase:** Dynamic electronic or industrial rock music that increases in intensity as the horde advances.

### 3.3.2 Sound Effects (SFX)

- **Feedback:** Sound effects for taking damage, placing objects, using medkits, and all gameplay-critical actions.
- **Weapons:** Distinct shooting, reloading, and empty-magazine click sounds for each weapon.
- **Enemies:** Distinct groans, footsteps, and attack sounds for each zombie type.
- **Environment:** Deadzone (toxic fog) ambience, door creaks, and Deadzone Terminal sounds.
- **UI:** Click, shop purchase, and confirmation sounds.

---

# 4. Narrative

Narrative is environmental. The story is told through the setting: why is the house isolated? What caused the Deadzone and the zombies?

---

# 5. Systems

## 5.1 UI (User Interface)

### 5.1.1 HUD (Heads-Up Display)

- **Bottom Left Corner:** Player health bar.
- **Bottom Right Corner:** Ammo counter (bullets in magazine / total bullets).
- **Top Left Corner:** Points counter and current wave number.
- **Center Screen:** Crosshair and hitmarkers.

## 5.2 Menus

- **Main Menu:** Start Game, Options, Credits, Controls, Exit.
- **Pause Menu:** Resume, Options, Controls, Back to Main Menu.
- **Game Over Screen:** Shows the reached wave and buttons for **Try Again** and **Exit**.
- **Shop Panel:** Interface opened when interacting with the bench, with buttons for each purchasable item.

## 5.3 Camera

- First-person camera positioned at the player's head.
- May include subtle effects such as head bob and camera shake (when taking damage or during explosions) to increase immersion.

---

# 6. Technical Details

## 6.1 Development Tools

- **Game Engine:** Unity 6000.2.10f1.
- **Language:** C#.
- **3D Modeling:** Blender.
- **Project Organization:** Miro and Notion.
- **Version Control:** Git and GitHub.

## 6.2 External Links

- **Project Board:** https://miro.com/app/board/uXjVGGCq7CA=/
- **Assets Used:**
  - FPS kit: https://assetstore.unity.com/packages/templates/systems/low-poly-shooter-pack-free-sample-144839
  - House: https://www.kenney.nl/assets/building-kit
  - Props 1: https://quaternius.com/packs/zombieapocalypsekit.html
  - Props 2: https://quaternius.com/packs/toonshootergamekit.html
  - Props 3: https://quaternius.com/packs/survival.html
  - Forest: https://assetstore.unity.com/packages/3d/vegetation/environment-pack-free-forest-sample-168396
  - House Furniture 1: https://kaylousberg.itch.io/furniture-bits
  - House Furniture 2: https://assetstore.unity.com/packages/3d/props/interior/low-poly-tavern-interior-245229
  - Forest Ground: (not specified)
- **References:**
  - Team Fortress 2: https://store.steampowered.com/app/440/Team_Fortress_2/
  - Unturned: https://store.steampowered.com/agecheck/app/304930/
  - Boxhead: https://www.miniplay.com/game/boxhead-2play-rooms
  - Combat Arms Cabin Fever: https://combatarms.fandom.com/wiki/Cabin_Fever
  - Left 4 Dead 2: https://store.steampowered.com/app/550/Left_4_Dead_2/
  - Call of Duty Zombies: https://www.callofduty.com/br/pt/blackops7/zombies
