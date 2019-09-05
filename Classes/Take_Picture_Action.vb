Imports HomeSeer.Jui.Views
Imports HomeSeer.Jui.Types
Imports HomeSeer.PluginSdk.Events
Imports HomeSeer.PluginSdk

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

    Private ReadOnly Property ErrorId As String
        Get
            Return $"{PageId}-error"
        End Get
    End Property

    Private ReadOnly Property Listener As IActionListener
        Get
            Return ActionListener
        End Get
    End Property

    Interface IActionListener
        Inherits ActionTypeCollection.IActionTypeListener
        Function GETPED() As Dictionary(Of Integer, Object)
    End Interface

    Protected Overrides Function GetName() As String
        Return "Take a Picture"
    End Function

    Public Overrides Function OnRunAction() As Boolean
        Dim Camera As CameraData
        Dim CamRef As String = ""
        Dim Images As Integer = 1
        Dim Interval As Single = 0.01
        Dim selectList As SelectListView
        Dim oInputView As InputView

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

    Protected Overrides Function OnConfigItemUpdate(configViewChange As AbstractView) As Boolean
        Select Case configViewChange.Id
            Case InputId1
                'Make a list of our current views.
                Dim Views As New Dictionary(Of String, AbstractView)
                For Each view As AbstractView In ConfigPage.Views
                    Views.Add(view.Id, view)
                Next
                Dim InputID1Value As String
                InputID1Value = configViewChange.GetStringValue
                If IsNumeric(InputID1Value) AndAlso InputID1Value > 1 Then
                    'check to see if we already have our interval input box.
                    'if we don't then add it.
                    If Not Views.Keys.Contains(InputId2) Then
                        Dim oInputView = New InputView(InputId2, "Wait Time Between Pictures (In Seconds)")
                        ConfigPage.AddView(oInputView)
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
        Return True
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
                Case ErrorId
                    'check for cameras
                    OnNewAction()
                    Configured = False
                    Exit For
            End Select
        Next

        Return Configured
    End Function

    Protected Overrides Sub OnNewAction()
        Dim selectList As SelectListView
        Dim ListOptionNames As New List(Of String)
        Dim ListOptionRefIDs As New List(Of String)
        Dim oInputView As InputView
        Dim arrCameras As Dictionary(Of Integer, Object)
        Dim refID As String
        Dim Camera As CameraData

        arrCameras = _plugin.GETPED()

        Try
            For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
                refID = kvp.Key
                Camera = _plugin.GetCameraData(kvp.Value)
                ListOptionNames.Add(Camera.Name)
                ListOptionRefIDs.Add(refID.ToString)
            Next
            selectList = New SelectListView(SelectListId1, "With Camera:", ListOptionNames, ListOptionRefIDs)
            If ConfigPage.Views.Count > 0 Then ConfigPage.RemoveAllViews()
            ConfigPage.AddView(selectList)
            oInputView = New InputView(InputId1, "Number of Pictures")
            ConfigPage.AddView(oInputView)
        Catch
            GenerateError("No Cameras were found.")
        End Try
    End Sub

    Public Sub GenerateError(msg As String)
        Dim lblError As LabelView = New LabelView(ErrorId, "Error", msg)
        Dim Views As New List(Of AbstractView)
        'remove the old message
        ConfigPage.RemoveViewById(ErrorId)

        'start the new list with the error message
        Views.Add(lblError)
        'Add in the rest of the views (if there are any)
        For Each view As AbstractView In ConfigPage.Views
            Views.Add(view)
        Next
        'reset the views
        ConfigPage.SetViews(Views)

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
                Case SelectListId1
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

