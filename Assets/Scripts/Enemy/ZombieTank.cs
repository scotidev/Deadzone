/// <summary>
/// Tank zombie. High health and damage, but extremely slow.
/// </summary>
public class ZombieTank : EnemyBase {

    protected override void InitializeStats() {
        maxHealth = 350f;
        moveSpeed = 1.8f;
        attackDamage = 30f;
        attackRange = 2.2f;
        attackCooldown = 2.5f;
    }
}
