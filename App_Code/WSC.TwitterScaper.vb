Imports Microsoft.VisualBasic

Namespace WSC
    ''' <summary>
    ''' Dependant on HtmlAgilityPack.CssSelectors.dll
    ''' inspiration: https://www.npmjs.com/package/twitter-screen-scrape
    ''' </summary>
    ''' <remarks></remarks>
    Public Class TwitterScaper
        Private Shared Pages As Integer = 3
        Private Shared CacheTimeSpan As New TimeSpan(1, 0, 0)
        Private Shared CacheKey As String = "Twitter-"

        Private Shared Function GetJSON(username As String, lastId As String) As Newtonsoft.Json.Linq.JObject
            Dim endPoint = String.Format("https://twitter.com/i/profiles/show/{0}/timeline?include_available_features=1& include_entities=1", username)

            If Not String.IsNullOrEmpty(lastId) Then
                endPoint &= "&max_position=" & lastId
            End If

            Using w = New Net.WebClient()
                Dim json_data = String.Empty
                '--Attempt to download JSON data as a string
                Try
                    json_data = w.DownloadString(endPoint)
                Catch generatedExceptionName As Exception
                End Try
                If String.IsNullOrEmpty(json_data) Then
                    Return Nothing
                Else
                    Dim json = Newtonsoft.Json.Linq.JObject.Parse(json_data)
                    Dim doc As New HtmlAgilityPack.HtmlDocument()
                    doc.LoadHtml(json("items_html"))

                    json("lastid") = String.Empty

                    Dim lastItem = doc.QuerySelectorAll(".original-tweet").Last()
                    If lastItem IsNot Nothing Then
                        json("lastid") = lastItem.Attributes("data-item-id").Value()
                    End If

                    Return json
                End If
            End Using
        End Function

        Private Shared Function GetHTML(username As String) As String
            Dim json As Newtonsoft.Json.Linq.JObject
            Dim html = String.Empty
            Dim lastID = String.Empty

            For p = 1 To Pages
                json = GetJSON(username, lastID)
                If json Is Nothing Then Exit For
                html &= json.Value(Of String)("items_html")
                If Not json.Value(Of Boolean)("has_more_items") Then Exit For
                lastID = json.Value(Of String)("lastid")
            Next

            Return html
        End Function

        Shared Function GetPosts(username As String, cached As Boolean) As List(Of Post)
            Dim html As String = String.Empty

            If cached Then
                '--try the cache
                html = DirectCast(HttpContext.Current.Cache(CacheKey & username), String)
                If String.IsNullOrEmpty(html) Then
                    html = GetHTML(username)
                    If Not String.IsNullOrEmpty(html) Then
                        HttpContext.Current.Cache.Insert(CacheKey & username, html, Nothing, System.Web.Caching.Cache.NoAbsoluteExpiration, CacheTimeSpan)
                    End If
                End If
            Else
                html = GetHTML(username)
            End If

            If String.IsNullOrEmpty(html) Then Return Nothing

            Dim ret As New List(Of Post)

            Dim doc As New HtmlAgilityPack.HtmlDocument()
            doc.LoadHtml(html)

            For Each node In doc.DocumentNode.QuerySelectorAll(".original-tweet")
                ret.Add(New Post(node))
            Next
            Return ret
        End Function

        Shared Function GetPosts(username As String) As List(Of Post)
            Return GetPosts(username, True)
        End Function

        Class Post
            Property ID As String
            Property isRetweet As Boolean
            Property Username As String
            Property UserImage As String
            Property UserHandle As String
            Property Text As String
            Property Time As DateTime
            Property Images As New List(Of String)
            Property Link As String

            Sub New(node As HtmlAgilityPack.HtmlNode)
                For Each n In node.QuerySelectorAll(".tweet-text a[href^=""/""]")
					n.Attributes("href").Value = "http://twitter.com" & n.Attributes("href").Value
                Next

                Me.ID = node.Attributes("data-item-id").Value
                Me.isRetweet = node.QuerySelectorAll(".js-retweet-text").Count > 0
                Me.Text = node.QuerySelector(".tweet-text").InnerHtml
                Me.Time = (New DateTime(1970, 1, 1, 0, 0, 0, 0)).AddMilliseconds(node.QuerySelector(".js-short-timestamp").Attributes("data-time-ms").Value)
                Me.Link = node.Attributes("data-permalink-path").Value
                Me.Username = node.Attributes("data-name").Value
                Me.UserHandle = node.Attributes("data-screen-name").Value
                Me.UserImage = node.QuerySelector(".avatar").Attributes("src").Value

                Dim pics = node.QuerySelectorAll(".multi-photos .multi-photo[data-image-url], [data-card-type=photo] [data-image-url]")
                For Each pic In pics
                    Me.Images.Add(pic.Attributes("data-image-url").Value)
                Next
            End Sub
        End Class

    End Class
End Namespace