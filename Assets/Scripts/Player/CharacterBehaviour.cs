// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Character Abstract Behaviour.
    /// </summary>
    public abstract class CharacterBehaviour : MonoBehaviour
    {
        #region UNITY

        /// <summary>
        /// Awake.
        /// </summary>
        protected virtual void Awake() { }

        /// <summary>
        /// Start.
        /// </summary>
        protected virtual void Start() { }

        /// <summary>
        /// Update.
        /// </summary>
        protected virtual void Update() { }

        /// <summary>
        /// Late Update.
        /// </summary>
        protected virtual void LateUpdate() { }

        #endregion

        #region GETTERS

        /// <summary>
        /// Returns the player character's main camera.
        /// </summary>
        public abstract Camera GetCameraWorld();

        /// <summary>
        /// Returns a reference to the Inventory component.
        /// </summary>
        public abstract InventoryBehaviour GetInventory();

        /// <summary>
        /// [ADICIONADO] Retorna verdadeiro se o personagem estiver interagindo com um menu/loja.
        /// </summary>
        public abstract bool IsInterfaceMode();

        /// <summary>
        /// [ADICIONADO] Define se o personagem deve entrar ou sair do modo de interface.
        /// Isso bloqueia ações como atirar e mover a câmera.
        /// </summary>
        public abstract void SetInterfaceMode(bool value);

        /// <summary>
        /// [ADICIONADO] Guarda ou revela a arma do personagem via animação.
        /// </summary>
        public abstract void SetHolstered(bool value);

        /// <summary>
        /// Returns true if the Crosshair should be visible.
        /// </summary>
        public abstract bool IsCrosshairVisible();
        /// <summary>
        /// Returns true if the character is running.
        /// </summary>
        public abstract bool IsRunning();

        /// <summary>
        /// Returns true if the character is aiming.
        /// </summary>
        public abstract bool IsAiming();
        /// <summary>
        /// Returns true if the game cursor is locked.
        /// </summary>
        public abstract bool IsCursorLocked();

        /// <summary>
        /// Returns true if the tutorial text should be visible on the screen.
        /// </summary>
        public abstract bool IsTutorialTextVisible();

        /// <summary>
        /// Returns the Movement Input.
        /// </summary>
        public abstract Vector2 GetInputMovement();
        /// <summary>
        /// Returns the Look Input.
        /// </summary>
        public abstract Vector2 GetInputLook();

        // ==============================================================
        //  [ADICIONADO] IsJumping()
        // ==============================================================
        //  AULA: O QUE É UMA CLASSE ABSTRATA E POR QUE ELA EXISTE AQUI?
        //
        //  Imagine que CharacterBehaviour é um "contrato" assinado entre
        //  dois scripts que precisam se comunicar:
        //
        //  → Movement.cs precisa saber: "o jogador está pulando?"
        //  → Character.cs é quem lê o input e sabe a resposta.
        //
        //  O problema: Movement.cs não conhece Character.cs diretamente.
        //  A solução: ambos falam através do "contrato" CharacterBehaviour.
        //
        //  "abstract" significa: "todo filho OBRIGATORIAMENTE deve implementar
        //  este método". É como dizer "este campo do contrato não pode ficar
        //  em branco — quem assinar tem que preencher".
        //
        //  Character.cs herda de CharacterBehaviour e é obrigado a fornecer
        //  um IsJumping() real. Movement.cs chama playerCharacter.IsJumping()
        //  sem precisar saber quem está do outro lado.
        //
        //  Esse padrão se chama "programação orientada a interfaces/abstrações"
        //  e é um dos pilares do código profissional: os módulos se comunicam
        //  por contratos, não por dependências diretas.
        /// <summary>
        /// [ADICIONADO] Retorna verdadeiro se o jogador está pressionando o botão de pulo.
        /// </summary>
        public abstract bool IsJumping();

        #endregion

        #region ANIMATION

        /// <summary>
        /// Ejects a casing from the equipped weapon.
        /// </summary>
        public abstract void EjectCasing();
        /// <summary>
        /// Fills the character's equipped weapon's ammunition by a certain amount, or fully if set to -1.
        /// </summary>
        public abstract void FillAmmunition(int amount);

        /// <summary>
        /// Sets the equipped weapon's magazine to be active or inactive!
        /// </summary>
        public abstract void SetActiveMagazine(int active);

        /// <summary>
        /// Reload Animation Ended.
        /// </summary>
        public abstract void AnimationEndedReload();

        /// <summary>
        /// Inspect Animation Ended.
        /// </summary>
        public abstract void AnimationEndedInspect();
        /// <summary>
        /// Holster Animation Ended.
        /// </summary>
        public abstract void AnimationEndedHolster();

        #endregion
    }
}