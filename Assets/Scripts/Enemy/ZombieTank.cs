// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  ZombieTank é o inimigo pesado do jogo.
//  Lento e com muito HP — serve como "esponja de dano" que força
//  o jogador a gastar muita munição em um só alvo.
//  Desbloqueado a partir da onda 4.
//
//  ESTRATÉGIA DE DESIGN:
//  O ZombieTank muda o ritmo do combate. Enquanto ele absorve
//  munição, outros zumbis chegam pelo lado. Cria dilema:
//  "continuo nele ou cuido dos rápidos primeiro?"

/// <summary>
/// Zumbi resistente. Vida muito alta e dano elevado, mas extremamente lento.
/// Desbloqueado a partir da onda 4.
/// </summary>
public class ZombieTank : Enemy {

    protected override void InitializeStats() {
        maxHealth      = 350f;  // Absorve muito dano antes de morrer
        moveSpeed      = 1.8f;  // Muito lento — jogador tem tempo de desviar
        attackDamage   = 30f;   // Um acerto tira 30% da vida do jogador
        attackRange    = 2.2f;  // Alcance maior (braços longos do tanque)
        attackCooldown = 2.5f;  // Ataque lento mas devastador
    }
}
