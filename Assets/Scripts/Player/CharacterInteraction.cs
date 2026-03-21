using InfimaGames.LowPolyShooterPack;
using UnityEngine;

// ==============================================================
//  O QUE FAZ O CharacterInteraction?
// ==============================================================
//  Este script é uma PONTE (padrão "Bridge") entre dois sistemas:
//
//  1. O NOSSO sistema (BuildingController, ShopManager...)
//  2. O sistema da Infima Games (Low Poly Shooter Pack — pacote importado)
//
//  O problema: a Infima tem um script "Character.cs" que controla
//  câmera, movimento e arma do jogador. Para pausar esses controles
//  quando o jogador abre a loja ou entra no modo construção, precisamos
//  chamar métodos desse script.
//
//  Como o código da Infima é externo (não devemos modificá-lo),
//  este script serve como intermediário — ele sabe como "falar" com
//  o Character.cs da Infima sem misturar nosso código com o deles.
//
//  Analogia: é como um tradutor entre dois idiomas diferentes.
public class CharacterInteraction : MonoBehaviour {

    // ==============================================================
    //  O QUE É O PADRÃO SINGLETON?
    // ==============================================================
    //  Singleton garante que só exista UMA única instância deste script
    //  em toda a cena. "Instance" é uma variável estática (pertence à
    //  CLASSE, não a um objeto específico) que guarda essa instância.
    //  Outros scripts acessam com: CharacterInteraction.Instance.SetHolstered(true)
    //  sem precisar arrastar o objeto no Inspector.
    //
    //  "{ get; private set; }" significa:
    //    - qualquer script pode LER  (get é público)
    //    - só este script pode ESCREVER (set é private)
    public static CharacterInteraction Instance { get; private set; }

    // Referência para o Character.cs da Infima Games.
    // "private" porque só este script precisa falar diretamente com ele.
    private Character playerCharacter;

    // ==============================================================
    //  Awake() — executado quando o objeto nasce na cena
    // ==============================================================
    private void Awake() {
        // Lógica Singleton: se não existe instância ainda, eu sou ela.
        // Se já existe outra, me destruo para não duplicar.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        // Destroy(gameObject) remove este GameObject da cena imediatamente.

        // ==============================================================
        //  O QUE É GetComponent<T>()?
        // ==============================================================
        //  Busca um componente do tipo T no MESMO GameObject.
        //  Aqui: "encontre o Character.cs da Infima neste mesmo objeto
        //         e guarde a referência em playerCharacter".
        //  CharacterInteraction e Character.cs precisam estar no mesmo
        //  GameObject (o Player) para isso funcionar.
        playerCharacter = GetComponent<Character>();
    }

    // ==============================================================
    //  SetInterfaceMode(bool isPaused)
    // ==============================================================
    //  Alterna o personagem entre "Modo Gameplay" e "Modo Interface".
    //
    //  isPaused = true  → câmera trava, movimento para, atirar desativa,
    //                     cursor do mouse aparece (modo loja/menu).
    //  isPaused = false → tudo volta ao normal (modo gameplay).
    //
    //  Chamado pelo ShopManager ao abrir e fechar a loja.
    public void SetInterfaceMode(bool isPaused) {
        // "if ... return" = verificação de segurança.
        // Se playerCharacter for null (componente não encontrado), sai
        // sem fazer nada — evita um erro NullReferenceException.
        if (playerCharacter == null) return;

        // Delega a chamada para o Character.cs da Infima.
        // Este método deles cuida de travar/destravar câmera, movimento e cursor.
        playerCharacter.SetInterfaceMode(isPaused);
    }

    /// <summary>
    /// Hides or reveals the player's weapon by toggling the holster animation.
    /// holstered = true  → hides the weapon (player cannot shoot).
    /// holstered = false → reveals the weapon (player can shoot).
    /// Called by BuildingController when entering/exiting build mode to prevent shooting while building.
    /// </summary>
    /// <param name="holstered"></param>
    public void SetHolstered(bool holstered) {
        if (playerCharacter == null) return;
        playerCharacter.SetHolstered(holstered);
    }
}
