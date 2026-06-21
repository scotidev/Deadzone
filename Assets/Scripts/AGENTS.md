# RULES

This file serves as a guide for agents to locate each responsibility within the project's script folders. Each section describes the purpose of scripts in that directory.

Everytime a new script is created it should also update this file accordingly.

## /Animations

Contains scripts related to animation event handling.

### /Animations/AnimationReceiver.cs

/// Handles animation events for weapons that are in the scene but not attached to the player. Prevents errors from animation events with no receiver.

### /Animations/WeaponAnimationEventHandler.cs

/// Handles all the animation events that come from the weapon in the asset.

## /Audio

Contains scripts related to audio playback and state machine behaviors.

### /Audio/AudioSettings.cs

/// Audio Settings struct used to interact with the AudioManagerService.

### /Audio/PlaySoundBehaviour.cs

/// Plays an AudioClip using the centralized audio service when a state machine enters this state.

### /Audio/PlaySoundCharacterBehaviour.cs

/// Helper StateMachineBehaviour that allows us to more easily play a specific weapon sound.

## /Building

Contains scripts related to building mechanics.

### /Building/BuildingController.cs

/// Controller responsible for managing the building mechanics in the game.

### /Building/GhostObject.cs

/// Attached to "_Ghost" prefabs. Responsible for changing the ghost's material to green (valid) or red (invalid) based on whether the placement is valid. Controlled by BuildingController.

## /Core

Contains core game systems and foundational scripts.

### /Core/GameManager.cs

/// Persistent singleton that manages global game state and time scale. Attach this component to a persistent GameObject in the loader scene.

### /Core/PlayerCache.cs

/// Caches a reference to the Player GameObject so other scripts don't need to call GameObject.FindWithTag("Player") repeatedly. Caching the result once eliminates this cost.

### /Core/Pooling/GameObjectPool.cs

/// Manages GameObject reuse to avoid costly Instantiate/Destroy calls. Reduces garbage collection (GC) pressure caused by frequent allocations.

### /Core/Pooling/PooledObject.cs

/// Attached to pooled GameObjects to allow them to be returned to the pool instead of destroyed. Automatically stops coroutines on OnDisable().

### /Core/SceneLoader.cs

/// Persistent manager responsible for all scene transitions in the game. Shows a loading screen with a progress bar while the new scene loads asynchronously.

### /Core/SlowMotionManager.cs

/// Singleton facade for the slow motion system.

## /EasterEgg

Contains scripts related to the Penguin Easter egg hidden feature.

### /EasterEgg/EasterEggTarget.cs

/// Attach to the photo frame GameObject. Detects consecutive pistol shots and after the required number of hits without missing, activates the Penguin easter egg. Only activates once per game.

### /EasterEgg/PenguinMode.cs

/// Static manager that tracks whether the Penguin Easter egg has been activated. Only one activation per game session.

## /Economy

Contains scripts related to economy, upgrades, and player progression.

### /Economy/AmmoManager.cs

/// Manages ammo/quantity purchases for all item types in the shop. Handles the '+ammo' button logic in a scalable way.

### /Economy/EconomyManager.cs

/// Manages the player's currency system. Tracks the player's money, handles transactions, and notifies listeners of changes.

### /Economy/PlayerProgress.cs

/// Tracks the player's progression through the game. Stores weapon unlocks, upgrade levels, ammo reserves, and buildable quantities. Runtime-only data (not saved between sessions).

### /Economy/UpgradeManager.cs

/// Manages weapon upgrades and applies stat changes to weapons at runtime. Reads WeaponDataSO to calculate upgraded stats and applies them to Weapon instances.

## /Enemy

Contains scripts related to enemy types and behaviors.

### /Enemy/EnemyAttack.cs

/// Controls the melee attack behavior for all enemy types.

### /Enemy/EnemyBase.cs

/// Abstract base class for all enemy types in the game. Static event OnAnyEnemyDied allows the WaveManager to count deaths without needing a direct reference to each individual enemy.

### /Enemy/EnemyFollow.cs

/// Responsible for making the enemy follow the player using Unity's NavMeshAgent. Handles walk animation and idle sound playback.

### /Enemy/PenguinEnemy.cs

/// EnemyBase subclass for the Penguin transformation. Has 1 HP, zero attack damage, keeps wave-scaled reward from the original zombie.

### /Enemy/TutorialDummyZombie.cs

/// A special zombie for the tutorial that stays still and only takes damage. Deactivates specified barriers when it dies to allow player progression.

### /Enemy/ZombieBoss.cs

/// Boss zombie enemy type. Much stronger and tankier than regular zombies.

### /Enemy/ZombieCop.cs

/// Zombie Cop - A more aggressive zombie variant with high damage output.

### /Enemy/ZombieDefault.cs

/// Default Zombie with balanced stats.

### /Enemy/ZombieDoctor.cs

/// Zombie Doctor - A more durable zombie variant with high health pool.

## /Environment

Contains scripts related to world objects, safe zones, and traps.

### /Environment/FogController.cs

/// Controls the fog ParticleSystem lifecycle. Fog starts disabled and is enabled when the tutorial ends.

### /Environment/SafeZone.cs

/// Trigger that defines the safe area of the house in the main scene. Poison damage logic is only active after the tutorial ends.

### /Environment/Teleport.cs

/// Central manager for teleportation logic between two linked teleport pads.

### /Environment/TeleportTrigger.cs

/// Simple proxy script placed on trigger colliders. Forwards collision events to the central Teleport manager.

## /Gameplay

Contains general gameplay scripts.

### /Gameplay/GameOverManager.cs

/// Singleton that orchestrates the Game Over flow when the player dies. Plays death sound, unlocks cursor, disables input, shows Game Over panel.

### /Gameplay/PauseManager.cs

/// Singleton that manages the game's pause state: UI visibility, cursor locking, input interception, and time scale delegation to GameManager.

### /Gameplay/PistolPickup.cs

/// Handles the pistol pickup interaction in the tutorial. Unlocks the pistol, activates the ammo HUD and player arms, removes barriers, and equips the weapon.

### /Gameplay/TutorialStarter.cs

/// Ensures the initial state for the tutorial: the player arms and ammo HUD start disabled.

## /Interfaces

Contains interface definitions used throughout the project.

### /Interfaces/IAudioManagerService.cs

/// Interface for audio manager service.

### /Interfaces/IDamageable.cs

/// Interface for objects that can take damage.

### /Interfaces/Interactable.cs

/// Abstract base class for all interactable objects in the game. Derived classes must implement Interact().

### /Interfaces/IShopItemCallback.cs

/// Interface for shop item callbacks.

## /Items

Contains scripts related to item behaviors and data containers.

### /Items/Barricade.cs

/// Represents a barricade that blocks enemy path to the player. Inherits from ItemBehaviour to unify item selection system.

### /Items/BearTrap.cs

/// BearTrap buildable item. Places a bear trap in the world. When triggered by an enemy, applies damage and stun.

### /Items/ExplosiveBarrel.cs

/// Explosive barrel buildable item. Places an explosive barrel in the world. When shot, triggers a chain reaction explosion with slow-motion effect.

### /Items/Grenade.cs

/// Grenade consumable item. Players hold fire button to arm, release to throw. Hold-to-charge mechanic with detonation and explosion damage.

### /Items/GrenadeThrown.cs

/// Thrown grenade behavior. Handles detonation timer, explosion physics, damage application, and VFX/audio.

### /Items/ItemBehaviour.cs

/// Base class for all selectable items in the player's inventory. Unifies the interface for weapons, consumables, and buildables.

### /Items/Medkit.cs

/// Medkit consumable item. Heals the player when used.

### /Items/Vest.cs

/// Vest armor item. Auto-equipped when unlocked/upgraded. Provides armor damage reduction. NOT selectable via keys 1-8.

### /Items/ScriptableObjects/BuildableDataSO.cs

/// ScriptableObject that represents a buildable item in the game.

### /Items/ScriptableObjects/GrenadeDataSO.cs

/// Scriptable Object that defines the data for a grenade item, including its damage, explosion radius, and maximum ammo capacity.

### /Items/ScriptableObjects/ItemDataSO.cs

/// Base ScriptableObject for all items.

### /Items/ScriptableObjects/MedkitDataSO.cs

/// Scriptable Object that defines the data for a medkit item, including its heal amount and maximum ammo capacity.

### /Items/ScriptableObjects/ShopItemDataSO.cs

/// Defines the display data and economy settings for a shop item card. Supports unlocks, upgrades, ammo purchases, and different item types.

### /Items/ScriptableObjects/TutorialStepSO.cs

/// ScriptableObject that defines a single tutorial step. Create instances via Assets > Create > Deadzone > Tutorial Step.

### /Items/ScriptableObjects/VestDataSO.cs

/// Scriptable Object that defines the data for a vest item, including its resistance value.

### /Items/ScriptableObjects/WeaponDataSO.cs

/// ScriptableObject that defines a weapon's base stats and how they scale with upgrades.

## /NPC

Contains scripts related to non-player characters.

### /NPC/MerchantDialogueCategorySO.cs

/// ScriptableObject that defines a category of merchant dialogue lines. Used by NPCAudio to organize dialogues by context.

### /NPC/NPC.cs

/// Represents the NPC that can be interacted with to open the shop interface. Also handles proximity detection for dialogue triggers.

### /NPC/NPCAudio.cs

/// Controls NPC audio playback including merchant dialogue lines for various shop events. Prevents dialogue overlap and manages subtitle display.

## /Player

Contains scripts related to player character logic, movement, inventory, health, and camera control.

### /Player/CameraLook.cs

/// Camera Look. Handles the rotation of the camera and player character when looking around.

### /Player/Character.cs

/// Main Character Component. This component handles the most important functions of the character, and interfaces with basically every part of the asset, it is the hub where it all converges.

### /Player/CharacterAnimationEventHandler.cs

/// Handles all the animation events that come from the character in the asset.

### /Player/CharacterBehaviour.cs

/// Character Abstract Behaviour.

### /Player/CharacterKinematics.cs

/// Handles all the Inverse Kinematics needed for our Character. Uses Unity's IK code.

### /Player/Inventory.cs

/// Manages the player's item selection and equipping logic. Handles weapons, buildables, and consumables through a unified slot system.

### /Player/InventoryBehaviour.cs

/// Abstract Inventory Class. Helpful so you can implement your own inventory system!

### /Player/Movement.cs

/// Controls character movement using Rigidbody. Ensures the player interacts properly with the ground, including slopes and stairs, and handles jumping and crouching.

### /Player/MovementBehaviour.cs

/// Abstract movement class. Handles interactions with the main movement component.

### /Player/PlayerHealth.cs

/// Manages the player's health. Implements IDamageable so enemies can deal damage via interface. Integrates with Vest for armor damage reduction.

### /Player/PlayerInteraction.cs

/// Manages player interaction with interactable objects in the game world. Uses raycasting from the player camera to detect interactable objects and enemies.

## /Services

Contains scripts related to service locator and game services.

### /Services/AudioManagerService.cs

/// Manages the spawning and playing of sounds. Implements the IAudioManagerService interface.

### /Services/Bootstraper.cs

/// Initializes all game services at startup.

### /Services/GameModeService.cs

/// Game Mode Service. Provides access to player character and game mode state.

### /Services/IGameModeService.cs

/// Interface for game mode services.

### /Services/IGameService.cs

/// Interface for all game services.

### /Services/ServiceLocator.cs

/// Simple service locator for IGameService instances.

## /TV

Contains scripts related to TV and video playback.

### /TV/TVAudioController.cs

/// Manages synchronized 3D audio playback with a video player on the TV. Handles mute/unmute interaction, occlusion through walls, and audio-video sync.

## /UI

Contains scripts related to user interface, menus, and HUD elements.

### /UI/BaseUI.cs

/// Base class for all UI panels in the game. Provides common functionality for showing and hiding panels.

### /UI/CanvasSpawner.cs

/// Player Interface. Manages canvas spawning for different screens.

### /UI/ControlsUI.cs

/// Manages the controls information panel UI.

### /UI/CreditsUI.cs

/// Manages the credits panel UI with auto-scrolling effect.

### /UI/GameOverUI.cs

/// Manages the Game Over screen, displaying the wave the player reached and providing buttons to retry or quit to the main menu.

### /UI/HealFeedbackUI.cs

/// Visual feedback effect displayed on screen edges when player heals. Shows a green vignette that pulses on screen edges.

### /UI/InteractionPromptUI.cs

/// Manages the interaction prompt UI element that displays context-sensitive messages to the player.

### /UI/ItemPreviewHandler.cs

/// Manages 3D item preview in shop UI. Handles spawning, rotating, positioning, scaling, and layer assignment.

### /UI/LoadingScreenUI.cs

/// Controls the loading screen UI: shows/hides the screen and updates the progress bar fill amount.

### /UI/LogoIntro.cs

/// Handles the logo intro scene. Waits for a specified duration before transitioning to the main menu. Allows skipping with any key press or mouse click if enabled.

### /UI/MapCardRelay.cs

/// Generic relay that forwards pointer events (hover enter, hover exit, click) as UnityEvents. Used alongside UIButtonFeedback to separate visual/audio feedback from domain logic.

### /UI/MenuManager.cs

/// Manages the main menu UI and navigation.

### /UI/OptionsUI.cs

/// Manages the options menu UI including mouse sensitivity and volume settings.

### /UI/PauseUI.cs

/// Manages the pause menu UI and its button interactions.

### /UI/SelectManager.cs

/// Controls the map selection screen, including background preview transitions and loading the selected map scene.

### /UI/ShopItemCard.cs

/// Represents a compact shop item card showing icon, name, and level.

### /UI/ShopManager.cs

/// Manages the shop interface system in the game.

### /UI/ShopUI.cs

/// Manages the shop UI including item cards and shop panel interactions.

### /UI/StatBarDisplay.cs

/// Displays a stat with label, icon, and three-layer bar (background, upgrade, current).

### /UI/StatBlockDisplay.cs

/// Visual stat display using 5 fillable blocks (bars). Each block can be partially filled.

### /UI/TextScalePulse.cs

/// Reusable component that applies a scale pulse animation to any Transform. Call Pulse() to briefly scale up and back down.

### /UI/TutorialEndTrigger.cs

/// Trigger that ends the tutorial and starts the first official wave. Activates the poison system and triggers the WaveManager.

### /UI/TutorialManager.cs

/// Singleton manager that controls the tutorial flow. Processes a queue of TutorialStepSO, checks completion conditions, and handles timeouts.

### /UI/TutorialTriggerZone.cs

/// Place this on a GameObject with a Collider (isTrigger = true). When the player enters the trigger, the assigned TutorialStepSO is queued.

### /UI/UIButtonFeedback.cs

/// Unified UI button feedback controller that handles visual scale animation and audio feedback on hover/click events.

### /UI/UIManager.cs

/// Central coordinator for UI. Manages all game panels and acts as a mediator between game systems and UI components.

### /UI/HUD/Crosshair.cs

/// Crosshair. Handles crosshair display and spread visualization.

### /UI/HUD/CurrencyUI.cs

/// UI component that displays the player's current currency in the HUD. Subscribes to EconomyManager events to update in real-time.

### /UI/HUD/Element.cs

/// Interface Element that can be used as a base for all other elements. Has a Tick method called every frame for updating state.

### /UI/HUD/ElementText.cs

/// Text Interface Element. Inherits from Element and adds a TextMeshProUGUI component for displaying text in the HUD.

### /UI/HUD/EnemyHealthBarUI.cs

/// Manages a single reusable health bar that displays above the currently targeted enemy. Shows only when the player's aim is on an enemy.

### /UI/HUD/FeedbackMessageUI.cs

/// Displays a temporary feedback message (e.g. 'Out of items!') with auto-hide and optional audio feedback.

### /UI/HUD/HitmarkerManager.cs

/// Manages the hitmarker system for the game. Displays visual feedback and plays audio when the player successfully hits an enemy.

### /UI/HUD/ImageItem.cs

/// Item Image. Displays the icon of the currently equipped item. Uses the unified GetIcon() method from ItemBehaviour for all item types.

### /UI/HUD/MerchantSubtitleUI.cs

/// Displays temporary subtitle text for merchant dialogue lines.

### /UI/HUD/PlayerHealthUI.cs

/// Manages the player health bar UI. Subscribes to PlayerHealth events and updates the green bar fill amount in real-time.

### /UI/HUD/TextAmmunitionCurrent.cs

/// Current Ammunition Text. Shows current magazine ammunition count.

### /UI/HUD/TextAmmunitionTotal.cs

/// Total Ammunition Text. Shows total reserve ammunition count.

### /UI/HUD/TextTutorial.cs

/// Interface component that hides or shows the tutorial text based on input.

### /UI/HUD/TutorialUI.cs

/// Controls the visual display of tutorials on the HUD. Manages showing/hiding with fade in/out and audio.

### /UI/HUD/VestUI.cs

/// Manages the vest armor bar UI. Subscribes to Vest events and updates the armor bar fill amount in real-time.

### /UI/HUD/WaveUI.cs

/// Persistent HUD panel that displays wave information during gameplay.

## /Utilities

Contains utility scripts for common operations.

### /Utilities/Log.cs

/// Custom logging utility with shorthand methods for log, warning, and error messages. Provides null-safe logging wrappers.

### /Utilities/Logger.cs

/// Custom logging utility using [Conditional] attribute so calls are completely removed from release builds. Zero performance cost in production.

### /Utilities/TimeHandler.cs

/// Developer debug tool that allows real-time adjustment of Time.timeScale via keyboard input.

### /Utilities/UtilitiesArrays.cs

/// Array Utilities. Provides helper methods for array operations.

### /Utilities/WeaponStatsCalculator.cs

/// Calculates weapon statistics from modifiers and provides normalization values for stat bars by scanning shop item data at runtime.

## /VFX

Contains scripts related to visual effects and lighting.

### /VFX/LightFlicker.cs

/// Simulates a flickering light effect for faulty or unstable light sources. Randomly changes light intensity at varying intervals.

### /VFX/PoliceLight.cs

/// Simulates a police light siren effect by alternating between two light sources (red and blue).

## /Wave

Contains scripts related to wave-based enemy spawning.

### /Wave/EnemySpawnConfig.cs

/// Defines an enemy type that can be spawned during waves. Configure in the WaveManager inspector.

### /Wave/EnemySpawner.cs

/// Enemy Spawn Point placed in the scene. Receives from WaveManager the list of available prefabs and quantity to spawn.

### /Wave/WaveButton.cs

/// Interactive button in the 3D world that starts the next wave of enemies.

### /Wave/WaveManager.cs

/// Singleton manager responsible for the entire wave lifecycle.

## /Weapons

Contains scripts related to weapon mechanics, attachments, projectiles, and weapon data.

### /Weapons/Magazine.cs

/// Magazine. Handles ammunition capacity by reading from WeaponDataSO and PlayerProgress to dynamically calculate magazine size based on weapon type and upgrade level.

### /Weapons/MagazineBehaviour.cs

/// Magazine Behaviour. Abstract base class defining magazine properties such as ammunition total and UI sprite.

### /Weapons/MeleeWeapon.cs

/// Implementation of a simple melee weapon. Performs a short-range attack that damages enemies using an OverlapBox check.

### /Weapons/Muzzle.cs

/// Muzzle. Handles muzzle flash particles, flash light, and fire audio when the weapon is fired.

### /Weapons/MuzzleBehaviour.cs

/// Muzzle Behaviour. Abstract base class defining muzzle properties including firing socket, sprite, audio, particles, and flash light.

### /Weapons/Projectile.cs

/// Projectile. Handles projectile movement, collision detection, damage application, and object pool recycling.

### /Weapons/Scope.cs

/// Weapon Scope. Handles scope appearance via sprite for the character's interface.

### /Weapons/ScopeBehaviour.cs

/// Scope Behaviour. Abstract base class defining scope properties such as the UI sprite for the scope reticle.

### /Weapons/UtilitiesWeapons.cs

/// Weapon Static Utilities. Provides extension methods for weapon attachment arrays.

### /Weapons/Weapon.cs

/// Weapon. Handles firing, reloading, ammo management, stat scaling from WeaponDataSO, and attachment integration. Central hub for all weapon gameplay logic.

### /Weapons/WeaponAttachmentManager.cs

/// Weapon Attachment Manager. Handles equipping and storing a Weapon's Attachments (scope, muzzle, magazine).

### /Weapons/WeaponAttachmentManagerBehaviour.cs

/// Weapon Attachment Manager Behaviour. Abstract base class providing access to equipped weapon attachments.

### /Weapons/WeaponBehaviour.cs

/// Base class for weapons, implementing ItemBehaviour interface. Maintains backward compatibility with existing Weapon.cs implementations.

## REMOVED SCRIPTS

The following scripts were removed as they served only debug/testing purposes and had no gameplay utility:

- Tests/AmmoCapFixValidationTest.cs
- UI/HUD/TextMouseLock.cs (UI debug text)
- UI/HUD/TextTimescale.cs (UI debug text)
- Utilities/StatBarDebugger.cs
