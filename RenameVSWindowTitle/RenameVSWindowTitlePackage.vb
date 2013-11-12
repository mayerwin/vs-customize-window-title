Imports System.Runtime.InteropServices
Imports EnvDTE
Imports EnvDTE80
Imports System.IO
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio
Imports System.Threading
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' This is the class that implements the package exposed by this assembly.
'''
''' The minimum requirement for a class to be considered a valid package for Visual Studio
''' is to implement the IVsPackage interface and register itself with the shell.
''' This package uses the helper classes defined inside the Managed Package Framework (MPF)
''' to do it: it derives from the Package class that provides the implementation of the 
''' IVsPackage interface and uses the registration attributes defined in the framework to 
''' register itself and its components with the shell.
''' </summary>
' The PackageRegistration attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class
' is a package.
'
' The InstalledProductRegistration attribute is used to register the information needed to show this package
' in the Help/About dialog of Visual Studio.

<PackageRegistration(UseManagedResourcesOnly:=True),
    InstalledProductRegistration("#110", "#112", "1.0", IconResourceID:=400),
    ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string),
    ProvideMenuResource("Menus.ctmenu", 1),
    Guid(GuidList.guidRenameVSWindowTitle3PkgString)>
<ProvideOptionPage(GetType(OptionPageGrid),
                   "Rename VS Window Title", "Rules", 0, 0, True)>
Public NotInheritable Class RenameVSWindowTitle
    Inherits Package

    'Private dte As EnvDTE.DTE
    Private ReadOnly DTE As DTE2
    Private ReadOnly _events As EnvDTE.Events
    Private ReadOnly _debuggerEvents As DebuggerEvents
    Private ReadOnly _solutionEvents As SolutionEvents
    Private ReadOnly _windowEvents As WindowEvents
    Private ReadOnly _documentEvents As DocumentEvents

    Private IDEName As String

    Private ResetTitleTimer As System.Windows.Forms.Timer

    ''' <summary>
    ''' Default constructor of the package.
    ''' Inside this method you can place any initialization code that does not require 
    ''' any Visual Studio service because at this point the package object is created but 
    ''' not sited yet inside Visual Studio environment. The place to do all the other 
    ''' initialization is the Initialize method.
    ''' </summary>
    Public Sub New()
        Me.DTE = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE80.DTE2)
        Me._events = Me.DTE.Events
        Me._debuggerEvents = _events.DebuggerEvents
        Me._solutionEvents = _events.SolutionEvents
        Me._windowEvents = _events.WindowEvents
        Me._documentEvents = _events.DocumentEvents
        AddHandler _debuggerEvents.OnEnterBreakMode, New _dispDebuggerEvents_OnEnterBreakModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnEnterRunMode, New _dispDebuggerEvents_OnEnterRunModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnEnterDesignMode, New _dispDebuggerEvents_OnEnterDesignModeEventHandler(AddressOf OnIdeEvent)
        AddHandler _debuggerEvents.OnContextChanged, New _dispDebuggerEvents_OnContextChangedEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.AfterClosing, New _dispSolutionEvents_AfterClosingEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.Opened, New _dispSolutionEvents_OpenedEventHandler(AddressOf OnIdeEvent)
        AddHandler _solutionEvents.Renamed, New _dispSolutionEvents_RenamedEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowCreated, New _dispWindowEvents_WindowCreatedEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowClosing, New _dispWindowEvents_WindowClosingEventHandler(AddressOf OnIdeEvent)
        AddHandler _windowEvents.WindowActivated, New _dispWindowEvents_WindowActivatedEventHandler(AddressOf OnIdeEvent)
        AddHandler _documentEvents.DocumentOpened, New _dispDocumentEvents_DocumentOpenedEventHandler(AddressOf OnIdeEvent)
        AddHandler _documentEvents.DocumentClosing, New _dispDocumentEvents_DocumentClosingEventHandler(AddressOf OnIdeEvent)
    End Sub

    Private Sub OnIdeEvent(ByVal gotfocus As Window, ByVal lostfocus As Window)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(ByVal document As Document)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(ByVal window As Window)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(ByVal oldname As String)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(ByVal reason As dbgEventReason)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(ByVal reason As dbgEventReason, ByRef executionaction As dbgExecutionAction)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent(newProc As EnvDTE.Process, newProg As EnvDTE.Program, newThread As EnvDTE.Thread, newStkFrame As EnvDTE.StackFrame)
        OnIdeEvent()
    End Sub

    Private Sub OnIdeEvent()
        If (Me.Settings.EnableDebugMode) Then
            WriteOutput("Debugger context changed. Updating title.")
        End If
        Me.UpdateWindowTitle(Me, EventArgs.Empty)
    End Sub
    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    ' Overriden Package Implementation
#Region "Package Members"
    ''' <summary>
    ''' Initialization of the package; this method is called right after the package is sited, so this is the place
    ''' where you can put all the initilaization code that rely on services provided by VisualStudio.
    ''' </summary>
    Protected Overrides Sub Initialize()
        MyBase.Initialize()
        DoInitialize()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        Me.ResetTitleTimer.Dispose()
        MyBase.Dispose(disposing:=disposing)
    End Sub

#End Region

    Private Sub DoInitialize()
        'Every 5 seconds, we check the window titles in case we missed an event.
        Me.ResetTitleTimer = New System.Windows.Forms.Timer() With {.Interval = 5000}
        AddHandler Me.ResetTitleTimer.Tick, AddressOf Me.UpdateWindowTitle
        Me.ResetTitleTimer.Start()
    End Sub

    Private ReadOnly Property Settings() As OptionPageGrid
        Get
            Return CType(GetDialogPage(GetType(OptionPageGrid)), OptionPageGrid)
        End Get
    End Property

    Private Function GetIDEName(ByVal str As String) As String
        Try
            Dim m = New Regex("^(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (" + Me.DTE.Name + ".* \(.+\)) \(.+\)$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 2 Then
                If (m.Groups.Count >= 3) Then
                    Return m.Groups(2).Captures(0).Value
                ElseIf (m.Groups.Count >= 2) Then
                    Return m.Groups(1).Captures(0).Value
                End If
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("IDE name (" + Me.DTE.Name + ") not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetIDEName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSSolutionName(ByVal str As String) As String
        Try
            Dim m = New Regex("^(.*)\\(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 4 Then
                Dim name = m.Groups(2).Captures(0).Value
                Dim state = GetVSState(str)
                Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
            Else
                m = New Regex("^(.*) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
                If (m.Success) AndAlso m.Groups.Count >= 3 Then
                    Dim name = m.Groups(1).Captures(0).Value
                    Dim state = GetVSState(str)
                    Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                Else
                    m = New Regex("^(.*) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
                    If (m.Success) AndAlso m.Groups.Count >= 3 Then
                        Dim name = m.Groups(1).Captures(0).Value
                        Dim state = GetVSState(str)
                        Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                    Else
                        If (Me.Settings.EnableDebugMode) Then WriteOutput("VSName not found: " & str & ".")
                        Return Nothing
                    End If
                End If
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetVSName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSState(ByVal str As String) As String
        Try
            Dim m = New Regex(" \((.*)\) - (" + Me.DTE.Name + ".*) " + Regex.Escape(Me.Settings.AppendedString) + "$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex(" \((.*)\) - (" + Me.DTE.Name + ".*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                Return m.Groups(1).Captures(0).Value
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("VSState not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetVSState Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private ReadOnly UpdateWindowTitleLock As Object = New Object()

    Private Sub UpdateWindowTitle(ByVal state As Object, ByVal e As EventArgs)
        If (Me.IDEName Is Nothing AndAlso Me.DTE.MainWindow IsNot Nothing) Then
            Me.IDEName = GetIDEName(Me.DTE.MainWindow.Caption)
        End If
        If (Me.IDEName Is Nothing) Then Return
        If (Not Monitor.TryEnter(UpdateWindowTitleLock)) Then Return
        Try
            Dim currentInstance = Diagnostics.Process.GetCurrentProcess()
            Dim vsInstances As Diagnostics.Process() = Diagnostics.Process.GetProcessesByName("devenv")
            Dim rewrite = False
            If Me.Settings.AlwaysRewriteTitles Then
                rewrite = True
            ElseIf vsInstances.Count >= Me.Settings.MinNumberOfInstances Then
                'Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                'The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well).
                Dim currentInstanceName = Path.GetFileNameWithoutExtension(Me.DTE.Solution.FullName)
                If String.IsNullOrEmpty(currentInstanceName) Then
                    rewrite = True
                ElseIf (From vsInstance In vsInstances Where vsInstance.Id <> currentInstance.Id
                        Select GetVSSolutionName(vsInstance.MainWindowTitle())).Any(Function(vsInstanceName) vsInstanceName IsNot Nothing AndAlso currentInstanceName = vsInstanceName) Then
                    rewrite = True
                End If
            End If
            Dim pattern As String
            Dim solution = Me.DTE.Solution
            If (solution Is Nothing OrElse solution.FullName = String.Empty) Then
                Dim document = Me.DTE.ActiveDocument
                Dim window = Me.DTE.ActiveWindow
                If ((document Is Nothing OrElse String.IsNullOrEmpty(document.FullName)) AndAlso (window Is Nothing OrElse String.IsNullOrEmpty(window.Caption))) Then
                    pattern = If(rewrite, Me.Settings.PatternIfNothingOpen, "[ideName]")
                Else
                    pattern = If(rewrite, Me.Settings.PatternIfDocumentButNoSolutionOpen, "[documentName] - [ideName]")
                End If
            Else
                If (Me.DTE.Debugger Is Nothing OrElse Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgDesignMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfDesignMode, "[solutionName] - [ideName]")
                ElseIf (Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgBreakMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfBreakMode, "[solutionName] (Debugging) - [ideName]")
                ElseIf (Me.DTE.Debugger.CurrentMode = dbgDebugMode.dbgRunMode) Then
                    pattern = If(rewrite, Me.Settings.PatternIfRunningMode, "[solutionName] (Running) - [ideName]")
                Else
                    Throw New Exception("No matching state found")
                End If
            End If
            Me.ChangeWindowTitle(GetNewTitle(pattern:=pattern))
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("UpdateWindowTitle exception: " + ex.ToString())
        Finally
            Monitor.Exit(UpdateWindowTitleLock)
        End Try
    End Sub

    Private Function GetNewTitle(ByVal pattern As String) As String
        Dim solution = Me.DTE.Solution
        Dim parentPath = ""
        Dim documentName = ""
        Dim solutionName = ""
        Dim document = Me.DTE.ActiveDocument
        If (document IsNot Nothing) Then
            documentName = Path.GetFileName(document.FullName)
            If (solution Is Nothing OrElse String.IsNullOrEmpty(solution.FullName)) Then
                Dim parents = Path.GetDirectoryName(document.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray()
                parentPath = GetParentPath(parents:=parents)
                pattern = ReplaceParentTags(pattern:=pattern, parents:=parents)
            End If
        Else
            Dim window = Me.DTE.ActiveWindow
            If (window IsNot Nothing AndAlso window.Caption <> Me.DTE.MainWindow.Caption) Then
                documentName = window.Caption
            ElseIf (solution Is Nothing OrElse String.IsNullOrEmpty(solution.FullName)) Then
                Return Me.IDEName
            End If
        End If
        If (solution IsNot Nothing AndAlso Not String.IsNullOrEmpty(solution.FullName)) Then
            Dim project = GetActiveProject(Me.DTE)
            If (project IsNot Nothing) Then
                pattern = pattern.Replace("[activeProjectName]", project.Name)
            End If
            solutionName = Path.GetFileNameWithoutExtension(solution.FullName)
            Dim parents = Path.GetDirectoryName(Me.DTE.Solution.FullName).Split(Path.DirectorySeparatorChar).Reverse().ToArray()
            parentPath = GetParentPath(parents:=parents)
            pattern = ReplaceParentTags(pattern:=pattern, parents:=parents)
            Dim activeConfig = DTE.Solution.Properties.Item("ActiveConfig").Value
            If (activeConfig IsNot Nothing) Then
                Dim activeConfiguration = activeConfig.ToString().Split(CType("|", Char()))
                If (activeConfiguration.Length = 2) Then
                    Dim configurationName = activeConfiguration(0)
                    Dim platformName = activeConfiguration(1)
                    pattern = pattern.Replace("[configurationName]", configurationName) _
                                     .Replace("[platformName]", platformName)
                End If
            End If
        End If
        Return pattern.Replace("[documentName]", documentName) _
                      .Replace("[solutionName]", solutionName) _
                      .Replace("[parentPath]", parentPath).Replace("[ideName]", Me.IDEName) + " " + Me.Settings.AppendedString
    End Function

    Private Function GetParentPath(ByVal parents As String()) As String
        Return Path.Combine(parents.Skip(Me.Settings.ClosestParentDepth - 1).Take(Me.Settings.FarthestParentDepth - Me.Settings.ClosestParentDepth + 1).Reverse().ToArray())
    End Function

    Private Function ReplaceParentTags(ByVal pattern As String, ByVal parents As String()) As String
        Dim matches = New Regex("\[parent([0-9]+)\]").Matches(pattern)
        For Each m As Match In matches
            If (Not m.Success) Then Continue For
            Dim depth = Integer.Parse(m.Groups(1).Captures(0).Value)
            If (depth <= parents.Length) Then
                pattern = pattern.Replace("[parent" + depth.ToString(Globalization.CultureInfo.InvariantCulture) + "]", parents(depth))
            End If
        Next
        Return pattern
    End Function

    Private Sub ChangeWindowTitle(ByVal title As String)
        Try
            Dim dispatcher = System.Windows.Application.Current.Dispatcher
            If (dispatcher IsNot Nothing) Then
                dispatcher.BeginInvoke((Sub()
                                            Try
                                                System.Windows.Application.Current.MainWindow.Title = Me.DTE.MainWindow.Caption
                                                If (System.Windows.Application.Current.MainWindow.Title <> title) Then
                                                    System.Windows.Application.Current.MainWindow.Title = title
                                                End If
                                            Catch
                                            End Try
                                        End Sub))
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("SetMainWindowTitle Exception: " + ex.ToString())
        End Try
    End Sub

    Private Shared Sub WriteOutput(ByVal str As String)
        Try
            Dim outWindow As IVsOutputWindow = TryCast(GetGlobalService(GetType(SVsOutputWindow)), IVsOutputWindow)
            Dim generalPaneGuid As Guid = VSConstants.OutputWindowPaneGuid.DebugPane_guid
            ' P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
            Dim generalPane As IVsOutputWindowPane = Nothing
            outWindow.GetPane(generalPaneGuid, generalPane)
            generalPane.OutputString("RenameVSWindowTitle: " & str & vbNewLine)
            generalPane.Activate()
        Catch
        End Try
    End Sub

    Private Shared Function GetWindowTitle(ByVal hWnd As IntPtr) As String
        Const nChars As Integer = 256
        Dim buff As New StringBuilder(nChars)
        If GetWindowText(hWnd, buff, nChars) > 0 Then
            Return buff.ToString()
        End If
        Return Nothing
    End Function

    Private Shared Function GetActiveProject(dte As DTE2) As Project
        Dim activeProject As Project = Nothing

        Dim activeSolutionProjects As Array = TryCast(dte.ActiveSolutionProjects, Array)
        If activeSolutionProjects IsNot Nothing AndAlso activeSolutionProjects.Length > 0 Then
            activeProject = TryCast(activeSolutionProjects.GetValue(0), Project)
        End If

        Return activeProject
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    '<DllImport("user32.dll")>
    'Private Shared Function GetShellWindow() As IntPtr
    'End Function

    'We could use the following to determine if user is an administrator
    '<DllImport("user32.dll", EntryPoint:="IsUserAnAdmin")>
    'Private Shared Function IsUserAnAdministrator() As Boolean
    'End Function
End Class