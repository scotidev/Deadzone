using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  EnemySpawner é um "ponto de spawn" colocado na cena pelo designer
//  em locais estratégicos (atrás de paredes, entradas de corredor etc.).
//
//  Quando o WaveManager inicia uma onda, ele chama SpawnEnemies()
//  em cada spawner, informando quantos inimigos e quais tipos spawnar.
//
//  Cada inimigo aparece em uma posição aleatória dentro do raio
//  do spawner, em um ponto válido do NavMesh.
//
//  VISUALIZAÇÃO NO EDITOR:
//  Selecione o GameObject do EnemySpawner na cena — aparecerá uma
//  esfera vermelha mostrando o raio de spawn. Use isso para
//  posicionar estrategicamente.

/// <summary>
/// Ponto de spawn de inimigos. Posicionado na cena pelo designer.
/// Recebe do WaveManager a lista de prefabs disponíveis e a
/// quantidade a spawnar, depois instancia os inimigos em posições
/// válidas do NavMesh próximas a si.
/// </summary>
public class EnemySpawner : MonoBehaviour {

    // ==============================================================
    //  CAMPOS SERIALIZADOS — configuráveis no Inspector
    // ==============================================================

    [Header("Configurações de Spawn")]
    [Tooltip("Distância máxima do centro do spawner onde inimigos podem aparecer.")]
    [SerializeField] private float spawnRadius = 3f;

    [Tooltip("Segundos de intervalo entre cada inimigo instanciado no lote. " +
             "Um pequeno delay (0.2–0.5s) evita picos de CPU por muitos Instantiate() simultâneos.")]
    [SerializeField] private float spawnDelay = 0.3f;

    // ==============================================================
    //  API PÚBLICA — chamada pelo WaveManager
    // ==============================================================

    /// <summary>
    /// Spawna EXATAMENTE UM inimigo imediatamente perto deste spawner.
    /// Chamado pelo WaveManager no sistema de throttle: cada vez que
    /// um inimigo morre e abre uma vaga, WaveManager chama este método
    /// no spawner escolhido aleatoriamente.
    ///
    /// Não usa Coroutine — o timing é controlado pelo WaveManager.
    /// </summary>
    public void SpawnSingleEnemy(List<EnemySpawnConfig> availableTypes) {
        if (availableTypes == null || availableTypes.Count == 0) return;

        GameObject prefab = PickWeightedRandom(availableTypes);
        Vector3 spawnPos  = GetValidSpawnPosition();
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    /// <summary>
    /// Spawna um lote completo de inimigos com delay entre cada um.
    /// Método legado — mantido para uso futuro ou debug.
    /// No sistema de throttle, o WaveManager usa SpawnSingleEnemy.
    /// </summary>
    public void SpawnEnemies(List<EnemySpawnConfig> availableTypes, int count) {
        if (availableTypes == null || availableTypes.Count == 0 || count <= 0)
            return;

        StartCoroutine(SpawnRoutine(availableTypes, count));
    }

    // ==============================================================
    //  COROUTINE DE SPAWN
    // ==============================================================
    //  O QUE É UMA COROUTINE?
    //  É um método especial em C# que pode ser PAUSADO e RETOMADO.
    //  O "yield return" é o ponto de pausa — a Unity executa tudo
    //  até o yield, pausa, continua o jogo normal, depois retorna
    //  ao próximo frame (ou após o delay).
    //
    //  Por que usar aqui?
    //  Sem Coroutine, spawnaríamos todos os inimigos no mesmo frame:
    //    → pico de CPU/GPU (todos os Instantiate() de uma vez)
    //    → possível queda de FPS
    //  Com Coroutine + WaitForSeconds, distribuímos o custo.
    //
    //  "IEnumerator" é o tipo de retorno obrigatório para Coroutines.
    //  Não retorna um valor real — é uma convenção do C# para iteradores.

    private IEnumerator SpawnRoutine(List<EnemySpawnConfig> availableTypes, int count) {
        for (int i = 0; i < count; i++) {
            // ==============================================================
            //  SELEÇÃO PONDERADA (Weighted Random)
            // ==============================================================
            //  Em vez de sortear um índice uniforme (todo prefab com a
            //  mesma chance), usamos PickWeightedRandom que considera o
            //  spawnWeight de cada tipo. ZombieTank (weight=1) aparece
            //  muito menos que ZombieDefault (weight=5).
            GameObject prefab  = PickWeightedRandom(availableTypes);
            Vector3 spawnPos   = GetValidSpawnPosition();

            Instantiate(prefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ==============================================================
    //  SELEÇÃO ALEATÓRIA PONDERADA
    // ==============================================================
    //  COMO FUNCIONA:
    //  1. Soma todos os pesos dos tipos disponíveis (ex: 5+3+1 = 9).
    //  2. Sorteia um float entre 0 e totalWeight (ex: 6.7).
    //  3. Percorre a lista acumulando pesos até o acumulado atingir
    //     o valor sorteado.
    //
    //  Exemplo com pesos 5, 3, 1 (total = 9):
    //    roll = 3.2 → Default (0–5): não     Fast (5–8): sim → ZombieFast
    //    roll = 0.5 → Default (0–5): sim → ZombieDefault
    //    roll = 8.5 → Default (0–5): não   Fast (5–8): não   Tank (8–9): sim
    //
    //  O resultado é que a probabilidade de cada tipo é exatamente
    //  proporcional ao seu peso: Default 55%, Fast 33%, Tank 11%.

    private GameObject PickWeightedRandom(List<EnemySpawnConfig> configs) {
        // Passo 1: calcular o peso total de todos os tipos disponíveis.
        float totalWeight = 0f;
        foreach (var config in configs)
            totalWeight += config.spawnWeight;

        // Passo 2: sortear um valor aleatório na faixa [0, totalWeight].
        // Random.Range com floats é inclusivo nos dois lados.
        float roll = Random.Range(0f, totalWeight);

        // Passo 3: percorrer a lista acumulando pesos até encontrar o vencedor.
        float cumulative = 0f;
        foreach (var config in configs) {
            cumulative += config.spawnWeight;
            if (roll <= cumulative)
                return config.prefab;
        }

        // Fallback de segurança: retorna o último da lista se não encontrou.
        // Pode ocorrer por imprecisão de ponto flutuante em casos extremos.
        return configs[configs.Count - 1].prefab;
    }

    // ==============================================================
    //  POSIÇÃO VÁLIDA NO NAVMESH
    // ==============================================================

    /// <summary>
    /// Tenta encontrar uma posição válida no NavMesh dentro do raio de spawn.
    /// Se não encontrar após 10 tentativas, usa a posição do próprio spawner.
    /// </summary>
    private Vector3 GetValidSpawnPosition() {
        for (int attempt = 0; attempt < 10; attempt++) {
            // ==============================================================
            //  Random.insideUnitCircle
            // ==============================================================
            //  Retorna um Vector2 aleatório dentro de um círculo de raio 1.
            //  Multiplicamos por spawnRadius para escalar o raio desejado.
            //  Ex: spawnRadius = 3 → posições em até 3 metros do centro.
            //  Usamos Vector2 (XZ) e não Vector3 porque queremos variação
            //  apenas horizontal — mantemos o Y do spawner.
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate    = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // ==============================================================
            //  NavMesh.SamplePosition(posição, out hit, distância, areaMask)
            // ==============================================================
            //  Encontra o ponto mais próximo no NavMesh a partir de "candidate",
            //  dentro de uma distância de busca de 2 metros.
            //
            //  Parâmetros:
            //    candidate    → ponto de partida da busca
            //    out hit      → resultado: dados do ponto encontrado no NavMesh
            //    2f           → raio de busca máximo em metros
            //    NavMesh.AllAreas → aceita qualquer tipo de área do NavMesh
            //
            //  Retorna true se encontrou um ponto válido.
            //  "hit.position" é a posição exata sobre o NavMesh.
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        // Fallback: spawna exatamente no centro do spawner se não achou posição válida.
        Debug.LogWarning($"[EnemySpawner] Não encontrou posição NavMesh válida perto de {name}. " +
                         "Usando posição central do spawner.");
        return transform.position;
    }

    // ==============================================================
    //  VISUALIZAÇÃO NO SCENE VIEW
    // ==============================================================
    //  OnDrawGizmosSelected() é chamado pela Unity APENAS quando
    //  este GameObject está selecionado no Editor (não aparece em jogo).
    //  Usamos para desenhar o raio de spawn visualmente.
    //
    //  Gizmos são auxiliares visuais de debug — não aparecem na tela do jogo.
    //  "DrawWireSphere" desenha uma esfera sem preenchimento na posição
    //  deste transform com o raio de spawn configurado.

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
