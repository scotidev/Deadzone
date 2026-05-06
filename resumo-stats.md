# Resumo: Sistema de Bars de Stats na ShopUI

## Visão Geral

Este documento detalha a implementação do sistema de exibição de estatísticas dinâmicas na interface da loja (ShopUI). O sistema mostra barras horizontais para cada stat do item selecionado, com visualização do valor atual e do valor após upgrade.

---

## Problema Original

O sistema anterior usava `StatBlockDisplay` que renderizava 5 bloquinhos por stat. O objetivo era substituir por barras horizontais simples (uma para valor atual, uma para upgrade).

---

## Arquivos Modificados/Criados

### 1. Scripts Novos

#### `Assets/Scripts/UI/StatBarDisplay.cs` (NOVO)
Responsável por renderizar uma linha de stat com 3 barras sobrepostas:
- **BackgroundBar** (cinza): sempre 100% - representa o limite máximo
- **CurrentBar** (ciano): fillAmount = current/max - mostra o valor atual
- **UpgradeBar** (verde): fillAmount = upgrade/max - mostra o valor após upgrade

**Campos principais:**
- `statLabel`: TextMeshProUGUI para exibir o nome do stat (dinâmico)
- `statIcon`: Image para exibir ícone do stat (automático por tipo)
- `backgroundBar`, `currentBar`, `upgradeBar`: Images com tipo Filled
- Campos de ícones: `iconDamage`, `iconFireRate`, `iconAmmo`, etc.

**Métodos públicos:**
- `Setup(string label, float maxValue, int slotIndex)`: configura label e valor máximo
- `SetValues(float current, float upgrade)`: atualiza as barras

---

### 2. Scripts Modificados

#### `Assets/Scripts/UI/ShopUI.cs`
Atualizado para usar o novo sistema de barras.

**Mudanças:**
- Campo `statBarPrefab` mudou de `StatBarDisplay` para `GameObject`
- Método `BuildDynamicStats()` reescrito para:
  - Limpar filhos antigos do container antes de criar novos
  - Configurar RectTransform das barras (anchor, pivot, size)
  - Usar posições fixas via VerticalLayoutGroup

```csharp
// Configuração do RectTransform para cada barra
rt.anchorMin = new Vector2(0f, 1f);  // Top-left
rt.anchorMax = new Vector2(1f, 1f);  // Top-right
rt.pivot = new Vector2(0.5f, 1f);   // Top-center
rt.sizeDelta = new Vector2(0f, 30f); // Altura fixa 30
```

---

#### `Assets/Scripts/Items/ScriptableObjects/MedkitDataSO.cs`
- Removido campo `healSpeed`
- Labels agora são: `["Heal", "Ammo"]` (não mais 3)
- Adicionado `healScaling` para upgrades

---

#### `Assets/Scripts/Items/ScriptableObjects/BuildableDataSO.cs`
- Adicionado enum `BuildableStatType` (Damage, Resistance, Ammo, Radius)
- Adicionado campo `displayStats` (array) - permite selecionar quais stats mostrar
- Adicionado `damageScaling` e `resistanceScaling` para upgrades

---

#### `Assets/Scripts/Utilities/WeaponStatsCalculator.cs`
Reescrito para fornecer constantes MAX para normalização:

```csharp
// Armas
MAX_DAMAGE_WEAPON = 44f
MAX_FIRE_RATE = 500f
MAX_AMMO_WEAPON = 33f
MAX_CRIT = 25f

// Medkit
MAX_HEAL = 110f

// Grenade
MAX_GRENADE_DAMAGE = 55f
MAX_GRENADE_RADIUS = 22f

// Vest
MAX_VEST_RESISTANCE = 130f

// Buildables
MAX_BUILDABLE_DAMAGE = 110f
MAX_BUILDABLE_RESISTANCE = 110f
MAX_BUILDABLE_RADIUS = 55f

// Método principal
GetMaxValueForStat(string statName)
```

---

### 3. Prefab Criado

#### `Assets/Prefabs/UI/StatBarDisplay.prefab`
Estrutura recomendada:

```
StatBarDisplay (GameObject com script)
├── StatLabel (TextMeshProUGUI)
├── StatIcon (Image)
└── BarContainer (RectTransform)
    ├── BackgroundBar (Image) - Type: Filled, Fill Method: Horizontal
    ├── CurrentBar (Image)    - Type: Filled, Fill Method: Horizontal  
    └── UpgradeBar (Image)     - Type: Filled, Fill Method: Horizontal
```

**Configuração no Inspector do StatBarDisplay:**
- `Stat Label`: referência ao TextMeshProUGUI
- `Stat Icon`: referência ao Image do ícone
- `Background Bar`: referência ao Image de background
- `Current Bar`: referência ao Image atual (ciano)
- `Upgrade Bar`: referência ao Image de upgrade (verde)
- `Bar Height`: 30 (padrão)
- `Bar Spacing`: (não usado mais, via VerticalLayoutGroup)

---

### 4. Configuração na Cena

#### StatsContainer (painel no ShopPanel)
Deve ter:
- **Vertical Layout Group** componente
  - Padding: 0
  - Spacing: 10
  - Child Force Expand → Width: true, Height: false
  - Control Child Size → Width: true, Height: true

---

## Stats por Tipo de Item

| Tipo de Item | Stats Exibidas |
|--------------|----------------|
| **Armas (Pistol, AK47, Shotgun)** | Damage, Fire Rate, Ammo, Crit |
| **Vest** | Resistance |
| **Medkit** | Heal, Ammo |
| **Grenade** | Damage, Radius, Ammo |
| **Barricade** | Resistance, Ammo (configurável via displayStats) |
| **Bear Trap** | Damage, Ammo (configurável via displayStats) |
| **Explosive Barrel** | Damage, Radius, Ammo (configurável via displayStats) |

---

## Fluxo de Dados

```
1. Jogador clica em um card (ShopItemCard)
         ↓
2. ShopUI.HandleCardSelected(itemData)
         ↓
3. ShopUI.UpdateSelectedItemInfo()
         ↓
4. ShopUI.BuildDynamicStats()
         ├─→ Limpa filhos antigos do statsContainer
         ├─→ Pega itemData.GetStatLabels() → ["Damage", "Fire Rate", "Ammo", "Crit"]
         ├─→ Pega itemData.GetStatValues(currentLevel)
         ├─→ Pega itemData.GetStatValues(nextLevel)
         │
         ↓
5. Para cada stat, cria StatBarDisplay:
         ├─→ Configura RectTransform (anchor top-left)
         ├─→ WeaponStatsCalculator.GetMaxValueForStat("Damage") → 44
         ├─→ StatBarDisplay.Setup("Damage", 44)
         └─→ StatBarDisplay.SetValues(current, upgrade)
```

---

## Como Configurar um Novo Prefab StatBarDisplay

1. **Criar GameObject** vazio na Hierarchy
2. **Adicionar componente** `StatBarDisplay`
3. **Criar filhos:**
   - `StatLabel`: TextMeshProUGUI (para o nome do stat)
   - `StatIcon`: Image (para o ícone)
   - `BarContainer`: RectTransform
     - `BackgroundBar`: Image (Type: Filled, Fill Method: Horizontal)
     - `CurrentBar`: Image (Type: Filled, Fill Method: Horizontal)
     - `UpgradeBar`: Image (Type: Filled, Fill Method: Horizontal)
4. **Configurar Source Image** de cada barra com um sprite qualquer (necessário para ativar tipo Filled)
5. **Arraste referências** no Inspector do StatBarDisplay
6. **Arraste sprites de ícones** nos campos correspondentes
7. **Criar prefab** arrastando para Assets/Prefabs/UI

---

## Problemas Identificados e Soluções

### Problema 1: Barras sobrepostas
- **Causa**: RectTransform com anchor no centro (0.5, 0.5)
- **Solução**: Configurar anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)

### Problema 2: ClearStatBars não funcionava
- **Causa**: Destroy não era instantâneo, objetos permaneciam na hierarchy
- **Solução**: Usar `foreach (Transform child in statsContainer) Destroy(child.gameObject)`

### Problema 3: Height das barras ficava 0
- **Causa**: VerticalLayoutGroup com ChildForceExpandHeight desabilitado
- **Solução**: Configurar sizeDelta no código após instantiate

---

## Futuras Melhorias

1. **Ícones**: Garantir que sprites de ícones estão configurados no prefab
2. **Barras de cores**: O sistema já suporta cores diferentes para current/upgrade
3. **Buildables**: Configurar `displayStats` para cada tipo (Barricade, Bear Trap, Barrel)

---

## Data de Implementação

Maio 2026