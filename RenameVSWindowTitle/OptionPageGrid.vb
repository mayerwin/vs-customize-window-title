Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.ComponentModel.Design
Imports Microsoft.Win32
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports System.Threading
Imports System.Text.RegularExpressions
Imports System.ComponentModel

<ClassInterface(ClassInterfaceType.AutoDual)>
<CLSCompliant(False), ComVisible(True)>
Public Class OptionPageGrid
    Inherits DialogPage

    Private _RewriteOnlyIfConflict As Boolean = False
    <Category("Rename VS Window Title")>
    <DisplayName("Only rewrite title if conflict")>
    <Description("If two instances do not have the same solution/project file name (e.g. mysolution.sln and mysolution2.sln), and this option is set to false, their title will not be rewritten.")>
    Public Property RewriteOnlyIfConflict() As Boolean
        Get
            Return Me._RewriteOnlyIfConflict
        End Get
        Set(ByVal value As Boolean)
            Me._RewriteOnlyIfConflict = value
        End Set
    End Property

    Private _MinNumberOfInstances As Integer = 2
    <Category("Rename VS Window Title")>
    <DisplayName("Activation threshold")>
    <Description("Min number of instances to activate extension. Default: 2.")>
    Public Property MinNumberOfInstances() As Integer
        Get
            Return Me._MinNumberOfInstances
        End Get
        Set(ByVal value As Integer)
            Me._MinNumberOfInstances = value
        End Set
    End Property

    Private _FarthestParentDepth As Integer = 1
    <Category("Rename VS Window Title")>
    <DisplayName("Farthest parent folder depth")>
    <Description("Distance of the farthest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")>
    Public Property FarthestParentDepth() As Integer
        Get
            Return Me._FarthestParentDepth
        End Get
        Set(ByVal value As Integer)
            Me._FarthestParentDepth = value
        End Set
    End Property

    Private _ClosestParentDepth As Integer = 1
    <Category("Rename VS Window Title")>
    <DisplayName("Closest parent folder depth")>
    <Description("Distance of the closest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")>
    Public Property ClosestParentDepth() As Integer
        Get
            Return Me._ClosestParentDepth
        End Get
        Set(ByVal value As Integer)
            Me._ClosestParentDepth = value
        End Set
    End Property
End Class
