// ==============================================================
//  O QUE É UMA INTERFACE EM C#?
// ==============================================================
//  Uma interface é um CONTRATO. Ela declara "quem assinar este
//  contrato DEVE ter o método TakeDamage". Ela não implementa
//  nada — só exige a promessa.
//
//  Por que usar interface aqui?
//  O EnemyAttack precisa causar dano ao jogador. Mas ele não deve
//  depender diretamente da classe "PlayerHealth" — senão qualquer
//  mudança nela quebraria o EnemyAttack.
//
//  Com a interface:
//    - EnemyAttack só conhece IDamageable.
//    - PlayerHealth implementa IDamageable (assina o contrato).
//    - Se amanhã criarmos NPCs aliados ou veículos que tomam dano,
//      é só implementar IDamageable neles também.
//
//  Isso é o princípio SOLID de "Dependency Inversion":
//  dependa de abstrações (interfaces), não de implementações concretas.

// ==============================================================
//  O QUE É UMA INTERFACE EM C#?
// ==============================================================
//  Uma interface é um CONTRATO. Ela declara "quem assinar este
//  contrato DEVE ter o método TakeDamage". Ela não implementa
//  nada — só exige a promessa.
//
//  Por que usar interface aqui?
//  O EnemyAttack precisa causar dano ao jogador. Mas ele não deve
//  depender diretamente da classe "PlayerHealth" — senão qualquer
//  mudança nela quebraria o EnemyAttack.
//
//  Com a interface:
//    - EnemyAttack só conhece IDamageable.
//    - PlayerHealth implementa IDamageable (assina o contrato).
//    - Se amanhã criarmos NPCs aliados ou veículos que tomam dano,
//      é só implementar IDamageable neles também.
//
//  Isso é o princípio SOLID de "Dependency Inversion":
//  dependa de abstrações (interfaces), não de implementações concretas.

/// <summary>
/// Interface implementada por qualquer objeto que pode receber dano.
/// Permite que inimigos causem dano ao jogador (e a outros objetos
/// danificáveis no futuro) sem precisar conhecer o tipo concreto —
/// basta chamar TakeDamage().
/// </summary>
public interface IDamageable {

    // ==============================================================
    //  O QUE É UM MÉTODO DE INTERFACE?
    // ==============================================================
    //  Métodos de interface são APENAS a assinatura (nome, parâmetros,
    //  retorno). Nenhum corpo { } aqui.
    //  Quem implementar a interface DEVE escrever o corpo do método.
    //
    //  "float amount" = o quanto de dano será aplicado.
    //  Exemplo: inimigo padrão chama TakeDamage(10f), tanque chama
    //  TakeDamage(30f). O PlayerHealth decide o que fazer com esse valor.

    /// <summary>
    /// Aplica a quantidade de dano informada neste objeto.
    /// </summary>
    /// <param name="amount">Quantidade de dano. Deve ser positivo.</param>
    void TakeDamage(float amount);
}
