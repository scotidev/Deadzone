using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Aula prática de melee em FPS (sem animação), usando princípios básicos de física:
    /// 1) Captura de input (tecla F);
    /// 2) Janela temporal (cooldown) para evitar spam;
    /// 3) Volume de acerto no espaço 3D (OverlapBox);
    /// 4) Aplicação de dano no alvo válido;
    /// 5) Feedback visual temporário (cubo fino) para depuração em runtime.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour {
        [Header("Melee")]

        [Tooltip("Dano causado em cada golpe melee.")]
        [SerializeField]
        private float meleeDamage = 30.0f;

        [Tooltip("Distância à frente da câmera usada como centro do golpe.")]
        [SerializeField]
        private float meleeRange = 1.4f;

        [Tooltip("Metade do tamanho da caixa de acerto (X,Y,Z).")]
        [SerializeField]
        private Vector3 meleeHalfExtents = new Vector3(0.22f, 0.22f, 0.7f);

        [Tooltip("Tempo mínimo entre golpes. Evita ataque por frame.")]
        [SerializeField]
        private float meleeCooldown = 0.35f;

        [Tooltip("Tempo que o retângulo visual fica ativo ao atacar.")]
        [SerializeField]
        private float meleeVisualDuration = 0.1f;

        [Tooltip("Posição local do retângulo visual em relação à câmera.")]
        [SerializeField]
        private Vector3 visualLocalPosition = new Vector3(0.18f, -0.2f, 0.7f);

        [Tooltip("Rotação local do retângulo visual em relação à câmera.")]
        [SerializeField]
        private Vector3 visualLocalEuler = new Vector3(0.0f, 0.0f, -20.0f);

        [Tooltip("Escala local do retângulo visual (fino e comprido).")]
        [SerializeField]
        private Vector3 visualLocalScale = new Vector3(0.08f, 0.16f, 0.8f);

        // Dependências centrais do jogador.
        private CharacterBehaviour playerCharacter;
        private CapsuleCollider playerCapsule;

        // Guarda o instante do último ataque aceito.
        private float lastMeleeTime = -10.0f;

        // Objeto visual temporário do ataque (apenas feedback).
        private GameObject meleeVisual;

        // Buffer reaproveitado para evitar alocação de memória a cada ataque.
        private readonly Collider[] meleeHits = new Collider[16];

        private void Awake() {
            // First principles: antes de atacar, precisamos saber "quem é o player" e qual collider ignorar.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
            playerCapsule = playerCharacter.GetComponent<CapsuleCollider>();
        }

        private void Start() {
            // Cria o retângulo visual de debug em runtime (não precisa prefab).
            SetupMeleeVisual();
        }

        private void Update() {
            // 1) Sem dispositivo de teclado, não há input.
            if (Keyboard.current == null)
                return;

            // 2) Se cursor está livre/menu aberto, não atacar (consistência com restante do game loop).
            if (!playerCharacter.IsCursorLocked() || playerCharacter.IsInterfaceMode())
                return;

            // 3) Dispara apenas no frame da transição "não pressionado -> pressionado".
            if (Keyboard.current.fKey.wasPressedThisFrame)
                TryMeleeAttack();
        }

        private void TryMeleeAttack() {
            // Princípio temporal: limitar taxa de eventos para previsibilidade de gameplay.
            if (Time.time - lastMeleeTime < meleeCooldown)
                return;

            lastMeleeTime = Time.time;

            // Feedback visual do golpe (curto e temporário).
            if (meleeVisual != null)
                StartCoroutine(ShowMeleeVisualRoutine());

            Camera cameraWorld = playerCharacter.GetCameraWorld();
            if (cameraWorld == null)
                return;

            // Geometria do ataque:
            // - centro = posição da câmera + frente * alcance
            // - orientação = rotação atual da câmera
            // Isso faz a caixa de acerto acompanhar exatamente a mira do jogador.
            Vector3 center = cameraWorld.transform.position + cameraWorld.transform.forward * meleeRange;
            Quaternion orientation = cameraWorld.transform.rotation;

            // OverlapBox = consulta de interseção de volume.
            // Em vez de raycast (linha), usamos volume para simular "golpe corpo a corpo".
            int hits = Physics.OverlapBoxNonAlloc(
                center,
                meleeHalfExtents,
                meleeHits,
                orientation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            // Evita dano duplicado no mesmo inimigo quando ele possui múltiplos colliders.
            var damagedEnemies = new HashSet<EnemyBase>();

            for (int i = 0; i < hits; i++) {
                Collider hitCollider = meleeHits[i];

                // Ignora entradas inválidas e o próprio jogador.
                if (hitCollider == null || hitCollider == playerCapsule)
                    continue;

                // Resolve alvo real no hierarquia (importante quando collider está em child/hitbox).
                EnemyBase enemy = hitCollider.GetComponentInParent<EnemyBase>();
                if (enemy == null)
                    continue;

                if (!damagedEnemies.Add(enemy))
                    continue;

                // Aplicação de efeito de gameplay: dano.
                enemy.TakeDamage(meleeDamage);

                // Feedback de depuração para validar acerto durante desenvolvimento.
                Debug.Log($"[MELEE] Acertou inimigo: {enemy.name}");
                Debug.DrawLine(cameraWorld.transform.position, enemy.transform.position, Color.red, 0.25f);
            }

            // Informação explícita no console quando o golpe não conectou.
            if (damagedEnemies.Count == 0)
                Debug.Log("[MELEE] Ataque sem acerto.");

            // Limpeza do buffer usado nesta execução.
            for (int i = 0; i < hits; i++)
                meleeHits[i] = null;
        }

        private IEnumerator ShowMeleeVisualRoutine() {
            // Liga no início do golpe...
            meleeVisual.SetActive(true);
            // ...espera um curto intervalo...
            yield return new WaitForSeconds(meleeVisualDuration);
            // ...e desliga para manter o retângulo visível apenas durante o ataque.
            if (meleeVisual != null)
                meleeVisual.SetActive(false);
        }

        private void SetupMeleeVisual() {
            Camera cameraWorld = playerCharacter.GetCameraWorld();
            if (cameraWorld == null)
                return;

            // Cria um cubo padrão e transforma em "lâmina" fina pela escala.
            meleeVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meleeVisual.name = "MeleeRect";
            meleeVisual.transform.SetParent(cameraWorld.transform, false);
            meleeVisual.transform.localPosition = visualLocalPosition;
            meleeVisual.transform.localRotation = Quaternion.Euler(visualLocalEuler);
            meleeVisual.transform.localScale = visualLocalScale;

            // O cubo é só visual; não deve interagir fisicamente.
            Collider visualCollider = meleeVisual.GetComponent<Collider>();
            if (visualCollider != null)
                visualCollider.enabled = false;

            // Estado inicial: oculto.
            meleeVisual.SetActive(false);
        }
    }
}
