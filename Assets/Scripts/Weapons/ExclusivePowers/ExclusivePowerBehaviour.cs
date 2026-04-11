// Copyright 2021, Infima Games. All Rights Reserved.

using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Base class for exclusive weapon powers that unlock at level 10.
    /// Each weapon can have a unique special ability that enhances its effectiveness.
    /// Examples: infinite ammo, explosive shots, bullet storm, etc.
    /// </summary>
    /// CONCEITO PEDAGÓGICO: Classes Abstratas e Herança
    /// Uma classe ABSTRATA é um template que não pode ser instanciado diretamente
    /// Ela serve como base para outras classes (subclasses) que HERDAM dela
    /// 
    /// ANALOGIA: "Veículo" é abstrato (não existe um "veículo" genérico)
    ///           Mas "Carro", "Moto", "Avião" herdam de Veículo e podem existir
    /// 
    /// BENEFÍCIOS:
    /// 1. Reutilização de código: Código comum fica na classe base
    /// 2. Polimorfismo: Podemos tratar todos os poderes como ExclusivePowerBehaviour
    /// 3. Estrutura: Força subclasses a implementar métodos obrigatórios (abstract)
    /// 
    /// NESTE CASO: Cada arma tem um poder diferente (InfiniteAmmoPower, ExplosiveShellsPower, etc)
    ///             Mas todos compartilham a mesma estrutura (ativar, desativar, efeitos)
    public abstract class ExclusivePowerBehaviour : MonoBehaviour
    {
        #region FIELDS

        /// <summary>
        /// The weapon this power is attached to.
        /// </summary>
        /// CONCEITO: protected = Visível nesta classe E nas subclasses (mas não fora)
        /// Permite que subclasses acessem o weapon sem tornar público
        protected WeaponBehaviour weapon;

        /// <summary>
        /// Whether this exclusive power is currently active.
        /// </summary>
        /// CONCEITO: Flag booleana para controlar estado
        /// Previne ativar duas vezes ou desativar algo já desativado
        protected bool isActive = false;

        /// <summary>
        /// Particle effect to play when power activates (optional).
        /// </summary>
        [SerializeField]
        protected ParticleSystem activationEffect;

        /// <summary>
        /// Sound effect to play when power activates (optional).
        /// </summary>
        [SerializeField]
        protected AudioClip activationSound;

        /// <summary>
        /// Unified audio service reference used to centralize playback.
        /// </summary>
        protected IAudioManagerService audioManagerService;

        #endregion

        #region UNITY

        /// <summary>
        /// Cache the weapon component on awake.
        /// </summary>
        protected virtual void Awake() {
            weapon = GetComponent<WeaponBehaviour>();
            if (weapon == null) {
                Debug.LogError($"[ExclusivePowerBehaviour] No WeaponBehaviour found on {gameObject.name}!");
            }

            // First principle: resolve shared services once and reuse them to avoid repeated lookups at runtime.
            audioManagerService = ServiceLocator.Current.Get<IAudioManagerService>();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Activates the exclusive power. Called when weapon reaches level 10.
        /// </summary>
        public virtual void ActivatePower() {
            if (isActive) {
                Debug.LogWarning($"[ExclusivePowerBehaviour] Power already active on {gameObject.name}");
                return;
            }

            isActive = true;
            
            // Play visual effect
            if (activationEffect != null) {
                activationEffect.Play();
            }

            // Play sound effect
            if (activationSound != null && weapon != null) {
                // First principle: world-origin sounds should be spatial so the listener perceives where they come from.
                audioManagerService?.PlaySFX3DAttached(activationSound, weapon.transform, 1f, 1f, 25f);
            }

            Debug.Log($"[ExclusivePowerBehaviour] Exclusive power activated on {gameObject.name}!");
            OnPowerActivated();
        }

        /// <summary>
        /// Deactivates the exclusive power. Can be called if weapon level drops below 10 (unlikely).
        /// </summary>
        public virtual void DeactivatePower() {
            if (!isActive) return;

            isActive = false;
            OnPowerDeactivated();
            Debug.Log($"[ExclusivePowerBehaviour] Exclusive power deactivated on {gameObject.name}");
        }

        /// <summary>
        /// Returns whether this power is currently active.
        /// </summary>
        public bool IsActive() => isActive;

        /// <summary>
        /// Override this method to implement the specific power's activation logic.
        /// Called once when the power is activated.
        /// </summary>
        /// CONCEITO PEDAGÓGICO: Métodos Abstratos
        /// 'abstract' significa que este método NÃO tem implementação na classe base
        /// Subclasses SÃO OBRIGADAS a implementar este método (ou não compilam)
        /// 
        /// ANALOGIA: "Todo veículo deve ter um método acelerar(), mas cada um implementa diferente"
        ///           Carro acelera pisando no acelerador, Avião aumenta potência dos motores
        /// 
        /// NESTE CASO: Cada poder tem lógica diferente ao ativar:
        /// - InfiniteAmmoPower: Munição infinita
        /// - ExplosiveShellsPower: Projéteis explodem
        /// - BulletStormPower: Dobra cadência de tiro
        /// 
        /// Mas TODOS devem implementar OnPowerActivated() - é obrigatório!
        protected abstract void OnPowerActivated();

        /// <summary>
        /// Override this method to implement the specific power's deactivation logic.
        /// Called once when the power is deactivated.
        /// </summary>
        /// CONCEITO: Método abstrato para cleanup
        /// Quando o poder é desativado, cada subclasse deve desfazer suas modificações
        /// Ex: BulletStormPower precisa restaurar a cadência de tiro original
        protected abstract void OnPowerDeactivated();

        /// <summary>
        /// Override this method if the power needs continuous update logic while active.
        /// Example: draining special resource, applying periodic effects, etc.
        /// </summary>
        protected virtual void UpdatePower() {
            // Optional: Override in derived classes if needed
        }

        #endregion

        #region UNITY

        /// <summary>
        /// Update is called once per frame. Calls UpdatePower() if power is active.
        /// </summary>
        protected virtual void Update() {
            if (isActive) {
                UpdatePower();
            }
        }

        #endregion
    }
}
