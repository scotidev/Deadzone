using System.Collections.Generic;
using InfimaGames.LowPolyShooterPack;
using UnityEngine;

namespace Deadzone.UI {

    /// <summary>
    /// Place this on a GameObject with a Collider (isTrigger = true).
    /// When the player enters the trigger, the assigned TutorialStepSO is queued.
    /// Useful for creating a tutorial phase with colliders placed before obstacles.
    /// </summary>
    public class TutorialTriggerZone : MonoBehaviour {

        #region SERIALIZED FIELDS

        [Header("References")]
        [Tooltip("Tutorial step to show when the player enters this trigger.")]
        [SerializeField] private TutorialStepSO tutorialStep;

        [Tooltip("Objects (like zombies) to activate when the player enters this trigger.")]
        [SerializeField] private List<GameObject> objectsToActivate;

        [Header("Settings")]
        [Tooltip("If true, the trigger deactivates after the first entry.")]
        [SerializeField] private bool triggerOnce = true;

        #endregion

        #region UNITY

        /// <summary>
        /// Chamado quando outro colisor entra no gatilho.
        /// Verifica se é o jogador e coloca o tutorial na fila.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            // Verifica se o objeto que entrou no trigger é o jogador (CharacterBehaviour)
            // O uso de GetComponentInParent garante que pegamos o script no objeto principal do jogador
            if (other.GetComponentInParent<CharacterBehaviour>() == null)
                return;

            // Ativa os componentes de comportamento nos objetos da lista (zumbis parados)
            if (objectsToActivate != null) {
                foreach (GameObject obj in objectsToActivate) {
                    if (obj == null) continue;

                    // Ativa o movimento
                    var follow = obj.GetComponent<EnemyFollow>();
                    if (follow != null) follow.enabled = true;

                    // Ativa o ataque
                    var attack = obj.GetComponent<EnemyAttack>();
                    if (attack != null) attack.enabled = true;
                }
            }

            // Se houver um tutorial configurado, envia ele para o Manager processar
            // O TutorialManager é um Singleton, então usamos .Instance para acessá-lo
            if (tutorialStep != null) {
                TutorialManager.Instance?.QueueTutorial(tutorialStep);
            }

            // Desativa este objeto para garantir que o tutorial não dispare múltiplas vezes
            if (triggerOnce)
                gameObject.SetActive(false);
        }

        #endregion

    }

}
