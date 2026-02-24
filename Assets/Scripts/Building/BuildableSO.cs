using UnityEngine;

// ==============================================================
//  O QUE É UM ScriptableObject?
// ==============================================================
//  Imagine uma "ficha de cadastro" de um produto numa loja.
//  Ela não é o produto em si — ela só guarda as informações: nome, foto, tamanho...
//  Um ScriptableObject funciona exatamente assim: é um arquivo salvo na pasta
//  Assets do projeto que guarda dados, sem precisar estar colado a um objeto na cena.
//  Cada item construível (barricada, barril, mina) tem sua própria ficha.
//  O BuildingController lê os dados dessa ficha para saber o que instanciar.

// ==============================================================
//  O QUE É [CreateAssetMenu]?
// ==============================================================
//  Esse texto entre colchetes chama-se "atributo" — é uma instrução
//  que a Unity lê antes de rodar o jogo.
//  Este atributo cria um atalho no Project Window:
//  botão direito → Create → Deadzone → Buildable Item
//  Isso cria um novo arquivo do tipo BuildableSO na pasta selecionada.
//  fileName = nome padrão do arquivo criado.
//  menuName = caminho que aparece no menu de contexto.
[CreateAssetMenu(fileName = "NewBuildable", menuName = "Deadzone/Buildable Item")]
public class BuildableSO : ScriptableObject {

    // ==============================================================
    //  O QUE É [Header("...")]?
    // ==============================================================
    //  [Header] é um atributo que desenha um título em negrito no Inspector
    //  da Unity, organizando visualmente os campos. Não afeta o código.

    [Header("Informações do Item")]

    // Nome exibido na tela quando o jogador aperta a tecla de construção.
    // "public string" = texto visível e editável por outros scripts e no Inspector.
    public string displayName;

    [Header("Prefabs")]

    // ==============================================================
    //  O QUE É UM Prefab?
    // ==============================================================
    //  Um Prefab é um "molde" salvo na pasta do projeto.
    //  Você monta o objeto na cena (com componentes, materiais, scripts),
    //  arrasta para a pasta Assets e ele vira um Prefab.
    //  A partir daí, o código pode criar cópias dele via Instantiate().
    //  Modificar o Prefab original atualiza TODAS as cópias no jogo.

    // O prefab REAL: o objeto permanente que fica na cena após o jogador confirmar.
    public GameObject realPrefab;

    // O prefab FANTASMA: versão transparente e colorida (verde/vermelho)
    // que aparece enquanto o jogador está decidindo onde colocar.
    // É temporário — destruído ao cancelar ou ao confirmar a colocação.
    public GameObject ghostPrefab;

    [Header("Rotação ao Colocar")]

    // ==============================================================
    //  POR QUE PRECISAMOS DESTA ROTAÇÃO?
    // ==============================================================
    //  O Blender (programa de modelagem 3D) usa eixos diferentes da Unity:
    //  Blender: eixo Z aponta para CIMA.
    //  Unity:   eixo Y aponta para CIMA.
    //  Quando um modelo é exportado do Blender para a Unity, ele chega
    //  "deitado" porque os eixos foram trocados na conversão.
    //  Para corrigir, configure aqui X = 270 (equivale a -90 graus).
    //
    //  Vector3 é uma estrutura que armazena 3 números (X, Y, Z).
    //  Aqui representa graus de rotação em cada eixo.
    //  Vector3.zero = (0, 0, 0) = sem rotação.
    public Vector3 placementRotationEuler = Vector3.zero;

    [Header("Tamanho da Verificação de Espaço")]

    // ==============================================================
    //  PARA QUE SERVE overlapBoxSize?
    // ==============================================================
    //  Antes de colocar o objeto, criamos uma caixa invisível desse tamanho
    //  para verificar se o espaço já está ocupado por outro objeto.
    //  É como tentar encaixar uma caixa num espaço antes de colocá-la de verdade.
    //  X = largura, Y = altura, Z = profundidade (em metros na cena Unity).
    //  new Vector3(1f, 1f, 1f) cria um vetor com todos os valores iguais a 1.
    //  O "f" após o número indica que é um float (número com casas decimais).
    public Vector3 overlapBoxSize = new Vector3(1f, 1f, 1f);
}
