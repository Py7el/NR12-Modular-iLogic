AddReference "System.Drawing"
Imports System.Windows.Forms
Imports SD = System.Drawing
Imports IO = System.IO

' NR12 Modular — prototipo do editor 2D para iLogic / Inventor 2024.
' VERSAO TESTE V0.2 - manipuladores de eventos por lambdas tipadas.
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
End Class

Private janela As Form
Private desenho As Panel
Private comboTipo As ComboBox
Private comboAcesso As ComboBox
Private comboAbertura As ComboBox
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
Private escala As Double = 0.10 ' pixels por milimetro; 1000 mm = 100 px
Private origemX As Double = 55
Private origemY As Double = 500

Sub CriarJanela()
    janela = New Form()
    janela.Text = "NR12 Modular | Editor de planta"
    janela.ClientSize = New SD.Size(1110, 740)
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
    subtitulo.Text = "01  PLANTA 2D     •     02  CONFIGURACAO     •     03  GERACAO 3D (futuro)"
    subtitulo.ForeColor = SD.Color.FromArgb(203, 187, 233)
    subtitulo.Location = New SD.Point(28, 56)
    subtitulo.Size = New SD.Size(800, 19)
    cab.Controls.Add(subtitulo)

    Dim textoAcao As New Label()
    textoAcao.Text = "Clique na planta para criar pontos. Um novo trecho permite linhas desconectadas."
    textoAcao.Location = New SD.Point(25, 104)
    textoAcao.Size = New SD.Size(760, 22)
    textoAcao.ForeColor = TEXTO
    janela.Controls.Add(textoAcao)

    desenho = New Panel()
    desenho.Location = New SD.Point(25, 134)
    desenho.Size = New SD.Size(770, 522)
    desenho.BackColor = SD.Color.White
    desenho.BorderStyle = BorderStyle.FixedSingle
    desenho.Cursor = Cursors.Cross
    desenho.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic).SetValue(desenho, True, Nothing)
    AddHandler desenho.Paint, Sub(s As Object, e As PaintEventArgs) DesenharPlanta(s, e)
    AddHandler desenho.MouseDown, Sub(s As Object, e As MouseEventArgs) CliquePlanta(s, e)
    janela.Controls.Add(desenho)

    Dim lateral As New Panel()
    lateral.Location = New SD.Point(811, 134)
    lateral.Size = New SD.Size(273, 522)
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

    AdicionarRotulo(lateral, "Tipo de painel", 96)
    comboTipo = AdicionarCombo(lateral, 120, New String() {"Tela", "Chapa", "Policarbonato", "Vazio"})
    AdicionarRotulo(lateral, "Acesso / portao", 163)
    comboAcesso = AdicionarCombo(lateral, 187, New String() {"Sem portao", "Portao simples", "Portao duplo", "Portao deslizante"})
    AdicionarRotulo(lateral, "Abertura", 230)
    comboAbertura = AdicionarCombo(lateral, 254, New String() {"Esquerda", "Direita", "Para dentro", "Para fora"})

    Dim aplicar As Button = CriarBotao(lateral, "Aplicar ao segmento", 15, 303, 240, 38, True)
    AddHandler aplicar.Click, Sub(s As Object, e As EventArgs) AplicarPropriedades(s, e)
    Dim novo As Button = CriarBotao(lateral, "Novo trecho", 15, 354, 115, 36, False)
    AddHandler novo.Click, Sub(s As Object, e As EventArgs) NovoTrecho(s, e)
    Dim apagar As Button = CriarBotao(lateral, "Excluir", 140, 354, 115, 36, False)
    AddHandler apagar.Click, Sub(s As Object, e As EventArgs) ExcluirSegmento(s, e)

    Dim ajuda As New Label()
    ajuda.Text = "AJUDA" & vbCrLf & vbCrLf & "Clique no vazio para desenhar." & vbCrLf & "Clique perto de uma linha para selecionar." & vbCrLf & "ESC encerra o trecho em desenho." & vbCrLf & "Delete exclui o segmento selecionado."
    ajuda.Location = New SD.Point(15, 413)
    ajuda.Size = New SD.Size(245, 104)
    ajuda.ForeColor = SD.Color.FromArgb(74, 69, 96)
    lateral.Controls.Add(ajuda)

    Dim rodape As New Panel()
    rodape.Location = New SD.Point(0, 675)
    rodape.Size = New SD.Size(1110, 65)
    rodape.BackColor = SD.Color.White
    janela.Controls.Add(rodape)

    Dim btnSalvar As Button = CriarBotao(rodape, "Salvar planta", 818, 12, 125, 38, True)
    AddHandler btnSalvar.Click, Sub(s As Object, e As EventArgs) AoClicarSalvarLayoutNR12(s, e)
    Dim btnCarregar As Button = CriarBotao(rodape, "Carregar", 965, 12, 118, 38, False)
    AddHandler btnCarregar.Click, Sub(s As Object, e As EventArgs) AoClicarCarregarLayoutNR12(s, e)
    lblStatus = New Label()
    lblStatus.Location = New SD.Point(25, 20)
    lblStatus.Size = New SD.Size(775, 26)
    lblStatus.ForeColor = SD.Color.FromArgb(74, 69, 96)
    lblStatus.Text = "Pronto para desenhar. Escala: 100 px por metro."
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
    Dim idx As Integer = IndiceProximo(e.X, e.Y)
    If idx >= 0 Then
        selecionado = idx
        ExibirPropriedades()
        desenho.Invalidate()
        Exit Sub
    End If
    Dim x As Double = Math.Round((e.X - origemX) / escala, 0)
    Dim y As Double = Math.Round((origemY - e.Y) / escala, 0)
    If temPonto Then
        If Math.Abs(x - ultimoX) + Math.Abs(y - ultimoY) > 1 Then
            Dim s As New SegmentoNR()
            s.X1 = ultimoX : s.Y1 = ultimoY
            s.X2 = x : s.Y2 = y
            s.Grupo = grupoAtual
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

Sub DesenharPlanta(sender As Object, e As PaintEventArgs)
    Dim g As SD.Graphics = e.Graphics
    g.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias
    Using grade As New SD.Pen(SD.Color.FromArgb(240, 236, 246), 1)
        For x As Integer = CInt(origemX) To desenho.Width Step 50
            g.DrawLine(grade, x, 0, x, desenho.Height)
        Next
        For y As Integer = CInt(origemY) To 0 Step -50
            g.DrawLine(grade, 0, y, desenho.Width, y)
        Next
    End Using
    For i As Integer = 0 To lista.Count - 1
        Dim s As SegmentoNR = lista(i)
        Dim cor As SD.Color = If(i = selecionado, AMBAR, If(s.Acesso = "Sem portao", ROXO, SD.Color.FromArgb(46, 158, 91)))
        Using linha As New SD.Pen(cor, If(i = selecionado, 5.0F, 3.0F))
            g.DrawLine(linha, CSng(origemX + s.X1 * escala), CSng(origemY - s.Y1 * escala), CSng(origemX + s.X2 * escala), CSng(origemY - s.Y2 * escala))
        End Using
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
        g.DrawString("X+ →   Y+ ↑  |  unidade: mm", fonte, SD.Brushes.Gray, 12, 10)
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
    comboAcesso.SelectedItem = s.Acesso
    comboAbertura.SelectedItem = s.Abertura
End Sub

Sub AplicarPropriedades(sender As Object, e As EventArgs)
    If selecionado < 0 OrElse selecionado >= lista.Count Then
        MessageBox.Show("Selecione um segmento primeiro.", "NR12 Modular")
        Exit Sub
    End If
    Dim s As SegmentoNR = lista(selecionado)
    s.Tipo = CStr(comboTipo.SelectedItem)
    s.Acesso = CStr(comboAcesso.SelectedItem)
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
    Try
        Dim linhas As New System.Collections.Generic.List(Of String)
        linhas.Add("# NR12-layout-v1; X1;Y1;X2;Y2;Grupo;Tipo;Acesso;Abertura")
        For Each s As SegmentoNR In lista
            Dim inv As System.Globalization.CultureInfo = System.Globalization.CultureInfo.InvariantCulture
            linhas.Add(String.Join(";", New String() {s.X1.ToString(inv), s.Y1.ToString(inv), s.X2.ToString(inv), s.Y2.ToString(inv), s.Grupo.ToString(inv), s.Tipo, s.Acesso, s.Abertura}))
        Next
        IO.File.WriteAllLines(arquivo, linhas.ToArray(), System.Text.Encoding.UTF8)
        lblStatus.Text = "Layout salvo em " & arquivo
    Catch ex As Exception
        MessageBox.Show(ex.Message, "Falha ao salvar")
    End Try
End Sub

Sub AoClicarCarregarLayoutNR12(sender As Object, e As EventArgs)
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
            If p.Length <> 8 Then Throw New Exception("Registro invalido: " & linha)
            Dim s As New SegmentoNR()
            s.X1 = Double.Parse(p(0), inv) : s.Y1 = Double.Parse(p(1), inv)
            s.X2 = Double.Parse(p(2), inv) : s.Y2 = Double.Parse(p(3), inv)
            s.Grupo = Integer.Parse(p(4), inv)
            s.Tipo = p(5) : s.Acesso = p(6) : s.Abertura = p(7)
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
