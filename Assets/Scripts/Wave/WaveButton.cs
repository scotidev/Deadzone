using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  WaveButton é um objeto interativo no mundo 3D (não é um botão
//  de UI). O jogador se aproxima dele e pressiona E para interagir.
//
//  Ele herda de Interactable — que define o contrato:
//    "qualquer Interactable DEVE implementar Interact()".
//  O PlayerInteraction detecta o WaveButton com Raycast e,
//  quando o jogador pressiona E, chama WaveButton.Interact().
//
//  MODIFICAÇÃO FEITA:
//  Adicionamos a verificação "if (WaveManager.Instance.IsWaveActive)"
//  para bloquear a interação enquanto uma onda está em andamento.
//  Sem isso, o jogador poderia iniciar duas ondas ao mesmo tempo,
//  duplicando os inimigos na cena.

/// <summary>
/// Botão interativo no mundo 3D que inicia a próxima onda de inimigos.
/// O jogador pressiona E para interagir (gerenciado por PlayerInteraction
/// via a classe base Interactable).
///
/// A interação é bloqueada enquanto uma onda já está em andamento.
/// </summary>
public class WaveButton : Interactable {

    // ==============================================================
    //  INTERACT() — implementação do contrato Interactable
    // ==============================================================
    //  "override" = estamos substituindo o método abstract de Interactable.
    //  Sem "override", o compilador acusaria erro porque Interactable
    //  declara Interact() como abstract (deve ser implementado).

    /// <summary>
    /// Inicia a próxima onda quando o jogador interage com este botão.
    /// Não faz nada se uma onda já estiver em andamento.
    /// </summary>
    public override void Interact() {
        // Verificação de null: se não houver WaveManager na cena, sai.
        if (WaveManager.Instance == null) return;

        // ==============================================================
        //  GUARD CLAUSE — bloqueio de onda ativa
        // ==============================================================
        //  "IsWaveActive" é a propriedade pública do WaveManager que
        //  retorna true enquanto a onda está em curso.
        //  Se já há uma onda ativa, apenas logamos e saímos.
        //  O WaveManager também tem esta verificação em StartNextWave(),
        //  mas preferimos bloquear aqui também — mais defensivo.
        if (WaveManager.Instance.IsWaveActive) {
            Debug.Log("[WaveButton] Onda já em andamento — interação ignorada.");
            return;
        }

        // Tudo OK: inicia a próxima onda.
        WaveManager.Instance.StartNextWave();
    }
}
