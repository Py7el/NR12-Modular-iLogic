# Arquitetura inicial

## Referência visual

Arquivo estudado: `ilogic/Painel_parametros.vb` do ZIP de organizador de gavetas fornecido pelo usuário.

Paleta aproximada do original:
- Roxo principal: RGB(124,42,232)
- Âmbar: RGB(254,197,48)
- Cabeçalho: RGB(27,16,56)
- Fundo: RGB(248,245,252)
- Borda: RGB(227,220,239)

## Coordenadas e dados

- Unidade do editor: milímetros.
- Vista em planta: eixo X para a direita; eixo Y para cima (invertido na tela).
- Cada trecho é formado por segmentos consecutivos entre cliques.
- **Novo trecho** interrompe a continuidade e permite criar um conjunto desconectado.
- Cada segmento armazena tipo de painel, tipo de acesso e sentido de abertura.
- No futuro, um portão com posição intermediária deve dividir o segmento em subtrechos e administrar postes sem duplicação.

## Limites atuais

- O ponto inicial do novo trecho é livre (sem snap/cotas).
- Portões são atributos do segmento inteiro, sem dimensão/largura física.
- Ainda não há dimensionamento, biblioteca CAD, cálculo de conformidade NR-12, checagem de colisões ou geração de montagem.

## Organização proposta

`01_Interface/`: interface WinForms iLogic e painel 2D

`02_Editor_2D/`: futuros modelos e comandos de desenho

`03_Engine_Geometrico/`: subdivisão e normalização de módulos

`04_Biblioteca_Componentes/`: Inventor IPT/IAM reutilizáveis

`05_Gerador_3D/`: inserção/atualização de ocorrências

`06_Documentacao/`: decisões e instruções

`07_Testes/`: cenários de teste
