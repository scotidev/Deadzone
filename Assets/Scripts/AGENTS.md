# RULES

This file serves as a guide for agents to locate each responsibility within the project's script folders. Each section describes the purpose of scripts in that directory.

Everytime a new script is created it should also update this file accordingly.

## /Player

Contains scripts related to player character logic, movement, inventory, health, and camera control.

### /Player/Character.cs

/// Main Character Component. This component handles the most important functions of the character, and interfaces with basically every part of the asset, it is the hub where it all converges.

### /Player/CharacterBehaviour.cs

/// Base class for the character behaviour. Contains shared logic between player and NPC characters.

### /Player/CharacterKinematics.cs

/// Character Kinematics. This component handles the procedural animation of the character, doing the math that makes all the animation look the way it does.

### /Player/Movement.cs

/// Movement. Handles the character movement using the new input system.

### /Player/MovementBehaviour.cs

/// Movement Behaviour. Contains data for movement settings like speed, crouch height, and jump force.

### /Player/CameraLook.cs

/// Camera Look. Handles the camera rotation and mouse look input.

### /Player/PlayerHealth.cs

/// Player Health. Manages player health, damage handling, and death logic.

### /Player/PlayerArmor.cs

/// Player Armor. Manages player armor points and damage absorption.

### /Player/Inventory.cs

/// Inventory. Handles the player inventory system for storing and selecting items.

### /Player/InventoryBehaviour.cs

/// Inventory Behaviour. Contains data for inventory slots and item management.

### /Player/ItemSelector.cs

/// Item Selector. Handles item selection logic when scrolling through inventory.

### /Player/PlayerInteraction.cs

/// Player Interaction. Handles interaction with interactable objects in the world using raycasting.

### /Player/CharacterAnimationEventHandler.cs

/// Character Animation Event Handler. Handles animation events triggered by the character's animator for weapon sounds and effects.

### /Player/CrouchDebugHelper.cs

/// Crouch Debug Helper. Debug tool for testing crouch functionality.

## /Weapons

Contains scripts related to weapon mechanics, attachments, projectiles, and weapon data.

### /Weapons/Weapon.cs

/// Weapon. This class handles most of the things that weapons need.

### /Weapons/WeaponBehaviour.cs

/// Weapon Behaviour. Base class for weapons with shared logic like fire modes and attributes.

### /Weapons/MeleeWeapon.cs

/// Melee Weapon. Handles melee attack logic and damage.

### /Weapons/Projectile.cs

/// Projectile. Handles the projectile movement, collision detection, and damage application.

### /Weapons/Muzzle.cs

/// Muzzle. Handles muzzle flash and sound when firing.

### /Weapons/MuzzleBehaviour.cs

/// Muzzle Behaviour. Contains data for muzzle settings like flash duration.

### /Weapons/Scope.cs

/// Scope. Handles scope overlay, zoom, and aiming accuracy.

### /Weapons/ScopeBehaviour.cs

/// Scope Behaviour. Contains data for scope settings like zoom level.

### /Weapons/Magazine.cs

/// Magazine. Handles ammunition reloading and ammo count.

### /Weapons/MagazineBehaviour.cs

/// Magazine Behaviour. Contains data for magazine capacity and reload time.

### /Weapons/WeaponAttachmentManager.cs

/// Weapon Attachment Manager. Handles attachment points on the weapon (scope, muzzle, magazine).

### /Weapons/WeaponAttachmentManagerBehaviour.cs

/// Weapon Attachment Manager Behaviour. Contains data for attachment configurations.

### /Weapons/UtilitiesWeapons.cs

/// Utilities Weapons. Utility functions specific to weapons like calculating spread.

### /Weapons/Data/WeaponDataSO.cs

/// Weapon Data SO. ScriptableObject containing weapon data for easy asset creation.

## /NPC

Contains scripts related to non-player characters (enemies).

### /NPC/NPC.cs

/// NPC. Handles enemy AI, pathfinding, and combat behavior.

### /NPC/NPCAudio.cs

/// NPC Audio. Handles enemy death sounds and alert sounds.

## /Environment

Contains scripts related to world objects and traps.

### /Environment/BearTrap.cs

/// Bear Trap. Handles trap activation and damage when triggered.

### /Environment/SafeZone.cs

/// Safe Zone. Handles safe area logic where enemies cannot enter.

### /Environment/FogController.cs

/// Fog Controller. Handles fog density and visibility changes.

## /Wave

Contains scripts related to wave-based enemy spawning.

### /Wave/WaveManager.cs

/// Wave Manager. Manages wave progression, enemy counts, and victory conditions.

### /Wave/EnemySpawner.cs

/// Enemy Spawner. Handles spawning enemies at defined points.

### /Wave/EnemySpawnConfig.cs

/// Enemy Spawn Config. Configuration for enemy spawn points and types.

### /Wave/WaveButton.cs

/// Wave Button. Debug button to start waves manually.

## /UI

Contains scripts related to user interface, menus, and HUD elements.

### /UI/BaseUI.cs

/// Base UI. Base class for all UI screens with common functionality.

### /UI/UIManager.cs

/// UI Manager. Manages all UI screens and transitions.

### /UI/MenuManager.cs

/// Menu Manager. Handles main menu and pause menu logic.

### /UI/ShopManager.cs

/// Shop Manager. Manages the in-game shop for purchasing items.

### /UI/ShopUI.cs

/// Shop UI. Handles shop screen display and item selection.

### /UI/ShopItemCard.cs

/// Shop Item Card. Individual shop item display in the UI.

### /UI/ShopItemDataSO.cs

/// Shop Item Data SO. ScriptableObject for shop item data.

### /UI/WeaponPreviewHandler.cs

/// Weapon Preview Handler. Handles weapon preview in the shop.

### /UI/PauseUI.cs

/// Pause UI. Handles pause menu display.

### /UI/OptionsUI.cs

/// Options UI. Handles options menu for settings.

### /UI/ControlsUI.cs

/// Controls UI. Displays control scheme information.

### /UI/CreditsUI.cs

/// Credits UI. Displays game credits.

### /UI/MenuButtonAudio.cs

/// Menu Button Audio. Handles menu button hover and click sounds.

### /UI/MenuImageScale.cs

/// Menu Image Scale. Handles menu background animation.

### /UI/LogoIntro.cs

/// Logo Intro. Handles logo animation at game start.

### /UI/CanvasSpawner.cs

/// Canvas Spawner. Spawns UI canvases for different screens.

### /UI/InteractionPromptUI.cs

/// Interaction Prompt UI. Shows interaction prompts when aiming at objects.

### /UI/HUD/Crosshair.cs

/// Crosshair. Handles crosshair display and spread visualization.

### /UI/HUD/PlayerHealthUI.cs

/// Player Health UI. Displays player health bar.

### /UI/HUD/PlayerArmorUI.cs

/// Player Armor UI. Displays player armor bar.

### /UI/HUD/CurrencyUI.cs

/// Currency UI. Displays player's current currency.

### /UI/HUD/HitmarkerManager.cs

/// Hitmarker Manager. Shows hitmarkers when damaging enemies.

### /UI/HUD/EnemyHealthBarUI.cs

/// Enemy Health Bar UI. Shows enemy health above their head.

### /UI/HUD/ImageWeapon.cs

/// Image Weapon. Displays equipped weapon image.

### /UI/HUD/TextAmmunitionCurrent.cs

/// Text Ammunition Current. Shows current magazine ammunition.

### /UI/HUD/TextAmmunitionTotal.cs

/// Text Ammunition Total. Shows total reserve ammunition.

### /UI/HUD/TextTimescale.cs

/// Text Timescale. Shows current timescale in debug.

### /UI/HUD/TextMouseLock.cs

/// Text Mouse Lock. Shows if mouse is locked.

### /UI/HUD/TextTutorial.cs

/// Text Tutorial. Shows tutorial messages.

### /UI/HUD/MerchantSubtitleUI.cs

/// Merchant Subtitle UI. Shows merchant dialogue subtitles.

### /UI/HUD/Element.cs

/// Element. Base class for HUD elements.

### /UI/HUD/ElementText.cs

/// Element Text. Text element with auto-localization.

### /UI/HUD/WaveUI.cs

/// Wave UI. Displays current wave information.

## /Services

Contains scripts related to service locator and game services.

### /Services/ServiceLocator.cs

/// Service Locator. Static class for accessing game services globally.

### /Services/IGameService.cs

/// IGame Service. Interface for all game services.

### /Services/IGameModeService.cs

/// IGame Mode Service. Interface for game mode services.

### /Services/GameModeService.cs

/// Game Mode Service. Base class for game mode services.

### /Services/Bootstraper.cs

/// Bootstraper. Initializes all game services at startup.

### /Services/AudioManagerService.cs

/// Audio Manager Service. Handles audio playback and management.

## /Economy

Contains scripts related to economy, upgrades, and player progression.

### /Economy/EconomyManager.cs

/// Economy Manager. Manages in-game currency and transactions.

### /Economy/PlayerProgress.cs

/// Player Progress. Saves and loads player progress data.

### /Economy/UpgradeManager.cs

/// Upgrade Manager. Handles weapon and player upgrades.

## /Exclusives

Contains exclusive item scripts with special abilities or effects.

### /Exclusives/ItemExclusive.cs

/// Item Exclusive. Base class for exclusive items with special properties.

### /Exclusives/WeaponExclusive.cs

/// Weapon Exclusive. Exclusive weapon with unique stats or effects.

### /Exclusives/PistolExclusive.cs

/// Pistol Exclusive. Exclusive variant of the pistol.

### /Exclusives/AK47Exclusive.cs

/// AK47 Exclusive. Exclusive variant of the AK47.

### /Exclusives/ShotgunExclusive.cs

/// Shotgun Exclusive. Exclusive variant of the shotgun.

### /Exclusives/MedkitExclusive.cs

/// Medkit Exclusive. Exclusive medkit with enhanced healing.

### /Exclusives/GrenadeExclusive.cs

/// Grenade Exclusive. Exclusive variant of grenades.

### /Exclusives/BearTrapExclusive.cs

/// Bear Trap Exclusive. Exclusive bear trap with better stats.

### /Exclusives/BarricadeExclusive.cs

/// Barricade Exclusive. Exclusive barricade variant.

### /Exclusives/ExplosiveBarrelExclusive.cs

/// Explosive Barrel Exclusive. Exclusive explosive barrel.

### /Exclusives/VestExclusive.cs

/// Vest Exclusive. Exclusive armor vest with more protection.

## /Gameplay

Contains general gameplay scripts.

### /Gameplay/PauseManager.cs

/// Pause Manager. Handles game pause state and input.

## /Utilities

Contains utility scripts for common operations.

### /Utilities/Log.cs

/// Log. Custom logging utility with controlled output.

### /Utilities/TimeHandler.cs

/// Time Handler. Handles time-related calculations.

### /Utilities/UtilitiesArrays.cs

/// Utilities Arrays. Array manipulation utilities.

### /Utilities/WeaponStatsCalculator.cs

/// Weapon Stats Calculator. Calculates weapon statistics from modifiers.

## /Interfaces

Contains interface definitions used throughout the project.

### /Interfaces/IDamageable.cs

/// IDamageable. Interface for objects that can take damage.

### /Interfaces/Interactable.cs

/// Interactable. Interface for objects that can be interacted with.

### /Interfaces/ISelectableItem.cs

/// ISelectable Item. Interface for items that can be selected in inventory.

### /Interfaces/IAudioManagerService.cs

/// IAudio Manager Service. Interface for audio service.
