# Refatoração do Sistema de Shop UI

## Visão Geral (Atualizado 06/05/2026)

Esta refatoração implementou um sistema dinâmico de stats para a loja do jogo, permitindo que cada tipo de item mostre seus próprios stats específicos na interface. O sistema foi atualizado para usar barras horizontais com 3 camadas (background, upgrade, current).

---

## Arquitetura - Responsabilidades de Cada Script

### 1. `ShopItemDataSO.cs` (ScriptableObject)
**Responsabilidade:** Armazenar dados de configuração de compra/unlock de itens.

**Campos principais:**
- `unlockCost` - Custo para desbloquear o item
- `baseUpgradeCost` - Custo base para upgrades
- `costPerPurchase` - Custo por compra de munição/quantidade
- `quantityPerPurchase` - Quantidade adicionada por compra
- `maxReserveQuantity` - Limite máximo que o jogador pode carregar

---

### 2. `ItemDataSO.cs` (Classe Abstrata)
**Responsabilidade:** Classe base abstrata para todos os dados de itens. Define a interface para stats.

**Métodos abstratos:**
- `GetStatLabels()` - Retorna labels das stats (ex: "Damage", "Fire Rate")
- `GetStatValues()` - Retorna valores das stats no nível 1
- `GetStatValues(int level)` - Retorna valores das stats no nível especificado

---

### 3. `WeaponDataSO.cs`
**Responsabilidade:** Dados específicos de armas.

**Stats disponíveis:** Damage, Fire Rate, Ammo, Crit

**Campos adicionados:**
- `baseCritChance` - Chance de crítico base (0-100%)
- `critChanceScaling` - quanto aumenta por nível

**Métodos:**
- `GetCritChanceAtLevel(int level)` - Calcula chance de crítico no nível

---

### 4. `VestDataSO.cs`
**Responsabilidade:** Dados específicos do colete.

**Stats disponíveis:** Resistance

---

### 5. `MedkitDataSO.cs`
**Responsabilidade:** Dados específicos do kit médico.

**Stats disponíveis:** Heal, Heal Speed, Ammo

---

### 6. `GrenadeDataSO.cs`
**Responsabilidade:** Dados específicos de granadas.

**Stats disponíveis:** Damage, Radius, Ammo

---

### 7. `BuildableDataSO.cs`
**Responsabilidade:** Dados de itens construíveis (barricadas, bear traps, barris).

**Stats disponíveis:** Damage, Resistance, Ammo, Explosion Radius

**Mudanças:**
- `length` removido
- `health` renomeado para `resistance`

---

### 8. `ShopUI.cs`
**Responsabilidade:** Gerencia toda a interface da loja, seleção de itens, compra e exibição de stats.

**Sistema de Stats Dinâmicos:**
- `statsContainer` - Panel onde as barras de stats serão instanciadas
- `statBlockPrefab` - Prefab do StatBlockDisplay que será clonado para cada stat
- `activeStatBlocks` - Lista dos StatBlockDisplay ativos (para destruir ao trocar de item)

**Método principal:**
```csharp
private void BuildDynamicStats() {
    // 1. Limpa stats anteriores
    ClearStatBlocks();
    
    // 2. Pega labels e valores do ItemData do item selecionado
    string[] labels = itemData.GetStatLabels();
    float[] currentValues = itemData.GetStatValues(currentLevel);
    float[] nextValues = itemData.GetStatValues(nextLevel);
    
    // 3. Para cada stat, cria um StatBlockDisplay
    for (int i = 0; i < labels.Length; i++) {
        StatBlockDisplay block = Instantiate(statBlockPrefab, statsContainer);
        block.SetMaxStatValue(WeaponStatsCalculator.STAT_BARS);
        block.SetStatValues(currentValues[i], nextValues[i]);
    }
}
```

**Mudanças Implementadas:**
1. Removido `currentAmmoText` - não é mais necessário
2. Removida lista `itemPricingConfigs` - agora usa dados diretos do ShopItemDataSO
3. Removido `ItemPricingConfig` struct
4. Sistema dinâmico de stats substituindo os 3 fixos (damage, fireRate, ammo)

---

### 9. `StatBarDisplay.cs` (NOVO -取代 StatBlockDisplay)
**Responsabilidade:** Exibe uma linha de stat com 3 barras horizontais sobrepostas:
- **BackgroundBar** (cinza): sempre 100% - representa o limite máximo
- **UpgradeBar** (verde): fillAmount = upgrade / max - mostra o valor após upgrade
- **CurrentBar** (ciano): fillAmount = current / max - mostra o valor atual

**Recursos:**
- Label do stat (TextMeshPro)
- Ícone do stat (Image) - ícones automáticos por tipo de stat
- 3 cores configuráveis no Inspector

**Métodos públicos:**
- `Setup(label, maxValue)`: configura label e valor máximo
- `SetValues(current, upgrade)`: atualiza as barras

---

### 10. `WeaponStatsCalculator.cs`
**Responsabilidade:** Fornece constantes MAX para normalização e método genérico.

**Constantes (valores baseados nos assets):**
- `MAX_DAMAGE_WEAPON = 44f`
- `MAX_FIRE_RATE = 500f`
- `MAX_AMMO_WEAPON = 33f`
- `MAX_CRIT = 25f`
- `MAX_HEAL = 110f`
- `MAX_GRENADE_DAMAGE = 55f`
- `MAX_GRENADE_RADIUS = 22f`
- `MAX_VEST_RESISTANCE = 130f`
- `MAX_BUILDABLE_DAMAGE = 110f`
- `MAX_BUILDABLE_RESISTANCE = 110f`

**Métodos:**
- `GetMaxValueForStat(statName)`: retorna o MAX correto baseado no nome do stat
- `Normalize(value, maxValue)`: retorna 0-1 para fillAmount

---

## Fluxo de Dados - Como os Stats Chegam à UI (Atualizado)

```
1. Jogador clica em um card (ShopItemCard)
         ↓
2. ShopUI.HandleCardSelected(itemData)
         ↓
3. ShopUI.UpdateSelectedItemInfo()
         ↓
4. ShopUI.BuildDynamicStats()
         ├─→ Pega itemData.GetStatLabels() → ["Damage", "Fire Rate", "Ammo", "Crit"]
         ├─→ Pega itemData.GetStatValues(currentLevel) → [15.5, 2.5, 30, 7]
         ├─→ Pega itemData.GetStatValues(nextLevel) → [17, 2.7, 33, 8]
         │
         ↓
5. Para cada stat, cria StatBarDisplay:
         ├─→ WeaponStatsCalculator.GetMaxValueForStat("Damage") → 44
         ├─→ StatBarDisplay.Setup("Damage", 44)
         └─→ StatBarDisplay.SetValues(15.5, 17)
                  ↓
6. StatBarDisplay desenha:
         - BackgroundBar: fill 100% (cinza)
         - UpgradeBar: fill 17/44 = 38% (verde)
         - CurrentBar: fill 15.5/44 = 35% (ciano)
```

---

## Sistema de Preços - Como Funciona

### Antes (dualidade):
- `ShopItemDataSO` tinha: ammoCost, ammoAmountPerPurchase
- `ShopUI` tinha: Lista `itemPricingConfigs` com dados duplicados

### Agora (unificado):
- Tudo está no `ShopItemDataSO`:
  - `CostPerPurchase` - preço por compra
  - `QuantityPerPurchase` - quantidade comprada
  - `MaxReserveQuantity` - limite máximo

### Compra de Munição:
```csharp
// No ShopUI.OnAmmoButtonPressed()
int cost = selectedItemData.CostPerPurchase;
int quantity = selectedItemData.QuantityPerPurchase;
int maxAmount = selectedItemData.MaxReserveQuantity;
```

---

## Stats por Tipo de Item (Atualizado)

| Tipo de Item | Stats Exibidas |
|--------------|----------------|
| **Armas (1,2,3)** | Damage, Fire Rate, Ammo, Crit |
| **Vest** | Resistance |
| **Barricade** | Resistance, Ammo (configurável via displayStats) |
| **Bear Trap** | Damage, Ammo (configurável via displayStats) |
| **Explosive Barrel** | Damage, Radius, Ammo (configurável via displayStats) |
| **Grenade** | Damage, Radius, Ammo |
| **Medkit** | Heal, Ammo (Heal Speed removido) |

---

## Como Configurar no Editor (Atualizado)

### 1. Criar Prefab do StatBarDisplay:
- Criar GameObject vazio na Hierarchy
- Adicionar componente `StatBarDisplay`
- Adicionar filho `StatLabel` (TextMeshProUGUI)
- Adicionar filho `StatIcon` (Image)
- Adicionar filho `BarContainer` (RectTransform)
  - Adicionar `BackgroundBar` (Image, Type: Filled, Fill Method: Horizontal)
  - Adicionar `UpgradeBar` (Image, Type: Filled, Fill Method: Horizontal)
  - Adicionar `CurrentBar` (Image, Type: Filled, Fill Method: Horizontal)
- Arraste as referências no Inspector do StatBarDisplay
- Configure os ícones (stat icons) no Inspector
- Arraste para Assets/Prefabs/UI para criar prefab

### 2. Configurar ShopUI:
- No objeto com script ShopUI:
  - `Stats Container`: Panel vazio onde barras aparecerão
  - `Stat Bar Prefab`: Prefab StatBarDisplay criado acima

### 3. Configurar BuildableDataSO:
- Cada buildable agora tem campo `Display Stats` (array de BuildableStatType)
- Selecione quais stats mostrar:
  - Barricade: Resistance, Ammo
  - Bear Trap: Damage, Ammo
  - Explosive Barrel: Damage, Radius, Ammo

### 4. Configurar WeaponDataSO existentes:
- Já possui baseCritChance (padrão 5f)
- Para customize, edite os assets diretamente

---

## Evolução Futura (Possíveis Melhorias)

1. **Ícones customizados por stat:** O StatBarDisplay já suporta ícones,configure os sprites no Inspector.

2. **Preview 3D:** O código ainda tem lógica de preview 3D no ShopUI. Pode ser movido para outro script (WeaponPreviewHandler).

3. **Classe base para todos os DataSO com nível:** O método GetStatValues(int level) usa clamping simples. Pode-se melhorar para usar o MaxUpgradeLevel do item dinamicamente.