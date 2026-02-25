using System;
using System.Collections;
using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  PlayerHealth gerencia a vida do jogador.
//  Ele implementa a interface IDamageable, o que significa que
//  qualquer inimigo pode chamar TakeDamage() sem saber que
//  está chamando PlayerHealth — só conhece o contrato IDamageable.
//
//  ONDE ADICIONAR ESTE SCRIPT:
//  No GameObject do Player, junto com Character.cs, Movement.cs etc.
//  A tag do Player DEVE ser "Player" para que os inimigos o encontrem.

/// <summary>
/// Gerencia a vida do jogador.
/// Implementa IDamageable para que inimigos causem dano via interface.
///
/// Adicione este componente ao GameObject do Player.
/// Certifique-se de que o Player possui a tag "Player".
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable {

    // ==============================================================
    //  CAMPOS SERIALIZADOS
    // ==============================================================
    //  [SerializeField] expõe o campo privado no Inspector da Unity.
    //  Isso permite ajustar o valor sem alterar o código.

    [Header("Configurações de Vida")]
    [Tooltip("Quantidade máxima de pontos de vida.")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Dano da Névoa (Fora da SafeZone)")]
    [Tooltip("Dano aplicado a cada tick enquanto o jogador está na névoa.")]
    [SerializeField] private float poisonDamagePerTick = 5f;

    [Tooltip("Intervalo em segundos entre cada tick de dano da névoa.")]
    [SerializeField] private float poisonTickInterval = 1f;

    // Vida atual é privada — só este script a altera diretamente.
    private float currentHealth;

    // Referência à coroutine ativa do veneno.
    // Guardamos para poder parar em StopPoisonDamage().
    private Coroutine poisonCoroutine;

    // ==============================================================
    //  EVENTOS C#
    // ==============================================================
    //  Eventos permitem que outros sistemas reajam a mudanças
    //  sem que PlayerHealth precise conhecê-los.
    //
    //  Exemplo de uso de OnHealthChanged:
    //    Um script de barra de vida faz:
    //      playerHealth.OnHealthChanged += AtualizarBarra;
    //    Quando o jogador toma dano, a barra atualiza sozinha.
    //
    //  "Action<float>" = delegate que recebe um float e não retorna nada.
    //  O float é a fração de vida (0.0 a 1.0) — útil para barras de vida.
    //
    //  "event" antes de Action impede que scripts externos disparem
    //  o evento diretamente (só podem assinar e desassinar com += e -=).

    /// <summary>
    /// Disparado toda vez que a vida muda. Parâmetro: fração 0.0–1.0.
    /// Útil para atualizar barras de vida na UI.
    /// </summary>
    public event Action<float> OnHealthChanged;

    /// <summary>
    /// Disparado quando o jogador morre (vida chega a zero).
    /// </summary>
    public event Action OnPlayerDied;

    /// <summary>
    /// Disparado quando o estado de envenenamento muda.
    /// Parâmetro: true = entrou na névoa, false = entrou na safezone.
    /// Útil para ativar efeito visual de tela (vinheta vermelha, etc.).
    /// </summary>
    public event Action<bool> OnPoisonStateChanged;

    // ==============================================================
    //  AWAKE — inicialização
    // ==============================================================

    private void Awake() {
        // Começa o jogo com vida cheia.
        currentHealth = maxHealth;
    }

    // ==============================================================
    //  IMPLEMENTAÇÃO DE IDamageable
    // ==============================================================

    /// <summary>
    /// Aplica dano ao jogador. Chamado pelo EnemyAttack quando ataca.
    /// Implementa o contrato da interface IDamageable.
    /// </summary>
    public void TakeDamage(float amount) {
        // Guarda de segurança: ignora dano se o jogador já está morto.
        if (currentHealth <= 0f) return;

        // ==============================================================
        //  Mathf.Max(a, b)
        // ==============================================================
        //  Retorna o maior entre dois valores.
        //  "Mathf.Max(0f, currentHealth - amount)" garante que a vida
        //  nunca fique negativa. Exemplo:
        //    currentHealth = 5, amount = 30 → sem Max ficaria -25
        //    com Max → resultado é 0 (sem vida, mas nunca negativo)
        currentHealth = Mathf.Max(0f, currentHealth - amount);

        // Notifica ouvintes (ex: barra de vida) sobre a mudança.
        // GetHealthFraction() retorna o percentual de vida restante.
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // Se a vida chegou a zero, ativa a sequência de morte.
        if (currentHealth <= 0f)
            Die();
    }

    // ==============================================================
    //  DANO DA NÉVOA (POISON)
    // ==============================================================

    /// <summary>
    /// Inicia o tick de dano da névoa. Chamado por SafeZone.OnTriggerExit.
    /// Não faz nada se o jogador já está sendo envenenado.
    /// </summary>
    public void StartPoisonDamage() {
        if (poisonCoroutine != null) return;
        poisonCoroutine = StartCoroutine(PoisonTick());
        OnPoisonStateChanged?.Invoke(true);
        Debug.Log("[PlayerHealth] Névoa ativada — tomando dano por segundo.");
    }

    /// <summary>
    /// Para o tick de dano da névoa. Chamado por SafeZone.OnTriggerEnter.
    /// Não faz nada se o jogador não está sendo envenenado.
    /// </summary>
    public void StopPoisonDamage() {
        if (poisonCoroutine == null) return;
        StopCoroutine(poisonCoroutine);
        poisonCoroutine = null;
        OnPoisonStateChanged?.Invoke(false);
        Debug.Log("[PlayerHealth] Névoa desativada — dentro da safezone.");
    }

    /// <summary>
    /// True enquanto o jogador está sofrendo dano da névoa.
    /// </summary>
    public bool IsInPoison => poisonCoroutine != null;

    /// <summary>
    /// Coroutine que aplica dano a cada poisonTickInterval segundos.
    /// Respeita Time.timeScale — pausa durante Pause Menu automaticamente.
    /// </summary>
    private IEnumerator PoisonTick() {
        while (true) {
            yield return new WaitForSeconds(poisonTickInterval);
            TakeDamage(poisonDamagePerTick);
        }
    }

    // ==============================================================
    //  CURA
    // ==============================================================

    /// <summary>
    /// Restaura vida pelo valor informado, sem ultrapassar maxHealth.
    /// Pode ser chamado por itens de cura, kits médicos, etc.
    /// </summary>
    public void Heal(float amount) {
        // ==============================================================
        //  Mathf.Min(a, b)
        // ==============================================================
        //  Retorna o menor entre dois valores.
        //  "Mathf.Min(maxHealth, currentHealth + amount)" garante que
        //  a vida nunca ultrapasse o máximo. Exemplo:
        //    maxHealth = 100, currentHealth = 90, amount = 30
        //    → sem Min ficaria 120 (inválido)
        //    → com Min → resultado é 100 (limitado ao máximo)
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    // ==============================================================
    //  MORTE
    // ==============================================================

    private void Die() {
        // Dispara o evento para que outros sistemas reajam (ex: tela de game over).
        OnPlayerDied?.Invoke();

        Debug.Log("[PlayerHealth] Jogador morreu.");
        // TODO: Implementar lógica de Game Over ou respawn aqui.
    }

    // ==============================================================
    //  GETTERS UTILITÁRIOS
    // ==============================================================

    /// <summary>
    /// Retorna a vida atual como fração entre 0 e 1.
    /// Exemplo: 70 HP de 100 → retorna 0.7f
    /// </summary>
    public float GetHealthFraction() => currentHealth / maxHealth;

    /// <summary>
    /// Retorna o valor absoluto de vida atual.
    /// </summary>
    public float GetCurrentHealth() => currentHealth;

    /// <summary>
    /// Retorna o valor máximo de vida.
    /// </summary>
    public float GetMaxHealth() => maxHealth;
}
