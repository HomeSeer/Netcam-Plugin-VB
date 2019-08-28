Imports HomeSeer.Jui.Views
Imports HomeSeer.Jui.Types
Imports HomeSeer.PluginSdk.Events

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
        Dim ListOptionNames As New List(Of String)
        Dim ListOptionRefIDs As New List(Of String)
        Dim arrCameras As Dictionary(Of Integer, Object)
        Dim refID As String
        Dim Camera As CameraData

        arrCameras = _plugin.GETPED

        For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
            refID = kvp.Key
            Camera = _plugin.GetCameraData(kvp.Value)
            ListOptionNames.Add(Camera.Name)
            ListOptionRefIDs.Add(refID.ToString)
        Next
        selectList = New SelectListView(SelectListId1, "Cameras", ListOptionNames, ListOptionRefIDs)
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