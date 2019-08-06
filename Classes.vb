Imports Newtonsoft.Json
Imports HomeSeer.Jui.Views
Public Module Classes
    Public Class jqDropList

    End Class

    Public Enum DeviceConfigViewType
        [Label]
        [Text]
        [Button]
    End Enum

    Public Class DeviceConfigPageData
        Public Property pageID As String
        Public Property pageName As String
        Dim Views As New List(Of DeviceConfigView)

        Public Sub AddView(ByVal name As String, ByVal value As String, ByVal type As DeviceConfigViewType)
            Dim dcv As New DeviceConfigView
            dcv.Name = name
            dcv.Value = value
            dcv.Type = type
            Views.Add(dcv)
        End Sub

        Public Function BuildJSON() As String
            Dim ConfigPage = Page.Factory.CreateDeviceConfigPage(pageID, "Camera Settings")
            Dim juiLabel As LabelView
            Dim juiInput As InputView
            Dim juiButton As ButtonView
            Dim sJSON As String

            For Each View As DeviceConfigView In Views
                Select Case View.Type
                    Case DeviceConfigViewType.Label
                        juiLabel = New LabelView(pageID & "-" & View.Name, Nothing, View.Value)
                        ConfigPage.AddView(juiLabel)
                    Case DeviceConfigViewType.Text
                        juiInput = New InputView(pageID & "-" & View.Name, View.Value, HomeSeer.Jui.Types.EInputType.Text)
                        ConfigPage.AddView(juiInput)
                    Case DeviceConfigViewType.Button
                        juiButton = New ButtonView(pageID & "-" & View.Name, View.Value, View.Value)
                        ConfigPage.AddView(juiButton)
                End Select
            Next
            sJSON = ConfigPage.ToJsonString
            Return sJSON
        End Function

        Class DeviceConfigView
            Public Property Name As String
            Public Property Value As String
            Public Property Type As DeviceConfigViewType
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
End Module


