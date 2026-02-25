// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  ZombieFast é o inimigo ágil do jogo.
//  Alta velocidade e cadência de ataque compensam a baixa resistência.
//  Desbloqueado a partir da onda 2.
//
//  ESTRATÉGIA DE DESIGN:
//  O jogador precisa priorizar ZombieFast antes que chegue perto.
//  Se chegar no melee, ataca rapidamente mas morre com poucos tiros.
//  Cria pressão no jogador para não ficar parado.

/// <summary>
/// Zumbi veloz. Baixa vida, alta velocidade e curto cooldown de ataque.
/// Desbloqueado a partir da onda 2.
/// </summary>
public class ZombieFast : Enemy {

    protected override void InitializeStats() {
        maxHealth      = 50f;   // Frágil — morre com 2 tiros de pistola
        moveSpeed      = 6.5f;  // Quase o dobro da velocidade do ZombieDefault
        attackDamage   = 7f;    // Dano baixo por acerto individual
        attackRange    = 1.6f;  // Alcance ligeiramente menor (mais ágil)
        attackCooldown = 1.0f;  // Ataca mais rápido que os demais
    }
}
