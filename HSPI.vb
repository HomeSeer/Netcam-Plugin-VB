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

    Protected Overrides ReadOnly Property SettingsFileName As String = "NetCamPlugin.ini"
    Public Overrides ReadOnly Property Id As String = "HSPI_NetCam"
    Public Overrides ReadOnly Property Name As String = "NetCam Plugin"
    Public Overrides ReadOnly Property SupportsConfigDevice As Boolean = True
    Public Overrides ReadOnly Property ActionCount As Integer = 1
    Public Overrides ReadOnly Property TriggerCount As Integer = 1
    Public Overrides ReadOnly Property HasTriggers As Boolean = True

    'A list of pages and page titles
    Public lstPages As SortedList(Of String, String) = New SortedList(Of String, String) From {
            {"EditCameras", "Edit/Add Cameras"},
            {"ViewImages", "View Images"}
            }

    Public Property ExePath As String
    Public Property FilePath As String
    Const Pagename = "Events"

    Public Sub New()
        ExePath = System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)
        FilePath = FixPath(ExePath & "/html/" & Id & "/images/")
        'this will output debug information from the pluinSDK to the console window.
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
        ActionTypes.AddActionType(GetType(Take_Picture_Action))
        TriggerTypes.AddTriggerType(GetType(Taken_Picture_Trigger))
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
        'we don't need to do anything here.
    End Sub

    'This is a custom function..., you can name it whatever you like as long as you change the name in the corresponding html page
    'You also don't have to pass parameters if you don't want to.
    'This function is being used by the viewimages.html page.
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
        Dim Camera As CameraData = Nothing
        Dim PED As Devices.PlugExtraData
        Try
            PED = HomeSeerSystem.GetPropertyByRef(refID, EProperty.PlugExtraData)
            Camera = GetCameraData(PED)
        Catch
        End Try
        Return Camera
    End Function

    Function GetCameraData(PED As Devices.PlugExtraData) As CameraData
        Dim sJSON As String
        Dim Camera As CameraData
        Try
            sJSON = PED("data")
            Camera = JsonConvert.DeserializeObject(Of CameraData)(sJSON, New JsonSerializerSettings())
        Catch
            Camera = Nothing
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

        'Rather than write out each property of the class individually, 
        'this process updates each value that was passed automatically.

        'the pageId has extra text on it that we don't use.
        pID = pID.Replace("md", "")

        'get all the values of the camera data that were edited.
        For Each view As AbstractView In p.Views
            dctCamera.Add(view.Name, view.GetStringValue)
        Next

        'get the camera data for the one we're editing
        Camera = GetCameraData(refID)

        'get all the properties of the cameradata class
        Dim oType As Type = Camera.GetType()
        Dim oProperties As PropertyInfo() = oType.GetProperties()

        'Go through the properties and if there is data that has been edited for it, then update that property.
        For Each oProperty As PropertyInfo In oProperties
            pName = pID & oProperty.Name
            If dctCamera.Keys.Contains(pName) Then
                'make sure we send the correct variable type.
                Select Case oProperty.PropertyType.Name
                    Case "Int32"
                        oProperty.SetValue(Camera, CInt(dctCamera(pName)))
                    Case "String"
                        oProperty.SetValue(Camera, dctCamera(pName))
                End Select
            End If
        Next

        Return Camera
    End Function

    Function GetCameraImages(CameraName As String) As List(Of String)
        Dim filename As String
        Dim arrImages As New List(Of String)
        filename = Dir(FilePath & CameraName & "*.jpg")

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
            'See if we need to fire any of our triggers on the events page
            CheckTriggers(CC.TargetRef)
        Next
    End Sub

    Public Sub CheckTriggers(RefID As String)
        Dim TrigsToCheck() As TrigActInfo
        TrigsToCheck = HomeSeerSystem.TriggerMatches(Name, 1, -1)
        If TrigsToCheck IsNot Nothing AndAlso TrigsToCheck.Count > 0 Then
            For Each TC As TrigActInfo In TrigsToCheck
                'if the selected camera refID matches the refID of the camera that took the picture then fire the trigger.
                HomeSeerSystem.TriggerFire(Name, TC)
            Next
        End If
    End Sub

    Public Overloads Function GetJuiDeviceConfigPage(ByVal deviceRef As String) As String
        Dim Camera As CameraData
        Dim JUIPage As Page = Nothing
        Dim pageID As String = "deviceconfig-page1"
        Camera = GetCameraData(deviceRef)

        JUIPage = PageFactory.CreateDeviceConfigPage(pageID, "Camera Settings").Page

        'For the LoadCameraData function to work correctly, the names of the input fields MUST correspond to the property names of your object.
        'If you wish to name them something different, the LoadCameraData will need a Select Case statement to match the input names to the properties.

        JUIPage.AddView(New InputView(pageID & "-Name", "Name", Camera.Name))
        JUIPage.AddView(New InputView(pageID & "-URL", "URL", Camera.URL))
        JUIPage.AddView(New InputView(pageID & "-MaxCount", "MaxCount", Camera.MaxCount, EInputType.Number))


        Return JUIPage.ToJsonString
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
End Class

