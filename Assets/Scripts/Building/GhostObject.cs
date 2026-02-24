using UnityEngine;

// ==============================================================
//  O QUE FAZ O GhostObject?
// ==============================================================
//  Este script fica colado nos prefabs "_Ghost" — a versão fantasma
//  de cada item construível (ex: BearTrap_Ghost, Barricade_Ghost).
//  Sua única responsabilidade é mudar a aparência visual do fantasma:
//
//  VERDE    → o espaço está livre, pode colocar!
//  VERMELHO → o espaço está ocupado, NÃO pode colocar!
//
//  Quem decide se é verde ou vermelho é o BuildingController.
//  O GhostObject só obedece: ele recebe um true/false e troca o material.
public class GhostObject : MonoBehaviour {

    // ==============================================================
    //  O QUE É [SerializeField]?
    // ==============================================================
    //  Normalmente, campos "private" são invisíveis no Inspector.
    //  O atributo [SerializeField] faz uma exceção: torna o campo
    //  visível no Inspector, mas mantém protegido no código.
    //  Por que usar em vez de "public"? Porque "public" deixaria outros
    //  scripts alterarem esses valores livremente — o que não queremos.

    [Header("Materiais")]

    // Material verde semi-transparente — aplicado quando o espaço está livre.
    // Um "Material" na Unity define a cor, textura e transparência de um objeto.
    [SerializeField] private Material validMaterial;

    // Material vermelho semi-transparente — aplicado quando o espaço está ocupado.
    [SerializeField] private Material invalidMaterial;

    // ==============================================================
    //  O QUE É Renderer[]?
    // ==============================================================
    //  Um "Renderer" é o componente da Unity responsável por DESENHAR
    //  um objeto 3D na tela. É ele que sabe qual material usar.
    //  Um modelo 3D complexo pode ter várias partes separadas na Hierarchy,
    //  cada uma com seu próprio Renderer.
    //  Ex: uma barricada pode ter o poste e as tábuas como filhos distintos.
    //  O "[]" indica que é um ARRAY (lista ordenada) de Renderers.
    private Renderer[] renderers;

    // Variável interna que guarda o estado atual (livre ou ocupado).
    // "bool" aceita apenas dois valores: true (verdadeiro) ou false (falso).
    private bool isPlaceable = false;

    // ==============================================================
    //  O QUE É Awake()?
    // ==============================================================
    //  Awake() é um método especial da Unity chamado automaticamente
    //  quando o objeto é CRIADO na cena, antes de qualquer outra coisa.
    //  É o "nascimento" do objeto — ideal para inicializar variáveis.
    //  Ordem de execução Unity: Awake → OnEnable → Start → Update.
    private void Awake() {
        // ==============================================================
        //  O QUE É GetComponentsInChildren<Renderer>()?
        // ==============================================================
        //  Busca um componente (aqui, Renderer) em:
        //    1. Este próprio GameObject
        //    2. Todos os GameObjects FILHOS (e filhos dos filhos, etc.)
        //  Retorna um array com TODOS os Renderers encontrados.
        //  Equivale a: "me dê todos os pedaços visuais deste objeto
        //               e de todos os seus filhos na Hierarchy".
        renderers = GetComponentsInChildren<Renderer>();
    }

    // ==============================================================
    //  SetPlaceable(bool placeable) — atualiza a cor do fantasma
    // ==============================================================
    //  Chamado pelo BuildingController a cada frame.
    //  Recebe true se o lugar está livre, false se está ocupado.
    //  Troca o material de TODOS os Renderers para verde ou vermelho.
    public void SetPlaceable(bool placeable) {
        isPlaceable = placeable;

        // ==============================================================
        //  O QUE É O OPERADOR TERNÁRIO ( condição ? a : b )?
        // ==============================================================
        //  É uma forma compacta de escrever um if/else em uma linha.
        //  Formato:  condição ? valor_se_verdadeiro : valor_se_falso
        //  Aqui: "se placeable for true, usa validMaterial (verde),
        //         senão usa invalidMaterial (vermelho)"
        //  É idêntico a escrever:
        //    Material mat;
        //    if (placeable) mat = validMaterial;
        //    else           mat = invalidMaterial;
        Material mat = placeable ? validMaterial : invalidMaterial;

        // ==============================================================
        //  O QUE É foreach?
        // ==============================================================
        //  É um laço que percorre CADA item de uma coleção, um por vez.
        //  A variável "r" representa o Renderer atual nessa passagem.
        //  Aqui: para cada Renderer encontrado, troca o material dele.
        //  ".material" é a propriedade do Renderer que define qual material
        //  ele usa. Substituí-la troca instantaneamente a cor do objeto.
        foreach (Renderer r in renderers)
            r.material = mat;
    }

    // ==============================================================
    //  O QUE É "=>" (Expression Body / Corpo de Expressão)?
    // ==============================================================
    //  É uma forma compacta de escrever um método que só tem um return.
    //  "public bool IsPlaceable() => isPlaceable;"
    //  É idêntico a:
    //    public bool IsPlaceable() { return isPlaceable; }
    //  Usado quando o método é simples o suficiente para caber em uma linha.
    public bool IsPlaceable() => isPlaceable;
}
