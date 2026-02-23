Sempre que for atuar em uma subpasta, verifique se existe um arquivo AGENTS.md nela e siga as diretrizes específicas daquele módulo.
Considere o arquivo .clineignore da raiz.
Use o sistema de input da Unity novo: Input System, usando o .current com .wasPressedThisFrame.
Todas as variáveis, nomes, classes, resumos, comentários, tudo deve ser escrito em inglês.
Acima de cada classe ou função criada, adicione comentários semânticos XML explicativos a fim de documentar, faça apenas para classes e funções.
Em todos os Scripts mantenha a boa prática de não colocar lógica dentro do Update, use o Update apenas para chamar funções, e a lógica deve estar dentro dessas funções.