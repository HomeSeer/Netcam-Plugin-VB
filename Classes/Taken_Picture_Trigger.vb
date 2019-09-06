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

    'Camera list
    Private ReadOnly Property SelectListId1 As String
        Get
            Return $"{PageId}-selectlist1"
        End Get
    End Property

    'Error label
    Private ReadOnly Property ErrorId As String
        Get
            Return $"{PageId}-error"
        End Get
    End Property

    'Implementation of the listener interface.
    Private ReadOnly Property Listener As ITriggerListener
        Get
            Return TriggerListener
        End Get
    End Property

    'Interface for communicating with the HSPI class
    Interface ITriggerListener
        Inherits TriggerTypeCollection.ITriggerTypeListener
        Function GetCameraData(refID As String) As CameraData
        Function GETPED() As Dictionary(Of Integer, Object)
        Function HasDevices() As Boolean
    End Interface

    'The value a user sees in the action list
    Protected Overrides Function GetName() As String
        Return "A Picture Was Taken"
    End Function

    Public Overrides Function ReferencesDeviceOrFeature(devOrFeatRef As Integer) As Boolean
        Return False
    End Function

    Public Overrides Function IsTriggerTrue(isCondition As Boolean) As Boolean
        '???? What is this for ??????
        Return True
    End Function

    'This is a custom function.
    'This is called to determine if the device refID in the HSPI.SetIOMulti function matches the selected ref in this trigger
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

    'this is called whenever a trigger is to be displayed to the user - whether completed or not.
    Public Overrides Function IsFullyConfigured() As Boolean
        Dim Configured As Boolean = True
        Dim selectList As SelectListView

        If Listener.HasDevices Then
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
                    Case ErrorId
                        'check for cameras
                        OnNewTrigger()
                        Configured = False
                        Exit For
                End Select
            Next
        Else
            ConfigPage.RemoveAllViews()
            GenerateError("No Cameras were found.")
            Configured = False
        End If

        Return Configured
    End Function

    'This is called when the trigger has been selected.
    Protected Overrides Sub OnNewTrigger()
        Dim selectList As SelectListView
        Dim ListOptionNames As New List(Of String)
        Dim ListOptionRefIDs As New List(Of String)
        Dim arrCameras As Dictionary(Of Integer, Object)
        Dim refID As String
        Dim Camera As CameraData

        arrCameras = Listener.GETPED()
        Try
            For Each kvp As KeyValuePair(Of Integer, Object) In arrCameras
                refID = kvp.Key
                Camera = Listener.GetCameraData(kvp.Value)
                ListOptionNames.Add(Camera.Name)
                ListOptionRefIDs.Add(refID.ToString)
            Next
            selectList = New SelectListView(SelectListId1, "With Camera:", ListOptionNames, ListOptionRefIDs)
            If ConfigPage.Views.Count > 0 Then ConfigPage.RemoveAllViews()
            ConfigPage.AddView(selectList)
        Catch
            GenerateError("No Cameras were found.")
        End Try
    End Sub

    'No additional logic needed for the update to the view so just return true
    Protected Overrides Function OnConfigItemUpdate(configViewChange As AbstractView) As Boolean
        Return True
    End Function

    'This is called when IsFullyConfigured returns True
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

    Public Sub GenerateError(msg As String)
        Dim lblError As LabelView = New LabelView(ErrorId, "Error", msg)
        Dim Views As New List(Of AbstractView)
        'remove the old message
        Try
            ConfigPage.RemoveViewById(ErrorId)
        Catch
        End Try
        'start the new list with the error message
        Views.Add(lblError)
        'Add in the rest of the views (if there are any)
        For Each view As AbstractView In ConfigPage.Views
            Views.Add(view)
        Next
        'reset the views
        ConfigPage.SetViews(Views)
    End Sub
End Class