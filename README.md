# NR12 Modular — iLogic (Inventor 2024)

Configurador de cercamentos modulares NR-12, inspirado no fluxo de desenho de planta 2D e geração de montagem 3D.

> **Estado:** protótipo do editor 2D e geração 3D geométrica preliminar. **Não validado em Inventor 2024 e não certificado para NR-12.**

## Primeira versão

- Desenho por clique de pontos, em coordenadas de planta (mm).
- Trechos independentes: botão **Novo trecho** encerra a polilinha em edição.
- Seleção individual de segmentos, com aplicação de tipo de painel.
- Módulo **sem janela**, **com janela**, **com porta**, tipo e sentido de porta.
- Snap de desenho livre, 90°, 45° e ângulo absoluto informado.
- Malha visível de 500 mm com encaixe nos cliques; **cotas editáveis em qualquer milímetro exato** por duplo clique na cota ou segmento.
- Ao modificar uma cota, move-se a extremidade final do segmento e os extremos coincidentes de segmentos do mesmo grupo. A geometria vizinha pode mudar de comprimento/ângulo; **a preservação de ortogonalidade é um aprimoramento futuro**.
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

## Biblioteca recebida — 7 - NR12.zip

O pacote CAD de referência contém uma montagem `.iam`, coluna-montante `.ipt`, base soldada `.iam`, tubo quadrado 50x50x2, chapas de base/fixação e pasta `OldVersions`. **Os arquivos CAD ainda não foram incorporados à geração.** Devem ser analisados no Inventor 2024 para confirmar referências e dimensões antes da reutilização.

## Regra de vão máximo entre postes

No gerador 3D, todo trecho sem porta maior que **2500 mm centro a centro** é subdividido em `Ceiling(comprimento/2500)` vãos iguais. Cada subdivisão cria um ponto de poste intermediário; os postes em coordenadas coincidentes são compartilhados. Trechos configurados como porta com comprimento maior que 2500 mm provocam aviso e não são gerados. A divisão ocorre no gerador e ainda não aparece como postes intermediários no editor 2D.

Exemplos: 3000→2×1500 mm; 5000→2×2500 mm; 6000→3×2000 mm. Regra de projeto fornecida pelo usuário, sem implicar validação da NR-12.

## Integracao com modelos CAD reais — etapa experimental

O gerador `05_Gerador_3D/Gerar_3D_NR12.vb` agora insere como ocorrências **reais** de Inventor:

- `7.1.2 - COLUNA-MONTANTE.ipt`
- `7.1.3 - BASE SOLDADA PARA MONTANTE.iam`

A base requer os dois arquivos `7.1.3.1 - CHAPA LISA 3-4x130x130 (BASE).ipt` e `7.1.3.2 - TUBO QUADRADO - 50 x 50 x 2  - 280mm.ipt`.

**Instalação:** extraia o ZIP entregue `NR12_Modelos_Reais.zip` na pasta em que está salva a sua montagem principal, produzindo `NR12_Modelos/...`. Não renomeie os arquivos. No Inventor, se aparecer resolução de referências para a base soldada, aponte para os IPT na mesma pasta; depois salve a montagem base com as referências resolvidas. 

**Limitação:** os modelos reais são inseridos por ocorrência e alinhados provisoriamente pelo centro XY da caixa envolvente e pelo Z mínimo. A origem e orientação dos modelos devem ser conferidas no Inventor 2024; o alinhamento por caixa não assegura coincidência de furação, chapa e perfil. A montagem principal original depende também de itens do Content Center que não acompanham o ZIP e **não** é usada diretamente.

As placas, molduras de janela e folhas de porta continuam sendo sólidos provisórios gerados por iLogic. O gerador não transforma a folha em uma porta mecânica articulada. Nada foi testado no Inventor 2024 nem certificado como solução NR-12.

**Cuidado ao regenerar:** ocorrências cujo nome começa com `NR12_` são apagadas e reinseridas pelo gerador. Faça os testes em uma cópia da montagem.
