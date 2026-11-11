# NR12 Modular — iLogic (Inventor 2024)

Configurador de cercamentos modulares NR-12, inspirado no fluxo de desenho de planta 2D e geração de montagem 3D.

> **Estado:** protótipo do editor 2D e geração 3D geométrica preliminar. **Não validado em Inventor 2024 e não certificado para NR-12.**

## Primeira versão

- Desenho por clique de pontos, em coordenadas de planta (mm).
- Trechos independentes: botão **Novo trecho** encerra a polilinha em edição.
- Seleção individual de segmentos, com aplicação de tipo de painel.
- Módulo **sem janela**, **com janela**, **com porta**, tipo e sentido de porta.
- Snap de desenho livre, 90°, 45° e ângulo absoluto informado.
- Fechar cerca ligando último ponto ao início ou pelo botão **Fechar cercado**.
- Salvar/carregar layout em formato TXT delimitado por ponto e vírgula, ao lado da montagem.
- Interface WinForms com identidade visual roxo/âmbar derivada do projeto de referência fornecido.

## Execução

1. Abra ou crie uma **montagem** do Inventor 2024 e salve-a.
2. Abra **Gerenciar > iLogic > Regras** e crie a regra `Editor_Planta_NR12`.
3. Copie o conteúdo de `01_Interface/Editor_Planta_NR12.vb` para a regra.
4. Execute a regra.
5. Crie **outra** regra iLogic na mesma montagem com o nome exato `Gerar_3D_NR12` e copie `05_Gerador_3D/Gerar_3D_NR12.vb`.
6. Execute o editor, desenhe, configure módulos e clique **GERAR 3D**. O editor salva o layout e aciona a segunda regra.

**Observação:** o código foi preparado sem acesso à instalação local do Inventor. É necessário validar a compilação e o comportamento no Inventor 2024 antes de uso em produção. O gerador cria componentes simplificados em `NR12_Gerado/`: poste sólido, painel sólido, porta sólida e moldura vazada (quatro barras). Portas não têm ferragens/articulação; painéis `Tela` ainda são placas sólidas. Na regeneração, ocorrências existentes cujo nome começa com `NR12_` são removidas da montagem e substituídas; mantenha modificações manuais fora desse prefixo.

## Próximas fases

Refinar editor 2D → motor geométrico (subdivisão em módulos, cantos e portões) → biblioteca de peças industriais reais → dimensões e ferragens → lista de materiais.

O atendimento à NR-12 exige análise de riscos e verificação específica da instalação. Este software não substitui a validação técnica.
