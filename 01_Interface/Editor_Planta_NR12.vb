AddReference "System.Drawing"
Imports System.Windows.Forms
Imports SD = System.Drawing
Imports IO = System.IO

' NR12 Modular — prototipo do editor 2D para iLogic / Inventor 2024.
' VERSAO TESTE V0.5.1 - sincronizacao do caminho de layout com gerador 3D.
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
    janela.ShowDialog()
End Sub

Private Class SegmentoNR
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
    textoAcao.Text = "Grade 500 mm ao desenhar | cotas exatas: duplo clique na linha ou medida (mm)."
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
    Dim aplicar As Button = CriarBotao(lateral, "Aplicar ao segmento", 15, 392, 240, 32, True)
    AddHandler aplicar.Click, Sub(s As Object, e As EventArgs) AplicarPropriedades(s, e)
    Dim novo As Button = CriarBotao(lateral, "Novo trecho", 15, 433, 113, 32, False)
    AddHandler novo.Click, Sub(s As Object, e As EventArgs) NovoTrecho(s, e)
    Dim fechar As Button = CriarBotao(lateral, "Fechar cercado", 136, 433, 119, 32, False)
    AddHandler fechar.Click, Sub(s As Object, e As EventArgs) FecharCercado(s, e)
    Dim apagar As Button = CriarBotao(lateral, "Excluir selecionado", 15, 474, 240, 30, False)
    AddHandler apagar.Click, Sub(s As Object, e As EventArgs) ExcluirSegmento(s, e)
    Dim ajuda As New Label()
    ajuda.Text = "AJUDA: selecione um segmento, escolha Com porta e clique Aplicar. Duplo clique na cota para editar. Desenho: 90 graus ou livre."
    ajuda.Location = New SD.Point(15, 517)
    ajuda.Size = New SD.Size(245, 55)
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
    lblStatus.Text = "Grade ativa: 500 mm (50 px). Clique nos cruzamentos."
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

Sub CliquePlanta(sender As Object, e As MouseEventArgs)
    If e.Button <> MouseButtons.Left Then Exit Sub
    'Fechamento: ao clicar no primeiro ponto de um trecho em andamento, conecta ao inicio.
    If temPonto Then
        Dim primeiro As SegmentoNR = Nothing
        For Each s As SegmentoNR In lista
            If s.Grupo = grupoAtual Then
                primeiro = s
                Exit For
            End If
        Next
        If primeiro IsNot Nothing Then
            Dim sx As Double = origemX + primeiro.X1 * escala
            Dim sy As Double = origemY - primeiro.Y1 * escala
            If Math.Sqrt(Math.Pow(e.X - sx, 2) + Math.Pow(e.Y - sy, 2)) <= 12 Then
                FecharCercado(Nothing, EventArgs.Empty)
                Exit Sub
            End If
        End If
    End If
    Dim idx As Integer = IndiceCota(e.X, e.Y)
    If idx < 0 Then idx = IndiceProximo(e.X, e.Y)
    If idx >= 0 Then
        selecionado = idx
        ExibirPropriedades()
        desenho.Invalidate()
        Exit Sub
    End If
    ' Snap permanente: TODOS os vertices devem pertencer a malha de 500 mm.
    Dim x As Double = ArredondarGrade((e.X - origemX) / escala)
    Dim y As Double = ArredondarGrade((origemY - e.Y) / escala)
    If temPonto Then
        Dim dx As Double = x - ultimoX
        Dim dy As Double = y - ultimoY
        Dim modo As String = CStr(comboAngulo.SelectedItem)
        If modo = "Passos de 90" Then
            ' Projeta no eixo mais proximo: horizontal ou vertical.
            If Math.Abs(dx) >= Math.Abs(dy) Then
                y = ultimoY
            Else
                x = ultimoX
            End If

        End If
        If Math.Abs(x - ultimoX) + Math.Abs(y - ultimoY) > 1 Then
            Dim s As New SegmentoNR()
            s.X1 = ultimoX : s.Y1 = ultimoY
            s.X2 = x : s.Y2 = y
            s.Grupo = grupoAtual
            s.Modulo = CStr(comboModulo.SelectedItem)
            s.Porta = CStr(comboPorta.SelectedItem)
            s.Tipo = CStr(comboTipo.SelectedItem)
            s.Abertura = CStr(comboAbertura.SelectedItem)
            s.Acesso = If(s.Modulo = "Com porta", s.Porta, "Sem portao")
            lista.Add(s)
            selecionado = lista.Count - 1
            ExibirPropriedades()
        End If
    End If
    ultimoX = x : ultimoY = y
    temPonto = True
    lblStatus.Text = "Ponto (" & x.ToString() & ", " & y.ToString() & ") mm. Trecho " & grupoAtual.ToString() & "."
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
    lblStatus.Text = "Cota atualizada para " & novaMedida.ToString("0.###") & " mm. Pontos ligados ajustados."
    desenho.Invalidate()
End Sub


' Convencao grafica de planta: a linha do vao NAO recebe painel continuo.
Private Sub DesenharSimboloPorta(g As SD.Graphics, s As SegmentoNR, cor As SD.Color)
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

Private Sub DesenharSimboloJanela(g As SD.Graphics, s As SegmentoNR, cor As SD.Color)
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
            DesenharSimboloPorta(g, s, cor)
        ElseIf s.Modulo = "Com janela" Then
            DesenharSimboloJanela(g, s, cor)
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
    comboPorta.Enabled = temPorta
    comboAbertura.Enabled = temPorta
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
    lblStatus.Text = "Propriedades aplicadas ao segmento " & (selecionado + 1).ToString() & "."
    desenho.Invalidate()
End Sub

Sub NovoTrecho(sender As Object, e As EventArgs)
    temPonto = False
    grupoAtual += 1
    selecionado = -1
    ExibirPropriedades()
    lblStatus.Text = "Novo trecho independente: clique no primeiro ponto."
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
    lblStatus.Text = "Cercado fechado. Clique para iniciar outro trecho."
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
        grupoAtual = 1
        For Each s As SegmentoNR In lista
            grupoAtual = Math.Max(grupoAtual, s.Grupo + 1)
        Next
        temPonto = False
        selecionado = -1
        ExibirPropriedades()
        desenho.Invalidate()
        lblStatus.Text = lista.Count.ToString() & " segmentos carregados."
    Catch ex As Exception
        MessageBox.Show(ex.Message, "Falha ao carregar")
    End Try
End Sub
