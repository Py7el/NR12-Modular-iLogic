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


## Calibração do conjunto real — versão 0.7 (experimental)

A regra `05_Gerador_3D/Gerar_3D_NR12.vb` foi atualizada para:

- Usar o sentido do perímetro fechado (área orientada) para determinar o **lado externo**. Em trechos independentes/abertos, considera como externo o lado direito do sentido em que foram desenhados.
- Calcular uma normal média nos cantos e orientar montantes/bases em função dela.
- Deslocar painel, janelas e portas para o plano externo dos grampos.
- Inserir a chapa real `7.1.4 - CHAPA LISA 1-2x130x185.ipt` **provisoriamente como chapa inferior**, com a base soldada e coluna acima. **A identificação desta chapa como a peça preta das imagens NÃO foi confirmada** e pode estar errada. Desative com `USAR_CHAPA_INFERIOR_REAL = False` caso não corresponda.
- Manter a subdivisão de vão máximo 2500 mm centro a centro.

**Parâmetros no começo do gerador:** `AFASTAMENTO_PLANO_GRADE_MM = 65`, `ANGULO_AJUSTE_GRAMPOS_GRAUS = 0`, `FOLGA_INFERIOR_GRADE_MM = 50` e `USAR_CHAPA_INFERIOR_REAL = True`. São pontos de partida, **não medidas extraídas/verificadas dos CAD**, e devem ser ajustados após inspeção na montagem.

**Limite importante:** a classificação de eixos pela caixa envolvente não revela a face CAD onde estão os grampos, nem garante o alinhamento das interfaces de solda e furação. A coluna e a base são posicionadas como ocorrências distintas; para fixação mecânica correta, será necessário definir referências locais e relações de montagem (ou usar o conjunto principal com referências resolvidas). Não use esta versão como montagem final de fabricação ou de conformidade NR-12. Testar em uma cópia: o gerador apaga as ocorrências anteriores cujos nomes comecem com `NR12_`.

## Correcao de cantos e base — v0.8

- O arquivo de base real `7.1.3 - BASE SOLDADA PARA MONTANTE.iam` ja incorpora a chapa real `7.1.3.1 - CHAPA LISA 3-4x130x130 (BASE).ipt`. A adicao errada da chapa avulsa `7.1.4` sob a base foi retirada.
- Poste de canto: usa normal do primeiro trecho incidente em vez de calcular a bissetriz (que causava orientacao de 45°). Ao compartilhar um canto, o poste continua unico.
- As outras faces do canto agora recebem dois **marcadores genericos** por face, identificados `NR12_PRESILHA_CANTO_REFERENCIA_...`, a 450 e 1550 mm de Z. Eles **nao sao a presilha CAD verdadeira e nao devem ser usados para fabricacao**. Podem ser desligados definindo `GerarMarcadoresCanto() = False`.
- Os montantes e bases continuam posicionados com orientacao de estimativa derivada da caixa envolvente; a face exata dos grampos, coincidencia de furos e alturas das interfaces **ainda exigem calibracao com referencia CAD local**. O arquivo do modelo da presilha nao veio como componente individual identificavel no ZIP.
- A regeneracao remove ocorrencias com prefixo `NR12_`. Validar tudo em uma copia da montagem.


## Grampos nas faces dos cantos — v0.9 (experimental)

No gerador `Gerar_3D_NR12.vb`, cada poste guarda as normais externas distintas dos **segmentos que possuem fechamento**; segmentos com `Tipo=Vazio` e sem porta não adicionam grampos. O conjunto real `7.1.2 - COLUNA-MONTANTE.ipt` é usado uma única vez por poste. Como o modelo da coluna já mostra grampos em uma face, o gerador só cria um par complementar (em duas alturas) para **cada face adicional**. No encontro de duas grades a 90°, haverá dois lados de fixação, sem girar o poste a 45°. Duas seções retas colineares com a mesma normal compartilham uma face.

**Limitação material:** o ZIP entregue não inclui um arquivo `GRAMPO_NR12.ipt` individual. Se existir um arquivo de grampo independente com esse nome em `NR12_Modelos`, ele é inserido como peça real; em caso contrário, o gerador cria blocos genéricos identificados no navegador por `NR12_GRAMPO_EXTRA_REFERENCIA_...`. **Os blocos não são os grampos do fabricante e não devem ser usados para fabricação.** A origem, a orientação e a interface de fixação do `GRAMPO_NR12.ipt` também devem ser conferidas no Inventor.

As funções de calibração ao final do gerador são `AfastamentoGrampoMm()`, `AlturaGrampoInferiorMm()` e `AlturaGrampoSuperiorMm()`. Os valores são provisórios e não vieram de cotas verificadas nos modelos.

Teste numa **cópia** do IAM: a regeneração remove e reinsere ocorrências de nome iniciado por `NR12_`. Ainda não houve compilação do código dentro do Autodesk Inventor 2024.


## Modelos reais de coluna de duas faces A/B — v1.0 experimental

Foram recebidos os dois IPT do usuário:
- `7.1.2 - COLUNA-MONTANTE 2 lado a.ipt`
- `7.1.2 - COLUNA-MONTANTE 2 lado b.ipt`

Copie **ambos** para `NR12_Modelos` ao lado da montagem principal `.iam`. O gerador passa a escolher um IPT de duas faces para cada canto de 90° com duas faces de fechamento. Postes com uma única face usam o `7.1.2 - COLUNA-MONTANTE.ipt` original. Não há mais inserção de grampos genéricos.

**Orientação A/B ainda precisa ser confirmada no Inventor**: `VarianteAParaGiroPositivo()` troca a correspondência entre A/B e sentido da curva. As prévias incluídas nos IPT permitem confirmar que os modelos são diferentes, mas não estabelecem as referências locais de montagem ou a face real dos grampos. Por padrão `InserirBaseSeparadaNosCantos()` retorna `False`, para evitar duplicação visual da chapa mostrada nas prévias; se os novos IPT não incluírem a base mecanicamente, ajustar para `True` após inspecionar o CAD.

**Advertência:** sem teste no Autodesk Inventor 2024, não há garantia de alinhamento dos furos, faces ou compatibilidade das referências. O gerador remove ocorrências anteriores com prefixo `NR12_` durante regeneração. Testar em cópia da montagem.


## Continuidade e porta no editor — v0.7 (experimental)

O painel carrega `NR12_layout.txt` automaticamente ao abrir e salva as modificacoes apos criacao/edicao/exclusao de segmentos, edicao de cotas, fechamento, insercao de porta, e ao fechar a janela. **Caso o arquivo existente nao possa ser lido, o editor bloqueia a gravacao para evitar perda dos dados originais.** A recuperacao automatica depende de o layout antigo ter sido salvo previamente.

- **DESENHAR / LIGAR**: clique num vertice existente para iniciar uma nova linha no mesmo ponto. Clique no meio de um painel para subdividi-lo e criar um ponto de conexao. Cliques no vazio respeitam a grade de 500 mm; vertices existentes e cotas editadas preservam coordenadas exatas.
- **SELECIONAR**: clique num segmento existente para alterar propriedades, inserir porta ou editar cota (duplo clique).
- **INSERIR PORTA NO TRECHO**: apos selecionar um painel em qualquer contorno, ajuste largura exata (300–2500 mm), distancia desde o ponto inicial do segmento, tipo e sentido de abertura numa unica janela. A insercao substitui o segmento por painel anterior / porta / painel posterior, sem quebrar o grupo de cercado; sobras de 1–99,99 mm sao recusadas.
- **Novo trecho** cria outra polilinha independente; ela pode comecar em ponto ja existente e conectar cercados.
- A configuracao e as alterações do editor continuam experimentais, e a geracao deve ser testada numa copia do IAM.

## Identificacao automatica das colunas A/B — gerador v1.1

A deteccao agora agrupa as faces de cada poste pela **direcao dos trechos** em vez da normal do fechamento. Uma quina de 90° com duas direcoes ativas ativa o CAD A ou B. Um painel segmentado em partes colineares continua usando o poste reto, e uma ligacao em T com uma direcao perpendicular e uma colinear pode ser reconhecida como duas direcoes; **isso nao valida a fixacao mecanica desse encontro**.

No fim da geracao, a caixa de resultado informa **quinas detectadas | colunas A | colunas B**. Se disser 0 quinas, conferir se os extremos coincidem no `NR12_layout.txt` e se os segmentos possuem fechamento. Para usar os modelos A/B, mantenha ambos os IPT em `NR12_Modelos` com seus nomes originais. A associacao entre A/B e a geometria fisica dos grampos ainda pode exigir inverter `VarianteAParaGiroPositivo()` apos testar no Inventor 2024.
