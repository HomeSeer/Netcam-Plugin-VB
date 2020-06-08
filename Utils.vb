Imports System.IO
Imports System.Net

'Everything in this module is specific to the NetCam plugin.

Module Utils
    Public Sub TakePicture(Camera As CameraData)
        Try
            'take a picture from a given camera
            'save it with the name and date/time stamp
            'and check to make sure we don't have to clean up old pictures
            Dim intPort As Integer = 80 'GetPortFromURL(strURL)

            Dim Now As System.DateTime = System.DateTime.Now
            Dim strDateStamp As String = Now.ToString("yyyy-MM-dd")
            Dim strTimeStamp As String = Now.ToString("HH-mm-ss-fff")
            Dim strPicName As String

            If Camera.Name = "" Then
                Console.WriteLine("Could not take picture - Name missing.")
            ElseIf Camera.URL = "" Then
                Console.WriteLine("Could not take picture - URL missing.")
            Else
                strPicName = _plugin.FilePath & Camera.Name & "(" & strDateStamp & "at" & strTimeStamp & ").jpg"
                GetURLImage(Camera.URL, "", intPort, False, strPicName)

                CheckPicList(Camera)
            End If

        Catch ex As Exception
            Console.Write("Error in Take Picture: " & ex.Message)
        End Try

    End Sub

    Sub CheckPicList(Camera As CameraData)
        Dim file As FileInfo
        Dim filename As String
        Dim arrPics As New Collection
        Dim PicName As String
        Dim DeleteCount As Integer
        Dim i As Integer = 0
        Dim MaxCount As Integer

        Dim files As FileInfo() = New DirectoryInfo(_plugin.FilePath).GetFiles()
        Dim upperBound As Integer = files.GetUpperBound(0)
        Dim dates(upperBound) As Date

        For index As Integer = 0 To upperBound Step 1
            dates(index) = files(index).CreationTime
        Next

        Array.Sort(dates, files)

        MaxCount = _plugin.GetMaxCount

        If files.Count > MaxCount Then
            DeleteCount = files.Count - MaxCount
            For Each file In files
                System.IO.File.Delete(file.FullName)
                i += 1
                If i >= DeleteCount Then Exit For
            Next
        End If
    End Sub

    Public Function GetURLImage(ByVal host_str As String,
                                    ByVal page As String,
                                    ByVal port As Integer,
                                    ByVal ByteArr As Boolean,
                                    Optional ByVal filename As String = "") As Object
        Dim url As String = ""
        Dim username As String = ""
        Dim password As String = ""
        Dim need_to_login As Boolean
        Dim s As String = ""
        Dim myDatabuffer(0) As Byte
        Dim i As Integer

        If InStr(UCase(host_str), "HTTP") = 0 Then
            ' add the HTTP
            host_str = "HTTP://" & host_str
        End If
        i = InStr(host_str, "//")
        s = Mid(host_str, i + 2)
        i = InStr(s, "@")
        If i = 0 Then
            need_to_login = False
        Else
            need_to_login = True
            s = Mid(s, 1, i - 1)
            username = MidString(s, 1, ":")
            password = MidString(s, 2, ":")
        End If
        If port <> 80 Then
            host_str = host_str & ":" & port.ToString
        End If
        url = host_str & page

        Try
            Dim myWebClient As New WebClient
            If need_to_login Then
                Dim networkCredential As New NetworkCredential(username, password)
                myWebClient.Credentials = networkCredential
            Else
                myWebClient.Credentials = CredentialCache.DefaultCredentials
            End If
            myWebClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)")
            myDatabuffer = myWebClient.DownloadData(host_str & page)
        Catch ex As Exception
            Return "ERROR: " & ex.Message
        End Try

        Try
            If filename <> "" Then
                ' save to file
                If Not filename.Trim.ToLower.StartsWith(FixPath(_plugin.FilePath.Trim.ToLower)) Then 'InStr(filename, ":\") = 0 Then
                    ' save in HS root dir
                    filename = FixPath(_plugin.FilePath & "\" & filename)
                End If
                SaveToFileBinary(filename, myDatabuffer)
            End If
        Catch ex As Exception
            Return "ERROR on Filewrite: " & ex.Message
        End Try

        If ByteArr Then
            Return myDatabuffer
        Else
            Return ""
        End If

    End Function

    Public Sub SaveToFileBinary(ByVal fname As String, ByRef data() As Byte)
        Try
            Console.WriteLine("save file: " & fname)
            Dim fs As FileStream = File.OpenWrite(FixPath(fname))
            fs.Write(data, 0, data.Length)
            fs.Close()
        Catch ex As Exception
            Console.Write("Error", "Error in SaveToFileBinary: " & ex.Message)
        End Try
    End Sub

    Public Function FixPath(ByVal fpath As String) As String
        If _plugin.os = HomeSeer.PluginSdk.Types.EOsType.Linux Then
            fpath = fpath.Replace("\", "/")
            fpath = fpath.Replace("//", "/")
            fpath = fpath.Replace("\/", "/")
            fpath = fpath.Replace("/\", "/")
        Else
            fpath = fpath.Replace("/", "\")
            fpath = fpath.Replace("\\", "\")
            fpath = fpath.Replace("\/", "\")
            fpath = fpath.Replace("/\", "\")
        End If
        Return fpath
    End Function

    Function MidString(ByVal st As String, ByVal index As Integer, ByVal sep As String) As String
        ' return the string at the index
        ' strings are separated by the "sep" parameter
        Dim i As Integer
        Dim ind As Integer
        Dim start As Integer
        Dim s As String = ""
        On Error Resume Next


        start = 1
        ind = 1
        Do
            i = InStr(start, st, sep)
            If i <> 0 Then
                If ind = index Then
                    ' found the end of the requested string
                    MidString = Mid(st, start, i - start)
                    Exit Function
                Else
                    ind = ind + 1
                    start = i + Len(sep) ' next string starts after the seperator
                End If
            Else
                ' no more seperators, string not found
                ' see if last string is the correct one, it may not be followed by a seperator
                If ind = index Then
                    s = Mid(st, start)
                Else
                    s = ""
                End If
                MidString = s
                Exit Function
            End If
        Loop
    End Function
End Module
