Imports System.Text
Imports Newtonsoft.Json
Imports HomeSeer.PluginSdk
Imports HomeSeer.Jui.Views
Imports System.IO
Imports HomeSeer.PluginSdk.Events

Public Class HSPI
    Inherits AbstractPlugin

    Public Overrides ReadOnly Property Id As String = "HSPI_NetCam"
    Public Overrides ReadOnly Property Name As String = "NetCam Plugin"
    Protected Overrides ReadOnly Property SettingsFileName As String = "NetCamPlugin.ini"
    Public Overrides ReadOnly Property SupportsConfigDevice As Boolean = True

    'A list of pages and page titles
    Public lstPages As SortedList(Of String, String) = New SortedList(Of String, String) From {
            {"EditCameras", "Edit/Add Cameras"},
            {"ViewImages", "View Images"}
            }

    Public Property ExePath As String
    Public Property FilePath As String

    Dim actions As New hsCollection
    Dim action As action
    Const Pagename = "Events"

    Public Sub New()
        ExePath = System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)
        FilePath = FixPath(ExePath & "/html/" & Id & "/images/")
    End Sub

    Protected Overrides Sub Initialize()
        Dim sPageTitle As String
        'get ini setting from the plugins ini file
        LoadSettingsFromIni()
        'check for the features pages and build the ones missing
        '(This is not needed for releases as the pages can be included in the zip file and HomeSeer will put them in the appropriate place.)
        CheckForFeaturePages()
        'Loop through the list of pages and initialize.
        For Each sPage As String In lstPages.Keys
            sPageTitle = lstPages(sPage)
            HomeSeerSystem.RegisterFeaturePage(Id, sPage & ".html", sPageTitle)
        Next
        Console.WriteLine("Initialized")
    End Sub

    Protected Overrides Function OnSettingsChange(pages As List(Of Page)) As Boolean
        Console.WriteLine("OnSettingsChange")

        For Each pageDelta In pages
            Dim page = Settings(pageDelta.Id)

            For Each settingDelta In pageDelta.Views
                page.UpdateViewById(settingDelta)

                Try
                    Dim newValue = settingDelta.GetStringValue()

                    If newValue Is Nothing Then
                        Continue For
                    End If

                    HomeSeerSystem.SaveINISetting(SettingsSectionName, settingDelta.Id, newValue, SettingsFileName)
                Catch exception As InvalidOperationException
                    Console.WriteLine(exception)
                End Try
            Next

            Settings(pageDelta.Id) = page
        Next

        Return True
    End Function

    Public Overrides Function OnStatusCheck() As PluginStatus
        Return PluginStatus.OK()
    End Function

    'This is a custom function..., you can name it whatever you like as long as you change the name in the corresponding html page
    'You also don't have to pass parameters if you don't want to.
    Function PagePreLoad(FunctionID As String) As String
        Dim sb = New StringBuilder
        Dim AriaSelected As String = "false"
        Dim ActiveTab As Integer = 1
        Dim Active As String = ""
        Dim Camera As CameraData
        Dim i As Integer = 1
        Dim iImage As Integer
        Dim arrImages As List(Of String)
        Dim arrDevices As List(Of Integer)

        arrDevices = HomeSeerSystem.GetRefsByInterface(Id)

        If gImageData IsNot Nothing Then
            ActiveTab = gImageData.Tab
            gImageData = Nothing
        End If

        Select Case FunctionID.ToLower
            Case "cameratabs"
                If arrDevices.Count > 0 Then
                    sb.AppendHTML("<ul class=""nav nav-tabs hs-tabs"" role=""tablist"">")
                    For Each sKey As String In arrDevices
                        If HomeSeerSystem.IsRefDevice(sKey) Then
                            If i = ActiveTab Then
                            Active = " active"
                            AriaSelected = "true"
                        End If
                        Camera = GetCameraData(sKey)
                        sb.AppendHTML("<li Class=""nav-item"">")
                        sb.AppendHTML("    <a Class=""nav-link waves-light" & Active & """ id=""settings-page" & i & ".tab"" data-toggle=""tab"" href=""#settings-page" & i & """ role=""tab"" aria-controls=""settings-page1"" aria-selected=""" & AriaSelected & """>" & Camera.Name & "</a>")
                        sb.AppendHTML("</li>")
                        i += 1
                        AriaSelected = "false"
                            Active = ""
                        End If
                    Next
                    sb.AppendHTML("</ul>")
                Else
                    sb.AppendHTML("No Cameras Found")
                End If
            Case "cameraimages"
                If arrDevices.Count > 0 Then
                    For Each sKey As String In arrDevices
                        If i = ActiveTab Then Active = " active show"
                        Camera = GetCameraData(sKey)
                        arrImages = GetCameraImages(Camera.Name)
                        sb.AppendHTML("<div Class=""tab-pane fade" & Active & """ role=""tabpanel"" aria-labelledby=""settings-page" & i & ".tab"" id=""settings-page" & i & """>")
                        sb.AppendHTML("    <div Class=""container"">")
                        For Each sImage As String In arrImages
                            sb.AppendHTML("<img id=""image-" & iImage & """ src=""/" & Id & "/images/" & sImage & """ height=""100"" width=""200"" /><button class=""waves-effect waves-dark btn btn-sm btn-primary m-0 mt-4"" onclick=""PostBackFunction('image-" & iImage & "','" & i & "');"">Delete Image</button>")
                            iImage += 1
                        Next
                        sb.AppendHTML("    </div>")
                        sb.AppendHTML("</div>")
                        Active = ""
                        i += 1
                    Next
                End If
        End Select
        Return sb.ToString
    End Function

    Function GetCameraData(refID As String) As CameraData
        Dim sJSON As String
        Dim Camera As CameraData
        Dim dv As Devices.HsDevice
        Dim PED As Devices.PlugExtraData
        dv = HomeSeerSystem.GetDeviceByRef(refID)
        PED = dv.PlugExtraData
        Camera = GetCameraData(PED)
        Return Camera
    End Function

    Function GetCameraData(PED As Devices.PlugExtraData) As CameraData
        Dim sJSON As String
        Dim Camera As CameraData
        Try
            sJSON = PED("data")
            sJSON = sJSON.Replace("\r\n  \", "")
            sJSON = sJSON.Replace("\r\n", "")
            sJSON = sJSON.Replace("\", "")
            sJSON = Strings.Right(sJSON, sJSON.Length - 1)
            sJSON = Strings.Left(sJSON, sJSON.Length - 1)
            Camera = JsonConvert.DeserializeObject(Of CameraData)(sJSON, New JsonSerializerSettings())
        Catch
            Camera = New CameraData
        End Try
        Return Camera
    End Function

    Sub SetCameraData(refID As String, Camera As CameraData)
        Dim PED As New Devices.PlugExtraData
        PED.AddNamed("data", Newtonsoft.Json.Linq.JObject.FromObject(Camera).ToString())
        HomeSeerSystem.UpdateDevicePropertyByRef(refID, Devices.EDeviceProperty.PlugExtraData, PED)
    End Sub

    Function GetCameraImages(CameraName As String) As List(Of String)
        Dim filename As String
        Dim arrImages As New List(Of String)
        filename = Dir(_plugin.FilePath & CameraName & "*.jpg")

        Do Until filename = ""
            arrImages.Add(filename)
            filename = Dir()
        Loop
        Return arrImages
    End Function

    Public Overrides Sub SetIOMulti(colSend As List(Of Devices.DeviceControlEvent))
        Dim Camera As CameraData
        Dim CC As Devices.DeviceControlEvent
        For Each CC In colSend
            Camera = GetCameraData(CC.DeviceRef)
            TakePicture(Camera)
        Next
    End Sub

    Public Overloads Function GetJuiDeviceConfigPage(ByVal deviceRef As String) As String
        Dim Camera As CameraData
        Camera = GetCameraData(deviceRef)
        Dim jpd As New JUIPageData
        jpd.pageID = "deviceconfig-page1"
        jpd.pageName = "Camera Settings"
        jpd.pageType = JUIPageType.DeviceConfig
        jpd.AddView("lblCamName", "Camera Name", JUIViewType.Label)
        jpd.AddView("txtCamName", Camera.Name, JUIViewType.Text)
        jpd.AddView("lblCamURL", "Camera URL", JUIViewType.Label)
        jpd.AddView("txtCamURL", Camera.URL, JUIViewType.Text)
        jpd.AddView("lblCamMaxInput", "Max Images", JUIViewType.Label)
        jpd.AddView("txtCamMaxInput", Camera.MaxCount, JUIViewType.Text)
        jpd.AddView("btnSubmit", "Submit", JUIViewType.Button)

        Return jpd.BuildJSON
    End Function

    Public Overloads Function SaveJuiDeviceConfigPage(pageContent As String, deviceRef As Integer) As String
        Dim Camera As CameraData
        Dim sJSOn As String = pageContent
        Camera = JsonConvert.DeserializeObject(Of CameraData)(sJSOn, New JsonSerializerSettings())
        SetCameraData(deviceRef, Camera)
    End Function

    Public Overloads Function HandleAction(ByVal ActInfo As TrigActInfo) As Boolean
        Dim Camera As CameraData
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0.01
        Dim UID As String
        Dim i As Integer
        UID = ActInfo.UID.ToString

        Try
            If Not (ActInfo.DataIn Is Nothing) Then
                DeSerializeObject(ActInfo.DataIn, action)
            Else
                Return False
            End If
            For Each sKey In action.Keys
                Select Case True
                    Case InStr(sKey, "Cameras_" & UID) > 0
                        CamRef = action(sKey)
                    Case InStr(sKey, "Images_" & UID) > 0
                        Images = action(sKey)
                    Case InStr(sKey, "Interval_" & UID) > 0
                        Interval = action(sKey)
                End Select
            Next
            Camera = GetCameraData(CamRef.ToString)
            If Camera IsNot Nothing Then
                Interval = Interval * 1000
                For i = 1 To Images
                    TakePicture(Camera)
                    Threading.Thread.Sleep(Interval)
                Next
            Else
                Console.Write("Camera not found. Action failed.")
            End If

        Catch ex As Exception
            Console.Write("Error executing action: " & ex.Message)
        End Try
        Return True
    End Function

    Public Overloads Function ActionConfigured(ByVal ActInfo As TrigActInfo) As Boolean
        Dim Configured As Boolean = False
        Dim sKey As String
        Dim itemsConfigured As Integer = 0
        Dim itemsToConfigure As Integer = 2
        Dim NeedInterval As Boolean = False
        Dim UID As String
        UID = ActInfo.UID.ToString

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)

            For Each sKey In action.Keys
                Select Case True
                    Case InStr(sKey, "Images_" & UID) > 0 AndAlso action(sKey) <> ""
                        If action(sKey) > 1 Then
                            itemsToConfigure += 1
                            NeedInterval = True
                        End If
                End Select
            Next

            For Each sKey In action.Keys
                Select Case True
                    Case InStr(sKey, "Cameras_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "Images_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                    Case NeedInterval AndAlso InStr(sKey, "Interval_" & UID) > 0 AndAlso action(sKey) <> ""
                        itemsConfigured += 1
                End Select
            Next
            If itemsConfigured = itemsToConfigure Then Configured = True
        End If
        Return Configured
    End Function

    Public Overloads Function ActionBuildUI(ByVal sUnique As String, ByVal ActInfo As TrigActInfo) As String
        Dim UID As String
        UID = ActInfo.UID.ToString
        Dim stb As New StringBuilder
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0
        Dim sKey As String
        Dim Camera As CameraData
        Dim arrCameras As Dictionary(Of Integer, Object)
        Dim SelectListOptions As New Dictionary(Of String, String)
        Dim jpd As New JUIPageData
        jpd.pageID = "events"
        jpd.pageName = "Camera Settings"
        jpd.pageType = JUIPageType.Event


        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        Else 'new event, so clean out the action object
            action = New action
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Cameras_" & UID) > 0
                    CamRef = action(sKey)
                Case InStr(sKey, "Images_" & UID) > 0
                    Images = action(sKey)
                Case InStr(sKey, "Interval_" & UID) > 0
                    Interval = action(sKey)
            End Select
        Next

        jpd.AddView("lblCamera", "Select Camera:", JUIViewType.Label)

        arrCameras = HomeSeerSystem.GetPropertyByInterface(Id, Devices.EDeviceProperty.PlugExtraData, True)

        SelectListOptions.Add("--Please Select--", "")
        For Each refID As Integer In arrCameras.Keys
            Camera = GetCameraData(arrCameras(refID))
            SelectListOptions.Add(Camera.Name, refID.ToString)
        Next

        jpd.AddView("Cameras_" & UID & sUnique, SelectListOptions, JUIViewType.SelectList)

        jpd.AddView("lblImages", "Images To Take:", JUIViewType.Label)
        jpd.AddView("Images_" & UID & sUnique, Images, JUIViewType.Text)

        jpd.AddView("lblInterval", "Seconds Between Images:", JUIViewType.Label)
        jpd.AddView("Interval_" & UID & sUnique, Interval, JUIViewType.Text)

        Return jpd.BuildHTML
    End Function

    Public Overloads Function ActionProcessPostUI(ByVal PostData As Collections.Specialized.NameValueCollection, ByVal ActInfo As TrigActInfo) As Devices.MultiReturn

        Dim Ret As New Devices.MultiReturn
        Dim UID As String
        Dim Found As Boolean
        UID = ActInfo.UID.ToString

        Ret.sResult = ""
        ' We cannot be passed info ByRef from HomeSeer, so turn right around and return this same value so that if we want, 
        '   we can exit here by returning 'Ret', all ready to go.  If in this procedure we need to change DataOut or TrigInfo,
        '   we can still do that.
        Ret.DataOut = ActInfo.DataIn
        Ret.TrigActInfo = ActInfo

        If PostData Is Nothing Then Return Ret
        If PostData.Count < 1 Then Return Ret

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        End If

        Dim parts As Collections.Specialized.NameValueCollection

        Dim sKey As String

        parts = PostData

        Do
            Found = False
            For Each sKey In action.Keys
                If sKey.Contains("_" & UID) Then
                    action.Remove(sKey)
                    Found = True
                    Exit For
                End If
            Next
        Loop Until Not Found

        Try
            For Each sKey In parts.Keys
                If sKey Is Nothing Then Continue For
                If String.IsNullOrEmpty(sKey.Trim) Then Continue For
                Select Case True
                    Case InStr(sKey, "Cameras_" & UID) > 0
                        action.Add(CObj(parts(sKey)), sKey)
                    Case InStr(sKey, "Images_" & UID) > 0
                        If IsNumeric(parts(sKey)) Then
                            action.Add(CObj(parts(sKey)), sKey)
                            'If parts(sKey) <=1 then 
                        End If
                    Case InStr(sKey, "Images_" & UID) > 0, InStr(sKey, "Interval_" & UID) > 0
                        If IsNumeric(parts(sKey)) Then action.Add(CObj(parts(sKey)), sKey)
                End Select
            Next
            If Not SerializeObject(action, Ret.DataOut) Then
                Ret.sResult = Name & " Error, Serialization failed. Signal Action not added."
                Return Ret
            End If
        Catch ex As Exception
            Ret.sResult = "ERROR, Exception in Action UI of " & Name & ": " & ex.Message
            Return Ret
        End Try

        ' All OK
        Ret.sResult = ""
        Return Ret
    End Function

    Public Overloads Function ActionFormatUI(ByVal ActInfo As TrigActInfo) As String
        Dim stb As New StringBuilder
        Dim sKey As String
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0
        Dim Camera As CameraData
        Dim UID As String
        UID = ActInfo.UID.ToString

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        End If

        For Each sKey In action.Keys
            Select Case True
                Case InStr(sKey, "Cameras_" & UID) > 0
                    CamRef = action(sKey)
                Case InStr(sKey, "Images_" & UID) > 0
                    Images = action(sKey)
                Case InStr(sKey, "Interval_" & UID) > 0
                    Interval = action(sKey)
            End Select
        Next
        Camera = GetCameraData(CamRef.ToString)
        If Camera IsNot Nothing Then
            If Images > 0 Then
                stb.Append(" the system will take ")
                Select Case Images
                    Case 1
                        stb.Append(" a picture ")
                    Case Is > 1
                        stb.Append(" " & Images.ToString & " pictures ")
                End Select
                stb.Append("with the " & Camera.Name & " camera")
                Select Case Images
                    Case 1
                        stb.Append(".")
                    Case Is > 1
                        stb.Append(" at " & Interval.ToString & " second intervals between images.")
                End Select
            Else
                stb.Append(" no images will be taken. ")
            End If
        Else
            stb.Append(" ERROR - Camera was not found. ")
        End If

        Return stb.ToString
    End Function

    Public Shadows Function ActionCount() As Integer
        Return 1
    End Function

    Public Shadows ReadOnly Property GetActionNameByNumber(ByVal ActionNumber As Integer) As String
        Get
            Select Case ActionNumber
                Case 1
                    Return Name & ": Take Picture"
            End Select
            Return ""
        End Get
    End Property

    Public Overrides Function PostBackProc(ByVal page As String, ByVal data As String, ByVal user As String, ByVal userRights As Integer) As String
        Dim response As String = ""

        Select Case page
            Case "editcameras.html"
                Try
                    Dim Camera = JsonConvert.DeserializeObject(Of CameraData)(data)
                    If Camera.Name <> "" And Camera.URL <> "" Then
                        Add_HSDevice(Camera)
                        response = "Camera was successfully added."
                    End If
                Catch exception As JsonSerializationException
                    Console.WriteLine(exception.Message)
                    response = "error"
                End Try
            Case "viewimages.html"
                Dim oImageData = JsonConvert.DeserializeObject(Of ImageData)(data)
                If oImageData.Image <> "" Then
                    Dim sImage As String = oImageData.Image
                    sImage = oImageData.Image
                    'strip off the html and keep just the image name
                    sImage = Strings.Right(sImage, sImage.Length - InStrRev(sImage, "/"))
                    sImage = _plugin.FilePath & sImage
                    sImage = sImage.Replace("+", " ")
                    'delete image
                    File.Delete(sImage)
                    gImageData = oImageData
                End If
            Case Else
                response = "error"
        End Select

        Return response
    End Function

    Sub Add_HSDevice(ByVal Camera As CameraData)
        Dim dd As Devices.NewDeviceData
        Dim df As Devices.DeviceFactory
        'Create a device factory with a local device created inside it.
        df = Devices.DeviceFactory.CreateDevice(_plugin.Id)
        'set the name of the device.
        df.WithName(Camera.Name)
        'set the type of the device.
        df.AsType(Devices.EDeviceType.Unknown, 0)

        Dim ff As Devices.FeatureFactory

        ff = Devices.FeatureFactory.CreateFeature(_plugin.Id)

        ff.WithName("Take Picture")
        ff.AddButton(1, "Take Picture")

        df.WithFeature(ff)

        'Put device specific data with the device.
        Dim PED As New Devices.PlugExtraData
        PED.AddNamed("data", Newtonsoft.Json.Linq.JObject.FromObject(Camera).ToString())
        df.WithExtraData(PED)

        'this bundles all the needed data from the device to send to HomeSeer.
        dd = df.PrepareForHs
        'this creates the device in HomeSeer using the bundled data.
        HomeSeerSystem.CreateDevice(dd)
    End Sub

    Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

End Class

