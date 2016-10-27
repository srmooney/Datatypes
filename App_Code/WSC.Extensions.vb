Imports System.Runtime.CompilerServices
Imports umbraco.NodeFactory

Namespace WSC.Extensions
    Public Module NodeExtensions
        <Extension()>Public Function DisplayName(n As Node) As String
            Dim p As [Property] = n.Properties("pageNavigationName")
            If p IsNot Nothing AndAlso Not String.IsNullOrEmpty(p.Value) Then
                Return p.Value
            End If
            Return n.Name
        End Function

        <Extension()>Public Function IsHidden(ByVal n As Node) As Boolean
            Dim p As [Property] = n.GetProperty("umbracoNaviHide")
            If p IsNot Nothing Then
                Return p.Value = "1"
            End If
            Return False
        End Function
    End Module

    Public Module ControlExtensions
        <Extension()>Public Sub SetPropertyValue(c As Control, key As String, value As Object)
            Dim type = c.GetType()
            Dim prop = type.GetProperty(key)
            If prop IsNot Nothing Then
                prop.SetValue(c, value, Nothing)
            End If
        End Sub
    End Module

    Public Module DateExtensions
        <Extension()>Public Function Quarter(d As Date) As Integer
            Return (d.Month - 1) \ 3 + 1
        End Function        
    End Module

    Public Module StringExtensions
        <Extension()>
        Public Function Chunk(input As String, chunkSize As Integer) As List(Of String)
            Return Enumerable.Range(0, input.Length / chunkSize).Select(Function(x) input.Substring(x * chunkSize, chunkSize))
        End Function
    End Module

    Public Module ObjectExtensions
        <Extension()>
        Public Function Inspect(o As Object) As String
            Dim myType = o.GetType
            Dim properties = umbraco.Core.TypeExtensions.GetAllProperties(myType).Where(Function(x) x.CanRead)
            Dim sb As New StringBuilder()

            For Each p In properties
                Try
                    sb.AppendFormat("{0}:{1}{2}", p.Name, p.GetValue(o, Nothing).ToString, vbCrLf)
                Catch ex As Exception
                    sb.AppendFormat("{0}:{1}{2}", p.Name, "error", vbCrLf)
                End Try
            Next

            Return sb.ToString
        End Function
    End Module

    Public Module FileExtensions
        <Extension()>
        Public Function VirtualPath(f As IO.FileInfo) As String
            Return "/" & f.FullName.Replace(HttpRuntime.AppDomainAppPath, String.Empty).Replace("\", "/")
        End Function
    End Module

    Public Module ExamineExtensions
        <Extension()>
        Public Function GetIndexDocumentCount(base As Global.Examine.Providers.BaseIndexProvider) As Integer
            Dim indexer = TryCast(base, Global.Examine.LuceneEngine.Providers.LuceneIndexer)
            If indexer Is Nothing Then Return -1
            Dim ret As Integer = -1
            Try
                'Using reader = indexer.GetIndexWriter().GetReader()
                '    ret = reader.NumDocs()
                'End Using
                Dim searcher As New Lucene.Net.Search.IndexSearcher(indexer.GetLuceneDirectory, True)
                Using reader = searcher.GetIndexReader()
                    ret = reader.NumDocs()
                End Using
            Catch ex As Exception
            End Try

            Return ret
        End Function

        <Extension()>
        Public Function GetIndexFieldCount(base As Global.Examine.Providers.BaseIndexProvider) As Integer
            Dim indexer = TryCast(base, Global.Examine.LuceneEngine.Providers.LuceneIndexer)
            If indexer Is Nothing Then Return -1
            Dim ret As Integer = -1
            Try
                'Using reader = indexer.GetIndexWriter().GetReader()
                '    ret = reader.GetFieldNames(Lucene.Net.Index.IndexReader.FieldOption.ALL).Count
                'End Using
                Dim searcher As New Lucene.Net.Search.IndexSearcher(indexer.GetLuceneDirectory, True)
                Using reader = searcher.GetIndexReader()
                    ret = reader.GetFieldNames(Lucene.Net.Index.IndexReader.FieldOption.ALL).Count
                End Using
            Catch ex As Exception
            End Try

            Return ret
        End Function

        <Extension()>
        Public Function GetWorkingDirectory(base As Global.Examine.Providers.BaseIndexProvider) As IO.DirectoryInfo
            Dim indexer = TryCast(base, Global.Examine.LuceneEngine.Providers.LuceneIndexer)
            If indexer Is Nothing Then Return Nothing
            Return indexer.WorkingFolder.GetDirectories("index")(0)
        End Function

        <Extension()>
        Public Function LastUpdate(base As Global.Examine.Providers.BaseIndexProvider) As Date
            Dim indexer = TryCast(base, Global.Examine.LuceneEngine.Providers.LuceneIndexer)
            If indexer Is Nothing Then Return Date.MinValue

            Return base.GetWorkingDirectory().LastWriteTime
        End Function
    End Module

    Public Module UriExtensions
        ''' <summary>
        ''' Retrieves a base domain name from a full domain name.
        ''' For example: www.west-wind.com produces west-wind.com
        ''' </summary>
        ''' <param name="domainName">Dns Domain name as a string</param>
        ''' <returns></returns>
        <Extension()>
        Public Function GetBaseDomain(domainName As String) As String
            Dim tokens = domainName.Split(".")

            ' only split 3 segments like www.west-wind.com
            If tokens Is Nothing OrElse tokens.Length <> 3 Then
                Return domainName
            End If

            Dim tok = New List(Of String)(tokens)
            Dim remove = tokens.Length - 2
            tok.RemoveRange(0, remove)

            Return tok(0) + "." + tok(1)
        End Function

        ''' <summary>
        ''' Returns the base domain from a domain name
        ''' Example: http://www.west-wind.com returns west-wind.com
        ''' </summary>
        ''' <param name="uri"></param>
        ''' <returns></returns>
        <Extension()>
        Public Function GetBaseDomain(uri As Uri) As String
            If uri.HostNameType = UriHostNameType.Dns Then
                Return GetBaseDomain(uri.DnsSafeHost)
            End If

            Return uri.Host
        End Function


    End Module

    Public Module ListExtensions
        <Extension()>
        Public Sub Move(Of T)(source As List(Of T), item As T, newIndex As Integer)
            If source Is Nothing Then
                Throw New ArgumentNullException("source")
            End If

            If item Is Nothing Then Exit Sub
            source.Remove(item)
            source.Insert(newIndex, item)
        End Sub

        <Extension()>
        Public Sub Move(Of T)(source As List(Of T), oldIndex As Integer, newIndex As Integer)
            If source Is Nothing Then
                Throw New ArgumentNullException("source")
            End If
            Dim item = source(oldIndex)
            If item Is Nothing Then Exit Sub
            source.Move(item, newIndex)
        End Sub
    End Module

End Namespace