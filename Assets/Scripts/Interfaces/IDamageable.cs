/// <summary>
/// Interface for any object that can take damage by calling TakeDamage().
/// </summary>
public interface IDamageable {

    /// <summary>
    /// Applies the specified amount of damage to this object.
    /// </summary>
    /// <param name="amount">The amount of damage. Must be positive.</param>
    void TakeDamage(float amount);
}
