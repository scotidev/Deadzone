Sempre que for atuar em uma subpasta, verifique se existe um arquivo AGENTS.md nela e siga as diretrizes específicas daquele módulo.
Use o sistema de input da Unity novo.
Todas as variáveis, nomes, classes, resumos, comentários, tudo deve ser escrito em inglês.
Acima de cada classe ou função criada, adicione comentários semânticos XML explicativos a fim de documentar, faça apenas para classes e funções.
Em todos os Scripts mantenha a boa prática de não colocar lógica dentro do Update, use o Update apenas para chamar funções, e a lógica deve estar dentro dessas funções.

# Regras de EXCLUSÃO DE CONTEXTO:
## NÃO INCLUA, NÃO LEIA, NÃO ANALISE, NÃO RESPONDA SOBRE NENHUM DOS ITENS ABAIXO:

# Pastas de Sistema (Ignora a pasta e todo o conteúdo)
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/MemoryCaptures/

# Pastas de Configuração e Pacotes 
/Packages/
/ProjectSettings/

# Arquivos de IDE e Compilação
*.csproj
*.sln
*.suo
*.user
*.userprefs
*.pdb
*.opendb
*.VC.db

# Metadados do Unity (Essencial para economizar contexto)
*.meta

# Assets Binários e Grandes (O Cline não consegue ler ou editar)
# Se ele tentar ler um .unity ou .prefab, ele vai desperdiçar todo o seu contexto.
*.unity
*.prefab
*.asset
*.mat
*.fbx
*.obj
*.mesh
*.anim
*.controller
*.overrideController
*.physicMaterial
*.physicsMaterial2D

# Mídia
*.png
*.jpg
*.jpeg
*.tga
*.psd
*.tif
*.tiff
*.wav
*.mp3
*.ogg
*.mp4
*.mov

# Plugins (Evita que a IA tente ler DLLs ou SDKs de terceiros)
/[Aa]ssets/[Pp]lugins/