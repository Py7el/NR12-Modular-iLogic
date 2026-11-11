Imports IO = System.IO
Imports CI = System.Globalization.CultureInfo
Imports IV = Inventor

' Parametros ficam nas funcoes no final desta regra para evitar declaracoes no namespace.

' NR12 Modular - gerador 3D preliminar, Inventor 2024.
' VERSAO 0.7.1 - parametros em funcoes para compatibilidade com compilacao iLogic.
' Subdivisao automatica max. 2500 mm entre eixos de postes.
' Montantes e bases inseridos a partir da biblioteca CAD real fornecida.
' Criar como regra iLogic separada: Gerar_3D_NR12
' Comprimentos em mm no TXT; Inventor utiliza cm internamente.
' Portas simples, duplas e de correr sao folhas independentes; sem articulacoes reais.
' Geometria simplificada: sem malha, ferragens ou validacao normativa.
Sub Main()
    If ThisDoc.Document.DocumentType <> IV.DocumentTypeEnum.kAssemblyDocumentObject Then
        MessageBox.Show("Execute esta regra em uma montagem.", "NR12")
        Exit Sub
    End If
    Dim montagem As IV.AssemblyDocument = CType(ThisDoc.Document, IV.AssemblyDocument)
    If String.IsNullOrEmpty(montagem.FullFileName) Then
        MessageBox.Show("Salve a montagem .iam antes de gerar.", "NR12")
        Exit Sub
    End If
    Dim raiz As String = IO.Path.GetDirectoryName(montagem.FullFileName)
    Dim fonte As String = IO.Path.Combine(raiz, "NR12_layout.txt")
    If Not IO.File.Exists(fonte) Then
        Dim resposta As System.Windows.Forms.DialogResult = System.Windows.Forms.MessageBox.Show( _
            "O layout nao foi encontrado na pasta desta montagem:" & vbCrLf & fonte & vbCrLf & vbCrLf & _
            "Deseja localizar manualmente o arquivo NR12_layout.txt?", _
            "NR12 - localizar planta", System.Windows.Forms.MessageBoxButtons.YesNo, _
            System.Windows.Forms.MessageBoxIcon.Question)
        If resposta <> System.Windows.Forms.DialogResult.Yes Then Exit Sub
        Using seletor As New System.Windows.Forms.OpenFileDialog()
            seletor.Title = "Selecione o layout NR12 salvo pelo editor 2D"
            seletor.Filter = "Layout NR12 (NR12_layout.txt)|NR12_layout.txt|Arquivos TXT (*.txt)|*.txt"
            seletor.CheckFileExists = True
            seletor.Multiselect = False
            seletor.InitialDirectory = raiz
            If seletor.ShowDialog() <> System.Windows.Forms.DialogResult.OK Then Exit Sub
            fonte = seletor.FileName
        End Using
    End If
    Dim dados As New System.Collections.Generic.List(Of String())
    For Each linha As String In IO.File.ReadAllLines(fonte)
        If String.IsNullOrWhiteSpace(linha) OrElse linha.StartsWith("#") Then Continue For
        Dim p As String() = linha.Split(";"c)
        If p.Length <> 8 AndAlso p.Length <> 10 Then Throw New Exception("Layout invalido: " & linha)
        dados.Add(p)
    Next
    If dados.Count = 0 Then
        MessageBox.Show("Nao existem segmentos para gerar.", "NR12")
        Exit Sub
    End If

    ' Regra geometrica: distancia maxima entre eixos de postes = 2500 mm.
    ' A subdivisao e aplicada ANTES da geracao de pecas e postes.
    ' Portas nao sao cortadas: suas aberturas devem ter no maximo 2500 mm.
    Dim invDiv As CI = CI.InvariantCulture
    Dim subdivididos As New System.Collections.Generic.List(Of String())
    For Each registro As String() In dados
        Dim ax As Double = Double.Parse(registro(0), invDiv)
        Dim ay As Double = Double.Parse(registro(1), invDiv)
        Dim bx As Double = Double.Parse(registro(2), invDiv)
        Dim by As Double = Double.Parse(registro(3), invDiv)
        Dim dist As Double = Math.Sqrt(Math.Pow(bx - ax, 2) + Math.Pow(by - ay, 2))
        If dist < 100 Then Throw New Exception("Trecho inferior a 100 mm: revise a planta.")
        Dim modulo As String = If(registro.Length = 10, registro(8), If(registro(6) = "Sem portao", "Sem janela", "Com porta"))
        If modulo = "Com porta" AndAlso dist > 2500.0001 Then
            Throw New Exception("Abertura de porta com " & Math.Round(dist, 1).ToString(invDiv) & _
              " mm ultrapassa o limite de 2500 mm entre postes. Divida o trecho na planta.")
        End If
        Dim n As Integer = CInt(Math.Ceiling(dist / 2500.0))
        If n < 1 Then n = 1
        For k As Integer = 0 To n - 1
            Dim parte As String() = CType(registro.Clone(), String())
            parte(0) = (ax + (bx - ax) * CDbl(k) / CDbl(n)).ToString("R", invDiv)
            parte(1) = (ay + (by - ay) * CDbl(k) / CDbl(n)).ToString("R", invDiv)
            parte(2) = (ax + (bx - ax) * CDbl(k + 1) / CDbl(n)).ToString("R", invDiv)
            parte(3) = (ay + (by - ay) * CDbl(k + 1) / CDbl(n)).ToString("R", invDiv)
            subdivididos.Add(parte)
        Next
    Next
    dados = subdivididos
    ' Modelos CAD reais entregues no pacote NR12_Modelos_Reais.zip.
    ' Copiar a pasta NR12_Modelos ao lado do arquivo .iam.
    Dim pastaModelos As String = IO.Path.Combine(raiz, "NR12_Modelos")
    Dim colunaReal As String = IO.Path.Combine(pastaModelos, "7.1.2 - COLUNA-MONTANTE.ipt")
    Dim baseReal As String = IO.Path.Combine(pastaModelos, "7.1.3 - BASE SOLDADA PARA MONTANTE.iam")
    Dim chapaInferiorReal As String = IO.Path.Combine(pastaModelos, "7.1.4 - CHAPA LISA 1-2x130x185.ipt")
    If Not IO.File.Exists(colunaReal) OrElse Not IO.File.Exists(baseReal) Then
        MessageBox.Show("Os modelos reais da coluna e base nao foram encontrados." & vbCrLf & _
            "Extraia NR12_Modelos_Reais.zip na pasta da montagem (.iam), mantendo NR12_Modelos.", "NR12 - modelos ausentes")
        Exit Sub
    End If
    Dim depend1 As String = IO.Path.Combine(pastaModelos, "7.1.3.1 - CHAPA LISA 3-4x130x130 (BASE).ipt")
    Dim depend2 As String = IO.Path.Combine(pastaModelos, "7.1.3.2 - TUBO QUADRADO - 50 x 50 x 2  - 280mm.ipt")
    If Not IO.File.Exists(depend1) OrElse Not IO.File.Exists(depend2) Then
        MessageBox.Show("Faltam arquivos IPT referenciados pela base soldada.", "NR12")
        Exit Sub
    End If
    If UsarChapaInferiorReal() AndAlso Not IO.File.Exists(chapaInferiorReal) Then
        MessageBox.Show("Chapa inferior real nao encontrada: " & chapaInferiorReal, "NR12")
        Exit Sub
    End If
    Dim dirPecas As String = IO.Path.Combine(raiz, "NR12_Gerado")
    IO.Directory.CreateDirectory(dirPecas)
    Dim geradas As New System.Collections.Generic.Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Dim pontos As New System.Collections.Generic.Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
    Dim comp As IV.AssemblyComponentDefinition = montagem.ComponentDefinition
    Dim ocorr As IV.ComponentOccurrences = comp.Occurrences
    Dim trans As IV.TransientGeometry = ThisApplication.TransientGeometry
    Dim inv As CI = CI.InvariantCulture
    Dim alturaMM As Double = 2000
    Dim posteMM As Double = 60
    Dim alturaCm As Double = alturaMM / 10
    Dim qtd As Integer = 0

    ' Normal media por vertice: permite orientar o montante nos cantos.
    Dim normaisPorPoste As New System.Collections.Generic.Dictionary(Of String, Double())(StringComparer.OrdinalIgnoreCase)
    For Each registro As String() In dados
        Dim aX As Double = Double.Parse(registro(0), inv)
        Dim aY As Double = Double.Parse(registro(1), inv)
        Dim bX As Double = Double.Parse(registro(2), inv)
        Dim bY As Double = Double.Parse(registro(3), inv)
        Dim nX As Double = 0, nY As Double = 0
        NormalExternaTrecho(registro, dados, nX, nY)
        AcumularNormal(normaisPorPoste, aX, aY, nX, nY)
        AcumularNormal(normaisPorPoste, bX, bY, nX, nY)
    Next

    ' Preparar pecas primeiro. Depois de validar todas, substituir ocorrencias antigas.
    For Each p As String() In dados
        Dim x1 As Double = Double.Parse(p(0), inv)
        Dim y1 As Double = Double.Parse(p(1), inv)
        Dim x2 As Double = Double.Parse(p(2), inv)
        Dim y2 As Double = Double.Parse(p(3), inv)
        Dim dx As Double = x2 - x1, dy As Double = y2 - y1
        Dim larguraMM As Double = Math.Sqrt(dx * dx + dy * dy)
        If larguraMM < 100 Then Throw New Exception("Segmento inferior a 100 mm; corrija no editor.")
        Dim modulo As String = If(p.Length = 10, p(8), If(p(6) = "Sem portao", "Sem janela", "Com porta"))
        Dim vazio As Boolean = p(5) = "Vazio"
        If modulo = "Com janela" Then
            If larguraMM < 300 Then Throw New Exception("Janela requer segmento com pelo menos 300 mm.")
            ObterCaixa(dirPecas, geradas, "MontanteJanela", 50, alturaMM, 35)
            ObterCaixa(dirPecas, geradas, "TravessaJanela", larguraMM - 100, 50, 35)
        ElseIf modulo = "Com porta" Then
            Dim tipoPorta As String = If(p.Length = 10, p(9), "Porta simples")
            If larguraMM <= 160 Then Throw New Exception("Porta requer segmento superior a 160 mm.")
            If tipoPorta = "Porta dupla" Then
                ObterCaixa(dirPecas, geradas, "FolhaPortaDupla", (larguraMM - 80) / 2, alturaMM - 100, 40)
            Else
                ObterCaixa(dirPecas, geradas, "FolhaPorta", larguraMM - 60, alturaMM - 100, 40)
            End If
        ElseIf Not vazio Then
            ObterCaixa(dirPecas, geradas, "Painel", larguraMM - 60, alturaMM - 100, 25)
        End If
    Next
    ' Substitui poste generico pelos arquivos reais de coluna e base.
    ' Sem alterar parametricamente esses modelos originais.

    ' Desfazer geracao anterior apenas depois que os arquivos forem criados.
    For i As Integer = ocorr.Count To 1 Step -1
        Dim o As IV.ComponentOccurrence = ocorr.Item(i)
        If o.Name.StartsWith("NR12_", StringComparison.OrdinalIgnoreCase) Then o.Delete()
    Next

    For Each p As String() In dados
        Dim x1 As Double = Double.Parse(p(0), inv)
        Dim y1 As Double = Double.Parse(p(1), inv)
        Dim x2 As Double = Double.Parse(p(2), inv)
        Dim y2 As Double = Double.Parse(p(3), inv)
        Dim dx As Double = x2 - x1, dy As Double = y2 - y1
        Dim largura As Double = Math.Sqrt(dx * dx + dy * dy)
        Dim ux As Double = dx / largura, uy As Double = dy / largura
        Dim modulo As String = If(p.Length = 10, p(8), If(p(6) = "Sem portao", "Sem janela", "Com porta"))
        Dim vazio As Boolean = p(5) = "Vazio"
        Dim nx As Double = 0, ny As Double = 0
        NormalExternaTrecho(p, dados, nx, ny)
        CriarPosteSeNovo(pontos, normaisPorPoste, ocorr, colunaReal, baseReal, chapaInferiorReal, x1, y1, trans, qtd)
        CriarPosteSeNovo(pontos, normaisPorPoste, ocorr, colunaReal, baseReal, chapaInferiorReal, x2, y2, trans, qtd)
        Dim larguraUtil As Double = largura - 60
        ' A grade fica do lado EXTERNO, sobre a linha dos grampos.
        Dim xInicial As Double = x1 + ux * 30 + nx * AfastamentoPlanoGradeMm()
        Dim yInicial As Double = y1 + uy * 30 + ny * AfastamentoPlanoGradeMm()
        Dim xFinal As Double = x2 + nx * AfastamentoPlanoGradeMm()
        Dim yFinal As Double = y2 + ny * AfastamentoPlanoGradeMm()
        If modulo = "Com janela" Then
            Dim montante As String = ObterCaixa(dirPecas, geradas, "MontanteJanela", 50, alturaMM, 35)
            Dim travessa As String = ObterCaixa(dirPecas, geradas, "TravessaJanela", largura - 100, 50, 35)
            Inserir(ocorr, montante, MatrizPainel(trans, xInicial, yInicial, ux, uy, 0), "NR12_JANELA_M" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, montante, MatrizPainel(trans, xFinal - ux * 80, yFinal - uy * 80, ux, uy, 0), "NR12_JANELA_M" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, travessa, MatrizPainel(trans, xInicial + ux * 50, yInicial + uy * 50, ux, uy, 0), "NR12_JANELA_T" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, travessa, MatrizPainel(trans, xInicial + ux * 50, yInicial + uy * 50, ux, uy, 1950), "NR12_JANELA_T" & qtd.ToString())
            qtd += 1
        ElseIf modulo = "Com porta" Then
            ' Porta representada VISUALMENTE ABERTA a 60 graus para ser diferenciada dos paineis.
            ' O movimento e estatico; nao inclui dobradicas, batentes ou juntas.
            Dim tipoPorta As String = If(p.Length = 10, p(9), "Porta simples")
            Dim abertura As String = p(7)
            Dim direito As Boolean = abertura.IndexOf("Direita", StringComparison.OrdinalIgnoreCase) >= 0
            Dim paraDentro As Boolean = abertura.IndexOf("dentro", StringComparison.OrdinalIgnoreCase) >= 0
            Dim sinal As Double = If(paraDentro, -1.0, 1.0)
            If tipoPorta = "Porta dupla" Then
                Dim meiaLargura As Double = (largura - 80) / 2
                Dim caminho As String = ObterCaixa(dirPecas, geradas, "FolhaPortaDupla", meiaLargura, alturaMM - 100, 40)
                Inserir(ocorr, caminho, MatrizPortaAberta(trans, xInicial, yInicial, ux, uy, FolgaInferiorGradeMm(), sinal * 60), "NR12_PORTA_DUPLA_A_" & qtd.ToString())
                qtd += 1
                Inserir(ocorr, caminho, MatrizPortaAberta(trans, xFinal - ux * 30, yFinal - uy * 30, -ux, -uy, FolgaInferiorGradeMm(), -sinal * 60), "NR12_PORTA_DUPLA_B_" & qtd.ToString())
                qtd += 1
            ElseIf tipoPorta = "Porta deslizante" Then
                Dim caminho As String = ObterCaixa(dirPecas, geradas, "FolhaPorta", larguraUtil, alturaMM - 100, 40)
                ' Simula a porta deslizada 40% para facilitar leitura visual; sem trilho mecanico.
                Dim deslocamento As Double = larguraUtil * 0.4
                Inserir(ocorr, caminho, MatrizPainel(trans, xInicial + ux * deslocamento - uy * 100 * sinal, yInicial + uy * deslocamento + ux * 100 * sinal, ux, uy, FolgaInferiorGradeMm()), "NR12_PORTA_DESLIZANTE_" & qtd.ToString())
                qtd += 1
            Else
                Dim caminho As String = ObterCaixa(dirPecas, geradas, "FolhaPorta", larguraUtil, alturaMM - 100, 40)
                If direito Then
                    Inserir(ocorr, caminho, MatrizPortaAberta(trans, xFinal - ux * 30, yFinal - uy * 30, -ux, -uy, FolgaInferiorGradeMm(), -sinal * 60), "NR12_PORTA_SIMPLES_DIREITA_" & qtd.ToString())
                Else
                    Inserir(ocorr, caminho, MatrizPortaAberta(trans, xInicial, yInicial, ux, uy, FolgaInferiorGradeMm(), sinal * 60), "NR12_PORTA_SIMPLES_ESQUERDA_" & qtd.ToString())
                End If
                qtd += 1
            End If
        ElseIf Not vazio Then
            Dim caminho As String = ObterCaixa(dirPecas, geradas, "Painel", larguraUtil, alturaMM - 100, 25)
            Inserir(ocorr, caminho, MatrizPainel(trans, xInicial, yInicial, ux, uy, FolgaInferiorGradeMm()), "NR12_PAINEL_" & qtd.ToString())
            qtd += 1
        End If
    Next
    montagem.Update2()
    MessageBox.Show("Geracao concluida: " & dados.Count.ToString() & " segmentos processados." & vbCrLf & _
                    "Pecas salvas em: " & dirPecas & vbCrLf & _
                    "Geometria de estudo: nao representa conformidade NR-12.", "NR12 Modular")
End Sub

' Normal externa: para perimetro fechado, usa a orientacao da polilinha.
' Em trechos abertos, adota o lado direito do sentido do desenho.
Sub NormalExternaTrecho(p As String(), todos As System.Collections.Generic.List(Of String()), ByRef nx As Double, ByRef ny As Double)
    Dim inv As CI = CI.InvariantCulture
    Dim x1 As Double = Double.Parse(p(0), inv)
    Dim y1 As Double = Double.Parse(p(1), inv)
    Dim x2 As Double = Double.Parse(p(2), inv)
    Dim y2 As Double = Double.Parse(p(3), inv)
    Dim dx As Double = x2 - x1, dy As Double = y2 - y1
    Dim comprimento As Double = Math.Sqrt(dx * dx + dy * dy)
    If comprimento < 0.000001 Then Throw New Exception("Segmento degenerado no layout.")
    Dim area2 As Double = 0
    Dim primeiro As String() = Nothing, ultimo As String() = Nothing
    For Each outro As String() In todos
        If outro(4) <> p(4) Then Continue For
        If primeiro Is Nothing Then primeiro = outro
        ultimo = outro
        Dim ax As Double = Double.Parse(outro(0), inv)
        Dim ay As Double = Double.Parse(outro(1), inv)
        Dim bx As Double = Double.Parse(outro(2), inv)
        Dim by As Double = Double.Parse(outro(3), inv)
        area2 += ax * by - bx * ay
    Next
    Dim sentido As Double = 1.0
    If primeiro IsNot Nothing AndAlso ultimo IsNot Nothing Then
        Dim fechado As Boolean = Math.Abs(Double.Parse(primeiro(0), inv) - Double.Parse(ultimo(2), inv)) < 0.01 AndAlso _
            Math.Abs(Double.Parse(primeiro(1), inv) - Double.Parse(ultimo(3), inv)) < 0.01
        If fechado AndAlso area2 < 0 Then sentido = -1.0
    End If
    nx = (dy / comprimento) * sentido
    ny = (-dx / comprimento) * sentido
End Sub

Function ChavePoste(x As Double, y As Double) As String
    Return Math.Round(x, 1).ToString(CI.InvariantCulture) & ";" & Math.Round(y, 1).ToString(CI.InvariantCulture)
End Function

Sub AcumularNormal(tabela As System.Collections.Generic.Dictionary(Of String, Double()), x As Double, y As Double, nx As Double, ny As Double)
    Dim chave As String = ChavePoste(x, y)
    If Not tabela.ContainsKey(chave) Then tabela.Add(chave, New Double() {0, 0})
    tabela(chave)(0) += nx
    tabela(chave)(1) += ny
End Sub

' Insere CAD real em pe e gira a direcao LOCAL dos grampos para a normal externa.
' Usa a dimensao mais longa para reconhecer o eixo vertical da coluna/base.
' Face real dos grampos ainda depende do ajuste AnguloAjusteGramposGraus().
Function InserirModeloOrientado(ocorr As IV.ComponentOccurrences, caminho As String, x As Double, y As Double, zBaseMm As Double, nx As Double, ny As Double, tg As IV.TransientGeometry, nome As String, chapaHorizontal As Boolean) As IV.ComponentOccurrence
    Dim o As IV.ComponentOccurrence = ocorr.Add(caminho, tg.CreateMatrix())
    o.Name = nome
    Dim b As IV.Box = o.RangeBox
    Dim dimX As Double = b.MaxPoint.X - b.MinPoint.X
    Dim dimY As Double = b.MaxPoint.Y - b.MinPoint.Y
    Dim dimZ As Double = b.MaxPoint.Z - b.MinPoint.Z
    Dim eixo As Integer = 3
    If chapaHorizontal Then
        ' A menor dimensao da chapa corresponde a espessura.
        If dimX <= dimY AndAlso dimX <= dimZ Then
            eixo = 1
        ElseIf dimY <= dimX AndAlso dimY <= dimZ Then
            eixo = 2
        End If
    Else
        If dimX >= dimY AndAlso dimX >= dimZ Then
            eixo = 1
        ElseIf dimY >= dimX AndAlso dimY >= dimZ Then
            eixo = 2
        End If
    End If
    Dim ang As Double = AnguloAjusteGramposGraus() * Math.PI / 180.0
    Dim fx As Double = nx * Math.Cos(ang) - ny * Math.Sin(ang)
    Dim fy As Double = nx * Math.Sin(ang) + ny * Math.Cos(ang)
    Dim ejeX As IV.Vector, ejeY As IV.Vector, ejeZ As IV.Vector
    If eixo = 1 Then
        ' X local vira Z; Y local aponta para o lado dos grampos.
        ejeX = tg.CreateVector(0, 0, 1)
        ejeY = tg.CreateVector(fx, fy, 0)
        ejeZ = tg.CreateVector(-fy, fx, 0)
    ElseIf eixo = 2 Then
        ' Y local vira Z; X local aponta para o lado dos grampos.
        ejeX = tg.CreateVector(fx, fy, 0)
        ejeY = tg.CreateVector(0, 0, 1)
        ejeZ = tg.CreateVector(fy, -fx, 0)
    Else
        ' Z local vira Z; X local aponta para o lado dos grampos.
        ejeX = tg.CreateVector(fx, fy, 0)
        ejeY = tg.CreateVector(-fy, fx, 0)
        ejeZ = tg.CreateVector(0, 0, 1)
    End If
    Dim matriz As IV.Matrix = tg.CreateMatrix()
    matriz.SetCoordinateSystem(tg.CreatePoint(0, 0, 0), ejeX, ejeY, ejeZ)
    o.Transformation = matriz
    ' Centralizar somente depois da rotacao; Z minimo deve ficar na altura definida.
    Dim caixa As IV.Box = o.RangeBox
    Dim meioX As Double = (caixa.MinPoint.X + caixa.MaxPoint.X) / 2
    Dim meioY As Double = (caixa.MinPoint.Y + caixa.MaxPoint.Y) / 2
    Dim zMin As Double = caixa.MinPoint.Z
    matriz.SetTranslation(tg.CreateVector(x / 10 - meioX, y / 10 - meioY, zBaseMm / 10 - zMin), False)
    o.Transformation = matriz
    Return o
End Function

Sub CriarPosteSeNovo(pontos As System.Collections.Generic.Dictionary(Of String, Boolean), normais As System.Collections.Generic.Dictionary(Of String, Double()), ocorr As IV.ComponentOccurrences, coluna As String, base As String, chapa As String, x As Double, y As Double, tg As IV.TransientGeometry, ByRef contador As Integer)
    Dim chave As String = ChavePoste(x, y)
    If pontos.ContainsKey(chave) Then Exit Sub
    pontos.Add(chave, True)
    Dim nx As Double = 1, ny As Double = 0
    If normais.ContainsKey(chave) Then
        Dim vetor As Double() = normais(chave)
        Dim tam As Double = Math.Sqrt(vetor(0) * vetor(0) + vetor(1) * vetor(1))
        If tam > 0.000001 Then
            nx = vetor(0) / tam : ny = vetor(1) / tam
        End If
    End If
    Dim zAcimaChapa As Double = 0
    If UsarChapaInferiorReal() Then
        Dim ocorrChapa As IV.ComponentOccurrence = InserirModeloOrientado(ocorr, chapa, x, y, 0, nx, ny, tg, "NR12_CHAPA_INFERIOR_" & contador.ToString(), True)
        zAcimaChapa = ocorrChapa.RangeBox.MaxPoint.Z * 10
        contador += 1
    End If
    InserirModeloOrientado(ocorr, base, x, y, zAcimaChapa, nx, ny, tg, "NR12_BASE_REAL_" & contador.ToString(), False)
    contador += 1
    InserirModeloOrientado(ocorr, coluna, x, y, zAcimaChapa, nx, ny, tg, "NR12_COLUNA_REAL_" & contador.ToString(), False)
    contador += 1
End Sub

' Rotaciona a folha em planta ao redor do seu ponto de dobradica para mostrá-la aberta.
Function MatrizPortaAberta(tg As IV.TransientGeometry, xMM As Double, yMM As Double, ux As Double, uy As Double, zMM As Double, anguloGrau As Double) As IV.Matrix
    Dim rad As Double = anguloGrau * Math.PI / 180.0
    Dim uxAberto As Double = ux * Math.Cos(rad) - uy * Math.Sin(rad)
    Dim uyAberto As Double = ux * Math.Sin(rad) + uy * Math.Cos(rad)
    Return MatrizPainel(tg, xMM, yMM, uxAberto, uyAberto, zMM)
End Function

Function MatrizPainel(tg As IV.TransientGeometry, xMM As Double, yMM As Double, ux As Double, uy As Double, zMM As Double) As IV.Matrix
    Dim m As IV.Matrix = tg.CreateMatrix()
    Dim origem As IV.Point = tg.CreatePoint(xMM / 10, yMM / 10, zMM / 10)
    Dim eixoX As IV.Vector = tg.CreateVector(ux, uy, 0)
    Dim eixoY As IV.Vector = tg.CreateVector(0, 0, 1)
    Dim eixoZ As IV.Vector = tg.CreateVector(uy, -ux, 0)
    m.SetCoordinateSystem(origem, eixoX, eixoY, eixoZ)
    Return m
End Function

Sub Inserir(ocorr As IV.ComponentOccurrences, caminho As String, matriz As IV.Matrix, nome As String)
    Dim o As IV.ComponentOccurrence = ocorr.Add(caminho, matriz)
    o.Name = nome
End Sub

Function ObterCaixa(pasta As String, cache As System.Collections.Generic.Dictionary(Of String, String), nome As String, larguraMM As Double, alturaMM As Double, espessuraMM As Double, Optional poste As Boolean = False) As String
    Dim chave As String = nome & "_" & Math.Round(larguraMM, 1).ToString(CI.InvariantCulture).Replace(".", "p") & "_" & Math.Round(alturaMM, 1).ToString(CI.InvariantCulture).Replace(".", "p") & "_" & Math.Round(espessuraMM, 1).ToString(CI.InvariantCulture).Replace(".", "p")
    If cache.ContainsKey(chave) Then Return cache(chave)
    Dim caminho As String = IO.Path.Combine(pasta, chave & ".ipt")
    If Not IO.File.Exists(caminho) Then
        Dim pdoc As IV.PartDocument = Nothing
        Try
            pdoc = CType(ThisApplication.Documents.Add(IV.DocumentTypeEnum.kPartDocumentObject, ThisApplication.FileManager.GetTemplateFile(IV.DocumentTypeEnum.kPartDocumentObject), False), IV.PartDocument)
            Dim definicao As IV.PartComponentDefinition = pdoc.ComponentDefinition
            Dim sk As IV.PlanarSketch = definicao.Sketches.Add(definicao.WorkPlanes.Item(3))
            Dim tg As IV.TransientGeometry = ThisApplication.TransientGeometry
            sk.SketchLines.AddAsTwoPointRectangle(tg.CreatePoint2d(0, 0), tg.CreatePoint2d(larguraMM / 10, alturaMM / 10))
            Dim perfil As IV.Profile = sk.Profiles.AddForSolid()
            Dim extr As IV.ExtrudeDefinition = definicao.Features.ExtrudeFeatures.CreateExtrudeDefinition(perfil, IV.PartFeatureOperationEnum.kJoinOperation)
            extr.SetDistanceExtent(espessuraMM / 10, IV.PartFeatureExtentDirectionEnum.kPositiveExtentDirection)
            definicao.Features.ExtrudeFeatures.Add(extr)
            pdoc.SaveAs(caminho, False)
        Finally
            If pdoc IsNot Nothing Then pdoc.Close(True)
        End Try
    End If
    cache.Add(chave, caminho)
    Return caminho
End Function


' ----- PARAMETROS DE CALIBRACAO DA VERSAO EXPERIMENTAL -----
' Edite os valores de retorno para ajustar o modelo real.
Function AfastamentoPlanoGradeMm() As Double
    Return 65.0
End Function

Function AnguloAjusteGramposGraus() As Double
    Return 0.0
End Function

Function UsarChapaInferiorReal() As Boolean
    Return True
End Function

Function FolgaInferiorGradeMm() As Double
    Return 50.0
End Function
