# Sistema de Seleção de Items - Projeto Deadzone

## Visão Geral
O projeto implements um **sistema unificado de seleção de items** onde todos os tipos de itens (armas, consumíveis, buildables) utilizam o mesmo mecanismo de entrada via teclas 1-8. Este design foi implementado durante uma refatoração explícita para padronizar a interação do jogador com diferentes tipos de equipamento.

## Scripts Responsáveis

### 1. Gerenciador Principal
- **`Assets/Scripts/Player/Inventory.cs`**
  - Contém a lógica central de seleção
  - Gerencia o array de items selecionáveis (`ItemBehaviour[] selectableItems`)
  - Processa inputs do sistema de Input System (teclas 1-8)
  - Controla o ciclo de vida de seleção/deseleção
  - Mantém compatibilidade com sistemas existentes de armas

### 2. Interface Base Abstrata
- **`Assets/Scripts/Items/ItemBehaviour.cs`**
  - Define o contrato que todos os items selecionáveis devem seguir
  - Métodos abstratos obrigatórios:
    - `GetItemID()` - Identificador único do item
    - `GetDisplayName()` - Nome para exibição na UI
    - `GetIcon()` - Ícone para HUD/inventário
    - `OnSelected()` - Chamado quando o item é selecionado
    - `OnDeselected()` - Chamado quando outro item é selecionado
    - `OnUse()` - Chamado quando o jogador usa o item (botão de fogo)
    - `CanBeUsed()` - Verifica se o item pode ser selecionado/usado
    - `OnUseExclusive()` - Versão aprimorada para upgrades nível 9+
    - `HasExclusiveUnlocked()` - Verifica se upgrade exclusivo está disponível

### 3. Implementações Específicas
Cada tipo de item herda de `ItemBehaviour` e implementa os métodos abstratos conforme sua funcionalidade:
- **Armas**: `Assets/Scripts/Weapons/WeaponBehaviour.cs`
- **Consumíveis**: `Assets/Scripts/Items/Medkit.cs`, `Grenade.cs`
- **Buildables**: `Assets/Scripts/Items/Barricade.cs`, `ExplosiveBarrel.cs`, `BearTrap.cs`
- **Equipamento Passivo**: `Assets/Scripts/Items/Vest.cs` (não faz parte da seleção 1-8)

## Fluxo de Execução da Seleção

### 1. Detecção de Input
```mermaid
graph TD
    A[Tecla 1-8 Pressionada] --> B[Input System]
    B --> C[Inventory.OnSelectItem(InputAction.CallbackContext)]
    C --> D{Fase = Performed?}
    D -->|Não| E[Ignorar]
    D -->|Sim| F{Character em modo de interface?}
    F -->|Sim| E
    F -->|Não| G[Selecionar por número da tecla]
```

### 2. Processamento da Seleção
```mermaid
graph TD
    G --> H[Extrair número da tecla do caminho do Input]
    H --> I[Converter para índice (1→0, 2→1, etc.)]
    I --> J[Validar índice no dicionário keyToIndex]
    J --> K[Verificar se índice está dentro dos limites do array]
    K --> L[Obter ItemBehaviour alvo do array]
    L --> M[Log de diagnóstico: tentativa de seleção]
    M --> N{Índice = 0 (primeiro item/pistola)?}
    N -->|Sim| O[Permitir seleção sempre]
    N -->|Não| P[Verificar CanBeUsed() do item]
    P -->|Falso| Q[Bloquear seleção + log de aviso]
    P -->|Verdadeiro| R[Aceitar seleção]
```

### 3. Ciclo de Vida da Seleção
```mermaid
graph TD
    R --> S[Deselecionar item atual (se existir)]
    S --> T[Chamar OnDeselected() no item atual]
    T --> U[Desativar GameObject do item atual]
    U --> V[Selecionar novo item]
    V --> W[Definir currentlySelected e currentSelectionIndex]
    W --> X[Ativar GameObject do novo item]
    X --> Y[Chamar OnSelected() no novo item]
    Y --> Z[Se for arma: atualizar referência no Character]
```

### 4. Uso do Item (Após Seleção)
```mermaid
graph TD
    AA[Botão de fogo pressionado] --> BB[Inventory não processa diretamente]
    BB --> CC[Character processa input de fogo]
    CC --> DD[Character chama método de uso do item atualmente equipado]
    DD --> EE[Chama OnUse() no ItemBehaviour atualmente selecionado]
    EE --> FF{Item pode ser usado? (CanBeUsed() + recursos)}
    FF -->|Falso| GG[Falha silenciosa ou feedback visual]
    FF -->|Verdadeiro| HH[Executar lógica específica do item]
    HH --> II[Consumir recursos se aplicável (munição, quantidade)]
```

## Pontos de Consistência e Design

### ✅ Aspectos Consistentes
1. **Interface Uniforme**: Todos os items, independentemente de tipo, respondem exatamente aos mesmos eventos de ciclo de vida (`OnSelected`, `OnDeselected`, `OnUse`)
2. **Validação Centralizada**: 
   - `CanBeUsed()` verifica apenas desbloqueio (para seleção)
   - Verificação de recursos (munição/quantidade) ocorre em `OnUse()` (para uso real)
   - Isso permite selecionar items desbloqueados mesmo sem recursos atuais
3. **Lifecycle Padronizado**: A sequência de desseleção → seleção é sempre executada na mesma ordem
4. **Compatibilidade Mantida**: Sistema de armas existente continua funcionando através de atualizações de referência no `Character`
5. **Extensibilidade Clarificada**: Novos tipos de item apenas precisam herdar de `ItemBehaviour` e implementar os métodos abstratos

### 🔒 Salvaguardas Implementadas
- **Primeiro Item Sempre Disponível**: Índice 0 (pistola) pode ser selecionado mesmo durante inicialização para garantir que o jogador nunca fique sem arma
- **Logs de Diagnóstico**: Mensagens detalhadas em cada etapa crítica para facilitar depuração
- **Validação de Limites**: Verificações rigorosas de índices arrays e referências nulas
- **Separação de Responsabilidades**: 
  - `Inventory`: Gerencia seleção e estado
  - `ItemBehaviour` implementations: Contém lógica específica de cada tipo de item
  - `Character`: Mantém compatibilidade com sistemas legados de armas

### 📌 Observações Importantes
- **Limite de 8 Items**: O sistema suporta exatamente 8 slots (teclas 1-8), conforme documentado no Inventory.cs
- **Exceção do Colete (Vest)**: O colete é equipado automaticamente e não participa da seleção 1-8 (linhas 13-14 do Inventory.cs)
- **Source of Truth**: Dados dos items vêm exclusivamente de ScriptableObjects (SOs), evitando duplicação de dados
- **Sistema Exclusivo**: Upgrades nível 9+ ativam lógica alternativa através de `OnUseExclusive()` e `HasExclusiveUnlocked()`

## Exemplos de Implementação

### Medkit.cs (Consumível)
```csharp
public override bool CanBeUsed() {
    // Apenas verifica se desbloqueado - quantidade verificada em OnUse()
    return PlayerProgress.Instance.IsItemUnlocked(GetItemID());
}

public override void OnUse() {
    if (!CanBeUsed()) return;
    // Cura o jogador e consome 1 medkit
    PlayerHealth playerHealth = GetComponentInParent<PlayerHealth>();
    playerHealth.Heal(medkitData.healAmount);
    PlayerProgress.Instance.ConsumeItem(GetItemID(), 1);
}
```

### WeaponBehaviour.cs (Arma)
```csharp
public override bool CanBeUsed() {
    // Verifica se arma está desbloqueada
    return PlayerProgress.Instance.IsItemUnlocked(GetItemID());
}

public override void OnUse() {
    if (!CanBeUsed()) return;
    // Lógica de disparo, consumo de munição, etc.
    FireWeapon();
}
```

## Conclusão
O sistema de seleção de items no projeto Deadzone demonstra **alta consistência arquitetural** através de:
1. Abstração bem definida via `ItemBehaviour`
2. Fluxo de execução padronizado e previsível
3. Separação clara de responsabilidades entre gerenciamento de seleção e lógica específica de items
4. Mecanismos de segurança e diagnósticos robustos
5. Design extensível que accommoda novos tipos de item sem modificações no núcleo do sistema

Esta abordagem reduz significativamente a complexidade de manutenção e minimiza riscos de inconsistências ao adicionar novos conteúdos ao jogo.