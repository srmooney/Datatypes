Imports Microsoft.VisualBasic
Imports umbraco.interfaces
Imports umbraco
Imports System.Web.UI.WebControls
Imports System.Web.UI
Imports umbraco.editorControls.PrevalueEditorExtensions

Namespace WSC.DataType
    Namespace Autocomplete

        <umbraco.Web.BaseRest.RestExtension(DataType.Identifier)>
        Public Class Base
            Private Shared Function GetOptions(datatypeDefinitionId As Integer) As Options
                Return DirectCast(HttpContext.Current.Cache(String.Concat(DataType.Identifier, "_options_", datatypeDefinitionId)), Options)
            End Function

            <umbraco.Web.BaseRest.RestExtensionMethod(ReturnXML:=False)>
            Public Shared Function GetData(datatypeDefinitionID As Integer) As String
                Dim autocompleteText = HttpContext.Current.Request("autocompleteText")
                Dim ret As New List(Of String)

                Dim options = GetOptions(datatypeDefinitionID)

                If (options IsNot Nothing AndAlso autocompleteText.Length >= options.MinLength) Then
                    If options.SQL.Contains("%@autoCompleteText%") Then
                        options.SQL = options.SQL.Replace("'%@autoCompleteText%'", "@0")
                        autocompleteText = "%" & autocompleteText & "%"
                    End If
                    Dim db = umbraco.Core.ApplicationContext.Current.DatabaseContext.Database
                    Dim strSQL = Core.Persistence.Sql.Builder.Append(options.SQL, autocompleteText)

                    ret = db.Fetch(Of String)(strSQL)
                    'ret.Add(db.LastCommand)
                End If

                Return Newtonsoft.Json.JsonConvert.SerializeObject(ret.ToArray).ToString
            End Function

        End Class

        Public Class Options
            Inherits editorControls.AbstractOptions

            Public Sub New()
                MyBase.New()
            End Sub

            Public Sub New(loadDefaults As Boolean)
                MyBase.New(loadDefaults)
            End Sub

            <ComponentModel.DefaultValue("")>
            Public Property SQL() As String

            <ComponentModel.DefaultValue(3)>
            Public Property MinLength() As Integer
        End Class

        Public Class PrevalEditor
            Inherits editorControls.AbstractJsonPrevalueEditor

            Private txtSQL As New TextBox()
            Private ddlMinLength As New DropDownList()

            Private m_Options As Options
            ''' <summary>
            ''' Gets the options data object that represents the current state of this datatypes configuration
            ''' </summary>
            Friend ReadOnly Property Options() As Options
                Get
                    If Me.m_Options Is Nothing Then
                        '--Deserialize any stored settings for this PreValueEditor instance
                        Me.m_Options = Me.GetPreValueOptions(Of Options)()
                        '--If still null, ie, object couldn't be de-serialized from PreValue[0] string value
                        If Me.m_Options Is Nothing Then
                            '--Create a new Options data object with the default values
                            Me.m_Options = New Options(True)
                        End If
                    End If

                    Return Me.m_Options
                End Get
            End Property

            Public Sub New(dataType As umbraco.cms.businesslogic.datatype.BaseDataType)
                MyBase.New(dataType)
            End Sub

            Public Overrides Sub Save()
                '--Set the database data-type
                Me.m_DataType.DBType = cms.businesslogic.datatype.DBTypes.Nvarchar
                '--Set the options
                Dim options = New Options() With {.MinLength = ddlMinLength.SelectedValue, .SQL = txtSQL.Text}
                '--Save the options as JSON
                Me.SaveAsJson(options)
                '--Cache settings
                HttpContext.Current.Cache(String.Concat(DataType.Identifier, "_options_", Me.m_DataType.DataTypeDefinitionId)) = options
            End Sub

            Protected Overrides Sub OnInit(e As EventArgs)
                MyBase.OnInit(e)
                Me.EnsureChildControls()
            End Sub

            Protected Overrides Sub CreateChildControls()
                MyBase.CreateChildControls()

                Me.txtSQL.TextMode = TextBoxMode.MultiLine
                Me.txtSQL.ID = "txtSQL"
                Me.txtSQL.Columns = 60
                Me.txtSQL.Rows = 10

                Me.ddlMinLength.ID = "ddlMinLength"
                Me.ddlMinLength.Items.Add(New ListItem("1"))
                Me.ddlMinLength.Items.Add(New ListItem("2"))
                Me.ddlMinLength.Items.Add(New ListItem("3"))
                Me.ddlMinLength.Items.Add(New ListItem("4"))
                Me.ddlMinLength.Items.Add(New ListItem("5"))

                Me.Controls.AddPrevalueControls(Me.txtSQL, Me.ddlMinLength)
            End Sub

            Protected Overrides Sub OnLoad(e As EventArgs)
                MyBase.OnLoad(e)
                Me.txtSQL.Text = Me.Options.SQL
                Try
                    Me.ddlMinLength.SelectedValue = Me.Options.MinLength
                Catch ex As Exception
                End Try
            End Sub

            Protected Overrides Sub RenderContents(writer As HtmlTextWriter)
                writer.AddPrevalueRow("ID", New LiteralControl(Me.m_DataType.DataTypeDefinitionId))
                writer.AddPrevalueRow("Min Length", "number of chars in the autocomplete text box before querying for data", Me.ddlMinLength)
                writer.AddPrevalueRow("SQL Expression:", "expects a result set with one fields : 'Text' - can include the token : @autoCompleteText", Me.txtSQL)
                writer.AddPrevalueRow("Post Url", "post autoCompleteText variable to the above url", New LiteralControl("/Base/" & DataType.Identifier & "/GetData/" & Me.m_DataType.DataTypeDefinitionId & "/"))

            End Sub
        End Class

        <ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Javascript, "ui/jqueryui.js", "UmbracoClient")>
        <ValidationProperty("IsValid")>
        Public Class DataEditor
            Inherits CompositeControl
            Implements IDataEditor

            Private data As IData
            Private opts As Options
            Private txtValue As New TextBox()

            ''' <summary>
            ''' Initializes a new instance of SqlAutoCompleteDataEditor
            ''' </summary>
            ''' <param name="data"></param>
            ''' <param name="options"></param>
            Friend Sub New(data As IData, options As Options)
                Me.data = data
                Me.opts = options
            End Sub

            ''' <summary>
            ''' Gets the property / datatypedefinition id - used to identify the current instance
            ''' </summary>
            Private ReadOnly Property DataTypeDefinitionId() As Integer
                Get
                    Return DirectCast(Me.data, editorControls.XmlData).DataTypeDefinitionId
                End Get
            End Property


            Public ReadOnly Property Editor As Control Implements IDataEditor.Editor
                Get
                    Return Me
                End Get
            End Property

            Public ReadOnly Property ShowLabel As Boolean Implements IDataEditor.ShowLabel
                Get
                    Return True
                End Get
            End Property

            Public ReadOnly Property TreatAsRichTextEditor As Boolean Implements IDataEditor.TreatAsRichTextEditor
                Get
                    Return False
                End Get
            End Property

            Protected Overrides Sub CreateChildControls()
                MyBase.CreateChildControls()

                Me.txtValue.ID = "txtValue"
                Me.txtValue.CssClass = "umbEditorTextField"
                Me.Controls.Add(Me.txtValue)
            End Sub

            Protected Overrides Sub OnLoad(e As EventArgs)
                MyBase.OnLoad(e)
                Me.EnsureChildControls()

                Dim startupScript As New StringBuilder()
                startupScript.Append("$(function(){")
                startupScript.AppendFormat("$('#{0}').each(function(){{", Me.txtValue.ClientID)
                startupScript.Append("var input = $(this);")
                startupScript.Append("input.autocomplete({")
                startupScript.AppendFormat("    minLength: {0},", Me.opts.MinLength)
                startupScript.Append("    source: function (request, response) {")
                startupScript.Append("        jQuery.ajax({")
                startupScript.Append("            type: 'POST',")
                startupScript.Append("            data: { autoCompleteText: request.term },")
                startupScript.Append("            contentType: 'application/x-www-form-urlencoded; charset=utf-8',")
                startupScript.AppendFormat("            url: '/Base/{0}/GetData/{1}/',", DataType.Identifier, Me.DataTypeDefinitionId)
                startupScript.Append("            dataType: 'json',")
                startupScript.Append("            success: response")
                startupScript.Append("        });")
                startupScript.Append("    },")
                startupScript.Append("    open: function (event, ui) {")
                startupScript.Append("        input.autocomplete('widget').width(300); /* TODO: can we get at the input field from the event or ui params ? */")
                startupScript.Append("    },")
                startupScript.Append("    autoFocus: true,")
                startupScript.Append("    create: function (event, ui) {")
                startupScript.Append("        input.autocomplete('widget').addClass('sql-auto-complete-widget');")
                startupScript.Append("    }")
                startupScript.Append("});")
                startupScript.Append("});")
                startupScript.Append("});")

                Dim css As New StringBuilder()
                css.Append("<style>")
                css.Append(".ui-menu { list-style: none; /*padding: 2px; margin: 0;*/ }")
                css.Append("</style>")

                ScriptManager.RegisterStartupScript(Me, GetType(DataEditor), Me.ClientID & "_init", startupScript.ToString, True)
                ScriptManager.RegisterStartupScript(Me, GetType(DataEditor), Me.ClientID & "_css", css.ToString, False)

                If Not Me.Page.IsPostBack Then
                    Me.txtValue.Text = Me.data.Value.ToString
                End If

                '--Put the options obj into cache so that the /base method can request it (where the sql statment is being used)
                HttpContext.Current.Cache(String.Concat(DataType.Identifier, "_options_", Me.DataTypeDefinitionId)) = Me.opts
            End Sub

            Public Sub Save() Implements IDataEditor.Save
                Me.data.Value = txtValue.Text
            End Sub

            Public ReadOnly Property IsValid As String
                Get
                    Return Me.txtValue.Text
                End Get
            End Property


        End Class

        Public Class DataType
            Inherits umbraco.cms.businesslogic.datatype.BaseDataType
            Implements IDataType

            Public Const Identifier As String = "9cd0dc2e-8c62-401a-8114-964707b07f4f"

            Private m_PrevalEditor As Autocomplete.PrevalEditor
            Private m_DataEditor As Autocomplete.DataEditor
            Private m_Data As IData
            Private m_Options As Autocomplete.Options

            Private ReadOnly Property Options As Options
                Get
                    If Me.m_Options Is Nothing Then
                        Me.m_Options = DirectCast(Me.PrevalueEditor, PrevalEditor).Options
                    End If
                    Return Me.m_Options
                End Get
            End Property

            Public Overrides ReadOnly Property DataTypeName As String Implements IDataType.DataTypeName
                Get
                    Return "Autocomplete"
                End Get
            End Property

            Public Overrides ReadOnly Property Id As Guid Implements IDataType.Id
                Get
                    Return New Guid(Identifier)
                End Get
            End Property

            Public Overrides ReadOnly Property PrevalueEditor As IDataPrevalue Implements IDataType.PrevalueEditor
                Get
                    If (Me.m_PrevalEditor Is Nothing) Then
                        Me.m_PrevalEditor = New PrevalEditor(Me)
                    End If

                    Return Me.m_PrevalEditor
                End Get
            End Property

            Public Overrides ReadOnly Property DataEditor As IDataEditor Implements IDataType.DataEditor
                Get
                    If (Me.m_DataEditor Is Nothing) Then
                        Me.m_DataEditor = New DataEditor(Me.Data, Me.Options)
                    End If

                    Return Me.m_DataEditor
                End Get
            End Property

            Public Overrides ReadOnly Property Data As IData Implements IDataType.Data
                Get
                    If Me.m_Data Is Nothing Then
                        Me.m_Data = New editorControls.XmlData(Me)
                    End If
                    Return Me.m_Data
                End Get
            End Property

            Public Overloads Property DataTypeDefinitionId As Integer Implements IDataType.DataTypeDefinitionId
                Get
                    Return MyBase.DataTypeDefinitionId
                End Get
                Set(value As Integer)
                    MyBase.DataTypeDefinitionId = value
                End Set
            End Property
        End Class
    End Namespace
End Namespace
