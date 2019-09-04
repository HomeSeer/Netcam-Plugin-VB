Imports HomeSeer.Jui.Views
Imports HomeSeer.Jui.Types
Imports HomeSeer.PluginSdk.Events

Public Class Taken_Picture_Trigger
    Inherits AbstractTriggerType

    Public Sub New()
        MyBase.New
    End Sub

    Public Sub New(ByVal id As Integer, ByVal eventRef As Integer, ByVal selectedSubTriggerIndex As Integer, ByVal dataIn As Byte())
        MyBase.New(id, eventRef, selectedSubTriggerIndex, dataIn)
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private ReadOnly Property Listener As IPictureTakenTriggerListener
        Get
            Return TriggerListener
        End Get
    End Property

    Private ReadOnly Property SelectListId1 As String
        Get
            Return $"{PageId}-selectlist1"
        End Get
    End Property

    Protected Overrides Sub OnNewTrigger()
        Dim selectList As SelectListView
        Dim ListOptionNames As New List(Of String)
        Dim ListOptionRefIDs As New List(Of String)
        Dim arrCameras As Dictionary(Of Integer, Object)
        Dim refID As String
        Dim Camera As CameraData

        arrCameras = Listener.GETPED()

        For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
            refID = kvp.Key
            Camera = _plugin.GetCameraData(kvp.Value)
            ListOptionNames.Add(Camera.Name)
            ListOptionRefIDs.Add(refID.ToString)
        Next
        'selectList = New SelectListView(SelectListId1, "Cameras", ListOptionNames, ListOptionRefIDs)
        Dim pf As PageFactory = PageFactory.CreateEventTriggerPage(PageId, "Taken a Picture").WithDropDownSelectList(SelectListId1, "Cameras", ListOptionNames, ListOptionRefIDs)
        'ConfigPage.AddView(selectList)
        ConfigPage = pf.Page
    End Sub

    Public Overrides Function IsTriggerTrue(isCondition As Boolean) As Boolean
        '???? What is this for ??????
        Return True
    End Function

    Public Function IsTrigger(refID As Integer) As Boolean
        Dim CorrectTrigger As Boolean = False
        Dim selectList As SelectListView

        For Each view As AbstractView In ConfigPage.Views
            Select Case view.Id
                Case SelectListId1
                    selectList = TryCast(view, SelectListView)
                    If selectList.OptionKeys(selectList.Selection) = refID Then
                        CorrectTrigger = True
                        Exit For
                    End If
            End Select
        Next

        Return CorrectTrigger
    End Function

    Protected Overrides Function OnConfigItemUpdate(configViewChange As AbstractView) As Boolean
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
                        'Console.WriteLine("Select List is Nothing")
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
                Case SelectListId1
                    selectList = TryCast(view, SelectListView)
                    Camera = selectList.GetSelectedOption
            End Select
        Next
        'Put the camera name in the output phrase
        PrettyString = PrettyString.Replace("[Camera]", Camera)
        Return PrettyString
    End Function

    Public Overrides Function ReferencesDeviceOrFeature(devOrFeatRef As Integer) As Boolean
        Return False
    End Function

    Protected Overrides Function GetName() As String
        Return "Taken a Picture"
    End Function

    Interface IPictureTakenTriggerListener
        Inherits TriggerTypeCollection.ITriggerTypeListener

        Function GETPED() As Dictionary(Of Integer, Object)
    End Interface
End Class