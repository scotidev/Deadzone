/// <summary>
/// Fast zombie. Low health, high speed, and short attack cooldown.
/// </summary>
public class ZombieFast : EnemyBase {

    protected override void InitializeStats() {
        maxHealth = 50f;
        moveSpeed = 6.5f;
        attackDamage = 7f;
        attackRange = 1.6f;
        attackCooldown = 1.0f;
    }
}
