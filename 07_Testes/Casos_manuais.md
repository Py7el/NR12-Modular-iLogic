# Testes manuais — versão inicial

Executar no Autodesk Inventor 2024, em montagem salva.

- [ ] Regra abre sem erros de compilação.
- [ ] Clique em dois pontos cria um segmento.
- [ ] Terceiro clique prolonga o trecho.
- [ ] **Novo trecho** separa segmentos não conectados.
- [ ] Selecionar segmento, trocar tipo e aplicar.
- [ ] Selecionar segmento, marcar portão e sentido de abertura.
- [ ] Salvar o layout; fechar e reabrir a regra; carregar o layout.
- [ ] Conferir coordenadas e propriedades persistidas.
- [ ] Verificar escala/desenho com tamanhos típicos de cercamento.

Registrar mensagens exatas de erro, incluindo linha da regra.

## Versao 0.3 — editor e gerador 3D

- [ ] Selecionar modulo sem janela, com janela, com porta e tipo de porta
- [ ] Angulos 90, 45, livre e absoluto informado
- [ ] Fechar um contorno pelo clique proximo do primeiro ponto
- [ ] Fechar contorno pelo botao; criar outro trecho desconectado
- [ ] Criar uma segunda regra chamada Gerar_3D_NR12
- [ ] Gerar uma planta com tres lados e conferir direcao, altura e posicao dos paineis
- [ ] Validar postes unicos em cantos compartilhados
- [ ] Gerar janela: quatro barras no lugar de uma chapa
- [ ] Gerar porta: folha simplificada sem ferragens
- [ ] Clicar gerar novamente e conferir que nao duplicou ocorrencias NR12_ anteriores
- [ ] Verificar caminho e unidades (mm no layout; cm na API do Inventor)

## Casos manuais — continuidade, portas e variantes A/B

- [ ] Salvar contorno, fechar janela, reabrir e confirmar que ele aparece automaticamente
- [ ] Comecar novo trecho clicando numa extremidade existente e continuar por outra linha
- [ ] Clicar no meio de um painel existente em DESENHAR; verificar se ele foi subdividido e que a ligacao parte desse ponto
- [ ] Alternar para SELECIONAR, clicar numa linha fechada e inserir porta com largura exata e posicao nao central
- [ ] Testar porta colada a extremidade (sobras 0 mm) e sobras abaixo de 100 mm (bloquear)
- [ ] Fechar e reabrir; confirmar porta, dimensoes, grupo e trecho novos persistidos
- [ ] Geração 3D de retangulo com quatro quinas: mensagem deve mostrar 4 quinas e 4 colunas A/B no total
- [ ] Geração 3D de linha com tres segmentos colineares: nenhum poste de canto A/B
- [ ] Checar nomes reais dos dois IPT de canto em NR12_Modelos e confirmar orientacao fisica no Inventor 2024
