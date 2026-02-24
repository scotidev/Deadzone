using TMPro;
using UnityEngine;

// ==============================================================
//  O QUE FAZ ESTE SCRIPT?
// ==============================================================
//  WaveUI é o painel HUD que mostra informações da onda atual
//  durante todo o gameplay. Diferente dos outros painéis (Shop,
//  Pause), ele fica SEMPRE visível — não é ocultado por HideAllPanels.
//
//  O WaveManager atualiza os textos chamando os métodos públicos:
//    UpdateWaveNumber()      → "Wave 3"
//    UpdateEnemiesRemaining() → "Enemies: 7"
//    SetStatus()             → "Wave 3 — Survive!"
//
//  HERANÇA DE BaseUI:
//  "public class WaveUI : BaseUI" significa que WaveUI herda toda a
//  estrutura de BaseUI (referência ao painel, Show/Hide, etc.).
//  BaseUI.Start() esconde o painel por padrão. Aqui no Start()
//  chamamos base.Start() (para rodar a lógica do pai) e em seguida
//  Show() para deixar o painel visível imediatamente.

// ==============================================================
//  O QUE É TMP_Text?
// ==============================================================
//  TMP_Text é o tipo base de TextMeshPro — o sistema de texto
//  avançado da Unity. Preferido ao Text legado (UnityEngine.UI.Text)
//  por oferecer renderização de maior qualidade, suporte a fontes
//  SDF, efeitos de texto, etc.
//  Adicione o componente "TextMeshPro - Text (UI)" no Canvas.
//  "using TMPro;" é necessário para usar este tipo.

/// <summary>
/// Painel HUD persistente que exibe informações de onda durante o gameplay.
/// Fica visível automaticamente ao iniciar — não é ocultado entre ondas.
///
/// O WaveManager atualiza os textos diretamente via métodos públicos.
/// </summary>
public class WaveUI : BaseUI {

    // ==============================================================
    //  CAMPOS SERIALIZADOS
    // ==============================================================
    //  Cada campo é preenchido no Inspector arrastando o objeto
    //  TextMeshPro correspondente do Canvas para o slot.
    //
    //  [Header("...")] cria uma seção visual separadora no Inspector.
    //  [Tooltip("...")] mostra um texto de ajuda ao passar o mouse.
    //  [SerializeField] expõe o campo privado no Inspector sem tornar public.

    [Header("Textos de Informação da Onda")]
    [Tooltip("Exibe o número da onda atual. Exemplo: 'Wave 3'")]
    [SerializeField] private TMP_Text waveNumberText;

    [Tooltip("Exibe quantos inimigos ainda estão vivos nesta onda.")]
    [SerializeField] private TMP_Text enemiesRemainingText;

    [Tooltip("Mensagem de status: entre ondas e durante a onda.")]
    [SerializeField] private TMP_Text statusText;

    // ==============================================================
    //  START — inicialização após todos os Awake() terminarem
    // ==============================================================

    protected override void Start() {
        // ==============================================================
        //  base.Start()
        // ==============================================================
        //  "base" acessa a classe pai (BaseUI). BaseUI.Start() esconde
        //  o painel por padrão (panel.SetActive(false)).
        //  Chamamos base.Start() primeiro para respeitar a lógica do pai,
        //  depois imediatamente chamamos Show() para tornar o painel visível.
        //  Resultado: o painel aparece assim que a cena carrega.
        base.Start();

        // Exibe o painel — WaveUI deve ser sempre visível durante gameplay.
        // Show() está definido em BaseUI e chama panel.SetActive(true).
        Show();

        // Inicializa os textos com os valores padrão (pré-jogo).
        UpdateWaveNumber(0);
        UpdateEnemiesRemaining(0);
        SetStatus("Interaja com o Wave Button para começar!");
    }

    // ==============================================================
    //  MÉTODOS PÚBLICOS — chamados pelo WaveManager
    // ==============================================================

    /// <summary>
    /// Atualiza o label do número da onda.
    /// Onda 0 (estado inicial) exibe "Wave —".
    /// </summary>
    public void UpdateWaveNumber(int wave) {
        if (waveNumberText != null)
            // ==============================================================
            //  INTERPOLAÇÃO DE STRING COM "$"
            // ==============================================================
            //  "$" antes das aspas ativa "string interpolation" do C#.
            //  "{wave}" dentro da string é substituído pelo valor da variável.
            //  Exemplo: wave = 3 → texto fica "Wave 3".
            //  "Wave —" é mostrado quando wave == 0 (antes da primeira onda).
            waveNumberText.text = wave == 0 ? "Wave —" : $"Wave {wave}";
    }

    /// <summary>
    /// Atualiza o label de inimigos restantes.
    /// Chamado pelo WaveManager a cada morte de inimigo.
    /// </summary>
    public void UpdateEnemiesRemaining(int count) {
        if (enemiesRemainingText != null)
            enemiesRemainingText.text = $"Inimigos: {count}";
    }

    /// <summary>
    /// Define a mensagem de status exibida ao jogador.
    /// Exemplos:
    ///   "Wave 3 — Survive!"
    ///   "Wave 2 cleared! Interaja com o Wave Button para continuar."
    /// </summary>
    public void SetStatus(string message) {
        if (statusText != null)
            statusText.text = message;
    }
}
