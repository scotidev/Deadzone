using UnityEngine;
using UnityEngine.InputSystem;

// ==============================================================
//  VISÃO GERAL: O QUE FAZ O BuildingController?
// ==============================================================
//  É o "gerente central" do modo de construção. Fica colado no mesmo
//  GameObject do Player e coordena TODO o fluxo de colocação de objetos:
//
//  PASSO 1: Jogador aperta tecla 6, 7 ou 8.
//  PASSO 2: O script cria um "fantasma" transparente do objeto escolhido.
//  PASSO 3: A cada frame, um raio sai da câmera e detecta onde o chão está.
//  PASSO 4: O fantasma se move para o ponto onde o raio acertou o chão.
//  PASSO 5: Uma caixa invisível verifica se o espaço está livre ou ocupado.
//  PASSO 6: O GhostObject pinta o fantasma de verde (livre) ou vermelho (ocupado).
//  PASSO 7: Jogador clica LMB → objeto real é instanciado no lugar do fantasma.
//  PASSO 8: ESC ou mesma tecla → cancela, fantasma é destruído, arma volta.
public class BuildingController : MonoBehaviour {

    // ==============================================================
    //  SINGLETON
    // ==============================================================
    //  Permite que outros scripts perguntem:
    //  "O BuildingController está ativo e em modo de colocação?"
    //  Sem precisar de referência direta arrastada no Inspector.
    //  Ex: BuildingController.Instance.IsPlacing
    public static BuildingController Instance { get; private set; }

    // ==============================================================
    //  CAMPOS SERIALIZADOS — configuráveis no Inspector
    // ==============================================================
    //  [Header("...")] cria um título visual no Inspector.
    //  [SerializeField] expõe o campo private no Inspector sem torná-lo public.
    //  Cada slot guarda um BuildableSO — a ficha de dados do item.

    [Header("Itens Construíveis (teclas 6 / 7 / 8)")]
    [SerializeField] private BuildableSO itemSlot6; // tecla 6: ex. Barricada
    [SerializeField] private BuildableSO itemSlot7; // tecla 7: ex. Barril Explosivo
    [SerializeField] private BuildableSO itemSlot8; // tecla 8: ex. Mina Terrestre

    // ==============================================================
    //  O QUE É LayerMask?
    // ==============================================================
    //  Na Unity, cada GameObject pertence a uma Layer (camada).
    //  Ex: "Ground", "Wall", "Obstacle", "Player", "Enemy"...
    //  LayerMask é um filtro: "meu raio/caixa deve interagir APENAS
    //  com objetos nas layers marcadas aqui".
    //  Você configura quais layers no Inspector (são checkboxes).
    //  Isso evita que o raio ou caixa detecte objetos indesejados.

    [Header("Configurações de Detecção")]
    [SerializeField] private LayerMask groundLayer;    // chão onde objetos podem ser colocados
    [SerializeField] private LayerMask wallLayer;      // paredes que bloqueiam o posicionamento
    [SerializeField] private LayerMask obstacleLayer;  // objetos que bloqueiam o posicionamento
    [SerializeField] private float maxPlacementDistance = 8f; // distância máxima do raio (metros)

    // Câmera ativa do jogador — usada para lançar o raio do centro da tela.
    private Camera playerCamera;

    // O objeto fantasma atualmente ativo na cena (null quando não há nenhum).
    private GameObject currentGhost;

    // Referência para o script GhostObject do fantasma atual.
    // Usado para chamar SetPlaceable(true/false) e mudar a cor.
    private GhostObject currentGhostObject;

    // A ficha (BuildableSO) do item atualmente selecionado pelo jogador.
    private BuildableSO selectedItem;

    // ==============================================================
    //  PROPRIEDADE IsPlacing
    // ==============================================================
    //  Retorna true se há um fantasma ativo na cena, false caso contrário.
    //  "currentGhost != null" é true enquanto o fantasma existir.
    //  "!= null" verifica se a variável não está vazia.
    //  Outros scripts (como PlayerInteraction) usam isto para bloquear
    //  interações enquanto o jogador está no modo construção.
    public bool IsPlacing => currentGhost != null;

    // ==============================================================
    //  Awake() — executado ANTES do Start(), quando o objeto nasce
    // ==============================================================
    private void Awake() {
        // Implementação do Singleton: garante apenas uma instância.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ==============================================================
    //  Start() — executado uma vez, no primeiro frame do jogo
    // ==============================================================
    //  Por que buscar a câmera no Start e não no Awake?
    //  Porque a câmera é filha do Player, e o Awake dos filhos nem
    //  sempre rodou quando o Awake do pai é chamado.
    //  O Start() garante que todos os Awakes já foram executados.
    private void Start() {
        // ==============================================================
        //  GetComponentInChildren<Camera>()
        // ==============================================================
        //  Busca o componente Camera neste GameObject OU em qualquer filho
        //  (incluindo filhos de filhos na Hierarchy).
        //  Equivale a: "me dê a câmera que está dentro do Player".
        playerCamera = GetComponentInChildren<Camera>();

        // Se não achou câmera filha, usa a câmera principal da cena.
        // Camera.main é um atalho da Unity para a câmera com a tag "MainCamera".
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    // ==============================================================
    //  Update() — executado a CADA FRAME (60x por segundo em 60fps)
    // ==============================================================
    //  É o "coração" do script — tudo que precisa ser verificado
    //  continuamente fica aqui.
    private void Update() {
        // Segurança: se por algum motivo a câmera não foi encontrada,
        // não executa nada (evita NullReferenceException).
        if (playerCamera == null) return;

        // Verifica teclas a cada frame — o jogador pode pressionar a qualquer momento.
        HandleSelectionInput();

        // Só atualiza o fantasma e verifica clique SE estiver em modo de colocação.
        // IsPlacing é true apenas enquanto currentGhost != null.
        if (IsPlacing) {
            UpdateGhostPosition(); // move o fantasma para onde a câmera mira
            HandlePlacementInput(); // verifica se o jogador clicou para confirmar
        }
    }

    // ==============================================================
    //  HandleSelectionInput() — detecta quais teclas foram pressionadas
    // ==============================================================
    //  "wasPressedThisFrame" retorna true APENAS no frame exato em que
    //  a tecla foi pressionada (não fica true enquanto segura).
    //  É como a diferença entre "pressionar" e "segurar" uma tecla.
    private void HandleSelectionInput() {
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            SelectItem(itemSlot6);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
            SelectItem(itemSlot7);
        else if (Keyboard.current.digit8Key.wasPressedThisFrame)
            SelectItem(itemSlot8);
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            CancelPlacement();
    }

    // ==============================================================
    //  SelectItem(BuildableSO item) — inicia o modo de colocação
    // ==============================================================
    //  Recebe a ficha (BuildableSO) do item escolhido e:
    //  1. Verifica se é um toggle (mesma tecla = cancelar)
    //  2. Guarda a arma do jogador via CharacterInteraction
    //  3. Instancia o fantasma na cena
    //  4. Desativa os colliders do fantasma
    //  5. Mostra o prompt de instrução na tela
    private void SelectItem(BuildableSO item) {
        // Se o slot não foi configurado no Inspector, não faz nada.
        if (item == null) return;

        // TOGGLE: se o mesmo item já está selecionado, apertar a tecla
        // novamente cancela o modo de construção.
        if (selectedItem == item) {
            CancelPlacement();
            return;
        }

        // Se havia um fantasma de outro item, destrói-o antes de criar o novo.
        DestroyCurrentGhost();
        UIManager.Instance?.ToggleInteractionPrompt(false);

        // Registra qual item foi selecionado agora.
        selectedItem = item;

        // ==============================================================
        //  O OPERADOR "?." (null-conditional operator)
        // ==============================================================
        //  Verifica se o objeto não é null ANTES de chamar o método.
        //  "CharacterInteraction.Instance?.SetHolstered(true)"
        //  É idêntico a:
        //    if (CharacterInteraction.Instance != null)
        //        CharacterInteraction.Instance.SetHolstered(true);
        //  Evita NullReferenceException se o script não estiver na cena.
        CharacterInteraction.Instance?.SetHolstered(true);
        // Guarda a arma: o jogador não pode atirar durante a construção.

        // Verifica se o item tem um ghostPrefab configurado no BuildableSO.
        if (item.ghostPrefab == null) {
            // ==============================================================
            //  Debug.LogWarning()
            // ==============================================================
            //  Imprime uma mensagem de AVISO (amarela) no Console da Unity.
            //  Não trava o jogo, mas avisa sobre erro de configuração.
            //  O "$" antes das aspas permite usar variáveis no texto:
            //  isso se chama "interpolação de strings" (string interpolation).
            //  {item.displayName} é substituído pelo valor real da variável.
            Debug.LogWarning($"[BuildingController] '{item.displayName}' não tem Ghost Prefab configurado no BuildableSO!");
            selectedItem = null;
            CharacterInteraction.Instance?.SetHolstered(false);
            return;
        }

        // ==============================================================
        //  Instantiate(prefab, posição, rotação)
        // ==============================================================
        //  Cria uma cópia do prefab na cena.
        //  - prefab:  o molde a ser copiado (item.ghostPrefab)
        //  - posição: onde na cena criar (Vector3.zero = ponto de origem (0,0,0))
        //  - rotação: qual rotação ter (Quaternion.identity = sem rotação)
        //  O fantasma começa em (0,0,0) e será movido no UpdateGhostPosition().
        //  Retorna o GameObject criado, que salvamos em currentGhost.
        currentGhost = Instantiate(item.ghostPrefab, Vector3.zero, Quaternion.identity);

        // Desativa todos os Colliders do fantasma para que ele não interfira
        // com o OverlapBox que verifica se o espaço está ocupado.
        // GetComponentsInChildren retorna todos os Colliders (inclusive de filhos).
        // O foreach percorre cada um e desativa com col.enabled = false.
        foreach (Collider col in currentGhost.GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Esconde o fantasma por enquanto.
        // SetActive(false) "desliga" o objeto: ele existe na cena mas fica invisível.
        // Ele aparecerá quando o raio acertar o chão em UpdateGhostPosition().
        currentGhost.SetActive(false);

        // Busca o script GhostObject que está neste prefab fantasma.
        // Ele controla a cor (verde/vermelho) do fantasma.
        // GetComponent<T>() busca o componente T neste MESMO GameObject.
        currentGhostObject = currentGhost.GetComponent<GhostObject>();

        // Mostra na tela as instruções para o jogador.
        // O $"..." com {variavel} é interpolação de strings.
        UIManager.Instance?.ToggleInteractionPrompt(true,
            $"{item.displayName}  ·  [LMB] Colocar  ·  [ESC] Cancelar");
    }

    // ==============================================================
    //  UpdateGhostPosition() — move o fantasma a cada frame
    // ==============================================================
    //  Esta é a função mais complexa do sistema.
    //  A cada frame, lança um raio do centro da tela em direção ao mundo
    //  e posiciona o fantasma onde o raio acerta o chão.
    private void UpdateGhostPosition() {
        // ==============================================================
        //  O QUE É UM Ray (raio)?
        // ==============================================================
        //  Um Ray é uma linha que começa em um ponto e vai em uma direção,
        //  indefinidamente. Na Unity, raios são usados para detectar o que
        //  está na frente do jogador — como uma "mira invisível".
        //
        //  ViewportPointToRay(x, y, z) cria um raio que sai de um ponto
        //  na câmera em coordenadas de Viewport (0 a 1):
        //    (0,0) = canto inferior esquerdo da tela
        //    (1,1) = canto superior direito da tela
        //    (0.5, 0.5) = centro EXATO da tela (onde fica o crosshair/mira)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // ==============================================================
        //  Monta a LayerMask do raio
        // ==============================================================
        //  O raio precisa detectar CHÃO e PAREDES.
        //  Se detectasse só o chão, o raio passaria através das paredes
        //  e o objeto poderia ser posicionado do outro lado delas.
        //  ".value != 0" verifica se alguma layer foi marcada no Inspector.
        //  Physics.DefaultRaycastLayers = todas as layers (fallback de segurança).
        LayerMask groundMask = groundLayer.value != 0 ? groundLayer : Physics.DefaultRaycastLayers;
        LayerMask raycastMask = wallLayer.value != 0 ? groundMask | wallLayer : groundMask;
        // O operador "|" (OR bit a bit) combina duas LayerMasks.
        // É como dizer: "detecta objetos na groundLayer OU na wallLayer".

        // ==============================================================
        //  Physics.Raycast(ray, out hit, distância, máscara)
        // ==============================================================
        //  Lança o raio no mundo 3D e verifica se acertou algum objeto.
        //  - ray:       o raio (ponto de origem + direção)
        //  - out hit:   variável de saída — se acertar algo, é preenchida
        //               com informações: ponto de impacto, normal, distância...
        //               "out" significa que o método vai ESCREVER nessa variável.
        //  - distância: até onde o raio alcança (maxPlacementDistance metros)
        //  - máscara:   filtra quais layers o raio pode detectar
        //  Retorna TRUE se acertou algo, FALSE se não acertou nada.
        //
        //  "hit.distance > 0.5f" evita que o fantasma apareça colado
        //  na arma do jogador (que também possui collider).
        if (Physics.Raycast(ray, out RaycastHit hit, maxPlacementDistance, raycastMask)
            && hit.distance > 0.5f) {

            // ==============================================================
            //  O QUE É hit.normal?
            // ==============================================================
            //  Normal é um vetor perpendicular (90°) à superfície atingida.
            //  Indica a "direção que a superfície está apontando".
            //
            //  Para o CHÃO (superfície horizontal):
            //    normal aponta para cima → normal.y ≈ 1.0
            //
            //  Para PAREDES (superfícies verticais):
            //    normal aponta para o lado → normal.y ≈ 0.0
            //
            //  Se normal.y < 0.5, o raio bateu em algo muito inclinado
            //  (parede ou rampa íngreme) — esconde o fantasma e sai.
            if (hit.normal.y < 0.5f) {
                currentGhost.SetActive(false);
                return; // "return" sai da função imediatamente.
            }

            // ==============================================================
            //  Calcula a posição elevada para o objeto ficar SOBRE o chão
            // ==============================================================
            //  hit.point é o ponto EXATO onde o raio tocou a superfície.
            //  O pivot (ponto central) do prefab fica no centro geométrico do objeto.
            //  Se colocássemos o pivot no hit.point, metade do objeto ficaria
            //  enterrada no chão. Por isso somamos metade da altura:
            //
            //  Vector3.up = atalho para Vector3(0, 1, 0) — direção para cima.
            //  Multiplicar por um número escala o vetor:
            //  Vector3.up * 2f = Vector3(0, 2, 0) → sobe 2 metros.
            //
            //  overlapBoxSize.y é a altura do objeto configurada no BuildableSO.
            //  * 0.5f = metade da altura → eleva o objeto exatamente o suficiente.
            Vector3 placementPos = hit.point + Vector3.up * (selectedItem.overlapBoxSize.y * 0.5f);

            // Move o fantasma para a posição calculada.
            // ".transform" é o componente Transform de todo GameObject.
            // Ele guarda posição, rotação e escala do objeto no espaço 3D.
            currentGhost.transform.position = placementPos;

            // ==============================================================
            //  Quaternion.Euler(x, y, z)
            // ==============================================================
            //  Um Quaternion é a representação matemática de rotação usada
            //  internamente pela Unity. É mais eficiente que ângulos de Euler,
            //  mas difícil de ler. Por isso usamos Quaternion.Euler():
            //  você passa os ângulos em graus (X, Y, Z) e ele converte.
            //  Aqui aplica a rotação corretiva configurada no BuildableSO
            //  (ex: X=270 para corrigir modelos exportados do Blender).
            currentGhost.transform.rotation = Quaternion.Euler(selectedItem.placementRotationEuler);

            // Monta a máscara de overlap: verifica paredes E obstáculos.
            LayerMask overlapMask = wallLayer.value != 0 ? obstacleLayer | wallLayer : obstacleLayer;

            // ==============================================================
            //  Physics.OverlapBox(centro, halfExtents, rotação, máscara)
            // ==============================================================
            //  Cria uma caixa invisível na posição e retorna todos os
            //  Colliders que estão dentro ou tocando essa caixa.
            //
            //  - centro:       posição central da caixa (placementPos)
            //  - halfExtents:  METADE de cada dimensão da caixa (x, y, z)
            //                  (a Unity usa "half extents" — não o tamanho total)
            //                  Por isso dividimos overlapBoxSize por 2 (* 0.5f)
            //  - rotação:      Quaternion.identity = sem rotação (alinhada ao mundo)
            //  - máscara:      só detecta objetos nas layers indicadas
            //
            //  Retorna um array de Colliders encontrados dentro da caixa.
            //  Se o array estiver VAZIO (Length == 0), o espaço está livre!
            Collider[] collisions = Physics.OverlapBox(
                placementPos,
                selectedItem.overlapBoxSize * 0.5f,
                Quaternion.identity,
                overlapMask
            );

            // Atualiza a cor do fantasma: verde se livre, vermelho se ocupado.
            // "collisions.Length == 0" é true se nenhum Collider foi encontrado.
            // O "?." evita erro caso currentGhostObject seja null.
            currentGhostObject?.SetPlaceable(collisions.Length == 0);

            // Exibe o fantasma (estava escondido se o raio não acertou o chão antes).
            currentGhost.SetActive(true);
        } else {
            // O raio não acertou nenhuma superfície válida (ou passou do alcance).
            // Esconde o fantasma para não ficar flutuando na posição antiga.
            currentGhost.SetActive(false);
        }
    }

    // ==============================================================
    //  HandlePlacementInput() — detecta o clique para confirmar
    // ==============================================================
    //  Verifica se o botão esquerdo do mouse foi pressionado neste frame.
    //  "wasPressedThisFrame" é true APENAS no frame exato do clique.
    private void HandlePlacementInput() {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryPlaceObject();
    }

    // ==============================================================
    //  TryPlaceObject() — tenta colocar o objeto real na cena
    // ==============================================================
    //  Só instancia o objeto real se TODAS as condições forem satisfeitas:
    //  1. O fantasma existe e está visível (SetActive(true))
    //  2. O espaço está livre (fantasma verde)
    //  3. O item tem um realPrefab configurado no BuildableSO
    private void TryPlaceObject() {
        // "activeSelf" é true se o objeto está ativo (SetActive(true)).
        if (currentGhost == null || !currentGhost.activeSelf) return;

        // Verifica se o GhostObject existe e se o lugar está livre.
        if (currentGhostObject == null || !currentGhostObject.IsPlaceable()) return;

        // Verifica se há um prefab real configurado no BuildableSO.
        if (selectedItem.realPrefab == null) return;

        // Instancia o objeto REAL na mesma posição e com a mesma rotação do fantasma.
        // Este objeto ficará permanentemente na cena (até ser destruído por outra lógica).
        Instantiate(selectedItem.realPrefab,
            currentGhost.transform.position,
            Quaternion.Euler(selectedItem.placementRotationEuler));

        // Encerra o modo de construção logo após colocar o objeto.
        CancelPlacement();
    }

    // ==============================================================
    //  DestroyCurrentGhost() — destrói apenas o fantasma
    // ==============================================================
    //  Separado do CancelPlacement() para poder ser chamado ao trocar
    //  de item sem mexer no estado da arma (evita flash de animação).
    private void DestroyCurrentGhost() {
        if (currentGhost != null) {
            // Destroy() remove o objeto da cena e libera a memória.
            // Diferente de SetActive(false) que apenas esconde —
            // Destroy() apaga o objeto definitivamente.
            Destroy(currentGhost);
            currentGhost = null; // Limpa a referência para evitar acessar objeto destruído.
        }
        currentGhostObject = null;
    }

    // ==============================================================
    //  CancelPlacement() — encerra o modo de construção completamente
    // ==============================================================
    //  Limpa tudo: destrói o fantasma, deseleciona o item,
    //  revela a arma do jogador e esconde o prompt de instrução.
    private void CancelPlacement() {
        DestroyCurrentGhost();
        selectedItem = null; // null = "nenhum item selecionado"
        CharacterInteraction.Instance?.SetHolstered(false); // revela a arma
        UIManager.Instance?.ToggleInteractionPrompt(false); // esconde o texto na tela
    }

    // ==============================================================
    //  OnDestroy() — chamado quando este objeto é removido da cena
    // ==============================================================
    //  Unity chama OnDestroy() automaticamente antes de destruir o objeto.
    //  Garante que o modo de construção seja cancelado se o Player for
    //  destruído (ex: morte do jogador, troca de cena).
    private void OnDestroy() {
        // Limpa o singleton APENAS se EU sou a instância atual.
        // Evita que outro BuildingController perca sua referência.
        if (Instance == this) Instance = null;
        CancelPlacement();
    }
}
