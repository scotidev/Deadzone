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
    /// Instancia um lote de inimigos, escolhendo aleatoriamente entre
    /// os prefabs fornecidos para cada unidade.
    ///
    /// O spawn é feito via Coroutine para distribuir o custo de
    /// Instantiate() ao longo do tempo (um inimigo por "spawnDelay").
    /// </summary>
    /// <param name="availablePrefabs">Lista de prefabs que podem spawnar nesta onda.</param>
    /// <param name="count">Quantidade de inimigos que ESTE spawner deve criar.</param>
    public void SpawnEnemies(List<GameObject> availablePrefabs, int count) {
        // Validação de segurança: não tenta spawnar sem dados válidos.
        if (availablePrefabs == null || availablePrefabs.Count == 0 || count <= 0)
            return;

        // StartCoroutine inicia a execução da rotina de spawn de forma
        // assíncrona — ela roda "em paralelo" com o resto do jogo,
        // pausando entre cada spawn via WaitForSeconds.
        StartCoroutine(SpawnRoutine(availablePrefabs, count));
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

    private IEnumerator SpawnRoutine(List<GameObject> availablePrefabs, int count) {
        for (int i = 0; i < count; i++) {
            // ==============================================================
            //  Random.Range(min, max) com inteiros
            // ==============================================================
            //  Retorna um número inteiro aleatório entre min (inclusivo)
            //  e max (EXclusivo). Com 3 prefabs (índices 0, 1, 2):
            //    Random.Range(0, 3) retorna 0, 1 ou 2.
            int prefabIndex    = Random.Range(0, availablePrefabs.Count);
            Vector3 spawnPos   = GetValidSpawnPosition();

            // ==============================================================
            //  Instantiate(prefab, position, rotation)
            // ==============================================================
            //  Cria uma CÓPIA do prefab na cena, na posição e rotação dadas.
            //  "Quaternion.identity" = sem rotação (0,0,0,1).
            //  O objeto nasce sem rotação aplicada.
            Instantiate(availablePrefabs[prefabIndex], spawnPos, Quaternion.identity);

            // ==============================================================
            //  yield return new WaitForSeconds(segundos)
            // ==============================================================
            //  Pausa a Coroutine por "spawnDelay" segundos antes de
            //  spawnar o próximo inimigo. O resto do jogo continua
            //  normalmente durante esse período.
            yield return new WaitForSeconds(spawnDelay);
        }
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
