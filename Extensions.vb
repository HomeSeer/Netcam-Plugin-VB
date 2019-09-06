Imports System.Runtime.CompilerServices
Imports System.Text
'Extensions must be in a module
Module Extensions
    'this adds an additional property to the stringbuilder class that includes a line break when adding to the string builder.
    <Extension()>
    Public Sub AppendHTML(ByRef sb As StringBuilder, ByVal item As String)
        sb.Append(item)
        sb.Append(Environment.NewLine)
    End Sub
End Module
