/// <summary>
/// Default Zombie with balanced stats.
/// </summary>
public class ZombieDefault : EnemyBase {

    protected override void InitializeStats() {
        maxHealth = 100f;
        moveSpeed = 3.0f;
        attackDamage = 10f;
        attackRange = 1.8f;
        attackCooldown = 1.5f;
    }
}
