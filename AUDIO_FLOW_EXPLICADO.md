# Fluxo de áudio do projeto (explicado para leigos)

Este projeto agora está **centralizado em um sistema principal de áudio**: `AudioManagerService` (em `Assets/Scripts/Audio/AudioManagerService.cs`).

## 1) Quem cria o sistema de áudio?

1. Quando o jogo inicia, o `Bootstraper` roda antes da cena carregar.
2. Ele cria um GameObject chamado **"Sound Manager"**.
3. Nesse GameObject, ele adiciona o componente `AudioManagerService`.
4. Esse objeto vira `DontDestroyOnLoad`, então **não é destruído ao trocar de cena**.
5. O serviço é registrado no `ServiceLocator`, para qualquer script pegar com:
   `ServiceLocator.Current.Get<IAudioManagerService>()`.

**Resumo simples:** o áudio principal não depende de você arrastar um manager manualmente em cada cena.

---

## 2) Como o áudio toca por dentro (mecânica real)

O `AudioManagerService` trabalha com dois jeitos de tocar som:

1. **BGM (música de fundo):**
   - Usa **um AudioSource fixo**, criado no próprio Sound Manager.
   - Esse source é 2D (`spatialBlend = 0`), com loop e fade opcional.

2. **Sons one-shot (efeitos, diálogos, tiros etc):**
   - Para cada som, ele cria um **GameObject temporário**.
   - Adiciona um `AudioSource` nesse objeto.
   - Configura volume e 2D/3D.
   - Toca o clip.
   - Quando termina, destrói esse GameObject automaticamente.

**Resumo simples:** música usa um player fixo; efeitos usam players descartáveis.

---

## 3) Fluxo dos sons de menu/UI

Exemplo: `MenuButtonAudio`, `HitmarkerManager`, `WaveManager`.

1. O script pega `IAudioManagerService`.
2. Quando acontece o evento (hover/click/hit/início de wave), chama:
   `PlaySFX2D(clip)`.
3. O serviço toca em 2D (sem posição no mundo).

Isso é ideal para UI porque o som deve ser sempre claro, independente da posição do jogador.

---

## 4) Fluxo dos sons de armas e tiros

### 4.1 Sons de animação da arma (holster, reload, tiro, empty)

1. A animação entra em um estado (Animator State).
2. `PlaySoundCharacterBehaviour` ou `PlaySoundBehaviour` é executado.
3. Eles escolhem o `AudioClip` da arma.
4. Chamam `PlayOneShot` ou `PlayOneShotDelayed` no serviço.
5. O serviço cria o GameObject temporário e toca.

### 4.2 Poder exclusivo da arma (nível 10)

1. `ExclusivePowerBehaviour.ActivatePower()` dispara.
2. Chama `PlaySFX3DAttached(...)`.
3. O som fica **espacial 3D** e segue o transform da arma.

---

## 5) Fluxo do NPC Merchant (diálogo + legenda)

Agora o `NPCAudio` trabalha com **pools aleatórios** de falas por contexto.

### 5.1 Contextos implementados

1. **Abrir loja**
   - `NPC.Interact()` chama `NPCAudio.PlayRandomShopOpenDialogue()`.
   - O sistema sorteia uma fala da lista `shopOpenDialogues`.

2. **Comprar munição**
   - No fluxo atual, o botão principal da direita vira **BUY AMMO** quando a arma está no nível máximo.
   - `ShopUI` dispara o evento `AmmoPurchased` quando a compra de munição é concluída.
   - `NPCAudio` escuta esse evento e sorteia uma fala da lista `ammoPurchaseDialogues`.

3. **Desbloquear arma**
   - `ShopUI` dispara o evento `WeaponUnlocked` quando uma arma é desbloqueada.
   - `NPCAudio` pega o `weaponID` e usa a lista específica daquela arma (`weaponUnlockDialogues`).

### 5.2 Como a legenda aparece

1. Cada fala tem:
   - `AudioClip`
   - `subtitle` (texto manual que você escreve)
   - `subtitleDurationOverride` (opcional)
2. O áudio toca em 2D (`PlayDialogue2D`).
3. O `MerchantSubtitleUI` mostra a legenda na tela pelo tempo do áudio (ou override).
4. Quando o tempo termina, a legenda some automaticamente.

### 5.3 Regra anti-poluição sonora

Se já tiver diálogo tocando, **novos diálogos são ignorados** até o atual terminar (foi a regra que você escolheu).

---

## 6) Música de fundo (BGM)

Quando algum script chamar `PlayBGM(clip, loop, fade)`:

1. O `AudioManagerService` usa o `bgmSource` fixo.
2. Se já existir música e tiver fade, ele faz transição suave.
3. Se não tiver fade, troca direto.

Também existe `StopBGM(fade)` e controle de volume de BGM separado.

---

## 7) Canais de volume existentes

Hoje você tem estes controles separados:

1. **BGM Volume** (`SetBGMVolume`)
2. **SFX Volume** (`SetSFXVolume`) para efeitos gerais (UI/gameplay)
3. **Dialogue Volume** (`SetDialogueVolume`) para fala do NPC

Isso permite diminuir diálogo sem mexer na música, por exemplo.

---

## 8) O que ainda usa AudioSource direto no GameObject?

O principal caso é o **som de passos** em `Movement.cs`:

- O player tem um `AudioSource` próprio no GameObject.
- Esse source toca/pausa em loop conforme movimento.

Esse caso é normal porque é um som contínuo de movimento (não só one-shot).

---

## 9) E o antigo `AudioManager.cs`?

Ele foi mantido como **wrapper legado** (compatibilidade), mas agora:

- não é mais o sistema principal;
- apenas encaminha chamadas para `IAudioManagerService`.

Em termos práticos: a fonte de verdade do áudio ficou centralizada no `AudioManagerService`.

---

## 10) Como configurar no Inspector (passo a passo rápido)

1. No NPC Merchant, mantenha o componente `NPCAudio`.
2. Preencha:
   - `Shop Open Dialogues` (várias opções aleatórias)
   - `Ammo Purchase Dialogues` (várias opções aleatórias)
   - `Weapon Unlock Dialogues` (uma lista por `weaponID`)
3. Para cada linha de diálogo, preencha:
   - `clip`
   - `subtitle` (seu texto manual)
   - opcionalmente `subtitleDurationOverride`
4. Na UI, crie um objeto de legenda (painel/texto no rodapé) e adicione `MerchantSubtitleUI`.
5. No `NPCAudio`, arraste a referência do `MerchantSubtitleUI` (ou deixe usar o singleton automático).
6. Em `ShopItemData`, ajuste `AmmoCost`, `AmmoAmountPerPurchase` e `WeaponData.maxReserveAmmo`.

---

## 11) Resumo final em uma frase

**Fluxo padrão:** evento de gameplay/UI (abrir loja, comprar munição, desbloquear arma) -> `NPCAudio` escolhe fala aleatória do contexto -> toca em `IAudioManagerService` (2D) -> `MerchantSubtitleUI` mostra legenda manual -> bloqueia novas falas até terminar.
