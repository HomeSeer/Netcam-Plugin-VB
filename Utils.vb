Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Net
Imports System.Runtime.Serialization.Formatters

Module Utils
    Public Sub CheckForFeaturePages()
        'We should be in the default HomeSeer folder.
        'Check if we have a folder and if all the pages are there.
        'If one is missing, then build it now.
        Dim path As String = _plugin.ExePath & "/" & _plugin.Id
        If Not Directory.Exists(path) Then
            Directory.CreateDirectory(path)
        End If
        Dim sPages As String() = Directory.GetFiles(path)
        Dim sPageTitle As String = ""
        For Each sPage As String In _plugin.lstPages.Keys
            If Not sPages.Contains(sPage) Then
                'If you have .html feature pages built, you can copy them instead of building them in code
                'BuildPage(sPage, sPageTitle)
            End If
        Next
    End Sub

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
        Dim filename As String
        Dim arrPics As New Collection
        Dim PicName As String
        Dim DeleteCount As Integer
        Dim i As Integer = 0

        filename = Dir(_plugin.FilePath & "*.jpg")

        Do Until filename = ""
            arrPics.Add(filename)
            filename = Dir()
        Loop

        If arrPics.Count > Camera.MaxCount Then
            DeleteCount = arrPics.Count - Camera.MaxCount
            For Each PicName In arrPics
                System.IO.File.Delete(_plugin.FilePath & PicName)
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
            Dim fs As FileStream = File.OpenWrite(FixPath(fname))
            fs.Write(data, 0, data.Length)
            fs.Close()
        Catch ex As Exception
            Console.Write("Error", "Error in SaveToFileBinary: " & ex.Message)
        End Try
    End Sub

    Public Function FixPath(ByVal fpath As String) As String
        fpath = fpath.Replace("/", "\")
        fpath = fpath.Replace("\\", "\")
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

    Public Function SerializeObject(ByRef ObjIn As Object, ByRef bteOut() As Byte) As Boolean
        If ObjIn Is Nothing Then Return False
        Dim str As New MemoryStream
        Dim sf As New Binary.BinaryFormatter

        Try
            sf.Serialize(str, ObjIn)
            ReDim bteOut(CInt(str.Length - 1))
            bteOut = str.ToArray
            Return True
        Catch ex As Exception
            Console.Write("Error: Serializing object " & ObjIn.ToString & " :" & ex.Message)
            Return False
        End Try

    End Function
    Public Function DeSerializeObject(ByRef bteIn() As Byte, ByRef ObjOut As Object) As Boolean
        ' Almost immediately there is a test to see if ObjOut is NOTHING.  The reason for this
        '   when the ObjOut is suppose to be where the deserialized object is stored, is that 
        '   I could find no way to test to see if the deserialized object and the variable to 
        '   hold it was of the same type.  If you try to get the type of a null object, you get
        '   only a null reference exception!  If I do not test the object type beforehand and 
        '   there is a difference, then the InvalidCastException is thrown back in the CALLING
        '   procedure, not here, because the cast is made when the ByRef object is cast when this
        '   procedure returns, not earlier.  In order to prevent a cast exception in the calling
        '   procedure that may or may not be handled, I made it so that you have to at least 
        '   provide an initialized ObjOut when you call this - ObjOut is set to nothing after it 
        '   is typed.
        If bteIn Is Nothing Then Return False
        If bteIn.Length < 1 Then Return False
        If ObjOut Is Nothing Then Return False
        Dim str As MemoryStream
        Dim sf As New Binary.BinaryFormatter
        Dim ObjTest As Object
        Dim TType As System.Type
        Dim OType As System.Type
        Try
            OType = ObjOut.GetType
            ObjOut = Nothing
            str = New MemoryStream(bteIn)
            ObjTest = sf.Deserialize(str)
            If ObjTest Is Nothing Then Return False
            TType = ObjTest.GetType
            If Not TType.Equals(OType) Then Return False
            ObjOut = ObjTest
            If ObjOut Is Nothing Then Return False
            Return True
        Catch exIC As InvalidCastException
            Return False
        Catch ex As Exception
            Console.Write("Error: DeSerializing object: " & ex.Message)
            Return False
        End Try

    End Function

    <Extension()>
    Public Sub AppendHTML(ByRef sb As StringBuilder, ByVal item As String)
        sb.Append(item)
        sb.Append(Environment.NewLine)
    End Sub

    <Extension()>
    Public Function ToInt(ByRef s As String) As Integer
        Return CInt(s)
    End Function
End Module
