using UnityEngine;

/// <summary>
/// Entry point of the game. Lives in the Loader scene alongside all persistent managers.
/// Previously responsible for calling SceneLoader.LoadMenu() directly.
/// Now that responsibility belongs to LogoIntro, which loads the Menu
/// after the intro animation finishes (or is skipped by the player).
/// This class is kept for reference and may be repurposed in the future.
/// </summary>
// ==============================================================
//  POR QUE ESTE SCRIPT FOI ESVAZIADO?
// ==============================================================
//  Antes, o Bootstrapper chamava SceneLoader.Instance.LoadMenu()
//  imediatamente no Start() — carregando o Menu sem nenhuma espera.
//
//  Agora o LogoIntro.cs assumiu essa responsabilidade:
//  ele exibe a logo, aguarda, e SÓ ENTÃO chama LoadMenu().
//
//  Se o Bootstrapper continuasse chamando LoadMenu() no Start(),
//  os dois scripts entrariam em conflito — a cena seria carregada
//  antes da animação terminar. Por isso o Start() foi removido.
//
//  Caso queira desativar a intro futuramente, reabilite o Start()
//  abaixo e remova o componente LogoIntro da cena Loader.
public class Bootstrapper : MonoBehaviour {
    // Start() removido — LogoIntro.cs controla a transição para o Menu.
    // private void Start() => SceneLoader.Instance.LoadMenu();
}
