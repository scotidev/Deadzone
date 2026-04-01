# PLANO DE IMPLEMENTAÇÃO: Kinematic Character Controller + FSM

## 📋 CONTEXTO DO PLANO

### O Que Estamos Fazendo?

Estamos **reconstruindo o sistema de movimentação do player** do zero, substituindo a implementação atual baseada em Rigidbody dinâmico por um **Kinematic Character Controller (KCC)** profissional combinado com uma **Finite State Machine (FSM)**.

Este é um projeto de **refatoração completa** do núcleo de gameplay, mantendo as integrações existentes (armas, animações, input) mas transformando completamente como o personagem se move no mundo.

### Por Que Escolhemos Este Plano?

#### Razões Técnicas:
1. **Controle Total:** KCC permite controlar cada aspecto do movimento, essencial para terrenos complexos
2. **Escalabilidade:** FSM facilita adicionar novos comportamentos sem quebrar o existente
3. **Qualidade Profissional:** É o padrão da indústria para jogos AAA
4. **Robustez:** Resolve problemas fundamentais do Rigidbody (rampas, escadas, precisão)

#### Razões Acadêmicas/Portfólio:
1. **Aprendizado Profundo:** Você entenderá física de jogos em nível avançado
2. **Diferencial:** "Implementei KCC do zero" é impressionante em portfólio
3. **Base Sólida:** Conhecimento transferível para qualquer engine/projeto
4. **Demonstração de Habilidade:** Mostra capacidade de arquitetar sistemas complexos

#### Por Que NÃO Usar Asset Pronto?
Para fins acadêmicos e de aprendizado, implementar do zero vale mais:
- ✅ Você aprende os conceitos fundamentais
- ✅ Pode explicar cada linha em apresentações
- ✅ Impressiona em entrevistas técnicas
- ✅ Não depende de código de terceiros

### Objetivo Principal

**Criar um sistema de movimentação robusto e escalável que suporte:**
- ✅ Andar, correr, agachar, pular
- ✅ Subir/descer escadas sem travamento
- ✅ Rampas com ajuste automático de velocidade
- ✅ Terrenos irregulares e hostis
- ✅ Controle preciso e responsivo
- ✅ Fácil manutenção e expansão
- ✅ Código limpo e bem documentado

### O Que NÃO Vai Mudar?

**Mantemos intactos:**
- ✅ Sistema de Input (Input System)
- ✅ Sistema de armas e inventário
- ✅ Animações (apenas ajustamos chamadas)
- ✅ Câmera (pequenas adaptações)
- ✅ Service Locator e arquitetura de serviços
- ✅ UI e HUD

**Só mudamos:**
- ❌ Como o personagem se move fisicamente
- ❌ Como estados são gerenciados
- ❌ Como colisões são detectadas

---

## 🎯 ESTRUTURA DO PLANO

### Divisão em Fases

```
FASE 1: FUNDAÇÃO (Semana 1)
├─ Criar estrutura base de KCC
├─ Implementar movimento básico
└─ Testar movimento horizontal

FASE 2: FÍSICA VERTICAL (Semana 2)
├─ Implementar gravidade customizada
├─ Sistema de detecção de chão
├─ Implementar pulo
└─ Testar movimento 3D completo

FASE 3: STATE MACHINE (Semana 3)
├─ Criar arquitetura de FSM
├─ Implementar estados básicos (Idle, Walking, Running)
├─ Sistema de transições
└─ Testar troca de estados

FASE 4: ESTADOS AVANÇADOS (Semana 4)
├─ Implementar estado InAir (Jumping, Falling)
├─ Implementar estado Crouching
├─ Ajustar transições complexas
└─ Testar todos os estados

FASE 5: TERRENOS COMPLEXOS (Semana 5)
├─ Sistema de Step Offset (escadas)
├─ Ajuste de velocidade em rampas
├─ Detecção de superfícies íngremes
└─ Testar em terrenos variados

FASE 6: INTEGRAÇÃO E POLIMENTO (Semana 6)
├─ Integrar com sistema de armas
├─ Ajustar animações
├─ Implementar recursos avançados (coyote time, jump buffer)
├─ Testar gameplay completo
└─ Documentar código
```

---

## 📚 FASE 1: FUNDAÇÃO (Semana 1)

### Objetivo da Fase
Criar a estrutura base do Kinematic Character Controller e implementar movimento horizontal básico (andar para frente/trás/lados).

### Por Que Começar Assim?
- Movimento horizontal é a base de tudo
- Permite testar a integração com Input System cedo
- Identifica problemas arquiteturais antes de adicionar complexidade
- Você vê resultados imediatos (motivação!)

---

### 📝 TAREFA 1.1: Criar Script CharacterMotor.cs

**O que é:** O "motor" que calcula e executa o movimento físico do personagem.

**Onde criar:** `Assets/Scripts/Player/CharacterMotor.cs`

**Código base:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// Kinematic Character Motor - Controla a física e movimento do personagem.
    /// Este componente é o "motor" que calcula e aplica movimento, colisões e física.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class CharacterMotor : MonoBehaviour
    {
        #region SERIALIZED FIELDS
        
        [Header("Movement Settings")]
        [Tooltip("Velocidade ao andar (m/s)")]
        [SerializeField] private float walkSpeed = 5.0f;
        
        [Tooltip("Velocidade ao correr (m/s)")]
        [SerializeField] private float runSpeed = 9.0f;
        
        [Tooltip("Velocidade ao mirar (m/s)")]
        [SerializeField] private float aimSpeed = 3.0f;
        
        [Tooltip("Quão rápido o personagem acelera/desacelera")]
        [SerializeField] private float movementSharpness = 15.0f;
        
        [Header("Ground Detection")]
        [Tooltip("Distância para verificar se está no chão")]
        [SerializeField] private float groundCheckDistance = 0.2f;
        
        [Tooltip("Layers consideradas como chão")]
        [SerializeField] private LayerMask groundMask = -1;
        
        [Tooltip("Raio da esfera para detecção de chão")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        
        #endregion
        
        #region PRIVATE FIELDS
        
        /// <summary>
        /// Referência ao CapsuleCollider do personagem
        /// </summary>
        private CapsuleCollider capsuleCollider;
        
        /// <summary>
        /// Velocidade atual do personagem (Vector3 representa direção e magnitude)
        /// </summary>
        private Vector3 velocity;
        
        /// <summary>
        /// Está tocando o chão?
        /// </summary>
        private bool isGrounded;
        
        /// <summary>
        /// Normal da superfície do chão (vetor perpendicular à superfície)
        /// </summary>
        private Vector3 groundNormal;
        
        /// <summary>
        /// Hit info da última detecção de chão
        /// </summary>
        private RaycastHit groundHit;
        
        #endregion
        
        #region PROPERTIES
        
        /// <summary>
        /// Getter: Verifica se o personagem está no chão
        /// </summary>
        public bool IsGrounded => isGrounded;
        
        /// <summary>
        /// Getter: Retorna a velocidade atual do personagem
        /// </summary>
        public Vector3 Velocity => velocity;
        
        #endregion
        
        #region UNITY LIFECYCLE
        
        private void Awake()
        {
            // Cacheia o CapsuleCollider - isso evita chamadas GetComponent toda frame
            capsuleCollider = GetComponent<CapsuleCollider>();
            
            // Inicializa velocity com Vector3.zero (parado)
            velocity = Vector3.zero;
        }
        
        private void FixedUpdate()
        {
            // FixedUpdate é chamado em intervalos fixos (padrão: 50 vezes/segundo)
            // Usamos para física porque precisa ser consistente independente do framerate
            
            // 1. Verifica se está no chão
            CheckGroundStatus();
        }
        
        #endregion
        
        #region MOVEMENT METHODS
        
        /// <summary>
        /// Move o personagem em uma direção específica.
        /// Este é o método principal chamado externamente para mover o player.
        /// </summary>
        /// <param name="direction">Direção de movimento (normalizado, em espaço mundial)</param>
        /// <param name="speedMultiplier">Multiplicador de velocidade (1.0 = walkSpeed, 1.8 = runSpeed, etc)</param>
        public void Move(Vector3 direction, float speedMultiplier = 1.0f)
        {
            // Calcula velocidade alvo baseada na direção e velocidade desejada
            Vector3 targetVelocity = direction * (walkSpeed * speedMultiplier);
            
            // Suaviza a transição entre velocidade atual e velocidade alvo
            // Isso cria aceleração/desaceleração suave (não instantânea)
            velocity = Vector3.Lerp(velocity, targetVelocity, movementSharpness * Time.fixedDeltaTime);
            
            // Calcula quanto o personagem deve se mover ESTE frame
            Vector3 moveAmount = velocity * Time.fixedDeltaTime;
            
            // Aplica o movimento ao transform
            transform.position += moveAmount;
        }
        
        #endregion
        
        #region GROUND DETECTION
        
        /// <summary>
        /// Verifica se o personagem está tocando o chão usando SphereCast.
        /// SphereCast é mais confiável que Raycast porque detecta superfícies em bordas.
        /// </summary>
        private void CheckGroundStatus()
        {
            // Calcula o ponto inicial do cast (base da cápsula)
            Vector3 capsuleBottom = GetCapsuleBottomPoint();
            
            // Lança uma esfera virtual para baixo para detectar o chão
            // SphereCast = "rolar uma bola para baixo e ver se bate em algo"
            if (Physics.SphereCast(
                capsuleBottom,                    // Ponto de origem
                groundCheckRadius,                // Raio da esfera
                Vector3.down,                     // Direção (para baixo)
                out groundHit,                    // Armazena informações da colisão
                groundCheckDistance,              // Distância máxima
                groundMask,                       // Apenas layers de chão
                QueryTriggerInteraction.Ignore))  // Ignora triggers
            {
                // Detectou chão!
                isGrounded = true;
                groundNormal = groundHit.normal; // Normal = direção perpendicular à superfície
            }
            else
            {
                // Não detectou chão (está no ar)
                isGrounded = false;
                groundNormal = Vector3.up; // Default: superfície plana imaginária
            }
        }
        
        /// <summary>
        /// Calcula o ponto inferior da cápsula (usado para ground check).
        /// </summary>
        private Vector3 GetCapsuleBottomPoint()
        {
            // CapsuleCollider tem: center, height, radius
            // Bottom = center - (metade da altura - raio)
            return transform.position + capsuleCollider.center - 
                   Vector3.up * (capsuleCollider.height / 2f - capsuleCollider.radius);
        }
        
        #endregion
        
        #region DEBUG VISUALIZATION
        
        /// <summary>
        /// Desenha gizmos na Scene View para debug visual.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (capsuleCollider == null) return;
            
            // Desenha esfera de ground check
            Vector3 bottomPoint = GetCapsuleBottomPoint();
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(bottomPoint - Vector3.up * groundCheckDistance, groundCheckRadius);
            
            // Desenha linha da normal do chão (se estiver no chão)
            if (isGrounded)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(groundHit.point, groundNormal * 0.5f);
            }
        }
        
        #endregion
    }
}
```

**Conceitos-chave explicados:**

1. **Vector3.Lerp()**: Interpola suavemente entre dois valores
   - `Lerp(A, B, 0.5)` = meio caminho entre A e B
   - Cria aceleração/desaceleração natural

2. **Time.fixedDeltaTime**: Tempo desde o último FixedUpdate
   - Multiplica por isso para tornar movimento independente do framerate

3. **SphereCast vs Raycast**:
   - Raycast = linha fina (pode passar entre objetos)
   - SphereCast = bola (detecta bordas e superfícies irregulares)

4. **Normal de Superfície**:
   - Vetor perpendicular à superfície (90 graus)
   - Usaremos depois para ajustar movimento em rampas

---

### 📝 TAREFA 1.2: Integrar com Character.cs

**O que fazer:** Fazer o Character.cs usar o novo CharacterMotor em vez do Rigidbody.

**Arquivo:** `Assets/Scripts/Player/Character.cs`

**Passos:**

1. **Adicionar referência ao CharacterMotor:**

```csharp
// Adicione no topo da classe Character, junto com outros campos
[Header("Movement")]
[Tooltip("Componente que controla a física do movimento")]
[SerializeField] private CharacterMotor characterMotor;
```

2. **Cachear no Awake():**

```csharp
protected override void Awake() {
    // Código existente...
    
    // ADICIONE ISTO:
    // Cacheia o CharacterMotor (evita GetComponent toda hora)
    if (characterMotor == null)
        characterMotor = GetComponent<CharacterMotor>();
    
    // Resto do código...
}
```

3. **Temporariamente desabilitar Movement.cs antigo:**
   - Vá no GameObject "Player" no Hierarchy
   - Desmarque o checkbox do componente "Movement"
   - NÃO delete ainda! Vamos remover só no final

---

### 📝 TAREFA 1.3: Criar Script PlayerController.cs

**O que é:** O controlador principal que conecta Input → Motor → Estado.

**Onde criar:** `Assets/Scripts/Player/PlayerController.cs`

**Código base:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    /// <summary>
    /// PlayerController - Gerencia input e controla o CharacterMotor.
    /// Este componente faz a ponte entre o que o jogador quer (input) e o que o motor executa (movimento).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public class PlayerController : MonoBehaviour
    {
        #region SERIALIZED FIELDS
        
        [Header("References")]
        [Tooltip("Referência ao componente Character")]
        [SerializeField] private Character character;
        
        [Tooltip("Referência ao componente CharacterMotor")]
        [SerializeField] private CharacterMotor motor;
        
        [Header("Movement")]
        [Tooltip("Multiplicador de velocidade ao correr (relativo a velocidade normal)")]
        [SerializeField] private float runSpeedMultiplier = 1.8f;
        
        [Tooltip("Multiplicador de velocidade ao mirar")]
        [SerializeField] private float aimSpeedMultiplier = 0.6f;
        
        #endregion
        
        #region UNITY LIFECYCLE
        
        private void Awake()
        {
            // Auto-referencia componentes se não foram setados no Inspector
            if (character == null)
                character = GetComponent<Character>();
            
            if (motor == null)
                motor = GetComponent<CharacterMotor>();
        }
        
        private void FixedUpdate()
        {
            // FixedUpdate para movimento (consistente com física)
            HandleMovement();
        }
        
        #endregion
        
        #region MOVEMENT
        
        /// <summary>
        /// Processa input de movimento e chama o motor para mover o personagem.
        /// </summary>
        private void HandleMovement()
        {
            // 1. Pega input do Character (vem do Input System)
            Vector2 inputMovement = character.GetInputMovement();
            
            // 2. Converte Vector2 (input 2D) para Vector3 (mundo 3D)
            // X = horizontal (esquerda/direita), Z = vertical (frente/trás), Y = 0 (não sobe/desce ainda)
            Vector3 moveDirection = new Vector3(inputMovement.x, 0f, inputMovement.y);
            
            // 3. Converte de espaço LOCAL para espaço MUNDIAL
            // LOCAL = "frente" é onde o player olha
            // MUNDIAL = "frente" é sempre direção Z+ no Unity
            moveDirection = transform.TransformDirection(moveDirection);
            
            // 4. Normaliza para evitar movimento mais rápido na diagonal
            // Sem normalizar: diagonal = sqrt(1² + 1²) = 1.414 (41% mais rápido!)
            // Com normalizar: todas direções = 1.0
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();
            
            // 5. Determina multiplicador de velocidade baseado no estado
            float speedMultiplier = 1f; // Padrão = velocidade normal (walk)
            
            if (character.IsAiming())
            {
                // Mirando = mais lento (precisão)
                speedMultiplier = aimSpeedMultiplier;
            }
            else if (character.IsRunning())
            {
                // Correndo = mais rápido
                speedMultiplier = runSpeedMultiplier;
            }
            
            // 6. Chama o motor para executar o movimento
            motor.Move(moveDirection, speedMultiplier);
        }
        
        #endregion
    }
}
```

**Conceitos-chave:**

1. **transform.TransformDirection()**: Converte coordenadas locais → mundiais
   - Essencial para movimento relativo à direção que o player olha

2. **Vector.sqrMagnitude vs magnitude**:
   - `sqrMagnitude` é mais rápido (não usa raiz quadrada)
   - Use para comparações (> 1, < 0.1, etc)

3. **Normalize()**:
   - Transforma vetor em comprimento = 1, mas mantém direção
   - Previne movimento mais rápido na diagonal

---

### 📝 TAREFA 1.4: Adicionar Componentes no Unity

**Passo a passo:**

1. **No GameObject "Player":**
   - Add Component → `CharacterMotor`
   - Add Component → `PlayerController`

2. **No PlayerController:**
   - Arraste o GameObject "Player" para o campo "Character"
   - Arraste o componente "CharacterMotor" para o campo "Motor"

3. **Configurar CharacterMotor:**
   - Walk Speed: `5`
   - Run Speed: `9`
   - Aim Speed: `3`
   - Movement Sharpness: `15`
   - Ground Check Distance: `0.2`
   - Ground Check Radius: `0.3`
   - Ground Mask: Selecione "Default" (ou camadas de chão)

4. **No Rigidbody (ainda existe no Player):**
   - Marque `Is Kinematic` = TRUE
   - Isso desliga a física dinâmica (não interfere com nosso KCC)

---

### 📝 TAREFA 1.5: Testar Movimento Básico

**Como testar:**

1. **Play no Unity**
2. **Use WASD para mover**
3. **Segure Shift para correr** (deve ficar mais rápido)
4. **Clique direito (mirar)** (deve ficar mais lento)

**Verificações:**

✅ Personagem se move suavemente
✅ Não atravessa paredes
✅ Velocidade muda ao correr/mirar
✅ Movimento diagonal não é mais rápido

**Problemas esperados:**

- ❌ Personagem não pula (normal, implementaremos na Fase 2)
- ❌ Cai através do chão (normal, falta gravidade)
- ❌ Pode atravessar objetos pequenos (normal, falta collision check)

**Se não funcionar:**

1. Verifique se `Movement.cs` antigo está desabilitado
2. Confirme que `Rigidbody.isKinematic = true`
3. Verifique se campos do PlayerController estão preenchidos
4. Veja o Console por erros

---

### ✅ Checklist da Fase 1

- [ ] CharacterMotor.cs criado com movimento horizontal
- [ ] Character.cs referencia CharacterMotor
- [ ] Movement.cs antigo desabilitado
- [ ] PlayerController.cs criado e integrado
- [ ] Componentes adicionados no Unity
- [ ] Movimento horizontal testado e funcionando
- [ ] Código comentado e compreendido
- [ ] Commit no Git: "Fase 1: Implementar movimento horizontal básico com KCC"

---

## 📚 FASE 2: FÍSICA VERTICAL (Semana 2)

### Objetivo da Fase
Implementar gravidade customizada, detecção de chão robusta e sistema de pulo.

### Por Que Agora?
- Movimento horizontal funciona, agora precisamos da terceira dimensão
- Gravidade e pulo são interdependentes (precisa de ambos juntos)
- Base para estados avançados (InAir, Jumping, Falling)

---

### 📝 TAREFA 2.1: Implementar Gravidade

**O que adicionar no CharacterMotor.cs:**

```csharp
// ADICIONE nos SERIALIZED FIELDS:
[Header("Gravity")]
[Tooltip("Força da gravidade (m/s²). Valor negativo puxa para baixo.")]
[SerializeField] private float gravity = -20f;

[Tooltip("Velocidade terminal máxima ao cair (m/s)")]
[SerializeField] private float maxFallSpeed = -20f;

// ADICIONE nos PRIVATE FIELDS:
/// <summary>
/// Componente vertical da velocidade (subir/cair)
/// </summary>
private float verticalVelocity;

// MODIFIQUE o método Move():
public void Move(Vector3 direction, float speedMultiplier = 1.0f)
{
    // --- Movimento Horizontal (já existia) ---
    Vector3 targetVelocity = direction * (walkSpeed * speedMultiplier);
    velocity = Vector3.Lerp(velocity, targetVelocity, movementSharpness * Time.fixedDeltaTime);
    
    // --- NOVO: Aplicar Gravidade ---
    ApplyGravity();
    
    // --- NOVO: Combinar movimento horizontal + vertical ---
    Vector3 moveAmount = velocity * Time.fixedDeltaTime;
    moveAmount.y = verticalVelocity * Time.fixedDeltaTime; // Adiciona componente Y
    
    // Aplica movimento
    transform.position += moveAmount;
}

// ADICIONE método novo:
/// <summary>
/// Aplica gravidade ao personagem.
/// </summary>
private void ApplyGravity()
{
    if (isGrounded)
    {
        // No chão: pequena força para baixo (mantém "colado" no chão)
        verticalVelocity = -2f;
    }
    else
    {
        // No ar: aplica gravidade (acelera para baixo)
        verticalVelocity += gravity * Time.fixedDeltaTime;
        
        // Limita velocidade de queda (velocidade terminal)
        verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
    }
}
```

**Por que -2f no chão?**
- Mantém personagem "grudado" no chão
- Previne micro-pulos em superfícies irregulares
- Se fosse 0, personagem "flutuaria" um pouco

**Velocidade terminal:**
- Limite realista de queda (objetos param de acelerar no ar)
- Previne bugs de cair infinitamente rápido

---

### 📝 TAREFA 2.2: Implementar Sistema de Pulo

**O que adicionar:**

```csharp
// ADICIONE nos SERIALIZED FIELDS:
[Header("Jump")]
[Tooltip("Força do pulo (m/s). Valores entre 4-8 são recomendados.")]
[SerializeField] private float jumpForce = 5f;

[Tooltip("Tempo mínimo entre pulos (segundos)")]
[SerializeField] private float jumpCooldown = 0.5f;

// ADICIONE nos PRIVATE FIELDS:
/// <summary>
/// Timestamp do último pulo (usado para cooldown)
/// </summary>
private float lastJumpTime = -999f;

/// <summary>
/// Está no meio de um pulo?
/// </summary>
private bool isJumping;

// ADICIONE método público:
/// <summary>
/// Tenta pular. Só funciona se estiver no chão e cooldown passou.
/// </summary>
public void Jump()
{
    // Verifica condições para pular
    if (!isGrounded)
    {
        Debug.Log("Não pode pular: não está no chão");
        return;
    }
    
    if (Time.time - lastJumpTime < jumpCooldown)
    {
        Debug.Log("Não pode pular: cooldown ativo");
        return;
    }
    
    // PULA!
    verticalVelocity = jumpForce; // Define velocidade vertical para cima
    isJumping = true;
    lastJumpTime = Time.time;
    
    Debug.Log($"Pulou! Velocidade vertical: {verticalVelocity}");
}

// MODIFIQUE ApplyGravity():
private void ApplyGravity()
{
    if (isGrounded && !isJumping)
    {
        verticalVelocity = -2f;
    }
    else
    {
        verticalVelocity += gravity * Time.fixedDeltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
        
        // Se começou a cair, não está mais "pulando"
        if (verticalVelocity < 0)
            isJumping = false;
    }
}
```

**Física do pulo explicada:**

1. **Impulso inicial:** `verticalVelocity = jumpForce` (ex: 5 m/s para cima)
2. **Subida:** Gravidade desacelera (-20 m/s²)
   - Frame 1: 5 m/s
   - Frame 2: 4.6 m/s
   - Frame 3: 4.2 m/s
   - ...
3. **Ápice:** verticalVelocity = 0 (parou de subir)
4. **Descida:** Continua acelerando para baixo
   - Frame N: -0.4 m/s
   - Frame N+1: -0.8 m/s
   - ...
5. **Aterrisagem:** `isGrounded = true`, reseta para -2

---

### 📝 TAREFA 2.3: Integrar Pulo no PlayerController

**O que adicionar no PlayerController.cs:**

```csharp
// ADICIONE no FixedUpdate():
private void FixedUpdate()
{
    HandleMovement();
    HandleJump(); // NOVO
}

// ADICIONE método novo:
/// <summary>
/// Processa input de pulo.
/// </summary>
private void HandleJump()
{
    // Verifica se o jogador quer pular
    if (character.IsJumping())
    {
        // Chama o motor para executar o pulo
        motor.Jump();
    }
}
```

**Simples assim!** O Character já tem `IsJumping()` implementado.

---

### 📝 TAREFA 2.4: Melhorar Detecção de Chão

**Problema:** SphereCast simples pode falhar em bordas.

**Solução:** Múltiplos raycasts + spherecast.

**Substitua CheckGroundStatus():**

```csharp
/// <summary>
/// Verifica status de chão usando múltiplas técnicas para máxima confiabilidade.
/// </summary>
private void CheckGroundStatus()
{
    Vector3 capsuleBottom = GetCapsuleBottomPoint();
    bool wasGrounded = isGrounded;
    
    // Método 1: SphereCast central
    bool sphereHit = Physics.SphereCast(
        capsuleBottom,
        groundCheckRadius,
        Vector3.down,
        out RaycastHit sphereCastHit,
        groundCheckDistance,
        groundMask,
        QueryTriggerInteraction.Ignore
    );
    
    // Método 2: Raycasts nas bordas (detecta bordas de plataformas)
    bool rayHit = CheckGroundWithRays(capsuleBottom, out RaycastHit rayCastHit);
    
    // Considera no chão se QUALQUER método detectou
    isGrounded = sphereHit || rayHit;
    
    // Atualiza groundHit com o mais próximo
    if (sphereHit && rayHit)
    {
        groundHit = (sphereCastHit.distance < rayCastHit.distance) ? sphereCastHit : rayCastHit;
    }
    else if (sphereHit)
    {
        groundHit = sphereCastHit;
    }
    else if (rayHit)
    {
        groundHit = rayCastHit;
    }
    
    // Atualiza normal do chão
    if (isGrounded)
    {
        groundNormal = groundHit.normal;
    }
    else
    {
        groundNormal = Vector3.up;
    }
}

/// <summary>
/// Lança raycasts em um padrão circular para detectar chão nas bordas.
/// </summary>
private bool CheckGroundWithRays(Vector3 center, out RaycastHit hit)
{
    hit = new RaycastHit();
    float rayLength = groundCheckDistance + 0.05f;
    int rayCount = 4; // 4 raios em cruz (frente, trás, esquerda, direita)
    
    for (int i = 0; i < rayCount; i++)
    {
        // Calcula ângulo do raio (0°, 90°, 180°, 270°)
        float angle = (360f / rayCount) * i;
        
        // Converte ângulo para direção
        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        
        // Offset do raio (nas bordas da cápsula)
        Vector3 rayStart = center + direction * (capsuleCollider.radius * 0.8f);
        
        // Lança raio para baixo
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit tempHit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            hit = tempHit;
            return true;
        }
    }
    
    return false;
}
```

**Por que 4 raios?**
- Detecta bordas de plataformas
- Previne "cair através" de cantos
- Mais confiável em superfícies irregulares

---

### 📝 TAREFA 2.5: Testar Física Vertical

**Como testar:**

1. **Crie plataformas em alturas diferentes**
   - GameObject → 3D Object → Cube
   - Escale para fazer plataformas (10x1x10)
   - Posicione em Y = 0, Y = 2, Y = 4

2. **Teste:**
   - ✅ Personagem cai ao sair de uma plataforma
   - ✅ Espaço pula (altura ~1-2 metros)
   - ✅ Não pode pular no ar (cooldown funciona)
   - ✅ Aterrissa suavemente

3. **Debug visual:**
   - Na Scene View, veja os gizmos (esferas verdes/vermelhas)
   - Verde = no chão
   - Vermelho = no ar

**Ajuste fino:**
- Se pulo muito alto: diminua `jumpForce`
- Se pulo muito baixo: aumente `jumpForce`
- Se cai muito rápido: aumente `gravity` (menos negativo, ex: -15)
- Se cai muito devagar: diminua `gravity` (mais negativo, ex: -25)

---

### ✅ Checklist da Fase 2

- [ ] Gravidade implementada e testada
- [ ] Sistema de pulo funcionando
- [ ] Detecção de chão melhorada (múltiplos raycasts)
- [ ] Personagem cai/sobe corretamente
- [ ] Cooldown de pulo funciona
- [ ] Debug gizmos visualizados na Scene
- [ ] Valores ajustados para "feel" bom
- [ ] Commit no Git: "Fase 2: Implementar gravidade e sistema de pulo"

---

## 📚 FASE 3: STATE MACHINE (Semana 3)

### Objetivo da Fase
Criar arquitetura de Finite State Machine (FSM) e implementar estados básicos (Idle, Walking, Running).

### Por Que FSM?

**Problema sem FSM:**
```csharp
// Código espaguete - RUIM
if (isMoving && isRunning && !isAiming && isGrounded && !isCrouching) {
    // ... 50 linhas de código
}
else if (isMoving && !isRunning && !isAiming && isGrounded) {
    // ... 30 linhas de código
}
// ... 10 mais condições aninhadas
```

**Solução com FSM:**
```csharp
// Limpo e organizado - BOM
currentState.Execute(); // Estado sabe o que fazer
```

**Vantagens:**
- ✅ Cada estado tem sua própria lógica isolada
- ✅ Transições explícitas (não há estados "impossíveis")
- ✅ Fácil adicionar novos estados
- ✅ Fácil debugar (você sabe exatamente em que estado está)

---

### 📝 TAREFA 3.1: Criar Classe Base PlayerState

**Onde criar:** `Assets/Scripts/Player/StateMachine/PlayerState.cs`

**Crie a pasta:** `Assets/Scripts/Player/StateMachine/`

**Código:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.StateMachine
{
    /// <summary>
    /// Classe base abstrata para todos os estados do player.
    /// Define o "contrato" que todos os estados devem seguir.
    /// </summary>
    public abstract class PlayerState
    {
        #region PROTECTED FIELDS
        
        /// <summary>
        /// Referência ao controlador do player (acesso aos componentes)
        /// </summary>
        protected PlayerController controller;
        
        /// <summary>
        /// Referência à state machine (para trocar de estado)
        /// </summary>
        protected PlayerStateMachine stateMachine;
        
        #endregion
        
        #region CONSTRUCTOR
        
        /// <summary>
        /// Construtor - inicializa referências.
        /// </summary>
        protected PlayerState(PlayerController controller, PlayerStateMachine stateMachine)
        {
            this.controller = controller;
            this.stateMachine = stateMachine;
        }
        
        #endregion
        
        #region ABSTRACT METHODS (devem ser implementados por estados concretos)
        
        /// <summary>
        /// Chamado quando o estado é ativado (transição para este estado).
        /// Use para inicializar variáveis, tocar animações, etc.
        /// </summary>
        public abstract void Enter();
        
        /// <summary>
        /// Chamado todo Update() enquanto este estado está ativo.
        /// Use para lógica que não depende de física (UI, input, etc).
        /// </summary>
        public abstract void Update();
        
        /// <summary>
        /// Chamado todo FixedUpdate() enquanto este estado está ativo.
        /// Use para lógica de física e movimento.
        /// </summary>
        public abstract void FixedUpdate();
        
        /// <summary>
        /// Chamado quando o estado é desativado (transição para outro estado).
        /// Use para limpar, parar animações, etc.
        /// </summary>
        public abstract void Exit();
        
        #endregion
        
        #region VIRTUAL METHODS (podem ser sobrescritos opcionalmente)
        
        /// <summary>
        /// Chamado para verificar se deve transicionar para outro estado.
        /// Retorna o próximo estado ou null se deve permanecer no atual.
        /// </summary>
        public virtual PlayerState CheckTransitions()
        {
            return null; // Por padrão, não transiciona
        }
        
        #endregion
    }
}
```

**Conceitos:**

1. **Abstract vs Virtual:**
   - `abstract`: DEVE ser implementado por classes filhas
   - `virtual`: PODE ser sobrescrito (mas tem implementação padrão)

2. **Enter/Update/FixedUpdate/Exit:**
   - Ciclo de vida de um estado
   - Similar aos métodos do MonoBehaviour

---

### 📝 TAREFA 3.2: Criar PlayerStateMachine

**Onde criar:** `Assets/Scripts/Player/StateMachine/PlayerStateMachine.cs`

**Código:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.StateMachine
{
    /// <summary>
    /// Gerenciador de estados do player.
    /// Controla qual estado está ativo e gerencia transições.
    /// </summary>
    public class PlayerStateMachine
    {
        #region PRIVATE FIELDS
        
        /// <summary>
        /// Estado atualmente ativo
        /// </summary>
        private PlayerState currentState;
        
        /// <summary>
        /// Referência ao controller (passada para os estados)
        /// </summary>
        private PlayerController controller;
        
        #endregion
        
        #region PROPERTIES
        
        /// <summary>
        /// Getter: Retorna o estado atual (para debug)
        /// </summary>
        public PlayerState CurrentState => currentState;
        
        /// <summary>
        /// Getter: Nome do estado atual (para debug)
        /// </summary>
        public string CurrentStateName => currentState?.GetType().Name ?? "None";
        
        #endregion
        
        #region CONSTRUCTOR
        
        /// <summary>
        /// Construtor - inicializa a state machine.
        /// </summary>
        public PlayerStateMachine(PlayerController controller)
        {
            this.controller = controller;
        }
        
        #endregion
        
        #region PUBLIC METHODS
        
        /// <summary>
        /// Inicializa a state machine com um estado inicial.
        /// </summary>
        public void Initialize(PlayerState startState)
        {
            currentState = startState;
            currentState.Enter();
            
            Debug.Log($"<color=cyan>[StateMachine]</color> Inicializado em: {CurrentStateName}");
        }
        
        /// <summary>
        /// Atualiza o estado atual (chama Update).
        /// </summary>
        public void Update()
        {
            // Executa lógica do estado atual
            currentState?.Update();
            
            // Verifica se deve transicionar
            CheckForTransition();
        }
        
        /// <summary>
        /// Atualiza o estado atual (chama FixedUpdate).
        /// </summary>
        public void FixedUpdate()
        {
            // Executa lógica de física do estado atual
            currentState?.FixedUpdate();
        }
        
        /// <summary>
        /// Força uma transição para um novo estado.
        /// </summary>
        public void ChangeState(PlayerState newState)
        {
            if (newState == null || newState == currentState)
                return;
            
            string previousStateName = CurrentStateName;
            
            // Sai do estado atual
            currentState?.Exit();
            
            // Troca para o novo estado
            currentState = newState;
            
            // Entra no novo estado
            currentState.Enter();
            
            Debug.Log($"<color=yellow>[StateMachine]</color> Transição: {previousStateName} → {CurrentStateName}");
        }
        
        #endregion
        
        #region PRIVATE METHODS
        
        /// <summary>
        /// Verifica se o estado atual quer transicionar para outro.
        /// </summary>
        private void CheckForTransition()
        {
            PlayerState nextState = currentState?.CheckTransitions();
            
            if (nextState != null)
            {
                ChangeState(nextState);
            }
        }
        
        #endregion
    }
}
```

**Fluxo de uma transição:**

```
Estado A está ativo
    ↓
CheckTransitions() retorna Estado B
    ↓
Estado A.Exit() é chamado
    ↓
currentState = Estado B
    ↓
Estado B.Enter() é chamado
    ↓
Estado B está ativo
```

---

### 📝 TAREFA 3.3: Criar Estados Básicos (Idle, Walking, Running)

#### Estado: Idle (Parado)

**Onde criar:** `Assets/Scripts/Player/StateMachine/States/IdleState.cs`

**Crie a pasta:** `Assets/Scripts/Player/StateMachine/States/`

**Código:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.StateMachine
{
    /// <summary>
    /// Estado Idle - Personagem parado, sem movimento.
    /// </summary>
    public class IdleState : PlayerState
    {
        public IdleState(PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("<color=green>[IdleState]</color> Entrou no estado Idle");
            
            // TODO: Tocar animação de Idle
            // controller.Animator.SetBool("IsIdle", true);
        }
        
        public override void Update()
        {
            // Idle não faz nada no Update (pode adicionar animações procedurais depois)
        }
        
        public override void FixedUpdate()
        {
            // Move com velocidade 0 (para suavemente)
            controller.Motor.Move(Vector3.zero, 0f);
        }
        
        public override void Exit()
        {
            Debug.Log("<color=red>[IdleState]</color> Saiu do estado Idle");
            
            // TODO: Parar animação de Idle
            // controller.Animator.SetBool("IsIdle", false);
        }
        
        public override PlayerState CheckTransitions()
        {
            // Se começou a mover, vai para Walking
            Vector2 input = controller.Character.GetInputMovement();
            if (input.sqrMagnitude > 0.1f)
            {
                return new WalkingState(controller, stateMachine);
            }
            
            // Se pulou (no chão), vai para Jumping
            // (implementaremos depois)
            
            return null; // Permanece em Idle
        }
    }
}
```

#### Estado: Walking (Andando)

**Onde criar:** `Assets/Scripts/Player/StateMachine/States/WalkingState.cs`

**Código:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.StateMachine
{
    /// <summary>
    /// Estado Walking - Personagem andando em velocidade normal.
    /// </summary>
    public class WalkingState : PlayerState
    {
        public WalkingState(PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("<color=green>[WalkingState]</color> Entrou no estado Walking");
            
            // TODO: Tocar animação de Walking
            // controller.Animator.SetBool("IsWalking", true);
        }
        
        public override void Update()
        {
            // Walking não precisa de lógica no Update (movimento é no FixedUpdate)
        }
        
        public override void FixedUpdate()
        {
            // Pega input de movimento
            Vector2 inputMovement = controller.Character.GetInputMovement();
            
            // Converte para direção 3D
            Vector3 moveDirection = new Vector3(inputMovement.x, 0f, inputMovement.y);
            moveDirection = controller.transform.TransformDirection(moveDirection);
            
            // Normaliza se necessário
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();
            
            // Move com velocidade normal (multiplier = 1.0)
            float speedMultiplier = 1.0f;
            
            // Se estiver mirando, reduz velocidade
            if (controller.Character.IsAiming())
            {
                speedMultiplier = controller.AimSpeedMultiplier;
            }
            
            controller.Motor.Move(moveDirection, speedMultiplier);
        }
        
        public override void Exit()
        {
            Debug.Log("<color=red>[WalkingState]</color> Saiu do estado Walking");
            
            // TODO: Parar animação de Walking
            // controller.Animator.SetBool("IsWalking", false);
        }
        
        public override PlayerState CheckTransitions()
        {
            Vector2 input = controller.Character.GetInputMovement();
            
            // Se parou de mover, vai para Idle
            if (input.sqrMagnitude < 0.1f)
            {
                return new IdleState(controller, stateMachine);
            }
            
            // Se começou a correr, vai para Running
            if (controller.Character.IsRunning())
            {
                return new RunningState(controller, stateMachine);
            }
            
            // Se pulou, vai para Jumping
            // (implementaremos depois)
            
            return null; // Permanece em Walking
        }
    }
}
```

#### Estado: Running (Correndo)

**Onde criar:** `Assets/Scripts/Player/StateMachine/States/RunningState.cs`

**Código:**

```csharp
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack.StateMachine
{
    /// <summary>
    /// Estado Running - Personagem correndo em velocidade aumentada.
    /// </summary>
    public class RunningState : PlayerState
    {
        public RunningState(PlayerController controller, PlayerStateMachine stateMachine) 
            : base(controller, stateMachine)
        {
        }
        
        public override void Enter()
        {
            Debug.Log("<color=green>[RunningState]</color> Entrou no estado Running");
            
            // TODO: Tocar animação de Running
            // controller.Animator.SetBool("IsRunning", true);
        }
        
        public override void Update()
        {
            // Running não precisa de lógica no Update
        }
        
        public override void FixedUpdate()
        {
            // Pega input de movimento
            Vector2 inputMovement = controller.Character.GetInputMovement();
            
            // Converte para direção 3D
            Vector3 moveDirection = new Vector3(inputMovement.x, 0f, inputMovement.y);
            moveDirection = controller.transform.TransformDirection(moveDirection);
            
            // Normaliza se necessário
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();
            
            // Move com velocidade de corrida (multiplier maior)
            controller.Motor.Move(moveDirection, controller.RunSpeedMultiplier);
        }
        
        public override void Exit()
        {
            Debug.Log("<color=red>[RunningState]</color> Saiu do estado Running");
            
            // TODO: Parar animação de Running
            // controller.Animator.SetBool("IsRunning", false);
        }
        
        public override PlayerState CheckTransitions()
        {
            Vector2 input = controller.Character.GetInputMovement();
            
            // Se parou de mover, vai para Idle
            if (input.sqrMagnitude < 0.1f)
            {
                return new IdleState(controller, stateMachine);
            }
            
            // Se parou de correr, volta para Walking
            if (!controller.Character.IsRunning())
            {
                return new WalkingState(controller, stateMachine);
            }
            
            // Se mirando, não pode correr (volta para Walking)
            if (controller.Character.IsAiming())
            {
                return new WalkingState(controller, stateMachine);
            }
            
            return null; // Permanece em Running
        }
    }
}
```

---

### 📝 TAREFA 3.4: Integrar FSM no PlayerController

**Modificações no PlayerController.cs:**

```csharp
using InfimaGames.LowPolyShooterPack.StateMachine; // ADICIONE

public class PlayerController : MonoBehaviour
{
    // ... campos existentes ...
    
    // ADICIONE:
    #region STATE MACHINE
    
    /// <summary>
    /// State machine que gerencia estados do player
    /// </summary>
    private PlayerStateMachine stateMachine;
    
    /// <summary>
    /// Getter: Acesso ao Character (para os estados)
    /// </summary>
    public Character Character => character;
    
    /// <summary>
    /// Getter: Acesso ao Motor (para os estados)
    /// </summary>
    public CharacterMotor Motor => motor;
    
    /// <summary>
    /// Getter: Multiplicador de velocidade ao correr (para os estados)
    /// </summary>
    public float RunSpeedMultiplier => runSpeedMultiplier;
    
    /// <summary>
    /// Getter: Multiplicador de velocidade ao mirar (para os estados)
    /// </summary>
    public float AimSpeedMultiplier => aimSpeedMultiplier;
    
    #endregion
    
    // MODIFIQUE Awake():
    private void Awake()
    {
        // ... código existente ...
        
        // ADICIONE:
        // Inicializa a state machine
        stateMachine = new PlayerStateMachine(this);
    }
    
    // ADICIONE Start():
    private void Start()
    {
        // Define estado inicial como Idle
        stateMachine.Initialize(new IdleState(this, stateMachine));
    }
    
    // MODIFIQUE Update() e FixedUpdate():
    private void Update()
    {
        stateMachine.Update();
    }
    
    private void FixedUpdate()
    {
        // REMOVA HandleMovement() e HandleJump()
        // Agora a state machine cuida disso
        
        stateMachine.FixedUpdate();
    }
    
    // OPCIONAL: Para debug no Inspector
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;
        
        GUI.Label(new Rect(10, 10, 300, 30), $"Estado: {stateMachine.CurrentStateName}", style);
    }
}
```

---

### 📝 TAREFA 3.5: Testar State Machine

**Como testar:**

1. **Play no Unity**
2. **Olhe para o canto superior esquerdo** (OnGUI mostra estado atual)
3. **Teste transições:**
   - Parado → "Idle"
   - Pressione W → "Walking"
   - Segure Shift → "Running"
   - Solte Shift → "Walking"
   - Solte W → "Idle"

4. **Verifique Console:**
   - Mensagens de Enter/Exit devem aparecer
   - Cor verde = Enter
   - Cor vermelha = Exit
   - Cor amarela = Transição

**Verificações:**

✅ Estado muda conforme input
✅ Velocidade muda entre Walking/Running
✅ Transições são suaves
✅ Console mostra logs coloridos

---

### ✅ Checklist da Fase 3

- [ ] PlayerState.cs (classe base) criado
- [ ] PlayerStateMachine.cs criado
- [ ] IdleState.cs implementado
- [ ] WalkingState.cs implementado
- [ ] RunningState.cs implementado
- [ ] PlayerController integrado com FSM
- [ ] Estados testados e funcionando
- [ ] Transições funcionam corretamente
- [ ] Debug GUI mostra estado atual
- [ ] Commit no Git: "Fase 3: Implementar Finite State Machine com estados básicos"

---

## 📚 FASES RESTANTES (Resumo)

### FASE 4: Estados Avançados (Semana 4)
- [ ] Criar InAirState (base para pulo/queda)
- [ ] Criar JumpingState
- [ ] Criar FallingState
- [ ] Criar CrouchingState
- [ ] Implementar transições complexas

### FASE 5: Terrenos Complexos (Semana 5)
- [ ] Sistema de Step Offset (escadas)
- [ ] Ajuste de velocidade em rampas
- [ ] Detecção de superfícies íngremes
- [ ] Deslizar em slopes muito íngremes
- [ ] Testar em terrenos variados

### FASE 6: Integração e Polimento (Semana 6)
- [ ] Integrar animações
- [ ] Coyote Time
- [ ] Jump Buffering
- [ ] Camera Bobbing
- [ ] Footstep sounds por material
- [ ] Particle effects
- [ ] Documentação final
- [ ] Remoção do código antigo
- [ ] Apresentação/demo

---

## 📊 PROGRESSO GERAL

```
[██░░░░] 33% Completo

✅ Fase 1: Fundação
✅ Fase 2: Física Vertical
✅ Fase 3: State Machine
⬜ Fase 4: Estados Avançados
⬜ Fase 5: Terrenos Complexos
⬜ Fase 6: Integração e Polimento
```

---

## 🎓 PRÓXIMOS PASSOS

**AGORA (Esta Semana):**
1. Complete as 3 tarefas da Fase 1
2. Teste movimento horizontal
3. Faça commit no Git

**Semana que vem:**
1. Implemente Fase 2 (gravidade e pulo)
2. Teste em plataformas de diferentes alturas
3. Ajuste valores para "feel" bom

**Depois:**
1. Continue seguindo as fases em ordem
2. Não pule etapas (cada uma depende da anterior)
3. Teste MUITO entre cada fase

---

## 💡 DICAS IMPORTANTES

### Para Estudante/Portfólio:
- ✅ **Comente MUITO o código** (mostre que você entende)
- ✅ **Tire screenshots do processo** (para apresentação)
- ✅ **Faça commits frequentes** (mostra evolução)
- ✅ **Documente problemas e soluções** (learning log)

### Para Apresentação:
- ✅ **Grave vídeos comparando antes/depois**
- ✅ **Prepare slides explicando arquitetura**
- ✅ **Mostre diagramas de FSM**
- ✅ **Demonstre cada feature implementada**

### Para Aprendizado:
- ✅ **Não copie/cole sem entender**
- ✅ **Experimente mudar valores**
- ✅ **Quebre o código de propósito** (aprenda consertando)
- ✅ **Faça perguntas quando tiver dúvidas**

---

## 🆘 QUANDO PEDIR AJUDA

**Sinais de que precisa de suporte:**
- ❌ Erro que não consegue resolver em 30min
- ❌ Não entende um conceito fundamental
- ❌ Código funciona mas não sabe por quê
- ❌ Stuck em uma tarefa por mais de 1 dia

**Como pedir ajuda eficientemente:**
1. Descreva o que você está tentando fazer
2. Mostre o código relevante
3. Explique o que você já tentou
4. Diga qual é o comportamento esperado vs atual
5. Inclua mensagens de erro (se houver)

---

## 📖 RECURSOS ADICIONAIS

### Conceitos para Estudar Paralelamente:
- Física básica (cinemática, velocidade, aceleração)
- Vetores e geometria 3D
- Máquinas de estado (FSM)
- Padrões de design (State Pattern, Observer)
- Raycasting e collision detection

### Vídeos Recomendados:
- [Sebastian Lague - Kinematic Character Controller](https://www.youtube.com/watch?v=6BsNsJPUqFE)
- [Code Monkey - State Machine](https://www.youtube.com/watch?v=Vt8aZDPzRjI)
- [Brackeys - Movement in Unity](https://www.youtube.com/watch?v=4HpC--2iowE)

---

## ✅ CHECKLIST GERAL DO PROJETO

### Semana 1:
- [ ] Fase 1 completa e testada
- [ ] Git commit realizado
- [ ] Código comentado

### Semana 2:
- [ ] Fase 2 completa e testada
- [ ] Gravidade e pulo funcionando
- [ ] Git commit realizado

### Semana 3:
- [ ] Fase 3 completa e testada
- [ ] FSM funcionando perfeitamente
- [ ] Git commit realizado

### Semana 4-6:
- [ ] Fases 4-6 completas
- [ ] Código documentado
- [ ] Apresentação preparada

---

## 🎯 OBJETIVO FINAL

Ao completar este plano, você terá:

✅ **Um sistema de movimentação AAA** robusto e escalável
✅ **Conhecimento profundo** de física de jogos
✅ **Portfólio impressionante** com código do zero
✅ **Base sólida** para qualquer projeto futuro
✅ **Diferencial** em entrevistas técnicas

**Boa sorte e mãos à obra! 🚀**

---

*Criado em: 01/04/2026*
*Projeto: Deadzone FPS*
*Autor: Gabriel (com suporte do GitHub Copilot)*
