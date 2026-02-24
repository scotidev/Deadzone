// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  ZombieDefault é o inimigo mais básico do jogo.
//  Ele HERDA tudo de Enemy (vida, dano, morte, evento) e só
//  precisa implementar InitializeStats() com seus próprios valores.
//
//  HERANÇA EM C#:
//  "public class ZombieDefault : Enemy" significa:
//    → ZombieDefault É UM Enemy.
//    → Tem todos os campos e métodos de Enemy automaticamente.
//    → Só adiciona ou sobrescreve o que for diferente.
//
//  "override" = "estou substituindo a implementação da classe pai".
//  Sem "override", o compilador acusaria erro porque InitializeStats()
//  foi declarado como "abstract" em Enemy — precisa ser implementado.
//
//  POLIMORFISMO:
//  O WaveManager guarda uma lista de GameObject (prefabs).
//  Quando instancia um ZombieDefault, a Unity encontra o script
//  ZombieDefault no prefab. Como ele É UM Enemy, o WaveManager
//  pode tratar todos os tipos de zumbi como "Enemy" genericamente.

/// <summary>
/// Zumbi padrão com stats balanceados.
/// Disponível desde a primeira onda.
/// Referência de equilíbrio: nem rápido demais, nem lento demais.
/// </summary>
public class ZombieDefault : Enemy {

    // ==============================================================
    //  IMPLEMENTAÇÃO DO CONTRATO ABSTRATO
    // ==============================================================
    //  "protected override" = visível para subclasses + sobrescreve o pai.
    //  Este método é chamado em Enemy.Awake() antes de qualquer outra coisa.
    //  Os valores definidos aqui determinam o comportamento em jogo.

    protected override void InitializeStats() {
        maxHealth      = 100f;  // Vida média — morre com ~4 tiros de pistola
        moveSpeed      = 3.0f;  // Velocidade de caminhada humana (3 m/s)
        attackDamage   = 10f;   // Dano moderado por acerto
        attackRange    = 1.8f;  // Alcance de braço estendido (metros)
        attackCooldown = 1.5f;  // Um ataque a cada 1,5 segundos
    }
}
