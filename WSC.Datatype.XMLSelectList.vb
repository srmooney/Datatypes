Imports Microsoft.VisualBasic
Imports umbraco
Imports umbraco.editorControls.PrevalueEditorExtensions
Imports umbraco.interfaces


Namespace WSC.DataType.XMLSelectList
    Public Class Options
        Inherits editorControls.AbstractOptions

        Public Sub New()
            MyBase.New()
        End Sub

        Public Sub New(loadDefaults As Boolean)
            MyBase.New(loadDefaults)
        End Sub

        <ComponentModel.DefaultValue("")>
        Public Property XmlFilePath() As String

        <ComponentModel.DefaultValue("")>
        Public Property XPathExpression() As String

        <ComponentModel.DefaultValue("")>
        Public Property TextColumn() As String

        <ComponentModel.DefaultValue("")>
        Public Property ValueColumn() As String

        <ComponentModel.DefaultValue("dropdown")>
        Public Property Type() As String
    End Class

    Public Class PrevalEditor
        Inherits editorControls.AbstractJsonPrevalueEditor

        Private xmlFilePath As editorControls.SettingControls.Pickers.PathPicker
        Private txtXPathExpression As TextBox
        Private txtTextColumn As TextBox
        Private txtValueColumn As TextBox
        Private ddlType As DropDownList

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
            Dim options = New Options() With {
                .XmlFilePath = xmlFilePath.Value,
                .XPathExpression = txtXPathExpression.Text,
                .TextColumn = txtTextColumn.Text,
                .ValueColumn = txtValueColumn.Text,
                .Type = ddlType.SelectedValue
            }
            '--Save the options as JSON
            Me.SaveAsJson(options)
        End Sub

        Protected Overrides Sub OnInit(e As EventArgs)
            MyBase.OnInit(e)
            Me.EnsureChildControls()
        End Sub

        Protected Overrides Sub CreateChildControls()
            MyBase.CreateChildControls()

            Me.xmlFilePath = New editorControls.SettingControls.Pickers.PathPicker With {.ID = "xmlFilePath"}
            Me.txtXPathExpression = New TextBox() With {.ID = "txtXPathExpression", .CssClass = "guiInputText guiInputStandardSize"}
            Me.txtTextColumn = New TextBox() With {.ID = "txtTextColumn", .CssClass = "guiInputText guiInputStandardSize"}
            Me.txtValueColumn = New TextBox() With {.ID = "txtValueColumn", .CssClass = "guiInputText guiInputStandardSize"}
            Me.ddlType = New DropDownList() With {.ID = "ddlType", .CssClass = "guiInput"}


            Me.Controls.AddPrevalueControls(Me.xmlFilePath, Me.txtXPathExpression, Me.txtTextColumn, Me.txtValueColumn, Me.ddlType)
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)

            If Not Page.IsPostBack Then
                Me.ddlType.Items.Add(New ListItem("Checkbox", "checkbox"))
                Me.ddlType.Items.Add(New ListItem("Dropdown", "dropdown"))
            End If

            Me.xmlFilePath.Value = Me.Options.XmlFilePath
            Me.txtXPathExpression.Text = Me.Options.XPathExpression
            Me.txtTextColumn.Text = Me.Options.TextColumn
            Me.txtValueColumn.Text = Me.Options.ValueColumn
            Try
                Me.ddlType.SelectedValue = Me.Options.Type
            Catch ex As Exception
            End Try
        End Sub

        Protected Overrides Sub RenderContents(writer As HtmlTextWriter)
            'writer.AddPrevalueRow("ID", New LiteralControl(Me.m_DataType.DataTypeDefinitionId))
            writer.AddPrevalueRow("XML File Path:", "Specify the path to the XML file.", Me.xmlFilePath)
            writer.AddPrevalueRow("XPath Expression:", "The XPath expression to select the nodes used in the XML file. $SiteID is available", Me.txtXPathExpression)
            writer.AddPrevalueRow("Text column:", "The name of the field used for the item's display text.", Me.txtTextColumn)
            writer.AddPrevalueRow("Value column:", "The name of the field used for the item's value.", Me.txtValueColumn)
            writer.AddPrevalueRow("Type:", "The type of the field used.", Me.ddlType)
        End Sub
    End Class


    <ValidationProperty("IsValid")>
    Public Class DataEditor
        Inherits CompositeControl
        Implements IDataEditor

        Private data As IData
        Private Options As Options
        Private cblValue As CheckBoxList
        Private ddlValue As DropDownList

        ''' <summary>
        ''' Initializes a new instance of SqlAutoCompleteDataEditor
        ''' </summary>
        ''' <param name="data"></param>
        ''' <param name="options"></param>
        Friend Sub New(data As IData, options As Options)
            Me.data = data
            Me.Options = options
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

            If Me.Options.Type = "checkbox" Then
                Me.cblValue = New CheckBoxList With {.ID = "cblValue", .CssClass = "umbEditorTextField", .RepeatColumns = 2}
                Me.Controls.Add(Me.cblValue)
            Else
                Me.ddlValue = New DropDownList With {.ID = "ddlValue", .CssClass = "umbEditorTextField"}
                Me.Controls.Add(Me.ddlValue)
            End If


        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Me.EnsureChildControls()

            If Not Me.Page.IsPostBack Then
                Dim path = IO.IOHelper.MapPath(Me.Options.XmlFilePath)
                If Me.Options.XPathExpression.Contains("$SiteID") Then
                    Dim id = HttpContext.Current.Request("id")
                    Dim doc = umbraco.Core.ApplicationContext.Current.Services.ContentService.GetById(id)
                    doc = umbraco.Core.ApplicationContext.Current.Services.ContentService.GetById(doc.Path.Split(",")(1))
                    'While doc.Level > 1
                    '    doc = umbraco.Core.ApplicationContext.Current.Services.ContentService.GetById(doc.ParentId)
                    'End While
                    'Me.Controls.Add(New LiteralControl("[[" & doc.Id & "]]"))
                    'Me.Controls.Add(New LiteralControl("[[" & Me.Options.XPathExpression.Replace("$SiteID", doc.Id) & "]]"))
                    Me.Options.XPathExpression = Me.Options.XPathExpression.Replace("$SiteID", doc.Id)
                End If

                If System.IO.File.Exists(path) Then
                    Dim xmlDS As New XmlDataSource()
                    xmlDS.DataFile = path
                    xmlDS.XPath = Me.Options.XPathExpression

                    If Me.Options.Type = "checkbox" Then
                        Me.cblValue.DataSource = xmlDS
                        Me.cblValue.DataTextField = Me.Options.TextColumn
                        Me.cblValue.DataValueField = Me.Options.ValueColumn
                        Me.cblValue.DataBind()
                        If Not String.IsNullOrEmpty(Me.data.Value) Then
                            Dim values As New List(Of String)
                            values.AddRange(Me.data.Value.ToString.Split(","))
                            If values.Count > 0 Then
                                For Each li As ListItem In Me.cblValue.Items
                                    li.Selected = values.Contains(li.Value)
                                Next
                            End If
                        End If
                    Else
                        Me.ddlValue.DataSource = xmlDS
                        Me.ddlValue.DataTextField = Me.Options.TextColumn
                        Me.ddlValue.DataValueField = Me.Options.ValueColumn
                        Me.ddlValue.DataBind()
                        If Not String.IsNullOrEmpty(Me.data.Value) Then
                            Try
                                Me.ddlValue.SelectedValue = Me.data.Value
                            Catch ex As Exception
                            End Try
                        End If
                    End If

                End If
            End If
        End Sub

        Public Sub Save() Implements IDataEditor.Save
            Me.data.Value = GetValues()
        End Sub

        Private Function GetValues() As String
            If Me.Options.Type = "checkbox" Then
                Dim values As New List(Of String)
                For Each li As ListItem In Me.cblValue.Items
                    If li.Selected Then values.Add(li.Value)
                Next
                Return String.Join(",", values.ToArray)
            Else
                Return Me.ddlValue.SelectedValue
            End If
        End Function

        Public ReadOnly Property IsValid As String
            Get
                Return GetValues()
            End Get
        End Property


    End Class

    Public Class DataType
        Inherits umbraco.cms.businesslogic.datatype.BaseDataType
        Implements IDataType

        Public Const Identifier As String = "5555f929-4c9b-4c1e-82e7-edca1831f35c"

        Private m_PrevalEditor As XMLSelectList.PrevalEditor
        Private m_DataEditor As XMLSelectList.DataEditor
        Private m_Data As IData
        Private m_Options As XMLSelectList.Options

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
                Return "XMLSelectList"
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