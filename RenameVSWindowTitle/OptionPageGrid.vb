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
    <Category("General")>
    <DisplayName("Only rewrite title if conflict")>
    <Description("If two instances do not have the same solution/project file name (e.g. mysolution.sln and mysolution2.sln), and this option is set to false, their title will not be rewritten. It is possible that a conflict can still be detected if patterns have been customized.")>
    Public Property RewriteOnlyIfConflict() As Boolean
        Get
            Return Me._RewriteOnlyIfConflict
        End Get
        Set(ByVal value As Boolean)
            Me._RewriteOnlyIfConflict = value
        End Set
    End Property

    Private _MinNumberOfInstances As Integer = 2
    <Category("General")>
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
    <Category("General")>
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
    <Category("General")>
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

    Private _PatternIfNothingOpen As String = "[ideName]"
    <Category("Patterns")>
    <DisplayName("No document or solution open")>
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]. Default: [ideName].")>
    Public Property PatternIfNothingOpen() As String
        Get
            Return Me._PatternIfNothingOpen
        End Get
        Set(ByVal value As String)
            Me._PatternIfNothingOpen = value
        End Set
    End Property

    Private _PatternIfDocumentButNoSolutionOpen As String = "[documentName] - [ideName]"
    <Category("Patterns")>
    <DisplayName("Document (no solution) open")>
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]. Default: [documentName] - [ideName].")>
    Public Property PatternIfDocumentButNoSolutionOpen() As String
        Get
            Return Me._PatternIfDocumentButNoSolutionOpen
        End Get
        Set(ByVal value As String)
            Me._PatternIfDocumentButNoSolutionOpen = value
        End Set
    End Property

    Private _PatternIfDesignMode As String = "[parentPath]\[solutionName] - [ideName]"
    <Category("Patterns")>
    <DisplayName("Solution in design mode")>
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]. Default: [parentPath]\[solutionName] - [ideName].")>
    Public Property PatternIfDesignMode() As String
        Get
            Return Me._PatternIfDesignMode
        End Get
        Set(ByVal value As String)
            Me._PatternIfDesignMode = value
        End Set
    End Property

    Private _PatternIfBreakMode As String = "[parentPath]\[solutionName] (Debugging) - [ideName]"
    <Category("Patterns")>
    <DisplayName("Solution in break mode")>
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]. Default: [parentPath]\[solutionName] (Debugging) - [ideName]. ' *' will be added at the end automatically to identify that the title is being improved.")>
    Public Property PatternIfBreakMode() As String
        Get
            Return Me._PatternIfBreakMode
        End Get
        Set(ByVal value As String)
            Me._PatternIfBreakMode = value
        End Set
    End Property

    Private _PatternIfRunningMode As String = "[parentPath]\[solutionName] (Running) - [ideName]"
    <Category("Patterns")>
    <DisplayName("Solution in running mode")>
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]. Default: [parentPath]\[solutionName] (Running) - [ideName]. ' *' will be added at the end automatically to identify that the title is being improved.")>
    Public Property PatternIfRunningMode() As String
        Get
            Return Me._PatternIfRunningMode
        End Get
        Set(ByVal value As String)
            Me._PatternIfRunningMode = value
        End Set
    End Property

    Private _EnableDebugMode As Boolean = False
    <Category("Debug")>
    <DisplayName("Enable debug mode")>
    <Description("Set to true to activate debug output to Output window.")>
    Public Property EnableDebugMode() As Boolean
        Get
            Return Me._EnableDebugMode
        End Get
        Set(ByVal value As Boolean)
            Me._EnableDebugMode = value
        End Set
    End Property
End Class
