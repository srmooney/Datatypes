Imports System.IO
Imports System.Web
Imports System.IO.Compression
Imports System.Text.RegularExpressions

Namespace WSC.Modules
    ''' <summary>
    ''' <system.webServer>
    '''     <modules>
    '''         <add name="WSC.Modules.HtmlMinifier" type="WSC.Modules.HtmlMinifier" />
    '''     </modules>
    ''' </system.webServer>
    ''' </summary>
    ''' <remarks></remarks>
    Public Class HtmlMinifier
        Implements IHttpModule

#Region "IHttpModule Members"

        Private Sub IHttpModule_Dispose() Implements IHttpModule.Dispose
            '--Nothing to dispose
        End Sub

        Private Sub IHttpModule_Init(context As HttpApplication) Implements IHttpModule.Init
            AddHandler context.PostReleaseRequestState, AddressOf context_BeginRequest
        End Sub

#End Region

        Private Sub context_BeginRequest(sender As Object, e As EventArgs)
            Dim app As HttpApplication = TryCast(sender, HttpApplication)
            Dim rawUrl = app.Request.RawUrl

            If IsCompilationDebug() Then Exit Sub
            If rawUrl.Contains(".axd") Then Exit Sub
            If rawUrl.Contains("/umbraco/") Then Exit Sub
            If Not app.Response.ContentType.Contains("html") Then Exit Sub

            app.Response.Filter = New WhitespaceFilter(app.Response.Filter)
        End Sub

        Public Shared ReadOnly Property IsCompilationDebug() As Boolean
            Get
                Dim compilation As Configuration.CompilationSection = TryCast(ConfigurationManager.GetSection("system.web/compilation"), Configuration.CompilationSection)
                If compilation IsNot Nothing Then
                    Return compilation.Debug
                End If
                '--by default, return false!
                Return False
            End Get
        End Property

#Region "Stream filter"

        Private Class WhitespaceFilter
            Inherits Stream

            Public Sub New(sink As Stream)
                _sink = sink
            End Sub

            Private _sink As Stream
            Private Shared reg As New Regex("(?<=[^])\t{2,}|(?<=[>])\s{2,}(?=[<])|(?<=[>])\s{2,11}(?=[<])|(?=[\n])\s{2,}")

#Region "Properites"

            Public Overrides ReadOnly Property CanRead() As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property CanSeek() As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property CanWrite() As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides Sub Flush()
                _sink.Flush()
            End Sub

            Public Overrides ReadOnly Property Length() As Long
                Get
                    Return 0
                End Get
            End Property

            Private _position As Long
            Public Overrides Property Position() As Long
                Get
                    Return _position
                End Get
                Set(value As Long)
                    _position = value
                End Set
            End Property

#End Region

#Region "Methods"

            Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
                Return _sink.Read(buffer, offset, count)
            End Function

            Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
                Return _sink.Seek(offset, origin)
            End Function

            Public Overrides Sub SetLength(value As Long)
                _sink.SetLength(value)
            End Sub

            Public Overrides Sub Close()
                _sink.Close()
            End Sub

            Public Overrides Sub Write(buffer__1 As Byte(), offset As Integer, count As Integer)
                Dim data As Byte() = New Byte(count - 1) {}
                Buffer.BlockCopy(buffer__1, offset, data, 0, count)
                Dim html As String = System.Text.Encoding.[Default].GetString(buffer__1)

                html = reg.Replace(html, String.Empty)

                Dim outdata As Byte() = System.Text.Encoding.[Default].GetBytes(html)
                _sink.Write(outdata, 0, outdata.GetLength(0))
            End Sub

#End Region

        End Class

#End Region



    End Class
End Namespace


