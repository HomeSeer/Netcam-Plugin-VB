Imports Newtonsoft.Json
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

'Everything in this module is specific to the NetCam plugin.

Public Module GeneralClasses
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

    Public Class HSTimer
        Inherits Timers.Timer
        Public Property RefIDs As New SortedList(Of String, DateTime)
    End Class
End Module


