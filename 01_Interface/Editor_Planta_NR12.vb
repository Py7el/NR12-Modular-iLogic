AddReference "System.Drawing"
Imports System.Windows.Forms
Imports SD = System.Drawing
Imports IO = System.IO

' NR12 Modular — prototipo do editor 2D para iLogic / Inventor 2024.
' VERSAO TESTE V0.7 - auto-carregar/salvar, conectar desenhos e porta com cotas exatas.
' A malha de 500 mm limita apenas novos cliques, nao a edicao de cotas.
' Inspirado na composicao e paleta do Painel_parametros.vb do projeto original.
' Ainda nao gera montagem 3D nem valida requisitos normativos.

Sub Main()
    If ThisDoc.Document.DocumentType <> DocumentTypeEnum.kAssemblyDocumentObject Then
        MessageBox.Show("Abra uma montagem (.iam) antes de executar o editor.", "NR12 Modular")
        Exit Sub
    End If
    If String.IsNullOrEmpty(ThisDoc.Document.FullFileName) Then
        MessageBox.Show("Salve a montagem antes de executar o editor.", "NR12 Modular")
        Exit Sub
    End If
    arquivo = IO.Path.Combine(IO.Path.GetDirectoryName(ThisDoc.Document.FullFileName), "NR12_layout.txt")
    CriarJanela()
    ' Recupera automaticamente o layout que pertence a esta montagem.
    If IO.File.Exists(arquivo) Then CarregarLayoutNR12()
    janela.ShowDialog()
End Sub

Public Class SegmentoNR
    Public X1 As Double
    Public Y1 As Double
    Public X2 As Double
    Public Y2 As Double
    Public Grupo As Integer
    Public Tipo As String = "Tela"
    Public Acesso As String = "Sem portao"
    Public Abertura As String = "Esquerda"
    Public Modulo As String = "Sem janela"
    Public Porta As String = "Porta simples"
End Class

Private janela As Form
Private desenho As Panel
Private comboTipo As ComboBox
Private comboAbertura As ComboBox
Private comboModulo As ComboBox
Private comboPorta As ComboBox
Private comboAngulo As ComboBox
Private campoAngulo As NumericUpDown
Private lblSelecionado As Label
Private lblStatus As Label
Private lista As New System.Collections.Generic.List(Of SegmentoNR)
Private selecionado As Integer = -1
Private grupoAtual As Integer = 1
Private temPonto As Boolean = False
Private ultimoX As Double
Private ultimoY As Double
Private arquivo As String = ""
Private modoDesenho As Boolean = True
Private botaoDesenhar As Button
Private botaoSelecionar As Button
Private carregamentoFalhou As Boolean = False
Private ReadOnly ROXO As SD.Color = SD.Color.FromArgb(124, 42, 232)
Private ReadOnly AMBAR As SD.Color = SD.Color.FromArgb(254, 197, 48)
Private ReadOnly ESCURO As SD.Color = SD.Color.FromArgb(27, 16, 56)
Private ReadOnly FUNDO As SD.Color = SD.Color.FromArgb(248, 245, 252)
Private ReadOnly BORDA As SD.Color = SD.Color.FromArgb(227, 220, 239)
Private ReadOnly TEXTO As SD.Color = SD.Color.FromArgb(31, 27, 46)
Private escala As Double = 0.10 ' 500 mm = 50 pixels
Private Const PASSO_GRADE_MM As Double = 500.0
Private origemX As Double = 55
Private origemY As Double = 500

Sub CriarJanela()
    janela = New Form()
    janela.Text = "NR12 Modular | Editor de planta"
    janela.ClientSize = New SD.Size(1110, 790)
    janela.StartPosition = FormStartPosition.CenterScreen
    janela.FormBorderStyle = FormBorderStyle.FixedDialog
    janela.MaximizeBox = False
    janela.BackColor = FUNDO
    janela.Font = New SD.Font("Arial", 9)
    janela.KeyPreview = True
    AddHandler janela.FormClosing, Sub(s As Object, e As FormClosingEventArgs)
        If Not carregamentoFalhou Then SalvarLayoutNR12()
    End Sub
    AddHandler janela.KeyDown, Sub(s As Object, e As KeyEventArgs) TeclaJanela(s, e)

    Dim cab As New Panel()
    cab.Location = New SD.Point(0, 0)
    cab.Size = New SD.Size(1110, 91)
    cab.BackColor = ESCURO
    janela.Controls.Add(cab)
    Dim titulo As New Label()
    titulo.Text = "NR12  /  MODULAR"
    titulo.ForeColor = SD.Color.White
    titulo.Font = New SD.Font("Arial", 19, SD.FontStyle.Bold)
    titulo.Location = New SD.Point(26, 13)
    titulo.Size = New SD.Size(430, 36)
    cab.Controls.Add(titulo)
    Dim subtitulo As New Label()
    subtitulo.Text = "01  PLANTA 2D     •     02  CONFIGURACAO     •     03  GERACAO 3D"
    subtitulo.ForeColor = SD.Color.FromArgb(203, 187, 233)
    subtitulo.Location = New SD.Point(28, 56)
    subtitulo.Size = New SD.Size(800, 19)
    cab.Controls.Add(subtitulo)

    Dim textoAcao As New Label()
    textoAcao.Text = "DESENHAR: clique num vertice ou linha para conectar | SELECIONAR: escolha propriedades e cotas."
    textoAcao.Location = New SD.Point(25, 104)
    textoAcao.Size = New SD.Size(760, 22)
    textoAcao.ForeColor = TEXTO
    janela.Controls.Add(textoAcao)

    desenho = New Panel()
    desenho.Location = New SD.Point(25, 134)
    desenho.Size = New SD.Size(770, 572)
    desenho.BackColor = SD.Color.White
    desenho.BorderStyle = BorderStyle.FixedSingle
    desenho.Cursor = Cursors.Cross
    desenho.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic).SetValue(desenho, True, Nothing)
    AddHandler desenho.Paint, Sub(s As Object, e As PaintEventArgs) DesenharPlanta(s, e)
    AddHandler desenho.MouseDown, Sub(s As Object, e As MouseEventArgs) CliquePlanta(s, e)
    AddHandler desenho.MouseDoubleClick, Sub(s As Object, e As MouseEventArgs) EditarCotaPorDuploClique(s, e)
    janela.Controls.Add(desenho)

    Dim lateral As New Panel()
    lateral.Location = New SD.Point(811, 134)
    lateral.Size = New SD.Size(273, 572)
    lateral.BackColor = SD.Color.White
    lateral.BorderStyle = BorderStyle.FixedSingle
    janela.Controls.Add(lateral)

    Dim rotulo As New Label()
    rotulo.Text = "PROPRIEDADES DO SEGMENTO"
    rotulo.Location = New SD.Point(15, 16)
    rotulo.Size = New SD.Size(250, 23)
    rotulo.Font = New SD.Font("Arial", 9, SD.FontStyle.Bold)
    rotulo.ForeColor = ESCURO
    lateral.Controls.Add(rotulo)

    lblSelecionado = New Label()
    lblSelecionado.Text = "Nenhum segmento selecionado"
    lblSelecionado.Location = New SD.Point(15, 49)
    lblSelecionado.Size = New SD.Size(250, 35)
    lateral.Controls.Add(lblSelecionado)

    AdicionarRotulo(lateral, "Modulo", 87)
    comboModulo = AdicionarCombo(lateral, 108, New String() {"Sem janela", "Com janela", "Com porta"})
    AddHandler comboModulo.SelectedIndexChanged, Sub(s As Object, e As EventArgs) AtualizarCamposPorta()
    AdicionarRotulo(lateral, "Painel / fechamento", 142)
    comboTipo = AdicionarCombo(lateral, 163, New String() {"Tela", "Chapa", "Policarbonato", "Vazio"})
    AdicionarRotulo(lateral, "Tipo de porta", 197)
    comboPorta = AdicionarCombo(lateral, 218, New String() {"Porta simples", "Porta dupla", "Porta deslizante"})
    AdicionarRotulo(lateral, "Abertura", 252)
    comboAbertura = AdicionarCombo(lateral, 273, New String() {"Esquerda", "Direita", "Para dentro", "Para fora"})
    AtualizarCamposPorta()
    AdicionarRotulo(lateral, "Restricao do angulo", 306)
    comboAngulo = AdicionarCombo(lateral, 327, New String() {"Passos de 90", "Livre"})
    Dim lblValor As New Label()
    lblValor.Text = "Grau:"
    lblValor.Visible = False
    lblValor.Location = New SD.Point(15, 361)
    lblValor.Size = New SD.Size(55, 20)
    lateral.Controls.Add(lblValor)
    campoAngulo = New NumericUpDown()
    campoAngulo.Minimum = 0
    campoAngulo.Maximum = 359
    campoAngulo.Value = 90
    campoAngulo.Location = New SD.Point(71, 358)
    campoAngulo.Size = New SD.Size(82, 25)
    campoAngulo.Visible = False
    lateral.Controls.Add(campoAngulo)
    botaoDesenhar = CriarBotao(lateral, "DESENHAR / LIGAR", 15, 351, 115, 34, True)
    AddHandler botaoDesenhar.Click, Sub(s As Object, e As EventArgs) DefinirModo(True)
    botaoSelecionar = CriarBotao(lateral, "SELECIONAR", 136, 351, 119, 34, False)
    AddHandler botaoSelecionar.Click, Sub(s As Object, e As EventArgs) DefinirModo(False)
    AtualizarBotoesModo()
    Dim aplicar As Button = CriarBotao(lateral, "Aplicar ao segmento", 15, 392, 240, 32, True)
    AddHandler aplicar.Click, Sub(s As Object, e As EventArgs) AplicarPropriedades(s, e)
    Dim novo As Button = CriarBotao(lateral, "Novo trecho", 15, 433, 113, 32, False)
    AddHandler novo.Click, Sub(s As Object, e As EventArgs) NovoTrecho(s, e)
    Dim fechar As Button = CriarBotao(lateral, "Fechar cercado", 136, 433, 119, 32, False)
    AddHandler fechar.Click, Sub(s As Object, e As EventArgs) FecharCercado(s, e)
    Dim inserirPorta As Button = CriarBotao(lateral, "INSERIR PORTA NO TRECHO", 15, 474, 240, 30, True)
    AddHandler inserirPorta.Click, Sub(s As Object, e As EventArgs) InserirPortaNoTrecho(s, e)
    Dim apagar As Button = CriarBotao(lateral, "Excluir selecionado", 15, 510, 240, 27, False)
    AddHandler apagar.Click, Sub(s As Object, e As EventArgs) ExcluirSegmento(s, e)
    Dim ajuda As New Label()
    ajuda.Text = "Para porta: SELECIONAR > clique na linha > INSERIR PORTA. Informe largura e posicao. Auto-salvamento ativo."
    ajuda.Location = New SD.Point(15, 543)
    ajuda.Size = New SD.Size(245, 29)
    ajuda.ForeColor = SD.Color.FromArgb(74, 69, 96)
    lateral.Controls.Add(ajuda)

    Dim rodape As New Panel()
    rodape.Location = New SD.Point(0, 725)
    rodape.Size = New SD.Size(1110, 65)
    rodape.BackColor = SD.Color.White
    janela.Controls.Add(rodape)

    Dim btn3D As Button = CriarBotao(rodape, "GERAR 3D", 664, 12, 139, 38, True)
    AddHandler btn3D.Click, Sub(s As Object, e As EventArgs) GerarMontagem3D(s, e)
    Dim btnSalvar As Button = CriarBotao(rodape, "Salvar planta", 818, 12, 125, 38, True)
    AddHandler btnSalvar.Click, Sub(s As Object, e As EventArgs) AoClicarSalvarLayoutNR12(s, e)
    Dim btnCarregar As Button = CriarBotao(rodape, "Carregar", 965, 12, 118, 38, False)
    AddHandler btnCarregar.Click, Sub(s As Object, e As EventArgs) AoClicarCarregarLayoutNR12(s, e)
    lblStatus = New Label()
    lblStatus.Location = New SD.Point(25, 20)
    lblStatus.Size = New SD.Size(620, 26)
    lblStatus.ForeColor = SD.Color.FromArgb(74, 69, 96)
    lblStatus.Text = "Desenhar/ligar ativo. Layout recuperado automaticamente ao abrir."
    rodape.Controls.Add(lblStatus)
End Sub

Sub AdicionarRotulo(pai As Control, txt As String, y As Integer)
    Dim l As New Label()
    l.Text = txt
    l.ForeColor = TEXTO
    l.Location = New SD.Point(15, y)
    l.Size = New SD.Size(240, 20)
    pai.Controls.Add(l)
End Sub

Function AdicionarCombo(pai As Control, y As Integer, itens As String()) As ComboBox
    Dim c As New ComboBox()
    c.DropDownStyle = ComboBoxStyle.DropDownList
    c.Location = New SD.Point(15, y)
    c.Size = New SD.Size(240, 26)
    c.Items.AddRange(itens)
    c.SelectedIndex = 0
    pai.Controls.Add(c)
    Return c
End Function

Function CriarBotao(pai As Control, titulo As String, x As Integer, y As Integer, largura As Integer, altura As Integer, principal As Boolean) As Button
    Dim b As New Button()
    b.Text = titulo
    b.Location = New SD.Point(x, y)
    b.Size = New SD.Size(largura, altura)
    b.FlatStyle = FlatStyle.Flat
    b.FlatAppearance.BorderSize = 0
    b.BackColor = If(principal, AMBAR, SD.Color.FromArgb(237, 227, 251))
    b.ForeColor = ESCURO
    b.Font = New SD.Font("Arial", 9, SD.FontStyle.Bold)
    pai.Controls.Add(b)
    Return b
End Function

Sub DefinirModo(desenhar As Boolean)
    modoDesenho = desenhar
    temPonto = False
    AtualizarBotoesModo()
    lblStatus.Text = If(desenhar, "DESENHAR: clique num ponto ou numa linha existente para ligar.", _
        "SELECIONAR: clique numa linha e depois edite as propriedades ou insira porta.")
    desenho.Invalidate()
End Sub

Sub AtualizarBotoesModo()
    If botaoDesenhar Is Nothing OrElse botaoSelecionar Is Nothing Then Exit Sub
    botaoDesenhar.BackColor = If(modoDesenho, AMBAR, SD.Color.FromArgb(237, 227, 251))
    botaoSelecionar.BackColor = If(modoDesenho, SD.Color.FromArgb(237, 227, 251), AMBAR)
End Sub

' Identifica pontas existentes mesmo quando suas coordenadas nao sao multiplas de 500.
Function CapturarVerticeExistente(px As Integer, py As Integer, ByRef x As Double, ByRef y As Double) As Boolean
    Dim melhor As Double = 13
    Dim encontrou As Boolean = False
    For Each s As SegmentoNR In lista
        Dim d1 As Double = Math.Sqrt(Math.Pow(px - (origemX + s.X1 * escala), 2) + Math.Pow(py - (origemY - s.Y1 * escala), 2))
        If d1 < melhor Then
            melhor = d1 : x = s.X1 : y = s.Y1 : encontrou = True
        End If
        Dim d2 As Double = Math.Sqrt(Math.Pow(px - (origemX + s.X2 * escala), 2) + Math.Pow(py - (origemY - s.Y2 * escala), 2))
        If d2 < melhor Then
            melhor = d2 : x = s.X2 : y = s.Y2 : encontrou = True
        End If
    Next
    Return encontrou
End Function

' Conectar em um trecho existente: cria um vertice e separa o painel antigo
' em duas partes; o novo desenho pode comecar desse ponto.
Function CapturarPontoEmTrecho(px As Integer, py As Integer, ByRef x As Double, ByRef y As Double) As Boolean
    Dim idx As Integer = IndiceProximo(px, py)
    If idx < 0 Then Return False
    Dim anterior As SegmentoNR = lista(idx)
    If anterior.Modulo = "Com porta" Then Return False
    Dim ax As Double = origemX + anterior.X1 * escala
    Dim ay As Double = origemY - anterior.Y1 * escala
    Dim bx As Double = origemX + anterior.X2 * escala
    Dim by As Double = origemY - anterior.Y2 * escala
    Dim dx As Double = bx - ax, dy As Double = by - ay
    Dim d2 As Double = dx * dx + dy * dy
    If d2 < 0.001 Then Return False
    Dim t As Double = Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / d2))
    If t < 0.001 OrElse t > 0.999 Then Return False
    x = anterior.X1 + (anterior.X2 - anterior.X1) * t
    y = anterior.Y1 + (anterior.Y2 - anterior.Y1) * t
    Dim antesMM As Double = Math.Sqrt(Math.Pow(x - anterior.X1, 2) + Math.Pow(y - anterior.Y1, 2))
    Dim depoisMM As Double = Math.Sqrt(Math.Pow(anterior.X2 - x, 2) + Math.Pow(anterior.Y2 - y, 2))
    ' O gerador 3D exige 100 mm minimos por segmento.
    If antesMM < 100 OrElse depoisMM < 100 Then Return False
    Dim posterior As New SegmentoNR()
    posterior.X1 = x : posterior.Y1 = y
    posterior.X2 = anterior.X2 : posterior.Y2 = anterior.Y2
    posterior.Grupo = anterior.Grupo : posterior.Tipo = anterior.Tipo
    posterior.Acesso = anterior.Acesso : posterior.Abertura = anterior.Abertura
    posterior.Modulo = anterior.Modulo : posterior.Porta = anterior.Porta
    anterior.X2 = x : anterior.Y2 = y
    lista.Insert(idx + 1, posterior)
    Return True
End Function

Sub CliquePlanta(sender As Object, e As MouseEventArgs)
    If e.Button <> MouseButtons.Left OrElse e.Clicks > 1 Then Exit Sub

    If Not modoDesenho Then
        Dim indice As Integer = IndiceCota(e.X, e.Y)
        If indice < 0 Then indice = IndiceProximo(e.X, e.Y)
        selecionado = indice
        ExibirPropriedades()
        desenho.Invalidate()
        Exit Sub
    End If

    Dim x As Double = ArredondarGrade((e.X - origemX) / escala)
    Dim y As Double = ArredondarGrade((origemY - e.Y) / escala)
    Dim bateuVertice As Boolean = CapturarVerticeExistente(e.X, e.Y, x, y)

    ' 90 graus: manter a ortogonalidade durante o desenho, exceto quando o
    ' destino e um vertice existente alinhado a origem.
    If temPonto AndAlso CStr(comboAngulo.SelectedItem) = "Passos de 90" Then
        If bateuVertice Then
            If Math.Abs(x - ultimoX) > 0.01 AndAlso Math.Abs(y - ultimoY) > 0.01 Then
                lblStatus.Text = "Vertice nao alinhado: use Livre para conectar em diagonal."
                Exit Sub
            End If
        Else
            If Math.Abs(x - ultimoX) >= Math.Abs(y - ultimoY) Then
                y = ultimoY
            Else
                x = ultimoX
            End If
        End If
    End If

    ' Perto de um painel: ligar a ele e criar um no intermediario.
    ' Este comportamento evita que o clique seja interpretado como selecao.
    Dim foiSubdividido As Boolean = False
    If Not bateuVertice Then
        Dim snapX As Double = x, snapY As Double = y
        If CapturarPontoEmTrecho(e.X, e.Y, snapX, snapY) Then
            ' Em modo ortogonal so conectar a um ponto alinhado.
            If Not temPonto OrElse CStr(comboAngulo.SelectedItem) = "Livre" OrElse _
                Math.Abs(snapX - ultimoX) < 0.01 OrElse Math.Abs(snapY - ultimoY) < 0.01 Then
                x = snapX : y = snapY : foiSubdividido = True
            Else
                ' Reverter a subdivisao nao e necessario: manter o no criado.
                lblStatus.Text = "No criado na linha. Use modo Livre para ligar nesta direcao."
                ultimoX = snapX : ultimoY = snapY : temPonto = True
                SalvarLayoutNR12()
                desenho.Invalidate()
                Exit Sub
            End If
        End If
    End If

    If temPonto Then
        Dim tamanho As Double = Math.Sqrt(Math.Pow(x - ultimoX, 2) + Math.Pow(y - ultimoY, 2))
        If tamanho >= 100 Then
            ' Permite fechar o proprio grupo ao tocar seu inicio, sem voltar a
            ' selecionar o painel antigo.
            Dim s As New SegmentoNR()
            s.X1 = ultimoX : s.Y1 = ultimoY
            s.X2 = x : s.Y2 = y
            s.Grupo = grupoAtual
            s.Modulo = CStr(comboModulo.SelectedItem)
            s.Tipo = CStr(comboTipo.SelectedItem)
            s.Porta = CStr(comboPorta.SelectedItem)
            s.Abertura = CStr(comboAbertura.SelectedItem)
            s.Acesso = If(s.Modulo = "Com porta", s.Porta, "Sem portao")
            lista.Add(s)
            selecionado = lista.Count - 1
            ExibirPropriedades()
        ElseIf tamanho > 0.01 Then
            lblStatus.Text = "Segmento inferior a 100 mm. Selecione outro ponto."
            Exit Sub
        End If
    End If
    ultimoX = x : ultimoY = y : temPonto = True
    If foiSubdividido OrElse lista.Count > 0 Then SalvarLayoutNR12()
    lblStatus.Text = "Ponto (" & x.ToString("0.##") & ", " & y.ToString("0.##") & ") mm. Clique para continuar ou ESC para novo trecho."
    desenho.Invalidate()
End Sub

Function ArredondarGrade(valor As Double) As Double
    Return Math.Round(valor / PASSO_GRADE_MM, 0, MidpointRounding.AwayFromZero) * PASSO_GRADE_MM
End Function

Function IndiceProximo(px As Integer, py As Integer) As Integer
    Dim melhor As Integer = -1
    Dim distancia As Double = 9
    For i As Integer = 0 To lista.Count - 1
        Dim s As SegmentoNR = lista(i)
        Dim ax As Double = origemX + s.X1 * escala
        Dim ay As Double = origemY - s.Y1 * escala
        Dim bx As Double = origemX + s.X2 * escala
        Dim by As Double = origemY - s.Y2 * escala
        Dim dx As Double = bx - ax, dy As Double = by - ay
        Dim d2 As Double = dx * dx + dy * dy
        If d2 < 0.0001 Then Continue For
        Dim t As Double = Math.Max(0, Math.Min(1, ((px - ax) * dx + (py - ay) * dy) / d2))
        Dim d As Double = Math.Sqrt(Math.Pow(px - ax - t * dx, 2) + Math.Pow(py - ay - t * dy, 2))
        If d < distancia Then distancia = d : melhor = i
    Next
    Return melhor
End Function

' Identifica a caixa visual da cota (e nao apenas o traco).
Function IndiceCota(px As Integer, py As Integer) As Integer
    For i As Integer = 0 To lista.Count - 1
        Dim s As SegmentoNR = lista(i)
        Dim ax As Double = origemX + s.X1 * escala
        Dim ay As Double = origemY - s.Y1 * escala
        Dim bx As Double = origemX + s.X2 * escala
        Dim by As Double = origemY - s.Y2 * escala
        Dim dx As Double = bx - ax, dy As Double = by - ay
        Dim tam As Double = Math.Sqrt(dx * dx + dy * dy)
        If tam < 0.01 Then Continue For
        Dim nx As Double = dy / tam, ny As Double = -dx / tam
        Dim cx As Double = (ax + bx) / 2 + nx * 17
        Dim cy As Double = (ay + by) / 2 + ny * 17
        If Math.Abs(px - cx) <= 38 AndAlso Math.Abs(py - cy) <= 12 Then Return i
    Next
    Return -1
End Function

Sub EditarCotaPorDuploClique(sender As Object, e As MouseEventArgs)
    If e.Button <> MouseButtons.Left Then Exit Sub
    Dim idx As Integer = IndiceCota(e.X, e.Y)
    If idx < 0 Then idx = IndiceProximo(e.X, e.Y)
    If idx < 0 Then Exit Sub
    selecionado = idx
    ExibirPropriedades()
    Dim atual As SegmentoNR = lista(idx)
    Dim comprimento As Double = Math.Sqrt(Math.Pow(atual.X2 - atual.X1, 2) + Math.Pow(atual.Y2 - atual.Y1, 2))
    ' Caixa simples sem controles TextBox para evitar conflito com a API Inventor.
    Dim textoMedida As String = Microsoft.VisualBasic.Interaction.InputBox( _
        "Novo comprimento exato em mm:", _
        "Editar cota - Segmento " & (idx + 1).ToString(), _
        comprimento.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture))
    If String.IsNullOrWhiteSpace(textoMedida) Then Exit Sub
    textoMedida = textoMedida.Trim().Replace(" ", "")
    Dim medida As Double
    If Not Double.TryParse(textoMedida, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, medida) Then
        If Not Double.TryParse(textoMedida.Replace(","c, "."c), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, medida) Then
            MessageBox.Show("Informe um numero valido em milimetros.", "NR12")
            Exit Sub
        End If
    End If
    If Double.IsNaN(medida) OrElse Double.IsInfinity(medida) OrElse medida < 1 OrElse medida > 100000 Then
        MessageBox.Show("Informe um comprimento entre 1 e 100000 mm.", "NR12")
        Exit Sub
    End If
    AtualizarComprimentoExato(idx, medida)
End Sub

Sub AtualizarComprimentoExato(idx As Integer, novaMedida As Double)
    Dim s As SegmentoNR = lista(idx)
    Dim deltaX As Double = s.X2 - s.X1
    Dim deltaY As Double = s.Y2 - s.Y1
    Dim anterior As Double = Math.Sqrt(deltaX * deltaX + deltaY * deltaY)
    If anterior < 0.000001 Then Exit Sub
    Dim antigoX As Double = s.X2
    Dim antigoY As Double = s.Y2
    Dim novoX As Double = s.X1 + deltaX * novaMedida / anterior
    Dim novoY As Double = s.Y1 + deltaY * novaMedida / anterior
    ' Mantem coincidencia do vertice com os segmentos vizinhos do mesmo grupo.
    ' Segmentos adjacentes podem mudar de comprimento/angulo.
    For i As Integer = 0 To lista.Count - 1
        If i = idx Then Continue For
        Dim vizinho As SegmentoNR = lista(i)
        If vizinho.Grupo <> s.Grupo Then Continue For
        If Math.Abs(vizinho.X1 - antigoX) < 0.01 AndAlso Math.Abs(vizinho.Y1 - antigoY) < 0.01 Then
            vizinho.X1 = novoX : vizinho.Y1 = novoY
        End If
        If Math.Abs(vizinho.X2 - antigoX) < 0.01 AndAlso Math.Abs(vizinho.Y2 - antigoY) < 0.01 Then
            vizinho.X2 = novoX : vizinho.Y2 = novoY
        End If
    Next
    s.X2 = novoX : s.Y2 = novoY
    If temPonto AndAlso s.Grupo = grupoAtual Then
        If Math.Abs(ultimoX - antigoX) < 0.01 AndAlso Math.Abs(ultimoY - antigoY) < 0.01 Then
            ultimoX = novoX : ultimoY = novoY
        End If
    End If
    ExibirPropriedades()
    SalvarLayoutNR12()
    lblStatus.Text = "Cota atualizada e salva: " & novaMedida.ToString("0.###") & " mm."
    desenho.Invalidate()
End Sub


' Convencao grafica de planta: a linha do vao NAO recebe painel continuo.
Private Sub DesenharSimboloPorta(g As SD.Graphics, indice As Integer, cor As SD.Color)
    Dim s As SegmentoNR = lista(indice)
    Dim x1 As Single = CSng(origemX + s.X1 * escala)
    Dim y1 As Single = CSng(origemY - s.Y1 * escala)
    Dim x2 As Single = CSng(origemX + s.X2 * escala)
    Dim y2 As Single = CSng(origemY - s.Y2 * escala)
    Dim dx As Double = x2 - x1, dy As Double = y2 - y1
    Dim tam As Double = Math.Sqrt(dx * dx + dy * dy)
    If tam < 1 Then Exit Sub
    Dim ux As Double = dx / tam, uy As Double = dy / tam
    Dim lado As Double = If(s.Abertura = "Para dentro" OrElse s.Abertura = "Direita", -1.0, 1.0)
    Dim nx As Double = -uy * lado, ny As Double = ux * lado
    Using caneta As New SD.Pen(cor, 2.7F)
        ' Pontos de apoio ao redor do vao.
        g.DrawLine(caneta, CSng(x1 - nx * 5), CSng(y1 - ny * 5), CSng(x1 + nx * 5), CSng(y1 + ny * 5))
        g.DrawLine(caneta, CSng(x2 - nx * 5), CSng(y2 - ny * 5), CSng(x2 + nx * 5), CSng(y2 + ny * 5))
        If s.Porta = "Porta dupla" Then
            Dim meioX As Double = (x1 + x2) / 2, meioY As Double = (y1 + y2) / 2
            Dim raio As Double = tam / 2
            g.DrawLine(caneta, x1, y1, CSng(x1 + nx * raio), CSng(y1 + ny * raio))
            g.DrawLine(caneta, x2, y2, CSng(x2 + nx * raio), CSng(y2 + ny * raio))
            g.DrawLine(SD.Pens.Gray, CSng(meioX - 4), CSng(meioY - 4), CSng(meioX + 4), CSng(meioY + 4))
        ElseIf s.Porta = "Porta deslizante" Then
            g.DrawLine(caneta, x1, y1, x2, y2)
            g.DrawLine(caneta, CSng(x1 + nx * 9), CSng(y1 + ny * 9), CSng(x2 + nx * 9), CSng(y2 + ny * 9))
            Dim mx As Double = (x1 + x2) / 2, my As Double = (y1 + y2) / 2
            g.DrawLine(caneta, CSng(mx - ux * 9 + nx * 9), CSng(my - uy * 9 + ny * 9), CSng(mx + ux * 9 + nx * 9), CSng(my + uy * 9 + ny * 9))
        Else
            Dim direita As Boolean = s.Abertura = "Direita"
            Dim hx As Double = If(direita, x2, x1), hy As Double = If(direita, y2, y1)
            Dim pontaX As Double = hx + nx * tam, pontaY As Double = hy + ny * tam
            g.DrawLine(caneta, CSng(hx), CSng(hy), CSng(pontaX), CSng(pontaY))
            ' Traço de abertura 90 graus, pontilhado.
            Using tracejado As New SD.Pen(SD.Color.FromArgb(130, cor), 1)
                tracejado.DashStyle = SD.Drawing2D.DashStyle.Dash
                g.DrawLine(tracejado, CSng(pontaX), CSng(pontaY), CSng(If(direita, x1, x2)), CSng(If(direita, y1, y2)))
            End Using
        End If
    End Using
End Sub

Private Sub DesenharSimboloJanela(g As SD.Graphics, indice As Integer, cor As SD.Color)
    Dim s As SegmentoNR = lista(indice)
    Dim x1 As Single = CSng(origemX + s.X1 * escala)
    Dim y1 As Single = CSng(origemY - s.Y1 * escala)
    Dim x2 As Single = CSng(origemX + s.X2 * escala)
    Dim y2 As Single = CSng(origemY - s.Y2 * escala)
    Dim dx As Double = x2 - x1, dy As Double = y2 - y1
    Dim tam As Double = Math.Sqrt(dx * dx + dy * dy)
    If tam < 1 Then Exit Sub
    Dim nx As Double = -dy / tam * 5, ny As Double = dx / tam * 5
    Using caneta As New SD.Pen(cor, 2)
        g.DrawLine(caneta, CSng(x1 + nx), CSng(y1 + ny), CSng(x2 + nx), CSng(y2 + ny))
        g.DrawLine(caneta, CSng(x1 - nx), CSng(y1 - ny), CSng(x2 - nx), CSng(y2 - ny))
    End Using
End Sub

Sub DesenharPlanta(sender As Object, e As PaintEventArgs)
    Dim g As SD.Graphics = e.Graphics
    g.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias
    Dim passoPx As Integer = CInt(PASSO_GRADE_MM * escala)
    Using grade As New SD.Pen(SD.Color.FromArgb(226, 220, 240), 1)
        For x As Integer = CInt(origemX) To desenho.Width Step passoPx
            g.DrawLine(grade, x, 0, x, desenho.Height)
        Next
        For x As Integer = CInt(origemX) - passoPx To 0 Step -passoPx
            g.DrawLine(grade, x, 0, x, desenho.Height)
        Next
        For y As Integer = CInt(origemY) To desenho.Height Step passoPx
            g.DrawLine(grade, 0, y, desenho.Width, y)
        Next
        For y As Integer = CInt(origemY) - passoPx To 0 Step -passoPx
            g.DrawLine(grade, 0, y, desenho.Width, y)
        Next
    End Using
    For i As Integer = 0 To lista.Count - 1
        Dim s As SegmentoNR = lista(i)
        Dim cor As SD.Color = If(i = selecionado, AMBAR, If(s.Modulo = "Com porta", SD.Color.FromArgb(46, 158, 91), If(s.Modulo = "Com janela", SD.Color.FromArgb(37, 145, 210), ROXO)))
        If s.Modulo = "Com porta" Then
            DesenharSimboloPorta(g, i, cor)
        ElseIf s.Modulo = "Com janela" Then
            DesenharSimboloJanela(g, i, cor)
        Else
            Using linha As New SD.Pen(cor, If(i = selecionado, 5.0F, 3.0F))
                g.DrawLine(linha, CSng(origemX + s.X1 * escala), CSng(origemY - s.Y1 * escala), CSng(origemX + s.X2 * escala), CSng(origemY - s.Y2 * escala))
            End Using
        End If
        ' Cota sempre exibida, ligeiramente afastada do eixo do segmento.
        Dim ex As Double = origemX + s.X1 * escala
        Dim ey As Double = origemY - s.Y1 * escala
        Dim fx As Double = origemX + s.X2 * escala
        Dim fy As Double = origemY - s.Y2 * escala
        Dim vx As Double = fx - ex, vy As Double = fy - ey
        Dim vlen As Double = Math.Sqrt(vx * vx + vy * vy)
        If vlen > 0.01 Then
            Dim ladoX As Double = vy / vlen, ladoY As Double = -vx / vlen
            Dim cx As Single = CSng((ex + fx) / 2 + ladoX * 17)
            Dim cy As Single = CSng((ey + fy) / 2 + ladoY * 17)
            Dim comprimentoMM As Double = Math.Sqrt(Math.Pow(s.X2 - s.X1, 2) + Math.Pow(s.Y2 - s.Y1, 2))
            Dim cotaTxt As String = comprimentoMM.ToString("0.##") & " mm"
            Using fonteCota As New SD.Font("Arial", 8, SD.FontStyle.Bold)
                Dim tamanhoTxt As SD.SizeF = g.MeasureString(cotaTxt, fonteCota)
                Dim fundoCota As New SD.RectangleF(cx - tamanhoTxt.Width / 2 - 3, cy - tamanhoTxt.Height / 2 - 2, tamanhoTxt.Width + 6, tamanhoTxt.Height + 4)
                Using fundoBranco As New SD.SolidBrush(SD.Color.FromArgb(245, 255, 255, 255))
                    g.FillRectangle(fundoBranco, fundoCota)
                End Using
                Using tinta As New SD.SolidBrush(If(i = selecionado, SD.Color.FromArgb(116, 64, 0), ESCURO))
                    g.DrawString(cotaTxt, fonteCota, tinta, cx - tamanhoTxt.Width / 2, cy - tamanhoTxt.Height / 2)
                End Using
            End Using
        End If
        Using ponto As New SD.SolidBrush(ESCURO)
            g.FillEllipse(ponto, CSng(origemX + s.X1 * escala - 3), CSng(origemY - s.Y1 * escala - 3), 6, 6)
            g.FillEllipse(ponto, CSng(origemX + s.X2 * escala - 3), CSng(origemY - s.Y2 * escala - 3), 6, 6)
        End Using
    Next
    If temPonto Then
        Using p As New SD.SolidBrush(AMBAR)
            g.FillEllipse(p, CSng(origemX + ultimoX * escala - 4), CSng(origemY - ultimoY * escala - 4), 8, 8)
        End Using
    End If
    Using fonte As New SD.Font("Arial", 8)
        g.DrawString("X+ →   Y+ ↑  |  malha: 500 mm", fonte, SD.Brushes.Gray, 12, 10)
    End Using
End Sub

Sub ExibirPropriedades()
    If selecionado < 0 OrElse selecionado >= lista.Count Then
        lblSelecionado.Text = "Nenhum segmento selecionado"
        Exit Sub
    End If
    Dim s As SegmentoNR = lista(selecionado)
    Dim comprimento As Double = Math.Sqrt(Math.Pow(s.X2 - s.X1, 2) + Math.Pow(s.Y2 - s.Y1, 2))
    lblSelecionado.Text = "Segmento " & (selecionado + 1).ToString() & "  |  " & Math.Round(comprimento, 0).ToString() & " mm"
    comboTipo.SelectedItem = s.Tipo
    comboModulo.SelectedItem = s.Modulo
    comboPorta.SelectedItem = s.Porta
    comboAbertura.SelectedItem = s.Abertura
    AtualizarCamposPorta()
End Sub

Sub AtualizarCamposPorta()
    If comboModulo Is Nothing OrElse comboPorta Is Nothing OrElse comboAbertura Is Nothing Then Exit Sub
    Dim temPorta As Boolean = CStr(comboModulo.SelectedItem) = "Com porta"
    ' Os controles tambem servem para INSERIR uma porta em painel existente.
    comboPorta.Enabled = True
    comboAbertura.Enabled = True
End Sub

Sub AplicarPropriedades(sender As Object, e As EventArgs)
    If selecionado < 0 OrElse selecionado >= lista.Count Then
        MessageBox.Show("Selecione um segmento primeiro.", "NR12 Modular")
        Exit Sub
    End If
    Dim s As SegmentoNR = lista(selecionado)
    s.Tipo = CStr(comboTipo.SelectedItem)
    s.Modulo = CStr(comboModulo.SelectedItem)
    s.Porta = CStr(comboPorta.SelectedItem)
    s.Acesso = If(s.Modulo = "Com porta", s.Porta, "Sem portao")
    s.Abertura = CStr(comboAbertura.SelectedItem)
    SalvarLayoutNR12()
    lblStatus.Text = "Propriedades aplicadas e salvas ao segmento " & (selecionado + 1).ToString() & "."
    desenho.Invalidate()
End Sub


' Porta com largura e posicao (distancia a partir do INICIO do segmento)
' editaveis na mesma janela. Permite porta em segmento de perimetro fechado.
Sub InserirPortaNoTrecho(sender As Object, e As EventArgs)
    If selecionado < 0 OrElse selecionado >= lista.Count Then
        MessageBox.Show("Clique SELECIONAR e escolha a linha do cercado antes de inserir a porta.", "NR12")
        Exit Sub
    End If
    Dim original As SegmentoNR = lista(selecionado)
    If original.Modulo = "Com porta" Then
        MessageBox.Show("Este trecho ja possui porta. Selecione outro painel.", "NR12")
        Exit Sub
    End If
    Dim dx As Double = original.X2 - original.X1
    Dim dy As Double = original.Y2 - original.Y1
    Dim comprimento As Double = Math.Sqrt(dx * dx + dy * dy)
    If comprimento < 300 Then
        MessageBox.Show("O trecho precisa ter no minimo 300 mm para receber uma porta.", "NR12")
        Exit Sub
    End If
    Dim largura As Double = Math.Min(1000, Math.Min(2500, comprimento))
    Dim posicao As Double = (comprimento - largura) / 2
    Dim tipo As String = CStr(comboPorta.SelectedItem)
    Dim sentido As String = CStr(comboAbertura.SelectedItem)

    Using dialogo As New Form()
        dialogo.Text = "Inserir porta no contorno"
        dialogo.StartPosition = FormStartPosition.CenterParent
        dialogo.FormBorderStyle = FormBorderStyle.FixedDialog
        dialogo.MaximizeBox = False
        dialogo.MinimizeBox = False
        dialogo.ClientSize = New SD.Size(400, 255)
        Dim informacao As New Label()
        informacao.Text = "Trecho selecionado: " & comprimento.ToString("0.##") & " mm" & _
            "  |  Porta entre dois postes"
        informacao.Location = New SD.Point(16, 14) : informacao.Size = New SD.Size(365, 28)
        dialogo.Controls.Add(informacao)
        Dim rotL As New Label()
        rotL.Text = "Largura do portao (mm):"
        rotL.Location = New SD.Point(16, 52) : rotL.Size = New SD.Size(196, 22)
        dialogo.Controls.Add(rotL)
        Dim numL As New System.Windows.Forms.NumericUpDown()
        numL.DecimalPlaces = 2
        numL.Increment = 10D
        numL.Minimum = 300D
        numL.Maximum = CDec(Math.Min(2500, comprimento))
        numL.Value = CDec(largura)
        numL.Location = New SD.Point(217, 49) : numL.Size = New SD.Size(150, 27)
        dialogo.Controls.Add(numL)
        Dim rotP As New Label()
        rotP.Text = "Distancia desde inicio (mm):"
        rotP.Location = New SD.Point(16, 91) : rotP.Size = New SD.Size(195, 24)
        dialogo.Controls.Add(rotP)
        Dim numP As New System.Windows.Forms.NumericUpDown()
        numP.DecimalPlaces = 2
        numP.Increment = 10D
        numP.Minimum = 0D
        numP.Maximum = CDec(Math.Min(100000, comprimento))
        numP.Value = CDec(posicao)
        numP.Location = New SD.Point(217, 88) : numP.Size = New SD.Size(150, 27)
        dialogo.Controls.Add(numP)
        Dim rotT As New Label()
        rotT.Text = "Tipo de porta:"
        rotT.Location = New SD.Point(16, 131) : rotT.Size = New SD.Size(100, 21)
        dialogo.Controls.Add(rotT)
        Dim tipoPortaDlg As New System.Windows.Forms.ComboBox()
        tipoPortaDlg.DropDownStyle = ComboBoxStyle.DropDownList
        tipoPortaDlg.Items.AddRange(New Object() {"Porta simples", "Porta dupla", "Porta deslizante"})
        tipoPortaDlg.SelectedItem = tipo
        If tipoPortaDlg.SelectedIndex < 0 Then tipoPortaDlg.SelectedIndex = 0
        tipoPortaDlg.Location = New SD.Point(115, 127) : tipoPortaDlg.Size = New SD.Size(252, 27)
        dialogo.Controls.Add(tipoPortaDlg)
        Dim rotS As New Label()
        rotS.Text = "Abertura:"
        rotS.Location = New SD.Point(16, 167) : rotS.Size = New SD.Size(90, 21)
        dialogo.Controls.Add(rotS)
        Dim sentidoDlg As New System.Windows.Forms.ComboBox()
        sentidoDlg.DropDownStyle = ComboBoxStyle.DropDownList
        sentidoDlg.Items.AddRange(New Object() {"Esquerda", "Direita", "Para dentro", "Para fora"})
        sentidoDlg.SelectedItem = sentido
        If sentidoDlg.SelectedIndex < 0 Then sentidoDlg.SelectedIndex = 0
        sentidoDlg.Location = New SD.Point(115, 163) : sentidoDlg.Size = New SD.Size(252, 27)
        dialogo.Controls.Add(sentidoDlg)
        Dim confirmar As New Button()
        confirmar.Text = "Inserir porta"
        confirmar.Location = New SD.Point(164, 207) : confirmar.Size = New SD.Size(105, 33)
        confirmar.DialogResult = DialogResult.OK
        dialogo.Controls.Add(confirmar)
        Dim cancelar As New Button()
        cancelar.Text = "Cancelar"
        cancelar.Location = New SD.Point(279, 207) : cancelar.Size = New SD.Size(90, 33)
        cancelar.DialogResult = DialogResult.Cancel
        dialogo.Controls.Add(cancelar)
        dialogo.AcceptButton = confirmar : dialogo.CancelButton = cancelar
        If dialogo.ShowDialog(janela) <> DialogResult.OK Then Exit Sub

        largura = CDbl(numL.Value) : posicao = CDbl(numP.Value)
        tipo = CStr(tipoPortaDlg.SelectedItem) : sentido = CStr(sentidoDlg.SelectedItem)
    End Using

    If posicao < 0 OrElse posicao + largura > comprimento + 0.01 Then
        MessageBox.Show("Porta fora do segmento. Ajuste largura ou distancia desde inicio.", "NR12")
        Exit Sub
    End If
    Dim restoAntes As Double = posicao
    Dim restoDepois As Double = comprimento - (posicao + largura)
    ' Não gerar sobras com menos de 100 mm: o 3D rejeita paineis menores.
    If (restoAntes > 0.01 AndAlso restoAntes < 100) OrElse _
       (restoDepois > 0.01 AndAlso restoDepois < 100) Then
        MessageBox.Show("Os paineis adjacentes devem ter 0 ou pelo menos 100 mm.", "NR12")
        Exit Sub
    End If

    Dim ux As Double = dx / comprimento, uy As Double = dy / comprimento
    Dim iniX As Double = original.X1 + ux * posicao
    Dim iniY As Double = original.Y1 + uy * posicao
    Dim fimX As Double = original.X1 + ux * (posicao + largura)
    Dim fimY As Double = original.Y1 + uy * (posicao + largura)

    Dim pos As Integer = selecionado
    lista.RemoveAt(pos)
    Dim antes As New SegmentoNR()
    antes.X1 = original.X1 : antes.Y1 = original.Y1
    antes.X2 = iniX : antes.Y2 = iniY
    antes.Grupo = original.Grupo : antes.Tipo = original.Tipo
    antes.Modulo = original.Modulo : antes.Acesso = "Sem portao"
    antes.Porta = original.Porta : antes.Abertura = original.Abertura
    If restoAntes >= 100 Then
        lista.Insert(pos, antes) : pos += 1
    End If

    Dim porta As New SegmentoNR()
    porta.X1 = iniX : porta.Y1 = iniY
    porta.X2 = fimX : porta.Y2 = fimY
    porta.Grupo = original.Grupo : porta.Tipo = original.Tipo
    porta.Modulo = "Com porta" : porta.Acesso = tipo
    porta.Porta = tipo : porta.Abertura = sentido
    lista.Insert(pos, porta)
    selecionado = pos : pos += 1

    Dim depois As New SegmentoNR()
    depois.X1 = fimX : depois.Y1 = fimY
    depois.X2 = original.X2 : depois.Y2 = original.Y2
    depois.Grupo = original.Grupo : depois.Tipo = original.Tipo
    depois.Modulo = original.Modulo : depois.Acesso = "Sem portao"
    depois.Porta = original.Porta : depois.Abertura = original.Abertura
    If restoDepois >= 100 Then lista.Insert(pos, depois)

    ExibirPropriedades()
    SalvarLayoutNR12()
    lblStatus.Text = "Porta: " & largura.ToString("0.##") & " mm; inicia em " & _
        posicao.ToString("0.##") & " mm no trecho selecionado. Planta salva."
    desenho.Invalidate()
End Sub

Sub NovoTrecho(sender As Object, e As EventArgs)
    temPonto = False
    grupoAtual += 1
    selecionado = -1
    ExibirPropriedades()
    SalvarLayoutNR12()
    lblStatus.Text = "Novo trecho: clique num vertice existente para conectar ou no vazio para iniciar."
    desenho.Invalidate()
End Sub

Sub FecharCercado(sender As Object, e As EventArgs)
    If Not temPonto Then
        MessageBox.Show("Inicie um trecho antes de fechar.", "NR12")
        Exit Sub
    End If
    Dim primeiro As SegmentoNR = Nothing
    For Each s As SegmentoNR In lista
        If s.Grupo = grupoAtual Then
            primeiro = s
            Exit For
        End If
    Next
    If primeiro Is Nothing Then
        MessageBox.Show("Desenhe ao menos um segmento.", "NR12")
        Exit Sub
    End If
    Dim distancia As Double = Math.Sqrt(Math.Pow(ultimoX - primeiro.X1, 2) + Math.Pow(ultimoY - primeiro.Y1, 2))
    If distancia > 1 Then
        Dim fechamento As New SegmentoNR()
        fechamento.X1 = ultimoX : fechamento.Y1 = ultimoY
        fechamento.X2 = primeiro.X1 : fechamento.Y2 = primeiro.Y1
        fechamento.Grupo = grupoAtual
        fechamento.Modulo = CStr(comboModulo.SelectedItem)
        fechamento.Porta = CStr(comboPorta.SelectedItem)
        fechamento.Tipo = CStr(comboTipo.SelectedItem)
        fechamento.Abertura = CStr(comboAbertura.SelectedItem)
        fechamento.Acesso = If(fechamento.Modulo = "Com porta", fechamento.Porta, "Sem portao")
        lista.Add(fechamento)
    End If
    temPonto = False
    grupoAtual += 1
    selecionado = -1
    ExibirPropriedades()
    SalvarLayoutNR12()
    lblStatus.Text = "Cercado fechado e salvo. Pode conectar outro cercado pelo modo DESENHAR."
    desenho.Invalidate()
End Sub

Sub GerarMontagem3D(sender As Object, e As EventArgs)
    If lista.Count = 0 Then
        MessageBox.Show("Desenhe ao menos um segmento.", "NR12")
        Exit Sub
    End If
    If Not SalvarLayoutNR12() Then Exit Sub
    Try
        iLogicVb.RunRule("Gerar_3D_NR12")
        lblStatus.Text = "Regra 3D executada. Confira as ocorrencias da montagem."
    Catch ex As Exception
        MessageBox.Show("Falha na regra Gerar_3D_NR12:" & vbCrLf & ex.Message, "NR12")
    End Try
End Sub

Sub ExcluirSegmento(sender As Object, e As EventArgs)
    If selecionado >= 0 AndAlso selecionado < lista.Count Then
        lista.RemoveAt(selecionado)
        selecionado = -1
        ExibirPropriedades()
        SalvarLayoutNR12()
        desenho.Invalidate()
    End If
End Sub

Sub TeclaJanela(sender As Object, e As KeyEventArgs)
    If e.KeyCode = Keys.Escape Then
        NovoTrecho(Nothing, EventArgs.Empty)
    ElseIf e.KeyCode = Keys.Delete AndAlso (desenho.Focused OrElse janela.Focused) Then
        ExcluirSegmento(Nothing, EventArgs.Empty)
    End If
End Sub

Sub AoClicarSalvarLayoutNR12(sender As Object, e As EventArgs)
    SalvarLayoutNR12()
End Sub

Function SalvarLayoutNR12() As Boolean
    If carregamentoFalhou Then
        MessageBox.Show("Layout anterior nao carregado. O arquivo original foi preservado.", "NR12")
        Return False
    End If
    Try
        ' Resolve a pasta sempre pela montagem em que a regra esta sendo executada.
        Dim caminhoMontagem As String = ThisDoc.Document.FullFileName
        If String.IsNullOrWhiteSpace(caminhoMontagem) Then Throw New Exception("Salve primeiro a montagem .iam.")
        arquivo = IO.Path.Combine(IO.Path.GetDirectoryName(caminhoMontagem), "NR12_layout.txt")
        Dim linhas As New System.Collections.Generic.List(Of String)
        linhas.Add("# NR12-layout-v2; X1;Y1;X2;Y2;Grupo;Tipo;Acesso;Abertura;Modulo;Porta")
        Dim inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
        For Each s As SegmentoNR In lista
            linhas.Add(String.Join(";", New String() {s.X1.ToString(inv), s.Y1.ToString(inv), s.X2.ToString(inv), s.Y2.ToString(inv), s.Grupo.ToString(inv), s.Tipo, s.Acesso, s.Abertura, s.Modulo, s.Porta}))
        Next
        IO.File.WriteAllLines(arquivo, linhas.ToArray(), System.Text.Encoding.UTF8)
        lblStatus.Text = "Layout salvo: " & arquivo
        Return True
    Catch ex As Exception
        MessageBox.Show("Nao foi possivel salvar NR12_layout.txt:" & vbCrLf & ex.Message, "NR12 - falha ao salvar")
        Return False
    End Try
End Function

Sub AoClicarCarregarLayoutNR12(sender As Object, e As EventArgs)
    CarregarLayoutNR12()
End Sub

Sub CarregarLayoutNR12()
    arquivo = IO.Path.Combine(IO.Path.GetDirectoryName(ThisDoc.Document.FullFileName), "NR12_layout.txt")
    If Not IO.File.Exists(arquivo) Then
        MessageBox.Show("Arquivo ainda nao existe: " & arquivo, "NR12 Modular")
        Exit Sub
    End If
    Try
        Dim novos As New System.Collections.Generic.List(Of SegmentoNR)
        Dim inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
        For Each linha As String In IO.File.ReadAllLines(arquivo, System.Text.Encoding.UTF8)
            If String.IsNullOrWhiteSpace(linha) OrElse linha.StartsWith("#") Then Continue For
            Dim p As String() = linha.Split(";"c)
            If p.Length <> 8 AndAlso p.Length <> 10 Then Throw New Exception("Registro invalido: " & linha)
            Dim s As New SegmentoNR()
            s.X1 = Double.Parse(p(0), inv) : s.Y1 = Double.Parse(p(1), inv)
            s.X2 = Double.Parse(p(2), inv) : s.Y2 = Double.Parse(p(3), inv)
            s.Grupo = Integer.Parse(p(4), inv)
            s.Tipo = p(5) : s.Acesso = p(6) : s.Abertura = p(7)
            If p.Length = 10 Then
                s.Modulo = p(8) : s.Porta = p(9)
            ElseIf s.Acesso <> "Sem portao" Then
                s.Modulo = "Com porta" : s.Porta = "Porta simples"
            End If
            novos.Add(s)
        Next
        lista = novos
        carregamentoFalhou = False
        grupoAtual = 1
        For Each s As SegmentoNR In lista
            grupoAtual = Math.Max(grupoAtual, s.Grupo + 1)
        Next
        temPonto = False
        selecionado = -1
        ExibirPropriedades()
        desenho.Invalidate()
        lblStatus.Text = lista.Count.ToString() & " segmentos restaurados. Use DESENHAR para conectar e SELECIONAR para portas."
    Catch ex As Exception
        carregamentoFalhou = True
        MessageBox.Show("Nao foi possivel carregar o layout. Ele NAO sera sobrescrito:" & vbCrLf & ex.Message, "Falha ao carregar")
    End Try
End Sub
