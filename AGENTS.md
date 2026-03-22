This file contains the rules and guidelines for working on the AGENTS module of the project. Please read and follow these instructions carefully to ensure consistency and maintainability of the codebase.

# 1. CLEAN CODE RULES:
- Whenever working in a subfolder, check if there is an AGENTS.md file in it and follow the specific guidelines for that module.
- All variables, names, classes, summaries, comments, everything must be written in English.
- Above each class or function created, add XML semantic comments to document them, only for classes and functions.


# 2. ARCHITECTURE RULES
- Use the new Unity input system.
- In all scripts, maintain the good practice of not putting logic inside the Update method, use Update only to call functions, and the logic should be inside those functions.

# 3. CONTEXT EXCLUSION RULES:
## DO NOT INCLUDE, DO NOT READ, DO NOT ANALYZE, DO NOT RESPOND ABOUT ANY OF THE ITEMS BELOW:

### System Folders (Ignore the folder and all its contents)
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/MemoryCaptures/

### Configuration and Package Folders
/Packages/
/ProjectSettings/

### IDE and Compilation Files
*.csproj
*.sln
*.suo
*.user
*.userprefs
*.pdb
*.opendb
*.VC.db

### Unity Metadata (Essential for saving context)
*.meta

### Large and Binary Assets (The AI cannot read or edit these)
### If it tries to read a .unity or .prefab, it will waste all its context.
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

### Media
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

### Plugins (Prevents the AI from trying to read DLLs or third-party SDKs)
/[Aa]ssets/[Pp]lugins/