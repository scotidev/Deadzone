using UnityEngine;

// ==============================================================
//  O QUE É [System.Serializable]?
// ==============================================================
//  Marca esta classe para que a Unity consiga exibi-la e editar seus
//  campos no Inspector quando ela for usada como item de uma lista
//  ([SerializeField] private List<EnemySpawnConfig> enemyTypes).
//
//  Sem [System.Serializable], a Unity não sabe como serializar
//  (salvar/carregar/mostrar) a classe e o Inspector fica vazio.

/// <summary>
/// Define um tipo de inimigo que pode ser spawnado durante as ondas.
/// Configure no Inspector do WaveManager: prefab, onda mínima e peso de spawn.
///
/// O peso de spawn é relativo — não precisa somar 100%.
/// Exemplo de configuração equilibrada:
///   ZombieDefault  → weight = 5.0  (mais comum)
///   ZombieFast     → weight = 3.0  (frequência média)
///   ZombieTank     → weight = 1.0  (raro)
///   ZombieBoss     → weight = 0.5  (muito raro, futuro)
/// </summary>
[System.Serializable]
public class EnemySpawnConfig {

    [Tooltip("Prefab do inimigo. Deve ter os componentes Enemy, EnemyFollow, EnemyAttack e NavMeshAgent.")]
    public GameObject prefab;

    // ==============================================================
    //  ONDA MÍNIMA
    // ==============================================================
    //  Determina a partir de qual onda este inimigo pode aparecer.
    //  O WaveManager filtra automaticamente os tipos disponíveis
    //  comparando minimumWave com o número da onda atual.
    //  Ex: minimumWave = 4 → só aparece na onda 4, 5, 6...

    [Tooltip("A partir de qual onda este inimigo começa a aparecer.\n" +
             "Ex: 1 = onda 1+.  2 = onda 2+.  4 = onda 4+.")]
    [Min(1)]
    public int minimumWave = 1;

    // ==============================================================
    //  PESO DE SPAWN (Weighted Random)
    // ==============================================================
    //  O peso NÃO é percentagem direta — é uma influência relativa.
    //  O EnemySpawner soma todos os pesos disponíveis e sorteia um
    //  número aleatório nessa faixa.
    //
    //  Exemplo com 3 inimigos (pesos 5, 3, 1 → total = 9):
    //    roll 0.0–5.0 → ZombieDefault  (5/9 = ~55% de chance)
    //    roll 5.0–8.0 → ZombieFast     (3/9 = ~33% de chance)
    //    roll 8.0–9.0 → ZombieTank     (1/9 = ~11% de chance)
    //
    //  Para inimigos raros (Boss), use valores como 0.3 ou 0.5.
    //  Para inimigos comuns, use valores como 5.0 ou 10.0.

    [Tooltip("Peso de spawn relativo. Maior = mais frequente.\n" +
             "Sugestão: Default=5, Fast=3, Tank=1, Boss=0.5")]
    [Range(0.01f, 20f)]
    public float spawnWeight = 1f;
}
