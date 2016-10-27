Imports Microsoft.VisualBasic
Imports umbraco.interfaces
Imports umbraco
Imports System.Web.UI.WebControls
Imports System.Web.UI

Namespace WSC.DataType.Children
        Public Class DataType
        Inherits umbraco.cms.businesslogic.datatype.AbstractDataEditor

            Private _Editor As IDataEditor
            Private _baseData As IData
            Private _prevalueeditor As IDataPrevalue

            Public Overrides ReadOnly Property Data As IData
                Get
                    If _baseData Is Nothing Then
                        _baseData = New cms.businesslogic.datatype.DefaultData(Me)
                    End If
                    Return _baseData
                End Get
            End Property

            Public Overrides ReadOnly Property DataEditor As IDataEditor
                Get
                    If _Editor Is Nothing Then
                        _Editor = New DataEditor(Data)
                    End If
                    Return _Editor
                End Get
            End Property

            Public Overrides ReadOnly Property DataTypeName As String
                Get
                Return "Children as list"
                End Get
            End Property

            Public Overrides ReadOnly Property Id As Guid
                Get
                Return New Guid("02fe6ae2-47a5-44f5-92f7-cd29a5b6329a")
                End Get
            End Property

            Public Overrides ReadOnly Property PrevalueEditor As IDataPrevalue
                Get
                    If _prevalueeditor Is Nothing Then
                        _prevalueeditor = New editorControls.DefaultPrevalueEditor(Me, False)
                    End If
                    Return _prevalueeditor
                End Get
            End Property

        End Class

    <ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Css, "//cdnjs.cloudflare.com/ajax/libs/jquery.tablesorter/2.17.0/css/theme.ice.css")>
    <ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Javascript, "//cdnjs.cloudflare.com/ajax/libs/jquery.tablesorter/2.17.0/jquery.tablesorter.min.js")>
    Public Class DataEditor
        Inherits PlaceHolder
        Implements IDataEditor

        Public ReadOnly Property Editor As Control Implements IDataEditor.Editor
            Get
                Return Me
            End Get
        End Property

        Public ReadOnly Property ShowLabel As Boolean Implements IDataEditor.ShowLabel
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property TreatAsRichTextEditor As Boolean Implements IDataEditor.TreatAsRichTextEditor
            Get
                Return False
            End Get
        End Property

        Public Sub New(data As interfaces.IData)
        End Sub

        Protected Overrides Sub OnInit(e As EventArgs)
            MyBase.OnInit(e)
        End Sub

        Protected Overrides Sub OnLoad(e As EventArgs)
            MyBase.OnLoad(e)
            Dim u = umbraco.helper.GetCurrentUmbracoUser()

            Dim d = Nothing
            If Not Page.IsPostBack Then
                Dim sb As New StringBuilder
                Dim parentID = MyBase.Page.Request("id")
                Dim p = umbraco.Core.ApplicationContext.Current.Services.ContentService.GetById(parentID)
                Dim children = umbraco.Core.ApplicationContext.Current.Services.ContentService.GetChildren(parentID)

                If children.Count > 0 Then
                    sb.AppendFormat("<table class=""tablesorter tablesorter-ice"" id=""{0}"">", Me.ClientID)
                    sb.Append("<thead>")
                    sb.Append("<tr>")
                    sb.Append("<th>Name</th>")
                    sb.Append("<th>Create Date</th>")
                    sb.Append("<th>Last Update</th>")
                    sb.Append("<th>Published</th>")
                    sb.Append("<th>")
                    If (u.GetPermissions(p.Path).Contains("C")) Then
                        sb.Append("<a href=""#"" class=""add"">Add New</a>")
                    End If
                    sb.Append("</th>")
                    sb.Append("</tr>")
                    sb.Append("</thead>")
                    sb.Append("<tbody>")

                    For Each c In children
                        sb.Append("<tr>")
                        sb.AppendFormat("<td><a href=""/umbraco/editContent.aspx?id={1}"">{0}</a></td>", c.Name, c.Id)
                        sb.AppendFormat("<td>{0}</td>", c.CreateDate)
                        sb.AppendFormat("<td>{0}</td>", c.UpdateDate)
                        sb.AppendFormat("<td><input type=""checkbox"" disabled=""disabled"" {0}/></td>", If(c.Published, "checked=""checked""", String.Empty))
                        If (u.GetPermissions(c.Path).Contains("D")) Then
                            sb.AppendFormat("<td><a href=""#"" class=""delete"" data-id=""{0}"">delete</a></td>", c.Id)
                        End If
                        sb.Append("</tr>")
                    Next
                    sb.Append("</tbody>")
                    sb.Append("</table>")
					sb.AppendLine("<style>")
					sb.AppendFormat("#{0} th, #{0} td {{ width: auto; }}", Me.ClientID)
					sb.AppendLine("</style>")
                    sb.AppendLine("<script>")
                    sb.AppendLine("$(function(){")
                    sb.AppendLine(" var p = UmbClientMgr.mainTree().findNode(" & parentID & ", false);")
                    sb.AppendLine(" $('#" & Me.ClientID & "').tablesorter({")
                    sb.AppendLine("      sortList:[[0,0]]")
                    sb.AppendLine("     ,headers: { 4:{sorter:false} }")
                    sb.AppendLine("     ,textExtraction:{3:function(node, table, cellIndex){ return $(node).find('input').is(':checked'); }}")
                    sb.AppendLine(" }).on('click', 'a.delete', function(e){ e.preventDefault(); var id = $(this).data('id');")
                    sb.AppendLine("     var tr = $(this).closest('tr');")
                    sb.AppendLine("     UmbClientMgr.mainTree().onBeforeContext(p);")
                    sb.AppendLine("     UmbClientMgr.mainTree()._loadChildNodes(UmbClientMgr.mainTree().getActionNode().jsNode, function(){")
                    sb.AppendLine("         var n = UmbClientMgr.mainTree().findNode(id, false);")
                    sb.AppendLine("         if (n){")
                    sb.AppendLine("             UmbClientMgr.mainTree().onBeforeContext(n);")
                    sb.AppendLine("             n = UmbClientMgr.mainTree().getActionNode();")
                    sb.AppendLine("             if (confirm(UmbClientMgr.uiKeys()['defaultdialogs_confirmdelete'] + ' ""' + n.nodeName + '""?\n\n')) {")
                    sb.AppendLine("                 jQuery(window.top).trigger('nodeDeleting', []);")
                    sb.AppendLine("                 top.umbraco.presentation.webservices.legacyAjaxCalls.Delete(")
                    sb.AppendLine("                      n.nodeId")
                    sb.AppendLine("                     ,n.nodeName")
                    sb.AppendLine("                     ,n.nodeType")
                    sb.AppendLine("                     ,function(){jQuery(window.top).trigger('nodeDeleted', []); UmbClientMgr.mainTree().onBeforeContext(p); UmbClientMgr.mainTree()._loadChildNodes(UmbClientMgr.mainTree().getActionNode().jsNode, null); tr.remove();}")
                    sb.AppendLine("                     ,function(error) { jQuery(window.top).trigger('publicError', [error]); }")
                    sb.AppendLine("                 )")
                    sb.AppendLine("             }") '--End If(confirm)
                    sb.AppendLine("         }") '--End If(n)
                    sb.AppendLine("     });")
                    sb.AppendLine(" }).on('click', 'a.add', function(e){ e.preventDefault();")
                    sb.AppendLine("     UmbClientMgr.mainTree().onBeforeContext(p);")
                    sb.AppendLine("     top.UmbClientMgr.appActions().actionNew();")
                    sb.AppendLine(" })")
                    sb.AppendLine("});")
                    sb.AppendLine("</script>")
                End If

                Me.Controls.Add(New LiteralControl(sb.ToString))
            End If
        End Sub

        Public Sub Save() Implements IDataEditor.Save
        End Sub
    End Class

End Namespace


