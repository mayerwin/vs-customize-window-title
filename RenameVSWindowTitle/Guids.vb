Imports System

Class GuidList
    Private Sub New()
    End Sub

    Public Const guidRenameVSWindowTitle3PkgString As String = "5126c493-138a-46d7-a04d-ad772f6be159"
    Public Const guidRenameVSWindowTitle3CmdSetString As String = "939a4ccc-55d2-4f90-8858-b7fce11bb09c"

    Public Shared ReadOnly guidRenameVSWindowTitle3CmdSet As New Guid(guidRenameVSWindowTitle3CmdSetString)
End Class