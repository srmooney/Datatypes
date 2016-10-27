Imports Microsoft.VisualBasic
Imports System.Web.UI.WebControls
Imports Umbraco.interfaces
Imports System.Web.UI
Imports Umbraco.editorControls.PrevalueEditorExtensions
Imports System.Text
Imports System.Collections.Generic
Imports System
Imports System.Web
Imports Umbraco.NodeFactory
Imports Umbraco.NodeExtensions
Imports System.Linq
Imports System.Xml.XPath
Imports System.Xml

Namespace WSC.DataType.RecurringEvent

    <ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Javascript, "/umbraco/plugins/WSC.RecurringEvent/RecurringEvent.js")>
    <ClientDependency.Core.ClientDependency(ClientDependency.Core.ClientDependencyType.Css, "/umbraco/plugins/WSC.RecurringEvent/RecurringEvent.css")>
    Public Class DataEditor
        Inherits CompositeControl
        Implements IDataEditor

        Private data As IData
        Private ddlType As New DropDownList()
        Private txtAmount As New TextBox()
        Private cblDays As New CheckBoxList()
        Private rblRepeatMonth As New RadioButtonList()
        Private hdnExceptions As New HiddenField()


        Public ReadOnly Property Editor As System.Web.UI.Control Implements IDataEditor.Editor
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

        Public Sub New(data As IData)
            Me.data = data
        End Sub

        Public Sub Save() Implements IDataEditor.Save
        End Sub

        Protected Overrides Sub CreateChildControls()
            MyBase.CreateChildControls()

            Me.ddlType.ID = "ddlType"
            Me.ddlType.Items.Add(New ListItem("None"))
            Me.ddlType.Items.Add(New ListItem("Daily"))
            Me.ddlType.Items.Add(New ListItem("Weekly"))
            Me.ddlType.Items.Add(New ListItem("Monthly"))
            Me.ddlType.Items.Add(New ListItem("Yearly"))

            Me.txtAmount.ID = "txtAmount"

            Me.cblDays.ID = "cblDays"
            Me.cblDays.RepeatLayout = RepeatLayout.Flow
            Me.cblDays.RepeatDirection = RepeatDirection.Horizontal
            Me.cblDays.Items.Add(New ListItem("Su"))
            Me.cblDays.Items.Add(New ListItem("Mo"))
            Me.cblDays.Items.Add(New ListItem("Tu"))
            Me.cblDays.Items.Add(New ListItem("We"))
            Me.cblDays.Items.Add(New ListItem("Th"))
            Me.cblDays.Items.Add(New ListItem("Fr"))
            Me.cblDays.Items.Add(New ListItem("Sa"))

            Me.rblRepeatMonth.ID = "ddlRepeatMonth"
            Me.rblRepeatMonth.RepeatLayout = RepeatLayout.Flow
            Me.rblRepeatMonth.Items.Add(New ListItem("Day of month (Date)", "Day of month"))
            Me.rblRepeatMonth.Items.Add(New ListItem("Day of week (Day)", "Day of week"))
            Me.rblRepeatMonth.SelectedIndex = 0

            Me.hdnExceptions.ID = "hdnExceptions"

            Me.Controls.AddPrevalueControls(Me.ddlType, Me.txtAmount, Me.cblDays, Me.rblRepeatMonth, Me.hdnExceptions)
        End Sub

        Protected Overrides Sub OnLoad(e As System.EventArgs)
            MyBase.OnLoad(e)
            Me.EnsureChildControls()

            Dim d As RecurringEvent.Data = Nothing
            If Page.IsPostBack Then
                Me.data.Value = Nothing

                If Me.ddlType.SelectedValue <> "None" Then
                    d = New Data()
                    d.Type = Me.ddlType.SelectedValue
                    For Each li As ListItem In Me.cblDays.Items
                        If li.Selected Then
                            d.WeekDays.Add(li.Value)
                        End If
                    Next
                    If Me.ddlType.SelectedValue = "Monthly" Then
                        d.MonthlyOn = Me.rblRepeatMonth.SelectedValue
                    End If
                    d.Exceptions.AddRange(Me.hdnExceptions.Value.Split(","))
                    Me.data.Value = d.Serialize()
                End If

            Else
                d = RecurringEvent.Data.Deserialize(Me.data.Value)
                If d IsNot Nothing Then
                    '--Populate controls
                    Me.ddlType.SelectedValue = d.Type
                    For Each li As ListItem In Me.cblDays.Items
                        Try
                            li.Selected = d.WeekDays.Contains(li.Value)
                        Catch ex As Exception

                        End Try
                    Next

                    If d.Type = "Monthly" Then
                        Me.rblRepeatMonth.SelectedValue = d.MonthlyOn
                    End If

                    Me.hdnExceptions.Value = String.Join(",", d.Exceptions.ToArray())
                End If
            End If



            Dim startupScript As New StringBuilder()
            startupScript.Append("$(function(){")
            startupScript.AppendFormat("$('#{0}').WSCRecurringEvent();", Me.ClientID)
            startupScript.Append("});")

            ScriptManager.RegisterStartupScript(Me, GetType(DataEditor), Me.ClientID & "_init", startupScript.ToString, True)
        End Sub

        Protected Overrides Sub RenderContents(writer As HtmlTextWriter)

            writer.AddPrevalueRow(String.Empty, String.Empty, Me.ddlType)
            writer.Write("<div class=""parameter"">")

            writer.AddAttribute("data-parameter", "weekly")
            writer.AddPrevalueRow("Repeat weekly on the following days:", Me.cblDays)

            writer.AddAttribute("data-parameter", "monthly")
            writer.AddPrevalueRow("Repat Monthly on:", Me.rblRepeatMonth)

            writer.AddAttribute("data-parameter", "daily weekly monthly yearly")
            writer.AddPrevalueRow("With the following exceptions:", New LiteralControl("<div class=""datepicker""></div>"), Me.hdnExceptions)

            'writer.AddPrevalueRow("Test Only:", New LiteralControl("<textarea>" & Me.data.Value & "</textarea>"))


            writer.Write("</div>")
        End Sub

        Public Overrides Sub RenderBeginTag(writer As HtmlTextWriter)
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "wsc-recurring-event")
            MyBase.RenderBeginTag(writer)
        End Sub
    End Class


    Public Class DataType
        Inherits Umbraco.cms.businesslogic.datatype.AbstractDataEditor

        Private Editor As IDataEditor

        Public Overrides ReadOnly Property DataTypeName As String
            Get
                Return "RecurringEvent"
            End Get
        End Property

        Public Overrides ReadOnly Property Id As System.Guid
            Get
                Return New System.Guid("48163e89-e1ae-40f6-848e-b563ae627d15")
            End Get
        End Property

        Public Overrides ReadOnly Property DataEditor As IDataEditor
            Get
                If Me.Editor Is Nothing Then
                    Me.Editor = New DataEditor(Data)
                End If
                Return Me.Editor
            End Get
        End Property
    End Class

    Public Class Data
        Property Type As String = String.Empty
        Property WeekDays As New List(Of String)
        Property MonthlyOn As String = String.Empty
        Property Exceptions As New List(Of String)

        <Newtonsoft.Json.JsonIgnore()>
        ReadOnly Property ExceptionDates As List(Of Date)
            Get
                Dim ret As New List(Of Date)
                For Each d As String In Exceptions
                    ret.Add(Date.ParseExact(d, "yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture))
                Next
                Return ret
            End Get
        End Property

        Sub New()
        End Sub

        Sub New(json As String)
            If Not String.IsNullOrEmpty(json) Then
                Dim d As Data = Deserialize(json)
                If d IsNot Nothing Then
                    Me.Exceptions = d.Exceptions
                    Me.Type = d.Type
                    Me.WeekDays = d.WeekDays
                    Me.MonthlyOn = d.MonthlyOn
                    Me.Exceptions = d.Exceptions
                End If
            End If
        End Sub

        Public Function Serialize() As String
            Return Newtonsoft.Json.JsonConvert.SerializeObject(Me)
        End Function

        Public Shared Function Deserialize(serializedState As String) As Data
            If String.IsNullOrEmpty(serializedState) Then Return Nothing

            Dim ret As New Data()
            Try
                ret = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Data)(serializedState)
            Catch ex As System.Exception
            End Try

            Return ret
        End Function

        Public Function HasEvent(startDate As Date, endDate As Date, testDate As Date) As Boolean
            If String.IsNullOrEmpty(Me.Type) OrElse Me.Type = "None" Then Return False

            If testDate < startDate OrElse testDate > endDate Then Return False

            If Me.Exceptions.Contains(testDate.ToString("yyyy-MM-dd")) Then Return False

            If Me.Type = "Daily" Then Return True

            Dim daysOfWeek As New List(Of String) From {"Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"}
            Dim dayOfWeek As String = daysOfWeek(testDate.DayOfWeek)

            If Me.Type = "Weekly" AndAlso Me.WeekDays.Contains(dayOfWeek) Then Return True

            Dim startDOW As String = daysOfWeek(startDate.DayOfWeek)
            If Me.Type = "Monthly" AndAlso Me.MonthlyOn = "Day of week" AndAlso dayOfWeek = startDOW Then Return True
            If Me.Type = "Monthly" AndAlso Me.MonthlyOn = "Day of month" AndAlso startDate.Day = testDate.Day Then Return True

            If Me.Type = "Yearly" AndAlso startDate.Month = testDate.Month AndAlso startDate.Day = testDate.Day Then Return True

            Return False
        End Function


    End Class

    <Umbraco.XsltExtension("wsc.library.recurringevent")>
    Public Class Library
        Public Shared Function GetNodesWithEvent(parentId As Integer, propertyAlias As String) As XPathNodeIterator
            Return XmlNodesToXML(Query.GetXmlNodesWithEvent(New Node(parentId), propertyAlias, Today, Today.AddDays(365), 0))
        End Function

        Public Shared Function GetNodesWithEvent(parentId As Integer, propertyAlias As String, maxNodes As Integer) As XPathNodeIterator
            Return XmlNodesToXML(Query.GetXmlNodesWithEvent(New Node(parentId), propertyAlias, Today, Today.AddDays(365), maxNodes))
        End Function

        Public Shared Function GetNodesWithEvent(parentId As Integer, propertyAlias As String, startDate As Date, endDate As Date, maxNodes As Integer) As XPathNodeIterator
            Return XmlNodesToXML(Query.GetXmlNodesWithEvent(New Node(parentId), propertyAlias, startDate, endDate, maxNodes))
        End Function

        Private Shared Function XmlNodesToXML(nodes As List(Of XmlNode)) As XPathNodeIterator
            Dim sb As New StringBuilder()
            sb.Append("<root>")
            For Each n As XmlNode In nodes
                sb.Append(n.OuterXml)
            Next
            sb.Append("</root>")

            Dim doc As New XmlDocument()
            doc.LoadXml(sb.ToString)

            Return doc.CreateNavigator().Select(".")
        End Function


    End Class

    Public Class Query
        Public Shared Function GetNodesWithEvent(parentId As Integer, propertyAlias As String) As List(Of Node)
            Return GetNodesWithEvent(New Node(parentId), propertyAlias)
        End Function

        Public Shared Function GetNodesWithEvent(parent As Node, propertyAlias As String) As List(Of Node)
            Return GetNodesWithEvent(parent, propertyAlias, Today, Today.AddDays(365), 0)
        End Function

        Public Shared Function GetNodesWithEvent(parentId As Integer, propertyAlias As String, startDate As Date, endDate As Date, maxNodes As Integer) As List(Of Node)
            Return GetNodesWithEvent(New Node(parentId), propertyAlias, startDate, endDate, maxNodes)
        End Function

        Public Shared Function GetNodesWithEvent(parent As Node, propertyAlias As String, startDate As Date, endDate As Date, maxNodes As Integer) As List(Of Node)
            Dim ret As New List(Of Node)
            For Each n As XmlNode In GetXmlNodesWithEvent(parent, propertyAlias, startDate, endDate, maxNodes)
                ret.Add(New Node(n))
            Next
            Return ret
        End Function


        Public Shared Function GetXmlNodesWithEvent(parent As Node, propertyAlias As String, startDate As Date, endDate As Date, maxNodes As Integer) As List(Of XmlNode)
            Dim ret As New List(Of XmlNode)
            If parent Is Nothing Then Return ret

            Dim nodes As List(Of INode) = parent.ChildrenAsList.Where(Function(x) x.GetProperty(Of Date)("endDate").Date >= startDate).ToList
            Dim dates As List(Of Date) = Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days).Select(Function(x) startDate.AddDays(x)).ToList

            For Each n As Node In nodes
                Dim nStartDate As Date = n.GetProperty(Of Date)("startDate")
                Dim nEndDate As Date = n.GetProperty(Of Date)("endDate")
                Dim recurJSON As String = n.GetProperty(Of String)(propertyAlias)
                Dim recur As New WSC.DataType.RecurringEvent.Data(recurJSON)

                If recur IsNot Nothing AndAlso Not String.IsNullOrEmpty(recur.Type) AndAlso recur.Type <> "None" Then
                    For Each d As Date In dates.Where(Function(x) x.Date >= nStartDate.Date AndAlso x.Date <= nEndDate.Date)
                            If recur.HasEvent(startDate, endDate, d) Then
                                Dim newStartDate As Date = nStartDate.AddDays(DateDiff(DateInterval.Day, nStartDate.Date, d))
                                Dim newEndDate As Date = nEndDate.AddDays(DateDiff(DateInterval.Day, nEndDate.Date, d))

                                Dim doc As New XmlDocument()
                                doc.LoadXml(n.ToXml().OuterXml)
                                Dim xml As XmlNode = doc.FirstChild
                                xml.SelectSingleNode("startDate").InnerText = newStartDate.ToString("yyyy-MM-ddTHH:mm:00")
                                xml.SelectSingleNode("endDate").InnerText = newEndDate.ToString("yyyy-MM-ddTHH:mm:00")
                                ret.Add(xml)

                            End If
                            
                        'If maxNodes > 0 AndAlso ret.Count >= maxNodes Then Exit For
                        Next

                Else
                    If (nEndDate.Date >= startDate) Then
                        ret.Add(n.ToXml)
                    End If
                End If

                'If maxNodes > 0 AndAlso ret.Count >= maxNodes Then Exit For
            Next

            ret.OrderBy(Function(x) x.SelectSingleNode("startDate"))

            If maxNodes > 0 Then
                ret = ret.Take(maxNodes).ToList
            End If

            Return ret
        End Function

        
    End Class

End Namespace
