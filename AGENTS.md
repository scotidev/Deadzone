This file contains the rules and guidelines for working on the AGENTS module of the project. Please read and follow these instructions carefully to ensure consistency and maintainability of the codebase.

# CLEAN CODE RULES:

- Whenever working in a subfolder, check if there is an AGENTS.md file in it and follow the specific guidelines for that module.
- All variables, names, classes, summaries, comments, everything must be written in English.
- Above each class or function created, add XML semantic comments to document them, only for classes and functions.

# ARCHITECTURE RULES:

- Use the new Unity input system.
- In all scripts, maintain the good practice of not putting logic inside the Update method, use Update only to call functions, and the logic should be inside those functions.
- Using Unity Engine 6000.2.10f1. When needed, consult the documentation for this version using context7 MCP.

# TEACHING RULES:

- I'm a game development student, so everytime you write code, you should also comment on the lines up above it an explanation of what it does, covering the first principles of that code.

# CONTEXT EXCLUSION RULES:

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

_.csproj
_.sln
_.suo
_.user
_.userprefs
_.pdb
_.opendb
_.VC.db

### Unity Metadata (Essential for saving context)

\*.meta

### Large and Binary Assets (The AI cannot read or edit these)

### If it tries to read a .unity or .prefab, it will waste all its context.

_.unity
_.prefab
_.asset
_.mat
_.fbx
_.obj
_.mesh
_.anim
_.controller
_.overrideController
_.physicMaterial
_.physicsMaterial2D

### Media

_.png
_.jpg
_.jpeg
_.tga
_.psd
_.tif
_.tiff
_.wav
_.mp3
_.ogg
_.mp4
_.mov

### Plugins (Prevents the AI from trying to read DLLs or third-party SDKs)

/[Aa]ssets/[Pp]lugins/
