Imports System.Text
Imports Newtonsoft.Json
Imports HomeSeer.PluginSdk
Imports HomeSeer.Jui.Views
Imports System.IO
Imports HomeSeer.PluginSdk.Events
Imports System.Reflection
Imports HomeSeer.Jui.Types
Imports HomeSeer.PluginSdk.Devices
Imports HomeSeer.PluginSdk.Devices.Identification

Public Class HSPI
    Inherits AbstractPlugin

    Public Overrides ReadOnly Property Id As String = "HSPI_NetCam"
    Public Overrides ReadOnly Property Name As String = "NetCam Plugin"
    Protected Overrides ReadOnly Property SettingsFileName As String = "NetCamPlugin.ini"
    Public Overrides ReadOnly Property SupportsConfigDevice As Boolean = True
    Public Overrides ReadOnly Property ActionCount As Integer = 1

    'A list of pages and page titles
    Public lstPages As SortedList(Of String, String) = New SortedList(Of String, String) From {
            {"EditCameras", "Edit/Add Cameras"},
            {"ViewImages", "View Images"}
            }

    Public Property ExePath As String
    Public Property FilePath As String

    Dim actions As New hsCollection
    Dim action As action
    Dim triggers As New hsCollection
    Dim trigger As trigger
    Dim Commands As New hsCollection
    Const Pagename = "Events"


    Public Sub New()
        ExePath = System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)
        FilePath = FixPath(ExePath & "/html/" & Id & "/images/")
        LogDebug = True
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
        TPAction = New Take_Picture_Action()
        ActionTypes.AddActionType(GetType(Take_Picture_Action))
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

    Protected Overrides Sub BeforeReturnStatus()
        Throw New NotImplementedException()
    End Sub



    'Public Overrides Function OnStatusCheck() As PluginStatus
    '    Return PluginStatus.OK()
    'End Function

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
        Dim arrCameras As Dictionary(Of Integer, Object)

        arrCameras = HomeSeerSystem.GetPropertyByInterface(Id, EProperty.PlugExtraData, True)

        If gImageData IsNot Nothing Then
            ActiveTab = gImageData.Tab
            gImageData = Nothing
        End If

        Select Case FunctionID.ToLower
            Case "cameratabs"
                If arrCameras.Count > 0 Then
                    sb.AppendHTML("<ul class=""nav nav-tabs hs-tabs"" role=""tablist"">")
                    For Each Camera In arrCameras.Values
                        If i = ActiveTab Then
                            Active = " active"
                            AriaSelected = "true"
                        End If
                        sb.AppendHTML("<li Class=""nav-item"">")
                        sb.AppendHTML("    <a Class=""nav-link waves-light" & Active & """ id=""settings-page" & i & ".tab"" data-toggle=""tab"" href=""#settings-page" & i & """ role=""tab"" aria-controls=""settings-page1"" aria-selected=""" & AriaSelected & """>" & Camera.Name & "</a>")
                        sb.AppendHTML("</li>")
                        i += 1
                        AriaSelected = "false"
                        Active = ""
                    Next
                    sb.AppendHTML("</ul>")
                Else
                    sb.AppendHTML("No Cameras Found")
                End If
            Case "cameraimages"
                If arrCameras.Count > 0 Then
                    For Each Camera In arrCameras.Values
                        If i = ActiveTab Then Active = " active show"
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

    Public Function GetCameraData(refID As String) As CameraData
        Dim Camera As CameraData
        Dim PED As Devices.PlugExtraData
        PED = HomeSeerSystem.GetPropertyByRef(refID, EProperty.PlugExtraData)
        Camera = GetCameraData(PED)
        Return Camera
    End Function

    Function GetCameraData(PED As Devices.PlugExtraData) As CameraData
        Dim sJSON As String
        Dim Camera As CameraData
        Try
            sJSON = PED("data")
            Camera = JsonConvert.DeserializeObject(Of CameraData)(sJSON, New JsonSerializerSettings())
        Catch
            Camera = New CameraData
        End Try
        Return Camera
    End Function

    Sub SetCameraData(refID As String, Camera As CameraData)
        Dim PED As New Devices.PlugExtraData
        PED.AddNamed("data", Newtonsoft.Json.Linq.JObject.FromObject(Camera).ToString())
        HomeSeerSystem.UpdatePropertyByRef(refID, EProperty.PlugExtraData, PED)
    End Sub

    Function LoadCameraData(refID As String, p As Page) As CameraData
        Dim Camera As CameraData
        Dim dctCamera As New Dictionary(Of String, String)
        Dim pID As String = p.Id
        Dim pName As String = ""

        pID = pID.Replace("md", "")

        Camera = GetCameraData(refID)

        Dim oType As Type = Camera.GetType()
        Dim oProperties As PropertyInfo() = oType.GetProperties()

        For Each oProperty As PropertyInfo In oProperties
            pName = pID & oProperty.Name
            dctCamera.Add(pName, oProperty.GetValue(Camera))
        Next

        For Each view As AbstractView In p.Views
            dctCamera(view.Name) = view.GetStringValue
        Next

        For Each oProperty As PropertyInfo In oProperties
            pName = pID & oProperty.Name
            Select Case oProperty.PropertyType.Name
                Case "Int32"
                    oProperty.SetValue(Camera, CInt(dctCamera(pName)))
                Case "String"
                    oProperty.SetValue(Camera, dctCamera(pName))
            End Select
        Next

        Return Camera
    End Function

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

    Public Overrides Sub SetIOMulti(colSend As List(Of Devices.Controls.ControlEvent))
        Dim Camera As CameraData
        Dim CC As Devices.Controls.ControlEvent
        For Each CC In colSend
            Camera = GetCameraData(CC.TargetRef)
            TakePicture(Camera)
        Next
    End Sub

    Public Overloads Function GetJuiDeviceConfigPage(ByVal deviceRef As String) As String
        Dim Camera As CameraData
        Camera = GetCameraData(deviceRef)
        Dim jpd As New JUIPageData
        jpd.pageID = "deviceconfig-page1"
        jpd.pageName = "Camera Settings"
        jpd.pageType = EPageType.DeviceConfig

        'For the LoadCameraData function to work correctly, the name of the input field MUST correspond to the property names of your object.
        'If you wish to name them something different, the LoadCameraData will need a Select Case statement to match the input names to the properties.

        jpd.AddView("Name", Camera.Name, EViewType.Input)
        jpd.AddView("URL", Camera.URL, EViewType.Input)
        jpd.AddView("MaxCount", Camera.MaxCount, EViewType.Input, EInputType.Number)

        Return jpd.BuildJSON
    End Function

    Public Function GETPED() As Dictionary(Of Integer, Object)
        Return HomeSeerSystem.GetPropertyByInterface(Id, EProperty.PlugExtraData, True)
    End Function
    Protected Overrides Function OnDeviceConfigChange(deviceConfigPage As Page, deviceRef As Integer) As Boolean
        Dim Camera As CameraData
        Dim result As Boolean = True
        Try
            Camera = LoadCameraData(deviceRef, deviceConfigPage)
            SetCameraData(deviceRef, Camera)
        Catch
            result = False
        End Try
        Return result
    End Function

    Public Function HandleActionn(actInfo As TrigActInfo) As Boolean
        Dim Camera As CameraData
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0.01
        Dim UID As String
        Dim i As Integer
        UID = actInfo.UID.ToString

        Try
            If Not (actInfo.DataIn Is Nothing) Then
                DeSerializeObject(actInfo.DataIn, action)
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


    Public Function ActionConfiguredd(ByVal ActInfo As TrigActInfo) As Boolean
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

    Public Function ActionBuildUIi(actInfo As TrigActInfo) As String
        Dim UID As String
        UID = actInfo.UID.ToString
        Dim stb As New StringBuilder
        Dim refID As Integer
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
        jpd.pageType = EPageType.Generic


        If Not (actInfo.DataIn Is Nothing) Then
            DeSerializeObject(actInfo.DataIn, action)
        Else 'new event, so clean out the action object
            action = New Action
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

        jpd.AddView("lblCamera", "Select Camera:", EViewType.Label)

        arrCameras = HomeSeerSystem.GetPropertyByInterface(Id, EProperty.PlugExtraData, True)

        SelectListOptions.Add("--Please Select--", "")
        For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
            refID = kvp.Key
            Camera = GetCameraData(kvp.Value)
            SelectListOptions.Add(Camera.Name, refID.ToString)
        Next



        jpd.AddView("Cameras_" & UID, SelectListOptions, EViewType.SelectList)

        jpd.AddView("lblImages", "Images To Take:", EViewType.Label)
        jpd.AddView("Images_" & UID, Images, EViewType.Input)

        jpd.AddView("lblInterval", "Seconds Between Images:", EViewType.Label)
        jpd.AddView("Interval_" & UID, Interval, EViewType.Input)

        Return jpd.BuildHTML
    End Function

    Public Function ActionProcessPostUIi(postData As Dictionary(Of String, String), actInfo As TrigActInfo) As MultiReturn

        Dim Ret As New Devices.MultiReturn
        Dim UID As String
        Dim Found As Boolean
        UID = actInfo.UID.ToString

        Ret.sResult = ""
        ' We cannot be passed info ByRef from HomeSeer, so turn right around and return this same value so that if we want, 
        '   we can exit here by returning 'Ret', all ready to go.  If in this procedure we need to change DataOut or TrigInfo,
        '   we can still do that.
        Ret.DataOut = actInfo.DataIn
        Ret.TrigActInfo = actInfo

        If postData Is Nothing Then Return Ret
        If postData.Count < 1 Then Return Ret

        If Not (actInfo.DataIn Is Nothing) Then
            DeSerializeObject(actInfo.DataIn, action)
        End If

        Dim parts As Dictionary(Of String, String)

        Dim sKey As String

        parts = postData

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

    Public Function ActionFormatUIi(ByVal ActInfo As TrigActInfo) As String
        Dim stb As New StringBuilder
        Dim sKey As String
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0
        Dim Camera As CameraData = Nothing
        Dim UID As String
        UID = ActInfo.UID.ToString

        If Not (ActInfo.DataIn Is Nothing) Then
            DeSerializeObject(ActInfo.DataIn, action)
        End If
        Try
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
        Catch
        End Try
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

    ReadOnly Property ActionName(ByVal ActionNumber As Integer) As String
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

    Sub SetTriggers()
        Dim o As Object = Nothing
        If triggers.Count = 0 Then
            triggers.Add(o, "Took Picture")
        End If
    End Sub

    Public Overrides ReadOnly Property HasTriggers() As Boolean
        Get
            SetTriggers()
            Return IIf(triggers.Count > 0, True, False)
        End Get
    End Property

    Public Overrides ReadOnly Property TriggerCount As Integer
        Get
            SetTriggers()
            Return triggers.Count
        End Get
    End Property

    Public ReadOnly Property SubTriggerCount(ByVal TriggerNumber As Integer) As Integer
        Get
            Dim trigger As trigger
            If ValidTrig(TriggerNumber) Then
                trigger = triggers(TriggerNumber)
                If Not (trigger Is Nothing) Then
                    Return trigger.Count
                Else
                    Return 0
                End If
            Else
                Return 0
            End If
        End Get
    End Property

    Public Overrides Function GetTriggerNameByNumber(triggerNum As Integer) As String
        If Not ValidTrig(triggerNum) Then
            Return ""
        Else
            Return Name & ": " & triggers.Keys(triggerNum - 1)
        End If
    End Function

    Public Overrides Function GetSubTriggerNameByNumber(triggerNum As Integer, subTriggerNum As Integer) As String
        Dim trigger As trigger
        If ValidSubTrig(triggerNum, subTriggerNum) Then
            trigger = triggers(triggerNum)
            Return Name & ": " & trigger.Keys(subTriggerNum - 1)
        Else
            Return ""
        End If
    End Function

    Friend Function ValidTrig(ByVal TrigIn As Integer) As Boolean
        SetTriggers()
        If TrigIn > 0 AndAlso TrigIn <= triggers.Count Then
            Return True
        End If
        Return False
    End Function

    Public Function ValidSubTrig(ByVal TrigIn As Integer, ByVal SubTrigIn As Integer) As Boolean
        Dim trigger As trigger = Nothing
        If TrigIn > 0 AndAlso TrigIn <= triggers.Count Then
            trigger = triggers(TrigIn)
            If Not (trigger Is Nothing) Then
                If SubTrigIn > 0 AndAlso SubTrigIn <= trigger.Count Then Return True
            End If
        End If
        Return False
    End Function

    Public Overrides Function IsTriggerConfigValid(ByVal TrigInfo As TrigActInfo) As Boolean
        Dim Configured As Boolean = False
        Dim sKey As String
        Dim itemsConfigured As Integer = 0
        Dim itemsToConfigure As Integer = 3
        Dim UID As String
        UID = TrigInfo.UID.ToString

        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
            For Each sKey In trigger.Keys
                Select Case True
                    Case InStr(sKey, "Housecodes_" & UID) > 0 AndAlso trigger(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "DeviceCodes_" & UID) > 0 AndAlso trigger(sKey) <> ""
                        itemsConfigured += 1
                    Case InStr(sKey, "Commands_" & UID) > 0 AndAlso trigger(sKey) <> ""
                        itemsConfigured += 1
                End Select
            Next
            If itemsConfigured = itemsToConfigure Then Configured = True
        End If
        Return Configured
    End Function

    Public Enum DEVICE_COMMAND
        All_Lights_Off = 0
        All_Lights_On = 1
        UOn = 2
        UOff = 3
    End Enum
    Sub LoadCommands()
        Commands.Add(CObj("All Lights Off"), CStr(CInt(DEVICE_COMMAND.All_Lights_Off)))
        Commands.Add(CObj("All Lights On"), CStr(CInt(DEVICE_COMMAND.All_Lights_On)))
        Commands.Add(CObj("Device On"), CStr(CInt(DEVICE_COMMAND.UOn)))
        Commands.Add(CObj("Device Off"), CStr(CInt(DEVICE_COMMAND.UOff)))
    End Sub

    Public Overrides Function TriggerBuildUI(ByVal TrigInfo As TrigActInfo) As String
        Dim UID As String
        UID = TrigInfo.UID.ToString
        Dim Housecode As String = ""
        Dim DeviceCode As String = ""
        Dim Command As String = ""
        Dim sKey As String
        Dim SelectListOptions As New Dictionary(Of String, String)
        Dim jpd As New JUIPageData
        jpd.pageID = "events"
        jpd.pageName = "Camera Settings"
        jpd.pageType = EPageType.Generic


        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
        Else 'new event, so clean out the trigger object
            trigger = New trigger
        End If

        For Each sKey In trigger.Keys
            Select Case True
                Case InStr(sKey, "HouseCodes_" & UID) > 0
                    Housecode = trigger(sKey)
                Case InStr(sKey, "DeviceCodes_" & UID) > 0
                    DeviceCode = trigger(sKey)
                Case InStr(sKey, "Commands_" & UID) > 0
                    Command = trigger(sKey)
            End Select
        Next

        SelectListOptions = New Dictionary(Of String, String)
        jpd.AddView("lblHC", "Select House Code:", EViewType.Label)
        For Each C In "ABCDEFGHIJKLMNOP"
            SelectListOptions.Add(C, C)
        Next
        jpd.AddView("HouseCodes", SelectListOptions, EViewType.SelectList)

        SelectListOptions = New Dictionary(Of String, String)
        jpd.AddView("lblUC", "Select Unit Code:", EViewType.Label)
        SelectListOptions.Add("All", "All")
        For i = 1 To 16
            SelectListOptions.Add(i.ToString, i.ToString)
        Next
        jpd.AddView("DeviceCodes", SelectListOptions, EViewType.SelectList)

        If Commands.Count = 0 Then LoadCommands()

        SelectListOptions = New Dictionary(Of String, String)
        jpd.AddView("lblCommand", "Select Command:", EViewType.Label)
        For Each item In Commands.Keys
            SelectListOptions.Add(Commands(item), item)
        Next
        jpd.AddView("Commands", SelectListOptions, EViewType.SelectList)

        Return jpd.BuildHTML
    End Function

    Public Overrides Function TriggerProcessPostUI(ByVal PostData As Dictionary(Of String, String), ByVal TrigInfo As TrigActInfo) As MultiReturn
        Dim Ret As New MultiReturn
        Dim UID As String
        UID = TrigInfo.UID.ToString

        Ret.sResult = ""
        ' We cannot be passed info ByRef from HomeSeer, so turn right around and return this same value so that if we want, 
        '   we can exit here by returning 'Ret', all ready to go.  If in this procedure we need to change DataOut or TrigInfo,
        '   we can still do that.
        Ret.DataOut = TrigInfo.DataIn
        Ret.TrigActInfo = TrigInfo

        If PostData Is Nothing Then Return Ret
        If PostData.Count < 1 Then Return Ret

        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
        End If

        Dim sKey As String

        Try
            For Each sKey In PostData.Keys
                If sKey Is Nothing Then Continue For
                If String.IsNullOrEmpty(sKey.Trim) Then Continue For
                Select Case True
                    Case InStr(sKey, "HouseCodes_" & UID) > 0, InStr(sKey, "DeviceCodes_" & UID) > 0, InStr(sKey, "Commands_" & UID) > 0
                        trigger.Add(CObj(PostData(sKey)), sKey)
                End Select
            Next
            If Not SerializeObject(trigger, Ret.DataOut) Then
                Ret.sResult = Name & " Error, Serialization failed. Signal Trigger not added."
                Return Ret
            End If
        Catch ex As Exception
            Ret.sResult = "ERROR, Exception in Trigger UI of " & Name & ": " & ex.Message
            Return Ret
        End Try

        ' All OK
        Ret.sResult = ""
        Return Ret
    End Function

    Public Overrides Function TriggerFormatUI(ByVal TrigInfo As TrigActInfo) As String
        Dim stb As New StringBuilder
        Dim sKey As String
        Dim Housecode As String = ""
        Dim DeviceCode As String = ""
        Dim Command As String = ""
        Dim UID As String
        UID = TrigInfo.UID.ToString

        If Not (TrigInfo.DataIn Is Nothing) Then
            DeSerializeObject(TrigInfo.DataIn, trigger)
        End If

        For Each sKey In trigger.Keys
            Select Case True
                Case InStr(sKey, "HouseCodes_" & UID) > 0
                    Housecode = trigger(sKey)
                Case InStr(sKey, "DeviceCodes_" & UID) > 0
                    DeviceCode = trigger(sKey)
                Case InStr(sKey, "Commands_" & UID) > 0
                    Command = trigger(sKey)
            End Select
        Next

        stb.Append(" the system detected the " & Commands(Command) & " command ")
        stb.Append("on Housecode " & Housecode & " ")
        If DeviceCode = "ALL" Then
            stb.Append("from a Unitcode")
        Else
            stb.Append("from Unitcode " & DeviceCode)
        End If

        Return stb.ToString
    End Function

    Sub Add_HSDevice(ByVal Camera As CameraData)
        Dim dd As Devices.NewDeviceData
        Dim df As Devices.DeviceFactory
        'Create a device factory with a local device created inside it.
        df = Devices.DeviceFactory.CreateDevice(_plugin.Id)
        'set the name of the device.
        df.WithName(Camera.Name)
        'set the type of the device.
        df.AsType(EDeviceType.Generic, 0)

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

