using UnityEngine;

public abstract class ItemExclusive : MonoBehaviour
{
    [Header("Exclusive Upgrade Settings")]
    [SerializeField] protected int unlockLevel = 1; // O nível em que o upgrade exclusivo é desbloqueado.
    [SerializeField] protected string exclusiveDescription = "No exclusive upgrade description available."; // Descrição do que o upgrade exclusivo faz.

    protected bool isExclusiveUnlocked = false; // Flag para indicar se o upgrade exclusivo foi desbloqueado.
    protected bool hasExclusiveBeenPurchased = false; // Flag para indicar se o upgrade exclusivo já foi comprado/ativado.

    protected virtual void Awake()
    {
        // Base Awake method
    }

    public void SetupExclusive(int level, string description)
    {
        SetUnlockLevel(level);
        SetExclusiveDescription(description);
    }

    /// <summary>
    /// Inicializa o comportamento exclusivo. Chamado quando o item é instanciado ou ativado.
    /// </summary>
    public virtual void Initialize()
    {
        // Lógica de inicialização comum pode ser adicionada aqui, se necessário.
        // Por exemplo, verificar o nível atual do item ao iniciar.
    }

    /// <summary>
    /// Verifica se o upgrade exclusivo deve ser desbloqueado com base no nível atual do item.
    /// </summary>
    /// <param name="currentLevel">O nível atual do item.</param>
    public void CheckForExclusiveUnlock(int currentLevel)
    {
        if (!isExclusiveUnlocked && currentLevel >= unlockLevel)
        {
            isExclusiveUnlocked = true;
            OnExclusiveUnlocked();
        }
    }

    /// <summary>
    /// Chamado quando o upgrade exclusivo é desbloqueado.
    /// </summary>
    protected virtual void OnExclusiveUnlocked()
    {
        Debug.Log($"Exclusive upgrade unlocked for item at level {unlockLevel}!");
        // Lógica adicional específica pode ser adicionada em classes derivadas.
    }

    /// <summary>
    /// Ativa o upgrade exclusivo. Isso pode envolver aplicar buffs, mudar propriedades, etc.
    /// </summary>
    public virtual void ActivateExclusive()
    {
        if (isExclusiveUnlocked && !hasExclusiveBeenPurchased)
        {
            hasExclusiveBeenPurchased = true;
            ApplyExclusiveEffects();
            Debug.Log("Exclusive upgrade activated!");
        }
        else if (hasExclusiveBeenPurchased)
        {
            Debug.Log("Exclusive upgrade already activated.");
        }
        else
        {
            Debug.LogWarning("Attempted to activate exclusive upgrade before it was unlocked or purchased.");
        }
    }

    /// <summary>
    /// Aplica os efeitos do upgrade exclusivo. Deve ser implementado pelas classes derivadas.
    /// </summary>
    protected abstract void ApplyExclusiveEffects();

    /// <summary>
    /// Retorna a descrição do upgrade exclusivo.
    /// </summary>
    /// <returns>A string contendo a descrição do exclusivo.</returns>
    public string GetExclusiveDescription()
    {
        if (isExclusiveUnlocked || hasExclusiveBeenPurchased)
        {
            return exclusiveDescription;
        }
        else
        {
            return "Reach level " + unlockLevel + " to unlock exclusive upgrade.";
        }
    }

    /// <summary>
    /// Define o nível de desbloqueio para o upgrade exclusivo.
    /// </summary>
    /// <param name="level">O nível de desbloqueio.</param>
    public void SetUnlockLevel(int level)
    {
        this.unlockLevel = level;
    }

    /// <summary>
    /// Define a descrição do upgrade exclusivo.
    /// </summary>
    /// <param name="description">A descrição do upgrade.</param>
    public void SetExclusiveDescription(string description)
    {
        this.exclusiveDescription = description;
    }

    /// <summary>
    /// Verifica se o upgrade exclusivo já foi comprado/ativado.
    /// </summary>
    /// <returns>True se o exclusivo foi ativado, False caso contrário.</returns>
    public bool HasExclusiveBeenPurchased()
    {
        return hasExclusiveBeenPurchased;
    }

    /// <summary>
    /// Verifica se o upgrade exclusivo está desbloqueado (mas não necessariamente comprado).
    /// </summary>
    /// <returns>True se o exclusivo está desbloqueado, False caso contrário.</returns>
    public bool IsExclusiveUnlocked()
    {
        return isExclusiveUnlocked;
    }
}
