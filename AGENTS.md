This file contains the rules and guidelines for working on the AGENTS module of the project. Please read and follow these instructions carefully to ensure consistency and maintainability of the codebase.

# 1. CLEAN CODE RULES:
- Whenever working in a subfolder, check if there is an AGENTS.md file in it and follow the specific guidelines for that module.
- All variables, names, classes, summaries, comments, everything must be written in English.
- Above each class or function created, add XML semantic comments to document them, only for classes and functions.


# ARCHITECURE RULES
- Use the new Unity input system.
- In all scripts, maintain the good practice of not putting logic inside the Update method, use Update only to call functions, and the logic should be inside those functions.

# CONTEXT EXCLUSION RULES:
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