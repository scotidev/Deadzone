using System.Collections.Generic;
using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  WaveManager é o "maestro" do sistema de hordas. Ele coordena:
//    1. O início de cada onda (quando o jogador interage com WaveButton).
//    2. Quantos inimigos spawnar e de quais tipos.
//    3. A distribuição de inimigos entre os spawners da cena.
//    4. A contagem de mortes (ouvindo o evento Enemy.OnAnyEnemyDied).
//    5. O fim da onda (quando todos morreram) e a liberação do botão.
//
//  PADRÃO SINGLETON:
//  "public static WaveManager Instance" garante que exista apenas
//  uma instância. Outros scripts acessam via WaveManager.Instance
//  sem precisar de referência arrastada no Inspector.

/// <summary>
/// Manager singleton responsável por todo o ciclo de vida das ondas:
///   1. Jogador interage com WaveButton → StartNextWave() é chamado.
///   2. Calcula quantos inimigos e quais tipos estão disponíveis.
///   3. Distribui os inimigos entre os EnemySpawners da cena.
///   4. Ouve Enemy.OnAnyEnemyDied para contar mortes.
///   5. Quando o último inimigo morre, encerra a onda.
///
/// REGRAS DE ESCALONAMENTO:
///   Onda 1          → sempre 5 inimigos, só ZombieDefault.
///   Ondas 2–3       → +12–16% vs onda anterior, ZombieDefault + ZombieFast.
///   Onda 4 em diante → +12–16% vs onda anterior, todos os três tipos.
/// </summary>
public class WaveManager : MonoBehaviour {

    // ==============================================================
    //  SINGLETON
    // ==============================================================
    //  "{ get; private set; }" = qualquer script pode LER (get público)
    //  mas só WaveManager pode ESCREVER (set privado).
    //  Inicializado em Awake(), nulo antes disso.
    public static WaveManager Instance { get; private set; }

    // ==============================================================
    //  CAMPOS SERIALIZADOS — configurar no Inspector
    // ==============================================================

    [Header("Prefabs de Inimigos")]
    [Tooltip("Prefab do ZombieDefault — disponível desde a onda 1.")]
    [SerializeField] private GameObject zombieDefaultPrefab;

    [Tooltip("Prefab do ZombieFast — desbloqueado a partir da onda 2.")]
    [SerializeField] private GameObject zombieFastPrefab;

    [Tooltip("Prefab do ZombieTank — desbloqueado a partir da onda 4.")]
    [SerializeField] private GameObject zombieTankPrefab;

    [Header("Spawners da Cena")]
    [Tooltip("Arraste aqui TODOS os GameObjects com EnemySpawner da cena.")]
    [SerializeField] private List<EnemySpawner> spawners;

    [Header("HUD")]
    [Tooltip("Referência ao componente WaveUI na cena.")]
    [SerializeField] private WaveUI waveUI;

    // ==============================================================
    //  ESTADO DA ONDA
    // ==============================================================

    private int  currentWave        = 0;   // Onda atual (0 = nenhuma iniciada)
    private int  enemiesAlive       = 0;   // Contagem de vivos nesta onda
    private int  lastWaveEnemyCount = 5;   // Inimigos da onda anterior (para calcular escalonamento)
    private bool isWaveActive       = false; // Trava o botão durante a onda

    // ==============================================================
    //  PROPRIEDADES PÚBLICAS (somente leitura)
    // ==============================================================
    //  "=>" é uma expression body — atalho para { return ...; }
    //  Outros scripts (WaveButton) leem IsWaveActive para decidir
    //  se permitem nova interação.

    /// <summary>True enquanto uma onda está em andamento.</summary>
    public bool IsWaveActive => isWaveActive;

    /// <summary>Número da onda atual (0 = nenhuma iniciada ainda).</summary>
    public int CurrentWave => currentWave;

    // ==============================================================
    //  UNITY — AWAKE / ONENABLE / ONDISABLE
    // ==============================================================

    private void Awake() {
        // Lógica Singleton: se não existe instância, eu sou ela.
        // Se já existe outra, me destruo para não duplicar.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable() {
        // ==============================================================
        //  INSCRIÇÃO EM EVENTO (subscribe)
        // ==============================================================
        //  "+=" adiciona HandleEnemyDied à lista de ouvintes do evento.
        //  Toda vez que qualquer Enemy morrer e disparar OnAnyEnemyDied,
        //  a Unity chamará automaticamente HandleEnemyDied() aqui.
        //
        //  Por que em OnEnable e não em Awake?
        //  OnEnable/OnDisable são PARES garantidos: sempre que o objeto
        //  for ativado/desativado, OnEnable/OnDisable rodam em sequência.
        //  Isso garante que nunca esquecemos de cancelar a inscrição.
        Enemy.OnAnyEnemyDied += HandleEnemyDied;
    }

    private void OnDisable() {
        // ==============================================================
        //  CANCELAR INSCRIÇÃO (unsubscribe)
        // ==============================================================
        //  "-=" remove HandleEnemyDied da lista de ouvintes.
        //  OBRIGATÓRIO: sem isso, quando o WaveManager for destruído,
        //  o evento ainda tentaria chamar HandleEnemyDied num objeto
        //  que não existe mais → MissingReferenceException.
        Enemy.OnAnyEnemyDied -= HandleEnemyDied;
    }

    // ==============================================================
    //  API PÚBLICA — chamada pelo WaveButton
    // ==============================================================

    /// <summary>
    /// Inicia a próxima onda de inimigos.
    /// Chamado por WaveButton.Interact() quando o jogador pressiona E.
    /// Bloqueia se uma onda já estiver em andamento.
    /// </summary>
    public void StartNextWave() {
        // Guard: bloqueia chamadas duplicadas enquanto a onda está ativa.
        if (isWaveActive) {
            Debug.LogWarning("[WaveManager] StartNextWave() chamado durante onda ativa. Ignorado.");
            return;
        }

        // Verifica se há spawners configurados antes de continuar.
        if (spawners == null || spawners.Count == 0) {
            Debug.LogError("[WaveManager] Nenhum EnemySpawner atribuído! Configure no Inspector.");
            return;
        }

        // Incrementa o número da onda.
        currentWave++;

        // Calcula quantos inimigos esta onda deve ter.
        int totalEnemies  = GetEnemyCountForWave(currentWave);
        lastWaveEnemyCount = totalEnemies; // Salva para usar no cálculo da próxima onda.
        enemiesAlive      = totalEnemies;
        isWaveActive      = true;

        // Notifica o GameManager do novo estado (bloqueia loja, etc.).
        // "?." = null-conditional: só chama se GameManager.Instance existir.
        GameManager.Instance?.SetState(GameState.InWave);

        // Determina quais tipos de inimigo estão disponíveis nesta onda.
        List<GameObject> availablePrefabs = GetAvailablePrefabs(currentWave);

        // Divide os inimigos entre todos os spawners e os instancia.
        DistributeEnemiesAcrossSpawners(availablePrefabs, totalEnemies);

        // Atualiza o HUD com as informações da nova onda.
        if (waveUI != null) {
            waveUI.UpdateWaveNumber(currentWave);
            waveUI.UpdateEnemiesRemaining(enemiesAlive);
            waveUI.SetStatus($"Wave {currentWave} — Sobreviva!");
        }

        // ==============================================================
        //  Debug.Log()
        // ==============================================================
        //  Imprime uma mensagem no Console da Unity.
        //  Útil durante desenvolvimento para rastrear o que acontece.
        //  Debug.Log    = informação (texto branco)
        //  Debug.LogWarning = aviso (texto amarelo)
        //  Debug.LogError   = erro   (texto vermelho, não quebra o jogo)
        Debug.Log($"[WaveManager] Onda {currentWave} iniciada — {totalEnemies} inimigos em {spawners.Count} spawner(s).");
    }

    // ==============================================================
    //  DISTRIBUIÇÃO ENTRE SPAWNERS
    // ==============================================================

    /// <summary>
    /// Divide o total de inimigos igualmente entre todos os spawners.
    /// O resto da divisão inteira vai para o primeiro spawner.
    ///
    /// Exemplo: 11 inimigos, 3 spawners → 4 / 3 / 4 inimigos.
    /// (4 no primeiro absorve o resto de 11 % 3 = 2 → 3+2 = 5... )
    /// Exemplo correto: base = 11/3 = 3, resto = 11%3 = 2
    ///   Spawner 0 → 3 + 2 = 5
    ///   Spawner 1 → 3
    ///   Spawner 2 → 3
    /// </summary>
    private void DistributeEnemiesAcrossSpawners(List<GameObject> prefabs, int totalEnemies) {
        // Divisão inteira: quantos inimigos cada spawner recebe no mínimo.
        int baseCount = totalEnemies / spawners.Count;
        // Resto da divisão: os "sobras" vão para o primeiro spawner.
        int remainder = totalEnemies % spawners.Count;

        for (int i = 0; i < spawners.Count; i++) {
            // O operador ternário "condição ? seTrue : seFalse"
            // adiciona o resto apenas ao spawner índice 0.
            int countForThisSpawner = baseCount + (i == 0 ? remainder : 0);

            if (countForThisSpawner > 0)
                spawners[i].SpawnEnemies(prefabs, countForThisSpawner);
        }
    }

    // ==============================================================
    //  CALLBACK DE MORTE DE INIMIGO
    // ==============================================================

    /// <summary>
    /// Chamado automaticamente toda vez que qualquer Enemy na cena morre.
    /// Assinado ao evento Enemy.OnAnyEnemyDied em OnEnable().
    /// </summary>
    private void HandleEnemyDied() {
        // ==============================================================
        //  Mathf.Max(a, b)
        // ==============================================================
        //  Decrementa enemiesAlive mas nunca deixa ir abaixo de 0.
        //  Sem isso, se HandleEnemyDied fosse chamado mais vezes que
        //  o esperado, poderíamos ter valores negativos.
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        // Atualiza o contador na HUD em tempo real.
        if (waveUI != null)
            waveUI.UpdateEnemiesRemaining(enemiesAlive);

        // Quando todos morreram, encerra a onda.
        if (enemiesAlive <= 0)
            OnWaveCompleted();
    }

    /// <summary>
    /// Chamado quando o último inimigo da onda morre.
    /// Libera o WaveButton e atualiza o estado do jogo.
    /// </summary>
    private void OnWaveCompleted() {
        isWaveActive = false;

        // Retorna ao estado Playing para que a loja volte a funcionar.
        GameManager.Instance?.SetState(GameState.Playing);

        if (waveUI != null)
            waveUI.SetStatus($"Wave {currentWave} concluída! Interaja com o Wave Button para continuar.");

        Debug.Log($"[WaveManager] Onda {currentWave} finalizada!");
    }

    // ==============================================================
    //  CÁLCULOS DE ONDA
    // ==============================================================

    /// <summary>
    /// Retorna a quantidade de inimigos para a onda informada.
    ///
    /// Onda 1 → sempre 5 (fixo, amigável para tutorial).
    /// Onda N → Ceil(lastWaveCount * Random(1.12, 1.16))
    ///          = 12% a 16% mais que a onda anterior.
    ///
    /// Mathf.CeilToInt arredonda PARA CIMA. Exemplo:
    ///   5 * 1.13 = 5.65 → CeilToInt → 6 inimigos.
    ///   Nunca perde fração — sempre arredonda para mais.
    /// </summary>
    private int GetEnemyCountForWave(int wave) {
        if (wave == 1) return 5;

        // ==============================================================
        //  Random.Range(float, float)
        // ==============================================================
        //  Com floats, o Range é INCLUSIVO nos dois lados.
        //  Retorna um valor decimal aleatório entre 1.12 e 1.16.
        //  Isso corresponde a +12% a +16% de crescimento.
        float scalingMultiplier = Random.Range(1.12f, 1.16f);
        return Mathf.CeilToInt(lastWaveEnemyCount * scalingMultiplier);
    }

    /// <summary>
    /// Retorna a lista de prefabs de inimigos permitidos nesta onda.
    ///
    /// Onda 1   → só ZombieDefault.
    /// Onda 2–3 → ZombieDefault + ZombieFast.
    /// Onda 4+  → ZombieDefault + ZombieFast + ZombieTank.
    ///
    /// O EnemySpawner escolhe aleatoriamente entre os prefabs desta lista
    /// para cada inimigo que instancia.
    /// </summary>
    private List<GameObject> GetAvailablePrefabs(int wave) {
        var prefabs = new List<GameObject>();

        // ZombieDefault sempre disponível (desde onda 1).
        if (zombieDefaultPrefab != null)
            prefabs.Add(zombieDefaultPrefab);

        // ZombieFast desbloqueado a partir da onda 2.
        if (wave >= 2 && zombieFastPrefab != null)
            prefabs.Add(zombieFastPrefab);

        // ZombieTank desbloqueado a partir da onda 4.
        if (wave >= 4 && zombieTankPrefab != null)
            prefabs.Add(zombieTankPrefab);

        if (prefabs.Count == 0)
            Debug.LogError("[WaveManager] Nenhum prefab de inimigo atribuído! Configure no Inspector.");

        return prefabs;
    }
}
