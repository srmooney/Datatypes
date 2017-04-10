Imports System.IO
Imports System.Security.Cryptography

Namespace WSC.Modules.Minifier
    ''' <summary>
    ''' <appSettings>
    '''     <add key="WSC.Minifier.Js" value="false" />
    '''     <add key="WSC.Minifier.Css" value="false" />
    ''' </appSettings>
    ''' <system.webServer>
    '''     <modules>
    '''         <add name="WSC.Modules.Minifier.MinifierModule" type="WSC.Modules.Minifier.MinifierModule" />
    '''     </modules>
    ''' </system.webServer>
    ''' <location path="inc">
    '''     <system.webServer>
    '''         <urlCompression doStaticCompression="false" doDynamicCompression="true" dynamicCompressionBeforeCache="false" />
    '''     </system.webServer>
    ''' </location>
    ''' </summary>
    ''' <remarks>Requires ClientDependency Framework for Minifing</remarks>
    Public Class MinifierModule
        Implements IHttpModule

        Private context As HttpApplication

#Region "Properties"
        Public ReadOnly Property CanMinifyJS As Boolean
            Get
                Dim configValue = ConfigurationManager.AppSettings("WSC.Minifier.Js")
                Return (String.IsNullOrEmpty(configValue) OrElse configValue.ToLower() <> "false")
            End Get
        End Property
        Public ReadOnly Property CanMinifyCSS As Boolean
            Get
                Dim configValue = ConfigurationManager.AppSettings("WSC.Minifier.Css")
                Return (String.IsNullOrEmpty(configValue) OrElse configValue.ToLower() <> "false")
            End Get
        End Property

        Public ReadOnly Property CanAddFilter As Boolean
            Get
				Dim rawUrl = context.Request.RawUrl.ToLower
                If rawUrl.Contains(".axd") Then Return False
                If rawUrl.Contains("/umbraco/") Then Return False
				If rawUrl.Contains("/app_plugins/") Then Return False
                If Not ShouldMinifyJS AndAlso Not ShouldMinifyCSS Then Return False
                Return True
            End Get
        End Property

        Public ReadOnly Property ShouldMinifyJS As Boolean
            Get
                Return Me.context.Response.ContentType.ToLower().Contains("javascript") AndAlso CanMinifyJS()
            End Get
        End Property

        Public ReadOnly Property ShouldMinifyCSS As Boolean
            Get
                Return Me.context.Response.ContentType.ToLower().Contains("text/css") AndAlso CanMinifyCSS()
            End Get
        End Property
#End Region


        Public Sub Dispose() Implements IHttpModule.Dispose
            Throw New NotImplementedException()
        End Sub

        Public Sub Init(context As HttpApplication) Implements IHttpModule.Init
            Me.context = context
            AddHandler context.PostReleaseRequestState, AddressOf context_PostReleaseRequestState
        End Sub

        ''' <summary>
        ''' Hook up the compressors.
        ''' </summary>
        ''' <param name="sender">The source of the event.</param>
        ''' <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        Private Sub context_PostReleaseRequestState(sender As Object, e As EventArgs)
            If context.Context.IsDebuggingEnabled Then Exit Sub

            Dim rawURL = context.Request.RawUrl
            Dim isImage = (rawURL.Contains("/elements/") Or rawURL.Contains("/media/"))

            If isImage Or CanAddFilter Then
                context.Response.Expires = 60 * 24 * 7.5 '--just over a week
                context.Response.Cache.SetCacheability(HttpCacheability.Public)
            End If


            '--Add Filters
            If Not CanAddFilter Then Exit Sub

            '--Is this a request for javascript file?
            If ShouldMinifyJS Then
                Me.context.Response.Filter = New JsStream(Me.context.Response.Filter, Me.context)
            End If

            '--Is this a request for css file?
            If ShouldMinifyCSS Then
                Me.context.Response.Filter = New CssStream(Me.context.Response.Filter, Me.context)
            End If

        End Sub
    End Class


    Public Class CssStream
        Inherits StreamBase
        Public Sub New(input As Stream, context As HttpApplication)
            MyBase.New(input, AddressOf MinifyCss, context)
        End Sub

        Public Shared Function MinifyCss(css As String) As String
            Try
                Return ClientDependency.Core.CompositeFiles.CssMin.CompressCSS(css)
            Catch ex As Exception
                umbraco.Core.Logging.LogHelper.Error(Of MinifierModule)(css, ex)
            End Try
            Return css
        End Function
    End Class

    Public Class JsStream
        Inherits StreamBase

        Public Sub New(input As Stream, context As HttpApplication)
            MyBase.New(input, AddressOf MinifyJs, context)
        End Sub

        Public Shared Function MinifyJs(js As String) As String
            Try
                Return ClientDependency.Core.CompositeFiles.JSMin.CompressJS(js)
            Catch ex As Exception
                umbraco.Core.Logging.LogHelper.Error(Of MinifierModule)(js, ex)
            End Try
            Return js
        End Function
    End Class


    Public MustInherit Class StreamBase
        Inherits Stream
        Protected inputStream As Stream
        Private ResponseContent As StringBuilder
        Private context As HttpApplication

        ''' <summary>
        ''' This function will perform the actual minification.
        ''' The argument to a function will be a content of a response stream, converted to string.
        ''' </summary>
        Protected MinifyContent As Func(Of String, String) = Nothing

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

        Public Overrides ReadOnly Property Length() As Long
            Get
                Return Me.inputStream.Length
            End Get
        End Property

        Public Overrides Property Position() As Long
            Get
                Return Me.inputStream.Position
            End Get
            Set
                Me.inputStream.Position = Value
            End Set
        End Property

        Public NotOverridable Overrides Sub Flush()
            Me.inputStream.Flush()
        End Sub

        Public Overrides Function Read(buffer As Byte(), offset As Integer, count As Integer) As Integer
            Return Me.inputStream.Read(buffer, offset, count)
        End Function

        Public Overrides Function Seek(offset As Long, origin As SeekOrigin) As Long
            Return Me.inputStream.Seek(offset, origin)
        End Function

        Public Overrides Sub SetLength(value As Long)
            Me.inputStream.SetLength(value)
        End Sub




        Public Overrides Sub Close()
            UpdateContent()
            Me.inputStream.Close()
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StreamBase"/> class.
        ''' </summary>
        ''' <param name="inputStream">The input stream of content that will be manipulated.</param>
        ''' <param name="minifyAction">The minify method that will be used to compress the content. The argument to the function is a response content, converted to string.</param>
        Public Sub New(inputStream As Stream, minifyAction As Func(Of String, String), context As HttpApplication)
            If inputStream Is Nothing Then
                Throw New ArgumentNullException("inputStream")
            End If
            If minifyAction Is Nothing Then
                Throw New ArgumentNullException("minufyAction")
            End If

            Me.inputStream = inputStream
            Me.MinifyContent = minifyAction
            Me.ResponseContent = New StringBuilder()
            Me.context = context
        End Sub

        Public NotOverridable Overrides Sub Write(buffer__1 As Byte(), offset As Integer, count As Integer)
            Dim data As Byte() = New Byte(count - 1) {}
            Buffer.BlockCopy(buffer__1, offset, data, 0, count)
            ResponseContent.Append(System.Text.Encoding.UTF8.GetString(buffer__1))
        End Sub

        Public Sub UpdateContent()
            Dim Content = Me.ResponseContent.ToString
            Dim minified = Content
            'umbraco.Core.Logging.LogHelper.Info(Of StreamBase)("Header: " & HttpContext.Current.Request.Headers("Accept-Encoding"))
            'umbraco.Core.Logging.LogHelper.Info(Of StreamBase)("Inputstream: " & Me.inputStream.GetType.ToString())
            Dim hash = ComputeMD5Hash(Content)
            Dim cached = Cache.Check(hash)
            If Not String.IsNullOrEmpty(cached) Then
                minified = cached
            Else
                minified = Me.MinifyContent(Content)
                Cache.Update(hash, minified)
            End If

            Dim minifiedBytes = System.Text.Encoding.UTF8.GetBytes(minified)
            inputStream.Write(minifiedBytes, 0, minifiedBytes.GetLength(0))
            context.Response.Cache.SetETag(hash)
            'context.Response.Cache.SetCacheability(HttpCacheability.Public)
            'context.Response.Expires = 60 * 24 * 30 '--just under a month
            'context.Response.Headers.Add("Connection", "keep-alive")
        End Sub

        Private Shared Function ComputeMD5Hash(input As String) As String
            If input Is Nothing Then
                Throw New ArgumentNullException("input")
            End If

            Using hasher As MD5 = MD5.Create()
                Dim dbytes As Byte() = hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input))
                Dim sBuilder As New StringBuilder()
                For n As Integer = 0 To dbytes.Length - 1
                    sBuilder.Append(dbytes(n).ToString("X2"))
                Next n
                Return sBuilder.ToString()
            End Using
        End Function
    End Class

    Public Class Cache
        Private Shared baseDirectory As String = "~/App_Data/TEMP/WSCMinifier/"

        Private Shared Sub EnsureDirectory()
            If Not Directory.Exists(HttpContext.Current.Server.MapPath(baseDirectory)) Then
                Directory.CreateDirectory(HttpContext.Current.Server.MapPath(baseDirectory))
            End If
        End Sub

        Public Shared Sub Update(key As String, input As String)
            EnsureDirectory()
            Dim serverPath = Path.Combine(HttpContext.Current.Server.MapPath(baseDirectory), key & ".txt")
            If File.Exists(serverPath) Then
                File.Delete(serverPath)
            End If
            File.WriteAllText(serverPath, input)
        End Sub

        Public Shared Function Check(key As String) As String
            EnsureDirectory()
            Dim serverPath = Path.Combine(HttpContext.Current.Server.MapPath(baseDirectory), key & ".txt")

            If File.Exists(serverPath) Then
                Return File.ReadAllText(serverPath)
            Else
                Return String.Empty
            End If
        End Function
    End Class

End Namespace

