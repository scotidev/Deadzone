// Copyright 2021, Infima Games. All Rights Reserved.

using System.Linq;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack {
    /// <summary>
    /// Controla a locomoção do personagem usando Rigidbody.
    /// 
    /// Nesta versão, além da movimentação base, foram adicionados:
    /// - leitura robusta de chão (ground probe);
    /// - tratamento de inclinação (slope) para reduzir deslizamento;
    /// - subida de degrau (stair stepping) sem transformar escada em rampa contínua.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class Movement : MovementBehaviour {
        #region FIELDS SERIALIZED

        [Header("Audio Clips")]

        [Tooltip("The audio clip that is played while walking.")]
        [SerializeField]
        private AudioClip audioClipWalking;

        [Tooltip("The audio clip that is played while running.")]
        [SerializeField]
        private AudioClip audioClipRunning;

        [Header("Speeds")]

        [SerializeField]
        private float speedWalking = 5.0f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float speedRunning = 9.0f;

        [Tooltip("How fast the player moves while aiming."), SerializeField]
        private float speedAiming = 3.0f;

        [Header("Crouch")]

        [Tooltip("How fast the player moves while crouching."), SerializeField]
        // ===== AULA: VELOCIDADE DE CROUCH =====
        // Velocidade de movimento enquanto agachado
        // 
        // COMPARAÇÃO DE VALORES:
        // - speedWalking = 5.0f (velocidade normal)
        // - speedRunning = 9.0f (velocidade correndo)
        // - speedCrouching = 1.8f (MUITO mais lento - aproximadamente 36% da velocidade normal)
        //
        // POR QUE TÃO LENTO?
        // 1. Realismo: Agachar e se mover é difícil na vida real
        // 2. Gameplay: Equilibra a vantagem de ser menos visível com desvantagem de velocidade
        // 3. Stealth: Movimento lento = mais silencioso = furtivo
        // 
        // O QUE É UM FLOAT?
        // float = "floating point number" = número com vírgula (exemplo: 1.8, 2.5, 10.0)
        // Em C#, sempre usamos ponto (.) ao invés de vírgula (,) para decimais
        // O "f" no final (1.8f) diz ao C# que é um float e não um double (outro tipo de número)
        private float speedCrouching = 1.8f;

        [Tooltip("Height multiplier for capsule collider when crouching (0-1)."), SerializeField]
        // Multiplicador de altura da cápsula quando agachado (0.5 = metade da altura)
        // Princípio: reduzir collider permite passar sob obstáculos baixos
        private float crouchHeightMultiplier = 0.5f;

        [Tooltip("Camera height offset when crouching (negative to lower)."), SerializeField]
        // Offset vertical da câmera quando agachado (negativo = abaixa a câmera)
        // Princípio: a câmera deve seguir a altura da "cabeça" do personagem para manter imersão
        // Valor ajustado: -0.25 é mais sutil que -0.5 (estava abaixando demais)
        private float crouchCameraOffset = -0.25f;

        [Tooltip("How fast the crouch transition happens."), SerializeField]
        // Velocidade da transição de agachar/levantar (mais alto = transição mais rápida)
        // Princípio: interpolação suave evita mudanças bruscas que causam motion sickness
        private float crouchTransitionSpeed = 8.0f;

        [Header("Jump")]
        [Tooltip("Jump Strength, values between 4 and 8 are recommended.")]
        [SerializeField]
        private float jumpForce = 5.0f;

        [Header("Grounding")]

        [Tooltip("Extra distance used to probe the ground below the capsule.")]
        [SerializeField]
        // Distância extra para "enxergar" o chão logo abaixo da cápsula.
        // Princípio: ray/sphere cast detecta contato antes de perder o grounded por pequenas irregularidades.
        private float groundProbeDistance = 0.2f;

        [Tooltip("Maximum walkable slope angle.")]
        [SerializeField]
        // Ângulo máximo que consideramos caminhável. Acima disso, não tratamos como chão estável.
        // Princípio: normal da superfície define se é "chão" ou "parede" para locomoção.
        private float maxGroundAngle = 60.0f;

        [Tooltip("Keeps the body glued to the ground while grounded.")]
        [SerializeField]
        // Força para manter o corpo aderido ao chão quando grounded.
        // Princípio: evita micro-quiques em descidas e transições de malha.
        private float groundStickForce = 25.0f;

        [Tooltip("Extra damping applied when idle on slopes to prevent sliding.")]
        [SerializeField]
        // Amortecimento quando parado em ladeira.
        // Princípio: remove energia horizontal residual que gera escorregamento.
        private float slopeIdleDamping = 8.0f;

        [Tooltip("How strongly downhill gravity is canceled on walkable slopes.")]
        [SerializeField]
        // Compensa a componente da gravidade ao longo do plano inclinado.
        // Princípio: gravidade pode ser decomposta em "normal" + "tangencial"; a tangencial causa slide.
        private float slopeAntiSlide = 1.0f;

        [Header("Stairs")]

        [Tooltip("Enable stair stepping when colliding with small ledges.")]
        [SerializeField]
        // Liga/desliga a lógica de step-up (subir degrau).
        private bool stairStepping = true;

        [Tooltip("Maximum step height that can be climbed.")]
        [SerializeField]
        // Equivalente conceitual ao step offset: altura máxima de degrau que pode ser vencida.
        private float maxStepHeight = 0.35f;

        [Tooltip("Forward distance used to detect step obstacles.")]
        [SerializeField]
        // Distância de checagem frontal para detectar o "espelho" do degrau.
        private float stepCheckDistance = 0.35f;

        [Tooltip("How smoothly the body is moved up while climbing steps.")]
        [SerializeField]
        // Limite de elevação por frame de física para suavizar a subida (menos tranco/motion sickness).
        private float stepSmooth = 0.12f;

        #endregion

        #region PROPERTIES

        //Velocity.
        private Vector3 Velocity {
            //Getter.
            get => rigidBody.linearVelocity;
            //Setter.
            set => rigidBody.linearVelocity = value;
        }

        #endregion

        #region FIELDS

        /// <summary>
        /// Attached Rigidbody.
        /// </summary>
        private Rigidbody rigidBody;
        /// <summary>
        /// Attached CapsuleCollider.
        /// </summary>
        private CapsuleCollider capsule;
        /// <summary>
        /// Attached AudioSource.
        /// </summary>
        private AudioSource audioSource;

        /// <summary>
        /// True if the character is currently grounded.
        /// </summary>
        private bool grounded;

        /// <summary>
        /// Current ground normal.
        /// </summary>
        private Vector3 groundNormal = Vector3.up;

        /// <summary>
        /// Player Character.
        /// </summary>
        private CharacterBehaviour playerCharacter;
        /// <summary>
        /// The player character's equipped weapon.
        /// </summary>
        private WeaponBehaviour equippedWeapon;

        /// <summary>
        /// Array of RaycastHits used for ground checking.
        /// </summary>
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        /// <summary>
        /// Moment (in seconds) since the last jump. This is used to calculate the cooldown between jumps, preventing jump spamming.
        /// </summary>
        private float lastJumpTime = -1f;

        /// <summary>
        /// Original height of the capsule collider (stored on Start to restore when standing up).
        /// Armazena a altura original da cápsula para poder restaurar ao levantar.
        /// </summary>
        private float originalCapsuleHeight;

        /// <summary>
        /// Original center Y position of the capsule collider.
        /// Armazena o centro Y original da cápsula (usado para ajustar quando agachar).
        /// </summary>
        private float originalCapsuleCenterY;

        /// <summary>
        /// Current target height for the capsule (smoothly interpolated).
        /// Altura alvo atual da cápsula (interpolada suavemente durante transição).
        /// </summary>
        private float targetCapsuleHeight;

        /// <summary>
        /// Current target center Y for the capsule (smoothly interpolated).
        /// Centro Y alvo atual da cápsula (interpolado suavemente durante transição).
        /// </summary>
        private float targetCapsuleCenterY;

        // ===== AULA: TRANSFORM =====
        // Transform é um componente que TODOS os GameObjects no Unity têm
        // Ele guarda 3 informações essenciais:
        // 1. Position (posição X, Y, Z no mundo 3D)
        // 2. Rotation (rotação em graus)
        // 3. Scale (tamanho/escala)
        //
        // Quando escrevemos "private Transform cameraTransform", estamos dizendo:
        // "Crie uma variável chamada cameraTransform que vai guardar uma REFERÊNCIA a um Transform"
        // Referência = ponteiro = endereço de memória apontando para um objeto específico
        //
        // POR QUE PRIVATE?
        // private = só este script pode acessar esta variável
        // public = outros scripts podem acessar
        // [SerializeField] = private mas aparece no Inspector do Unity
        private Transform cameraTransform;

        // ===== AULA: FLOAT E POSIÇÃO Y =====
        // originalCameraY guarda a posição Y ORIGINAL da câmera quando o jogo inicia
        // Por que guardar? Porque precisamos voltar a este valor quando o player levanta!
        //
        // SISTEMA DE COORDENADAS NO UNITY:
        // X = horizontal (esquerda/direita)
        // Y = vertical (cima/baixo) <- É o que nos interessa para agachar!
        // Z = profundidade (frente/trás)
        //
        // EXEMPLO PRÁTICO:
        // Se a câmera começa em Y=0 (em relação ao Root)
        // originalCameraY = 0.0f
        // Quando agachar, não mexemos mais na câmera (ela segue o Root)
        private float originalCameraY;

        // ===== AULA: TARGET (ALVO) E INTERPOLAÇÃO =====
        // targetCameraY é o valor ALVO para onde queremos que a câmera vá
        // 
        // CONCEITO DE INTERPOLAÇÃO:
        // Não movemos a câmera instantaneamente (isso causa "jerk" = movimento brusco)
        // Ao invés disso, movemos GRADUALMENTE de "posição atual" para "target"
        // É como um carro acelerando suavemente ao invés de dar um tranco
        //
        // EXEMPLO:
        // Frame 1: Camera Y=0.0, Target=-0.5, movemos um pouquinho -> Camera Y=-0.02
        // Frame 2: Camera Y=-0.02, Target=-0.5, movemos mais um pouco -> Camera Y=-0.04
        // Frame 3: Camera Y=-0.04, Target=-0.5, continua... -> Camera Y=-0.07
        // ... até chegar em -0.5 (transição suave!)
        private float targetCameraY;

        // ===== AULA: A SOLUÇÃO DO BUG - ROOT TRANSFORM =====
        // PROBLEMA ANTERIOR:
        // Tentávamos mover o "Armature" (os ossos do personagem)
        // MAS o Animator resetava a posição dele todo frame!
        // Era como tentar empurrar uma porta enquanto alguém do outro lado segura
        //
        // SOLUÇÃO NOVA:
        // Movemos o "SK_FP_CH_Default_Root" - o GameObject PAI que CONTÉM o Animator
        // É como pegar a sala inteira e mover, ao invés de tentar mover uma pessoa dentro da sala
        //
        // HIERARQUIA (de pai para filho):
        // Player (raiz, Y=0 no mundo)
        //   └─ SK_FP_CH_Default_Root (Y=1.8 local) <- MOVEMOS ESTE!
        //       ├─ Animator (controla animações)
        //       ├─ Camera (Y=0 local, SEGUE o Root automaticamente)
        //       └─ Armature (Y=0 local, Animator controla internamente)
        //
        // VANTAGEM:
        // O Animator continua funcionando DENTRO do Root normalmente
        // A câmera desce automaticamente porque é FILHA do Root
        // Não há conflito - todos são felizes! 🎉
        private Transform characterRootTransform;

        /// <summary>
        /// Original local Y position of the character root (SK_FP_CH_Default_Root).
        /// Posição Y local original do Root do personagem (normalmente 1.8).
        /// </summary>
        private float originalRootY;

        /// <summary>
        /// Current target Y position for the character root (smoothly interpolated).
        /// Posição Y alvo atual do Root do personagem (interpolada suavemente).
        /// </summary>
        private float targetRootY;

        /// <summary>
        /// Debug: armazena o último estado de crouch para detectar mudanças.
        /// </summary>
        private bool lastCrouchState = false;

        #endregion

        #region UNITY FUNCTIONS

        /// <summary>
        /// Awake.
        /// </summary>
        protected override void Awake() {
            //Get Player Character.
            playerCharacter = ServiceLocator.Current.Get<IGameModeService>().GetPlayerCharacter();
        }

        /// Initializes the FpsController on start.
        protected override void Start() {
            //Rigidbody Setup.
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            //Cache the CapsuleCollider.
            capsule = GetComponent<CapsuleCollider>();

            //Audio Source Setup.
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = audioClipWalking;
            audioSource.loop = true;

            // Crouch Setup - Inicializa as variáveis de agachamento
            // Armazena a altura original da cápsula para poder restaurar depois
            originalCapsuleHeight = capsule.height;
            // Armazena o centro Y original da cápsula
            originalCapsuleCenterY = capsule.center.y;
            // Inicializa os valores alvo com os originais (começa em pé)
            targetCapsuleHeight = originalCapsuleHeight;
            targetCapsuleCenterY = originalCapsuleCenterY;

            // Busca a câmera do player para ajustar sua altura
            // GetCameraWorld() retorna a câmera principal do personagem
            cameraTransform = playerCharacter.GetCameraWorld().transform;
            // Armazena a posição Y local original da câmera
            originalCameraY = cameraTransform.localPosition.y;
            // Inicializa o alvo da câmera com a posição original
            targetCameraY = originalCameraY;

            // ===== AULA: BUSCAR OBJETOS NA HIERARQUIA =====
            // transform.Find("nome") procura um GameObject FILHO direto deste objeto
            // É como procurar uma pasta dentro de outra pasta
            //
            // IMPORTANTE:
            // "transform" (minúsculo) = a Transform do GameObject onde este script está
            // Neste caso, o script está no "Player", então:
            // transform = Transform do Player
            // transform.Find("SK_FP_CH_Default_Root") = procura filho chamado "SK_FP_CH_Default_Root"
            //
            // RETORNO:
            // Se encontrar: retorna a Transform do objeto
            // Se NÃO encontrar: retorna null (nulo/vazio)
            //
            // POR QUE GUARDAR NUMA VARIÁVEL?
            // Se buscarmos todo frame com Find(), é LENTO (Unity percorre todos os filhos)
            // Buscamos UMA VEZ no Start() e guardamos em "characterRootTransform"
            // Depois, usamos a variável sempre que precisar (RÁPIDO!)
            characterRootTransform = transform.Find("SK_FP_CH_Default_Root");

            // ===== AULA: DEBUG.LOG =====
            // Debug.Log() imprime mensagens no Console do Unity
            // Serve para verificar se o código está funcionando
            // É como fazer "print" em Python ou "console.log" em JavaScript
            //
            // O $ ANTES DAS ASPAS:
            // Em C#, $ antes de "" significa "string interpolation"
            // Permite colocar variáveis dentro do texto usando {variavel}
            //
            // EXEMPLO:
            // string nome = "João";
            // Debug.Log($"Olá {nome}!"); // Imprime: Olá João!
            //
            // OPERADOR != (diferente de):
            // characterRootTransform != null significa "é diferente de nulo?"
            // Se encontrou o objeto: != null é true (verdadeiro)
            // Se NÃO encontrou: != null é false (falso)
            // ===== AULA: IF (CONDICIONAL) =====
            // if = "se" em português
            // Executa o código dentro das chaves {} APENAS SE a condição for verdadeira
            //
            // if (characterRootTransform != null) significa:
            // "Se characterRootTransform for diferente de null (ou seja, se encontramos o objeto)"
            //
            // ESTRUTURA:
            // if (condição) {
            //     código executado se condição = true
            // }
            // else {
            //     código executado se condição = false
            // }
            if (characterRootTransform != null) {
                // ===== AULA: LOCALPOSITION =====
                // Todo Transform tem duas posições:
                // 1. position = posição absoluta no MUNDO (World Space)
                // 2. localPosition = posição relativa ao PAI (Local Space)
                //
                // EXEMPLO PRÁTICO:
                // Imagine uma casa (Pai) na coordenada X=100 no mundo
                // Dentro da casa tem uma mesa (Filho) a 5 metros da parede (localPosition.x = 5)
                // A posição da mesa NO MUNDO seria: position.x = 100 + 5 = 105
                //
                // NESTE CASO:
                // Player está em Y=0 no mundo
                // SK_FP_CH_Default_Root tem localPosition.y = 1.8
                // Posição do Root no mundo = 0 + 1.8 = 1.8 (altura dos olhos do personagem)
                //
                // .y = acessa APENAS a componente Y (vertical)
                // Pegamos só o Y porque só nos interessa altura (não queremos X ou Z)
                originalRootY = characterRootTransform.localPosition.y;

                // Inicializa o target (alvo) com o valor original
                // No início do jogo, estamos em pé, então target = original
                targetRootY = originalRootY;

            }
            else {
                // ===== AULA: DEBUG.LOGERROR =====
                // Debug.LogError() é como Debug.Log() mas aparece em VERMELHO no Console
                // Usado para indicar ERROS que precisam de atenção
                // Se chegamos aqui, algo está errado na hierarquia do Player!
            }

        }

        protected override void FixedUpdate() {
            // 1) Primeiro detectamos chão e normal.
            // Princípio: todas as decisões de slope/escada dependem de saber se estamos grounded.
            ProbeGround();

            // 2) Depois aplicamos velocidade de movimento.
            MoveCharacter();

            // 3) Por fim, estabilizamos em chão inclinado.
            if (grounded && Velocity.y <= 0.0f) {
                // Cola no chão para reduzir "saltinhos" ao descer/andar em superfícies irregulares.
                rigidBody.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);

                // Remove a gravidade tangencial da ladeira para minimizar escorregamento parado.
                Vector3 slopeGravity = Vector3.ProjectOnPlane(Physics.gravity, groundNormal);
                rigidBody.AddForce(-slopeGravity * slopeAntiSlide, ForceMode.Acceleration);
            }
        }

        /// Moves the camera to the character, processes jumping and plays sounds every frame.
        protected override void Update() {
            //Get the equipped weapon!
            equippedWeapon = playerCharacter.GetInventory().GetEquipped();

            //Process Crouch - Processa a transição de agachamento
            ProcessCrouch();

            //Play Sounds!
            PlayFootstepSounds();
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Faz a leitura do chão usando SphereCast e define:
        /// - se o personagem está grounded;
        /// - qual a normal da superfície de apoio.
        /// 
        /// Regra: só considera superfície com inclinação até <see cref="maxGroundAngle"/>.
        /// </summary>
        private void ProbeGround() {
            Bounds bounds = capsule.bounds;
            Vector3 extents = bounds.extents;
            float radius = Mathf.Max(0.01f, extents.x - 0.02f);
            float castDistance = extents.y - radius + groundProbeDistance;

            Physics.SphereCastNonAlloc(
                bounds.center,
                radius,
                Vector3.down,
                groundHits,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            grounded = false;
            groundNormal = Vector3.up;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < groundHits.Length; i++) {
                RaycastHit hit = groundHits[i];

                if (hit.collider == null || hit.collider == capsule)
                    continue;

                // Usamos a normal para calcular o ângulo da superfície.
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle > maxGroundAngle)
                    continue;

                // Escolhe o hit válido mais próximo para obter uma normal estável do chão.
                if (hit.distance < bestDistance) {
                    bestDistance = hit.distance;
                    groundNormal = hit.normal;
                    grounded = true;
                }
            }

            // Limpeza do buffer non-alloc para não reaproveitar lixo de frame anterior.
            for (int i = 0; i < groundHits.Length; i++)
                groundHits[i] = new RaycastHit();
        }

        private void MoveCharacter() {
            #region Calculate Movement Velocity

            //Get Movement Input!
            Vector2 frameInput = playerCharacter.GetInputMovement();
            //Calculate local-space direction by using the player's input.
            var movement = new Vector3(frameInput.x, 0.0f, frameInput.y);

            //Speed calculation based on character state.
            // Prioridade: Aiming > Crouching > Running > Walking
            // Princípio: cada estado de movimento tem sua velocidade característica
            if (playerCharacter.IsAiming()) {
                //Multiply by the aiming speed (reduced movement).
                // Velocidade reduzida ao mirar para dar mais precisão
                movement *= speedAiming;
            }
            else if (playerCharacter.IsCrouching()) {
                //Multiply by the crouching speed (very slow movement).
                // Velocidade ainda mais reduzida ao agachar (movimento furtivo e cauteloso)
                movement *= speedCrouching;
            }
            else if (playerCharacter.IsRunning()) {
                //Multiply by the running speed.
                // Velocidade máxima ao correr
                movement *= speedRunning;
            }
            else {
                //Multiply by the normal walking speed.
                movement *= speedWalking;
            }

            //World space velocity calculation. This allows us to add it to the rigidbody's velocity properly.
            movement = transform.TransformDirection(movement);

            #endregion

            // Quando grounded, projetamos o movimento no plano do chão.
            // Princípio: evita ganhar/perder velocidade indevida em superfícies inclinadas.
            Vector3 desiredMovement = grounded ? Vector3.ProjectOnPlane(movement, groundNormal) : movement;

            // Tentativa de step-up somente quando há input e estamos no chão.
            if (stairStepping && grounded && frameInput.sqrMagnitude > 0.001f && Velocity.y <= 0.1f)
                TryStepUp(desiredMovement);

            Velocity = new Vector3(desiredMovement.x, Velocity.y, desiredMovement.z);

            // Anti-slide quando parado na ladeira.
            if (grounded && frameInput.sqrMagnitude <= 0.0001f && !playerCharacter.IsJumping()) {
                Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, groundNormal);
                Velocity -= planarVelocity * Mathf.Clamp01(Time.fixedDeltaTime * slopeIdleDamping);

                // Se já está quase parado no plano, zera completamente XZ para estabilizar.
                if (Vector3.ProjectOnPlane(Velocity, groundNormal).sqrMagnitude < 0.01f)
                    Velocity = new Vector3(0.0f, Mathf.Min(Velocity.y, 0.0f), 0.0f);
                else
                    Velocity = new Vector3(Velocity.x, Mathf.Min(Velocity.y, 0.0f), Velocity.z);
            }

            if (grounded && playerCharacter.IsJumping() && Time.time - lastJumpTime >= 0.5f) {
                lastJumpTime = Time.time;
                rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Tenta subir um degrau à frente do personagem.
        /// 
        /// Estratégia básica:
        /// 1) Ray baixo encontra obstáculo frontal (espelho do degrau);
        /// 2) Ray alto confirma espaço livre para passar;
        /// 3) Ray para baixo encontra topo do degrau;
        /// 4) Se altura e ângulo forem válidos, aplica elevação suave.
        /// </summary>
        /// <param name="desiredMovement">Direção de movimento desejada no mundo.</param>
        private void TryStepUp(Vector3 desiredMovement) {
            Vector3 moveDirection = new Vector3(desiredMovement.x, 0.0f, desiredMovement.z);
            if (moveDirection.sqrMagnitude <= 0.0001f)
                return;

            moveDirection.Normalize();

            Bounds bounds = capsule.bounds;
            Vector3 feet = new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);

            // Ray baixo: detecta barreira frontal na altura do pé.
            if (!Physics.Raycast(feet, moveDirection, out RaycastHit lowerHit, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            // Se for o próprio collider ou já for uma normal muito "de chão", não tratamos como degrau.
            if (lowerHit.collider == capsule || lowerHit.normal.y > 0.1f)
                return;

            // Ray alto: precisa estar livre para caber a cápsula ao subir.
            Vector3 upperOrigin = feet + Vector3.up * maxStepHeight;
            if (Physics.Raycast(upperOrigin, moveDirection, stepCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            // Ray para baixo após o obstáculo: encontra o topo real do degrau.
            Vector3 stepProbeOrigin = upperOrigin + moveDirection * stepCheckDistance;
            if (!Physics.Raycast(stepProbeOrigin, Vector3.down, out RaycastHit stepHit, maxStepHeight + 0.2f, ~0,
                    QueryTriggerInteraction.Ignore))
                return;

            if (stepHit.collider == capsule)
                return;

            float stepAngle = Vector3.Angle(stepHit.normal, Vector3.up);
            if (stepAngle > maxGroundAngle)
                return;

            float currentFoot = feet.y;
            float delta = stepHit.point.y - currentFoot;
            if (delta <= 0.01f || delta > maxStepHeight)
                return;

            // Elevação limitada por stepSmooth para evitar tranco visual.
            float stepDelta = Mathf.Min(delta, stepSmooth);
            rigidBody.MovePosition(rigidBody.position + Vector3.up * stepDelta);
        }

        /// <summary>
        /// Plays Footstep Sounds. This code is slightly old, so may not be great, but it functions alright-y!
        /// </summary>
        private void PlayFootstepSounds() {
            //Check if we're moving on the ground. We don't need footsteps in the air.
            if (grounded && rigidBody.linearVelocity.sqrMagnitude > 0.1f) {
                //Select the correct audio clip to play.
                audioSource.clip = playerCharacter.IsRunning() ? audioClipRunning : audioClipWalking;
                //Play it!
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            //Pause it if we're doing something like flying, or not moving!
            else if (audioSource.isPlaying)
                audioSource.Pause();
        }

        /// <summary>
        /// Processa o agachamento do personagem, ajustando suavemente:
        /// - Altura do CapsuleCollider (para passar sob obstáculos baixos)
        /// - Centro do CapsuleCollider (para manter os pés no chão)
        /// - Altura da câmera (para refletir visualmente o agachamento)
        /// - Posição do modelo visual (para que a mesh abaixe junto com a câmera)
        /// 
        /// Princípio: Interpolação suave (Lerp) evita transições bruscas que causam
        /// motion sickness e mantém a experiência de jogo fluida.
        /// </summary>
        private void ProcessCrouch() {
            // ===== AULA: MÉTODO ProcessCrouch() =====
            // Este método é chamado TODO FRAME no Update()
            // Ele é responsável por gerenciar a transição de agachamento (crouch)
            //
            // O QUE ELE FAZ:
            // 1. Verifica se o player está agachado ou em pé
            // 2. Calcula os valores ALVO (target) para cada componente
            // 3. Move SUAVEMENTE em direção aos alvos (interpolação)
            //
            // POR QUE TODO FRAME?
            // Para criar uma transição suave! Se mudássemos instantaneamente,
            // o player "pularia" de em pé para agachado = HORRÍVEL visualmente

            // ===== AULA: CHAMADA DE MÉTODO =====
            // playerCharacter.IsCrouching() chama um MÉTODO (função) de outro script
            // playerCharacter = objeto da classe Character
            // IsCrouching() = método que retorna true (agachado) ou false (em pé)
            //
            // O () VAZIO significa que o método não precisa de parâmetros
            // O método RETORNA um valor boolean (true/false)
            //
            // GUARDAMOS O RESULTADO EM UMA VARIÁVEL:
            // bool isCrouching = valor retornado pelo método
            bool isCrouching = playerCharacter.IsCrouching();

            // ===== AULA: LÓGICA CONDICIONAL (IF/ELSE) =====
            // Aqui definimos os ALVOS (targets) baseado no estado
            // É como um interruptor: se está agachado, alvos = valores agachados
            //                         se está em pé, alvos = valores originais

            if (isCrouching) {
                // ===== QUANDO ESTÁ AGACHADO =====

                // ===== AULA: MULTIPLICAÇÃO E ALTURA DA CÁPSULA =====
                // CapsuleCollider = o "corpo físico" do personagem no Unity
                // É um cilindro invisível que colide com paredes, chão, etc.
                //
                // CÁLCULO:
                // originalCapsuleHeight = 1.8 (altura original em metros)
                // crouchHeightMultiplier = 0.5 (50%)
                // targetCapsuleHeight = 1.8 * 0.5 = 0.9 metros
                //
                // POR QUE REDUZIR?
                // Para o player caber sob obstáculos baixos!
                // Imagine passar por baixo de uma mesa - precisa abaixar o collider
                targetCapsuleHeight = originalCapsuleHeight * crouchHeightMultiplier;

                // ===== AULA: AJUSTE DO CENTRO DA CÁPSULA =====
                // Quando reduzimos a altura da cápsula, precisamos ajustar o CENTRO
                // Se não ajustarmos, os pés do player vão FLUTUAR no ar!
                //
                // ANALOGIA:
                // Imagine uma régua de 20cm (capsule)
                // Centro está no meio = 10cm
                // Se cortamos a régua ao meio (agora tem 10cm)
                // O centro TAMBÉM precisa abaixar para 5cm
                //
                // MATEMÁTICA:
                // heightDifference = quanto a cápsula encolheu
                // Exemplo: 1.8 - 0.9 = 0.9 metros de diferença
                float heightDifference = originalCapsuleHeight - targetCapsuleHeight;

                // Dividimos por 2 porque o centro é no MEIO da cápsula
                // Se encolhemos 0.9m, o centro desce 0.9 / 2 = 0.45m
                // O SINAL NEGATIVO (-) faz descer (Y menor = mais baixo)
                targetCapsuleCenterY = originalCapsuleCenterY - (heightDifference * 0.5f);

                // ===== AULA: A SOLUÇÃO DO BUG - MOVER O ROOT! =====
                // AQUI ESTÁ A MÁGICA QUE RESOLVE O BUG VISUAL!
                //
                // LEMBRE-SE DO PROBLEMA:
                // Tentávamos mover o Armature, mas o Animator resetava a posição
                //
                // SOLUÇÃO:
                // Movemos o SK_FP_CH_Default_Root (PAI do Animator)
                //
                // CÁLCULO:
                // originalRootY = 1.8 (posição Y original do Root)
                // crouchCameraOffset = -0.25 (quanto queremos descer)
                // targetRootY = 1.8 + (-0.25) = 1.55
                //
                // SOMA COM NEGATIVO = SUBTRAÇÃO:
                // 1.8 + (-0.25) é o mesmo que 1.8 - 0.25 = 1.55
                targetRootY = originalRootY + crouchCameraOffset;

                // ===== AULA: POR QUE A CÂMERA NÃO PRECISA OFFSET? =====
                // HIERARQUIA (lembre-se):
                // Player (Y=0 mundo)
                //   └─ SK_FP_CH_Default_Root (Y=1.55 quando agachado) <- MOVEMOS ESTE!
                //       └─ Camera (Y=0 local em relação ao Root) <- SEGUE AUTOMATICAMENTE!
                //
                // MATEMÁTICA:
                // Posição da câmera no mundo = Posição do Root + Posição local da câmera
                // Quando em pé:     Câmera mundo = 1.8 + 0 = 1.8
                // Quando agachado:  Câmera mundo = 1.55 + 0 = 1.55 ✓ (desceu 0.25!)
                //
                // CONCLUSÃO:
                // NÃO precisamos mover a câmera manualmente!
                // Ela "herda" automaticamente o movimento do Root por ser filha dele
                // É a magia da hierarquia de GameObjects do Unity!
                targetCameraY = originalCameraY;
            }
            else {
                // ===== QUANDO ESTÁ EM PÉ =====
                // Simplesmente restauramos TODOS os valores originais
                // É como apertar "reset" - volta ao estado inicial
                targetCapsuleHeight = originalCapsuleHeight;
                targetCapsuleCenterY = originalCapsuleCenterY;
                targetRootY = originalRootY;
                targetCameraY = originalCameraY;
            }


            // Interpola suavemente a altura da cápsula em direção ao alvo
            capsule.height = Mathf.Lerp(capsule.height, targetCapsuleHeight,
                Time.deltaTime * crouchTransitionSpeed);

            // Interpola suavemente o centro Y da cápsula
            Vector3 center = capsule.center;
            center.y = Mathf.Lerp(center.y, targetCapsuleCenterY,
                Time.deltaTime * crouchTransitionSpeed);
            capsule.center = center;

            // NOVA ABORDAGEM: Move o SK_FP_CH_Default_Root inteiro
            // Vantagem: O Animator continua funcionando normalmente, não há conflito
            // A câmera (filha do Root) se move automaticamente junto!
            if (characterRootTransform != null) {
                Vector3 rootPos = characterRootTransform.localPosition;
                float oldRootY = rootPos.y;

                // Interpola suavemente a posição Y do Root
                rootPos.y = Mathf.Lerp(rootPos.y, targetRootY,
                    Time.deltaTime * crouchTransitionSpeed);
                characterRootTransform.localPosition = rootPos;

            }
        }

        #endregion
    }
}