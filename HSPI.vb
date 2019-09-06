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

''' <inheritdoc cref="AbstractPlugin"/>
''' <summary>
''' The plugin class for HomeSeer Sample Plugin that implements the <see cref="AbstractPlugin"/> base class.
''' </summary>
''' <remarks>
''' This class is accessed by HomeSeer and requires that its name be "HSPI" and be located in a namespace
'''  that corresponds to the name of the executable. For this plugin, "HomeSeerSamplePlugin_VB" the executable
'''  file is "HSPI_HomeSeerSamplePlugin_VB.exe" and this class is HSPI_HomeSeerSamplePlugin_VB.HSPI
''' <para>
''' If HomeSeer is unable to find this class, the plugin will not start.
''' </para>
''' </remarks>
Public Class HSPI
    Inherits AbstractPlugin
    Implements Take_Picture_Action.IActionListener, Taken_Picture_Trigger.ITriggerListener
    ''' <inheritdoc />
    ''' <remarks>
    ''' This ID is used to identify the plugin and should be unique across all plugins
    ''' <para>
    ''' This must match the MSBuild property $(PluginId) as this will be used to copy
    '''  all of the HTML feature pages located in .\html\ to a relative directory
    '''  within the HomeSeer html folder.
    ''' </para>
    ''' <para>
    ''' The relative address for all of the HTML pages will end up looking like this:
    '''  ..\Homeseer\Homeseer\html\HomeSeerSamplePlugin_VB\
    ''' </para>
    ''' </remarks>
    Public Overrides ReadOnly Property Id As String = "HSPI_NetCam"
    ''' <inheritdoc />
    ''' <remarks>
    ''' This is the readable name for the plugin that is displayed throughout HomeSeer
    ''' </remarks>
    Public Overrides ReadOnly Property Name As String = "NetCam Plugin"
    Protected Overrides ReadOnly Property SettingsFileName As String = "NetCamPlugin.ini"
    'Set this to true when you want to have a tab on a HomeSeer device for your plugin.
    'This is used in this plugin  to allow editing of camera data that is specifically tied to a device.
    Public Overrides ReadOnly Property SupportsConfigDevice As Boolean = True
    'these are used to set the path for camera images
    Public Property ExePath As String
    Public Property FilePath As String
    'this is used to hold image data between page requests
    Public gImageData As ImageData = Nothing

    Public Sub New()
        ExePath = System.IO.Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory)
        FilePath = FixPath(ExePath & "/html/" & Id & "/images/")
        'this will output debug information from the pluinSDK to the console window.
        LogDebug = True
    End Sub

    Protected Overrides Sub Initialize()
        'Setup anything that needs to be configured before a connection to HomeSeer is established
        ' like initializing the starting state of anything needed for the operation of the plugin

        'get ini setting from the plugins ini file
        LoadSettingsFromIni()
        'To avoid user confusion, only register the page needed to set up the devices for the plugin
        HomeSeerSystem.RegisterFeaturePage(Id, "AddCameras.html", "Add Cameras")
        HomeSeerSystem.RegisterDeviceIncPage(Id, "AddCameras.html", "Add Cameras")
        LoadAdditionalPages(True)
        Console.WriteLine("Initialized")
    End Sub

    Protected Overrides Function OnSettingChange(ByVal pageId As String, ByVal currentView As AbstractView, ByVal changedView As AbstractView) As Boolean
        'Not doing anything here
        Return True
    End Function

    Private Sub LoadAdditionalPages(Optional IsInit As Boolean = False)
        Dim refIDs As List(Of Integer)
        'If there are devices found in HomeSeer, then register the additional page and add the event items.
        'Check for HomeSeer Devices
        refIDs = HomeSeerSystem.GetRefsByInterface(Id)
        'If the plugin is starting up ,then check for 1 or more devices, else only check for the first device created
        If (refIDs.Count > 0 And IsInit) Or refIDs.Count = 1 Then
            HomeSeerSystem.RegisterFeaturePage(Id, "ViewImages.html", "View Images")
            ActionTypes.AddActionType(GetType(Take_Picture_Action))
            TriggerTypes.AddTriggerType(GetType(Taken_Picture_Trigger))
        End If
    End Sub

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

        'This allows the plugin to get the PED from all related HomeSeer devices
        'By setting the deviceOnly setting to true, you will only get data back from the top level/root/parent device of each device group
        arrCameras = HomeSeerSystem.GetPropertyByInterface(Id, EProperty.PlugExtraData, True)

        'This is specific to the viewimages.html page
        If gImageData IsNot Nothing Then
            ActiveTab = gImageData.Tab
            gImageData = Nothing
        End If

        Select Case FunctionID.ToLower
            Case "cameratabs"
                'this builds the tabs on the viewImages.html page.
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
                'this builds the images for each tab on the viewImages.html page.
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

    'This is a function used by the listener classes in the action and trigger classes to interact with the HSPI class in a thread safe manner.
    Public Function GetCameraData(refID As String) As CameraData Implements Take_Picture_Action.IActionListener.GetCameraData, Taken_Picture_Trigger.ITriggerListener.GetCameraData
        Dim Camera As CameraData = Nothing
        Dim PED As Devices.PlugExtraData
        Try
            'the camerdata class is kept in the PED of a device.
            PED = HomeSeerSystem.GetPropertyByRef(refID, EProperty.PlugExtraData)
            Camera = GetCameraData(PED)
        Catch
        End Try
        Return Camera
    End Function

    Function GetCameraData(PED As Devices.PlugExtraData) As CameraData
        Dim sJSON As String
        Dim Camera As CameraData
        'the camerdata class is kept as a JSON string in the PED of a device.
        Try
            sJSON = PED("data")
            Camera = JsonConvert.DeserializeObject(Of CameraData)(sJSON, New JsonSerializerSettings())
        Catch
            Camera = Nothing
        End Try
        Return Camera
    End Function

    Sub SetCameraData(refID As String, Camera As CameraData)
        'the camerdata class is kept as a JSON string in the PED of a device.
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

        'the pageId may have extra text on it that we don't use.
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
        'This finds all the images that have been taken with the plugin
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
        'This is called when a HomeSeer Device CAPI control is interacted with
        Dim Camera As CameraData
        Dim CC As Devices.Controls.ControlEvent
        Dim ParentRef As String
        Dim dv As HsDevice

        For Each CC In colSend
            'we need the root device ref of the group, so find out if we already have it.
            dv = HomeSeerSystem.GetDeviceByRef(CC.TargetRef)
            If dv.Relationship = ERelationship.Feature Then
                'it's a feature device so the root of the group is the first associated device
                ParentRef = dv.AssociatedDevices(0)
            Else
                'it's the root so use that ref
                ParentRef = dv.Ref
            End If
            Camera = GetCameraData(ParentRef)
            TakePicture(Camera)
            'See if we need to fire any of our triggers on the events page
            CheckTriggers(ParentRef)
        Next
    End Sub

    Public Sub CheckTriggers(RefID As String)
        Dim TrigsToCheck() As TrigActInfo
        Dim TpT As Taken_Picture_Trigger
        Dim oTrigData As New Object
        'Get a list of all the triggers for this plugin
        TrigsToCheck = HomeSeerSystem.TriggerMatches(Name, 1, -1)
        If TrigsToCheck IsNot Nothing AndAlso TrigsToCheck.Count > 0 Then
            For Each TC As TrigActInfo In TrigsToCheck
                'load the trigger data into the plugin's trigger class
                TpT = New Taken_Picture_Trigger(TC.UID, TC.evRef, TC.SubTANumber - 1, TC.DataIn)
                'if the selected camera refID matches the refID of the camera that took the picture then fire the trigger.
                If TpT.IsTrigger(RefID) Then
                    HomeSeerSystem.TriggerFire(Name, TC)
                End If
            Next
        End If
    End Sub

    Public Overloads Function GetJuiDeviceConfigPage(ByVal deviceRef As String) As String
        'this is called because we set a boolean flag for it (SupportsConfigDevice)
        'This builds a tab on the device specific to the plugin
        Dim Camera As CameraData
        Dim JUIPage As Page = Nothing
        Dim pageID As String = "deviceconfig-page1"
        Camera = GetCameraData(deviceRef)

        'create your page canvas
        JUIPage = PageFactory.CreateDeviceConfigPage(pageID, "Camera Settings").Page

        'For the LoadCameraData function to work correctly, the names of the input fields MUST correspond to the property names of your object.
        'If you wish to name them something different, the LoadCameraData will need a Select Case statement to match the input names to the properties.

        'add your control objects to your page canvas
        JUIPage.AddView(New InputView(pageID & "-Name", "Name", Camera.Name))
        JUIPage.AddView(New InputView(pageID & "-URL", "URL", Camera.URL))
        JUIPage.AddView(New InputView(pageID & "-MaxCount", "MaxCount", Camera.MaxCount, EInputType.Number))

        'this will build a JSON structure that HomeSeer will render in the same configuration as the rest of the application.
        Return JUIPage.ToJsonString
    End Function

    Protected Overrides Function OnDeviceConfigChange(deviceConfigPage As Page, deviceRef As Integer) As Boolean
        'This is called when data changes are being saved from your device config tab.
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

    Public Function GETPED() As Dictionary(Of Integer, Object) Implements Take_Picture_Action.IActionListener.GETPED, Taken_Picture_Trigger.ITriggerListener.GETPED
        'This is a function used by the listener classes in the action and trigger classes to interact with the HSPI class in a thread safe manner.
        Return HomeSeerSystem.GetPropertyByInterface(Id, EProperty.PlugExtraData, True)
    End Function

    Public Function HasDevices() As Boolean Implements Take_Picture_Action.IActionListener.HasDevices, Taken_Picture_Trigger.ITriggerListener.HasDevices
        'This is a function used by the listener classes in the action and trigger classes to interact with the HSPI class in a thread safe manner.
        Dim refIDs As List(Of Integer) = Nothing
        Dim bHasDevices As Boolean = False

        'Check for HomeSeer Devices
        refIDs = HomeSeerSystem.GetRefsByInterface(Id)
        If refIDs IsNot Nothing AndAlso refIDs.Count > 0 Then
            bHasDevices = True
        End If

        Return bHasDevices
    End Function

    Public Overrides Function PostBackProc(ByVal page As String, ByVal data As String, ByVal user As String, ByVal userRights As Integer) As String
        'This function is called when the AJAX postback is implemented and executed on your html feature pages.
        Dim response As String = ""

        'this determines which feature posted the request
        Select Case page.ToLower
            Case "addcameras.html"
                Try
                    'the property names of the cameradata class were used in the internalData variable for the AJAX postback.
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
                'the property names of the imagedata class were used in the internalData variable for the AJAX postback.
                Dim oImageData = JsonConvert.DeserializeObject(Of ImageData)(data)
                If oImageData.Image <> "" Then
                    Dim sImage As String = oImageData.Image
                    sImage = oImageData.Image
                    'strip off the html and keep just the image name
                    sImage = Strings.Right(sImage, sImage.Length - InStrRev(sImage, "/"))
                    sImage = FilePath & sImage
                    sImage = sImage.Replace("+", " ")
                    'delete image
                    File.Delete(sImage)
                    'we need to save the data in a global variable to be picked up on the page rebuild.
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
        'Use the device factory to create an area to hold the device data that is used to create the device
        df = Devices.DeviceFactory.CreateDevice(Id)
        'set the name of the device.
        df.WithName(Camera.Name)
        'set the type of the device.
        df.AsType(EDeviceType.Generic, 0)

        'this is the what you use to create feature(child) devices for you device group
        Dim ff As Devices.FeatureFactory

        'create a new feature data holder.
        ff = Devices.FeatureFactory.CreateFeature(Id)

        'add the properties for your feature.
        ff.WithName("Take Picture")
        ff.AddButton(1, "Take Picture")

        'Add the feature data to the device data in the device factory
        df.WithFeature(ff)

        'Put device specific data with the device. (this is the cameradata class)
        Dim PED As New Devices.PlugExtraData
        PED.AddNamed("data", Newtonsoft.Json.Linq.JObject.FromObject(Camera).ToString())
        df.WithExtraData(PED)

        'this bundles all the needed data from the device to send to HomeSeer.
        dd = df.PrepareForHs
        'this creates the device in HomeSeer using the bundled data.
        HomeSeerSystem.CreateDevice(dd)

        'check to see if we need to add additional pages now.
        LoadAdditionalPages()
    End Sub
End Class

