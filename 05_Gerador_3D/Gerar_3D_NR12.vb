Imports IO = System.IO
Imports CI = System.Globalization.CultureInfo
Imports IV = Inventor

' NR12 Modular - gerador 3D preliminar, Inventor 2024.
' Criar como regra iLogic separada: Gerar_3D_NR12
' Comprimentos em mm no TXT; Inventor utiliza cm internamente.
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
        MessageBox.Show("Layout inexistente: " & fonte, "NR12")
        Exit Sub
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
        ElseIf Not vazio OrElse modulo = "Com porta" Then
            ObterCaixa(dirPecas, geradas, If(modulo = "Com porta", "FolhaPorta", "Painel"), larguraMM - 60, alturaMM - 100, If(modulo = "Com porta", 40, 25))
        End If
    Next
    Dim postePath As String = ObterCaixa(dirPecas, geradas, "Poste", posteMM, posteMM, alturaMM, True)

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
        CriarPosteSeNovo(pontos, ocorr, postePath, x1, y1, trans, qtd)
        CriarPosteSeNovo(pontos, ocorr, postePath, x2, y2, trans, qtd)
        Dim larguraUtil As Double = largura - 60
        Dim xInicial As Double = x1 + ux * 30
        Dim yInicial As Double = y1 + uy * 30
        If modulo = "Com janela" Then
            Dim montante As String = ObterCaixa(dirPecas, geradas, "MontanteJanela", 50, alturaMM, 35)
            Dim travessa As String = ObterCaixa(dirPecas, geradas, "TravessaJanela", largura - 100, 50, 35)
            Inserir(ocorr, montante, MatrizPainel(trans, xInicial, yInicial, ux, uy, 0), "NR12_JANELA_M" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, montante, MatrizPainel(trans, x2 - ux * 80, y2 - uy * 80, ux, uy, 0), "NR12_JANELA_M" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, travessa, MatrizPainel(trans, x1 + ux * 80, y1 + uy * 80, ux, uy, 0), "NR12_JANELA_T" & qtd.ToString())
            qtd += 1
            Inserir(ocorr, travessa, MatrizPainel(trans, x1 + ux * 80, y1 + uy * 80, ux, uy, 1950), "NR12_JANELA_T" & qtd.ToString())
            qtd += 1
        ElseIf Not vazio OrElse modulo = "Com porta" Then
            Dim nome As String = If(modulo = "Com porta", "FolhaPorta", "Painel")
            Dim esp As Double = If(modulo = "Com porta", 40, 25)
            Dim caminho As String = ObterCaixa(dirPecas, geradas, nome, larguraUtil, alturaMM - 100, esp)
            Inserir(ocorr, caminho, MatrizPainel(trans, xInicial, yInicial, ux, uy, 50), "NR12_" & nome.ToUpperInvariant() & "_" & qtd.ToString())
            qtd += 1
        End If
    Next
    montagem.Update2()
    MessageBox.Show("Geracao concluida: " & dados.Count.ToString() & " segmentos processados." & vbCrLf & _
                    "Pecas salvas em: " & dirPecas & vbCrLf & _
                    "Geometria de estudo: nao representa conformidade NR-12.", "NR12 Modular")
End Sub

Sub CriarPosteSeNovo(pontos As System.Collections.Generic.Dictionary(Of String, Boolean), ocorr As IV.ComponentOccurrences, caminho As String, x As Double, y As Double, tg As IV.TransientGeometry, ByRef contador As Integer)
    Dim chave As String = Math.Round(x, 1).ToString(CI.InvariantCulture) & ";" & Math.Round(y, 1).ToString(CI.InvariantCulture)
    If pontos.ContainsKey(chave) Then Exit Sub
    pontos.Add(chave, True)
    Dim m As IV.Matrix = tg.CreateMatrix()
    ' Criado no esboco XY, extrudado em Z; poste local usa eixos globais.
    m.SetTranslation(tg.CreateVector((x - 30) / 10, (y - 30) / 10, 0))
    Inserir(ocorr, caminho, m, "NR12_POSTE_" & contador.ToString())
    contador += 1
End Sub

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
