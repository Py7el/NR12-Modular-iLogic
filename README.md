# NR12 Modular — iLogic (Inventor 2024)

Configurador de cercamentos modulares NR-12, inspirado no fluxo de desenho de planta 2D e geração de montagem 3D.

> **Estado:** protótipo inicial do editor 2D. A geração 3D e a validação normativa **não** estão implementadas.

## Primeira versão

- Desenho por clique de pontos, em coordenadas de planta (mm).
- Trechos independentes: botão **Novo trecho** encerra a polilinha em edição.
- Seleção individual de segmentos, com aplicação de tipo de painel.
- Identificação de portão e direção de abertura no segmento selecionado.
- Salvar/carregar layout em formato TXT delimitado por ponto e vírgula, ao lado da montagem.
- Interface WinForms com identidade visual roxo/âmbar derivada do projeto de referência fornecido.

## Execução

1. Abra ou crie uma **montagem** do Inventor 2024 e salve-a.
2. Abra **Gerenciar > iLogic > Regras** e crie a regra `Editor_Planta_NR12`.
3. Copie o conteúdo de `01_Interface/Editor_Planta_NR12.vb` para a regra.
4. Execute a regra.
5. Teste desenhos, seleção, configuração e salvamento; o editor **não cria componentes 3D** nesta etapa.

**Observação:** o código foi preparado sem acesso à instalação local do Inventor. É necessário validar a compilação e o comportamento no Inventor 2024 antes de uso em produção.

## Próximas fases

Editor 2D → motor geométrico (subdivisão em módulos, cantos e portões) → biblioteca de peças → gerador de montagem → lista de materiais.

O atendimento à NR-12 exige análise de riscos e verificação específica da instalação. Este software não substitui a validação técnica.
