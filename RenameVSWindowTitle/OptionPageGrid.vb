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

    Private _AlwaysRewriteTitles As Boolean = True
    <Category("General")>
    <DisplayName("Always rewrite titles")>
    <Description("If set to true, will rewrite titles even if no conflict is detected. Default: true. Conflicts may not be detected if patterns have been changed from the default.")>
    Public Property AlwaysRewriteTitles() As Boolean
        Get
            Return Me._AlwaysRewriteTitles
        End Get
        Set(ByVal value As Boolean)
            Me._AlwaysRewriteTitles = value
        End Set
    End Property

    Private _MinNumberOfInstances As Integer = 1
    <Category("General")>
    <DisplayName("Activation threshold")>
    <Description("Min number of instances that need to be opened to rewrite titles, if they are in conflict or if ""Always rewrite titles"" is set to true. Default: 1.")>
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
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]; [configurationName] (e.g. Release); [platformName] (e.g. x86). Default: [ideName].")>
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
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]; [configurationName] (e.g. Release); [platformName] (e.g. x86). Default: [documentName] - [ideName].")>
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
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]; [configurationName] (e.g. Release); [platformName] (e.g. x86). Default: [parentPath]\[solutionName] - [ideName].")>
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
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]; [configurationName] (e.g. Release); [platformName] (e.g. x86). Default: [parentPath]\[solutionName] (Debugging) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved.")>
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
    <Description("Special tags: [documentName] name of the active document or window; [solutionName] name of the active solution; [parentX] parent folder at the specified depth X, e.g. 1 for document/solution file parent folder; [parentPath] current solution path or, if no solution open, document path, with depth range as set in settings; [ideName]; [configurationName] (e.g. Release); [platformName] (e.g. x86). Default: [parentPath]\[solutionName] (Running) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved.")>
    Public Property PatternIfRunningMode() As String
        Get
            Return Me._PatternIfRunningMode
        End Get
        Set(ByVal value As String)
            Me._PatternIfRunningMode = value
        End Set
    End Property

    Private _AppendedString As String = "*"
    <Category("Patterns")>
    <DisplayName("Appended string")>
    <Description("String to be added at the end of the title to identify that it has been rewritten. If not default and Always rewrite titles, the detection of concurrent instances with the same title may not work. Default: '*'.")>
    Public Property AppendedString() As String
        Get
            Return Me._AppendedString
        End Get
        Set(ByVal value As String)
            Me._AppendedString = value
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
