Imports Newtonsoft.Json
Imports HomeSeer.Jui.Views
Imports HomeSeer.Jui.Types
Imports HomeSeer.PluginSdk.Events
Imports HomeSeer.PluginSdk
Imports System.Text
Imports System.IO
Imports System.Reflection
Imports HomeSeer.PluginSdk.Devices
Imports HomeSeer.PluginSdk.Devices.Identification
Public Module Classes
    Public Class JUIPageData
        Public Property pageID As String
        Public Property pageName As String

        Public Property pageType As EPageType
        Dim Views As New List(Of DeviceConfigView)

        Public Sub AddView(ByVal name As String, ByVal value As Object, ByVal type As HomeSeer.Jui.Types.EViewType, Optional subtype As EInputType = EInputType.Text)
            Dim dcv As New DeviceConfigView
            dcv.Name = name
            dcv.Value = value
            dcv.Type = type
            dcv.SubType = subtype
            Views.Add(dcv)
        End Sub

        Public Function BuildHTML() As String
            Dim sHTML As String
            sHTML = BuildPage.ToHtml
            Return sHTML
        End Function

        Public Function BuildJSON() As String
            Dim sJSON As String
            sJSON = BuildPage.ToJsonString
            Return sJSON
        End Function

        Function BuildPage() As Page
            Dim JUIPage As Page = Nothing
            Dim juiLabel As LabelView
            Dim juiInput As InputView
            Dim juiSelect As SelectListView

            Select Case pageType
                Case EPageType.Settings
                    JUIPage = PageFactory.CreateSettingsPage(pageID, pageName).Page
                Case EPageType.DeviceConfig
                    JUIPage = PageFactory.CreateDeviceConfigPage(pageID, pageName).Page
                Case EPageType.Generic
                    JUIPage = PageFactory.CreateDeviceConfigPage(pageID, pageName).Page
            End Select

            For Each View As DeviceConfigView In Views
                Select Case View.Type
                    Case EViewType.Label
                        juiLabel = New LabelView(pageID & "-" & View.Name, Nothing, View.Value)
                        JUIPage.AddView(juiLabel)
                    Case EViewType.Input
                        juiInput = New InputView(pageID & "-" & View.Name, View.Name, View.Value, View.SubType)
                        JUIPage.AddView(juiInput)
                    Case EViewType.SelectList
                        Dim SelectOptions As Dictionary(Of String, String)
                        SelectOptions = View.Value
                        juiSelect = New SelectListView(pageID & "-" & View.Name, View.Name, SelectOptions.Keys.ToList, SelectOptions.Values.ToList)
                        JUIPage.AddView(juiSelect)
                End Select
            Next
            Return JUIPage
        End Function

        Class DeviceConfigView
            Public Property Name As String
            Public Property Value As Object = ""
            Public Property Type As EViewType
            Public Property SubType As EInputType
        End Class
    End Class

    <JsonObject>
    Public Class CameraData
        <JsonProperty("Name")>
        Public Property Name As String
        <JsonProperty("URL")>
        Public Property URL As String

        <JsonProperty("MaxCount")>
        Public Property MaxCount As Integer = 30
    End Class

    <JsonObject>
    Public Class ImageData
        Public Property Image As String
        Public Property Tab As Integer
    End Class

    <Serializable()>
    Public Class hsCollection
        Inherits Dictionary(Of String, Object)
        Dim KeyIndex As New Collection

        Public Sub New()
            MyBase.New()
        End Sub

        Protected Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub

        Public Overloads Sub Add(value As Object, Key As String)
            If Not MyBase.ContainsKey(Key) Then
                MyBase.Add(Key, value)
                KeyIndex.Add(Key, Key)
            Else
                MyBase.Item(Key) = value
            End If
        End Sub

        Public Overloads Sub Remove(Key As String)
            On Error Resume Next
            MyBase.Remove(Key)
            KeyIndex.Remove(Key)
        End Sub

        Public Overloads Sub Remove(Index As Integer)
            MyBase.Remove(KeyIndex(Index))
            KeyIndex.Remove(Index)
        End Sub

        Public Overloads ReadOnly Property Keys(ByVal index As Integer) As Object
            Get
                Dim i As Integer
                Dim key As String = Nothing
                For Each key In MyBase.Keys
                    If i = index Then
                        Exit For
                    Else
                        i += 1
                    End If
                Next
                Return key
            End Get
        End Property

        Default Public Overloads Property Item(ByVal index As Integer) As Object
            Get
                Return MyBase.Item(KeyIndex(index))
            End Get
            Set(ByVal value As Object)
                MyBase.Item(KeyIndex(index)) = value
            End Set
        End Property

        Default Public Overloads Property Item(ByVal Key As String) As Object
            Get
                On Error Resume Next
                Return MyBase.Item(Key)
            End Get
            Set(ByVal value As Object)
                If Not MyBase.ContainsKey(Key) Then
                    Add(value, Key)
                Else
                    MyBase.Item(Key) = value
                End If
            End Set
        End Property
    End Class

    <Serializable()>
    Public Class action
        Inherits hsCollection
        Public Sub New()
            MyBase.New()
        End Sub
        Protected Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    <Serializable()>
    Public Class trigger
        Inherits hsCollection

        Public Sub New()
            MyBase.New()
        End Sub
        Protected Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Public Class Take_Picture_Action
        Inherits AbstractActionType
        Public Sub New()
            MyBase.New
        End Sub

        Public Sub New(ByVal id As Integer, ByVal eventRef As Integer, ByVal dataIn As Byte())
            MyBase.New(id, eventRef, dataIn)
        End Sub

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub

        Private Const ActionName As String = "Take a Picture"
        Private ReadOnly Property SelectListId1 As String
            Get
                Return $"{PageId}-selectlist1"
            End Get
        End Property

        Private ReadOnly Property InputId1 As String
            Get
                Return $"{PageId}-input1"
            End Get
        End Property

        Private ReadOnly Property InputId2 As String
            Get
                Return $"{PageId}-input2"
            End Get
        End Property

        Private ReadOnly Property Listener As ActionTypeCollection.IActionTypeListener
            Get
                Return ActionListener
            End Get
        End Property

        Protected Overrides Function GetName() As String
            Return ActionName
        End Function
        Public Overrides Function OnRunAction() As Boolean
            Dim Camera As CameraData
            Dim CamRef As String = ""
            Dim Images As Integer = 1
            Dim Interval As Single = 0.01
            Dim selectList As SelectListView
            Dim oInputView As InputView
            Dim Input2Value As String = ""

            For Each view As AbstractView In ConfigPage.Views
                Select Case view.Id
                    Case SelectListId1
                        selectList = TryCast(view, SelectListView)
                        CamRef = selectList.OptionKeys(selectList.Selection)
                    Case InputId1
                        oInputView = TryCast(view, InputView)
                        Images = oInputView.Value
                    Case InputId2
                        oInputView = TryCast(view, InputView)
                        Interval = oInputView.Value
                End Select
            Next

            Try
                Camera = _plugin.GetCameraData(CamRef.ToString)
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

        Public Overrides Function ReferencesDeviceOrFeature(ByVal devOrFeatRef As Integer) As Boolean
            Return False
        End Function

        Public Overrides Function IsFullyConfigured() As Boolean
            Dim Configured As Boolean = True
            Dim selectList As SelectListView
            Dim oInputView1 As InputView
            Dim oInputView2 As InputView

            For Each view As AbstractView In ConfigPage.Views
                Select Case view.Id
                    Case SelectListId1
                        selectList = TryCast(view, SelectListView)
                        'nothing selected? return false!!
                        If selectList?.GetSelectedOption() = "" Then
                            Configured = False
                            Exit For
                        End If
                    Case InputId1
                        oInputView1 = TryCast(view, InputView)
                        'no number of images? return false!!
                        If oInputView1?.Value Is Nothing OrElse oInputView1?.Value.Length = 0 Then
                            Configured = False
                            Exit For
                            'they put in less than 1? return false!!!
                        ElseIf oInputView1?.Value < 1 Then
                            Configured = False
                            Exit For
                        End If
                    Case InputId2
                        oInputView1 = TryCast(ConfigPage.GetViewById(InputId1), InputView)
                        oInputView2 = TryCast(view, InputView)
                        'no number of images? return false!!
                        If oInputView1?.Value Is Nothing OrElse oInputView1?.Value.Length = 0 Then
                            Configured = False
                            Exit For
                            'number of images less than 2? return false!!!
                        ElseIf oInputView1?.Value < 2 Then
                            Configured = False
                            Exit For
                            'no interval for an image count greater than 1? return false!!!!
                        ElseIf oInputView2?.Value Is Nothing OrElse oInputView2?.Value.Length = 0 Then
                            Configured = False
                            Exit For
                            'they put in a number less than 1? return false!!!!!
                        ElseIf oInputView2?.Value < 1 Then
                            Configured = False
                            Exit For
                        End If
                End Select
            Next

            Return Configured
        End Function

        Protected Overrides Sub OnNewAction()
            Dim selectList As SelectListView
            Dim SelectListOptions As New Dictionary(Of String, String)
            Dim oInputView As InputView
            Dim arrCameras As Dictionary(Of Integer, Object)
            Dim refID As String
            Dim Camera As CameraData

            arrCameras = _plugin.GETPED

            For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
                refID = kvp.Key
                Camera = _plugin.GetCameraData(kvp.Value)
                SelectListOptions.Add(Camera.Name, refID.ToString)
            Next
            selectList = New SelectListView(PageId & "-" & SelectListId1, "Cameras", SelectListOptions.Values.ToList, SelectListOptions.Keys.ToList)
            ConfigPage.AddView(selectList)
            oInputView = New InputView(InputId1, "Number of Pictures", EInputType.Number)
            ConfigPage.AddView(oInputView)
        End Sub

        Protected Overrides Sub OnEditAction(ByVal viewChanges As Page)
            'Make a list of out current views
            Dim Views As New Dictionary(Of String, AbstractView)
            For Each view As AbstractView In ConfigPage.Views
                Views.Add(view.Id, view)
            Next

            For Each changedView In viewChanges.Views

                If Not ConfigPage.ContainsViewWithId(changedView.Id) Then
                    Continue For
                End If

                ConfigPage.UpdateViewValueById(changedView.Id, changedView.GetStringValue)

                Select Case changedView.Id
                    Case InputId1
                        Dim InputID1Value As String
                        InputID1Value = changedView.GetStringValue
                        If IsNumeric(InputID1Value) AndAlso InputID1Value > 1 Then
                            'check to see if we already have our interval input box.
                            'if we don't then add it.
                            If Not Views.Keys.Contains(InputId2) Then
                                Dim inputview = New InputView(InputId2, "Interval")
                                ConfigPage.AddView(inputview)
                            End If
                        Else
                            If Views.Keys.Contains(InputId2) Then
                                'remove the interval input box.
                                Views.Remove(InputId2)
                                'reset the views
                                ConfigPage.SetViews(Views.Values)
                            End If
                        End If
                End Select
            Next
        End Sub

        Public Overrides Function GetPrettyString() As String
            Dim PrettyString As String = "Use the [Camera] camera to take [PictureCount] picture[ClosingText]"
            Dim Camera As String = ""
            Dim PictureCount As String = ""
            Dim Interval As String = ""
            Dim ClosingText As String = ""
            Dim selectList As SelectListView
            Dim oInputView As InputView

            For Each view As AbstractView In ConfigPage.Views
                Select Case view.Id
                    Case PageId & "-" & SelectListId1
                        selectList = TryCast(view, SelectListView)
                        Camera = selectList.GetSelectedOption
                    Case InputId1
                        oInputView = TryCast(view, InputView)
                        PictureCount = oInputView.GetStringValue
                    Case InputId2
                        oInputView = TryCast(view, InputView)
                        Interval = oInputView.GetStringValue
                End Select
            Next
            'Figure out how it should be phrased.
            'Start with the camera.
            PrettyString = PrettyString.Replace("[Camera]", Camera)
            'Next is the count of pictures to be taken.
            PrettyString = PrettyString.Replace("[PictureCount]", PictureCount)
            If PictureCount = 1 Then
                ClosingText = "."
            Else
                ClosingText = "s, once every " & Interval & " seconds."
            End If
            PrettyString = PrettyString.Replace("[ClosingText]", ClosingText)
            Return PrettyString
        End Function
    End Class

    Public Class Taken_Picture_Trigger
        Inherits AbstractTriggerType

        Public Sub New()
            MyBase.New
        End Sub

        Public Sub New(ByVal id As Integer, ByVal eventRef As Integer, ByVal dataIn As Byte())
            MyBase.New(id, eventRef, dataIn)
        End Sub

        Protected Overrides Sub Finalize()
            MyBase.Finalize()
        End Sub

        Private Const TriggerName As String = "Taken a Picture"
        Private ReadOnly Property SelectListId1 As String
            Get
                Return $"{PageId}-selectlist1"
            End Get
        End Property

        Protected Overrides Sub OnNewTrigger()
            Dim selectList As SelectListView
            Dim SelectListOptions As New Dictionary(Of String, String)
            Dim arrCameras As Dictionary(Of Integer, Object)
            Dim refID As String
            Dim Camera As CameraData

            arrCameras = _plugin.GETPED

            For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
                refID = kvp.Key
                Camera = _plugin.GetCameraData(kvp.Value)
                SelectListOptions.Add(Camera.Name, refID.ToString)
            Next
            selectList = New SelectListView(PageId & "-" & SelectListId1, "Cameras", SelectListOptions.Values.ToList, SelectListOptions.Keys.ToList)
            ConfigPage.AddView(selectList)
        End Sub

        Public Overrides Function IsTriggerTrue(isCondition As Boolean) As Boolean
            '???? What is this for ??????
            Return True
        End Function

        Public Overrides Function IsFullyConfigured() As Boolean
            Dim Configured As Boolean = True
            Dim selectList As SelectListView

            For Each view As AbstractView In ConfigPage.Views
                Select Case view.Id
                    Case SelectListId1
                        selectList = TryCast(view, SelectListView)
                        'nothing selected? return false!!
                        If selectList?.GetSelectedOption() = "" Then
                            Configured = False
                            Exit For
                        End If
                End Select
            Next

            Return Configured
        End Function

        Public Overrides Function GetPrettyString() As String
            Dim PrettyString As String = "The [Camera] camera was used to take a picture."
            Dim Camera As String = ""
            Dim selectList As SelectListView

            For Each view As AbstractView In ConfigPage.Views
                Select Case view.Id
                    Case PageId & "-" & SelectListId1
                        selectList = TryCast(view, SelectListView)
                        Camera = selectList.GetSelectedOption
                End Select
            Next
            'Figure out how it should be phrased.
            'Start with the camera.
            PrettyString = PrettyString.Replace("[Camera]", Camera)
            Return PrettyString
        End Function

        Public Overrides Function ReferencesDeviceOrFeature(devOrFeatRef As Integer) As Boolean
            Return False
        End Function

        Protected Overrides Function OnEditTrigger(viewChanges As Page) As Boolean
            For Each changedView In viewChanges.Views

                If Not ConfigPage.ContainsViewWithId(changedView.Id) Then
                    Continue For
                End If

                ConfigPage.UpdateViewValueById(changedView.Id, changedView.GetStringValue)
            Next
            Return True
        End Function

        Protected Overrides Function GetName() As String
            Return TriggerName
        End Function
    End Class
End Module


