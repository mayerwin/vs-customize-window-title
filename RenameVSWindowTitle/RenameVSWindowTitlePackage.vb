Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Text

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
ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string),
ProvideMenuResource("Menus.ctmenu", 1),
Guid(GuidList.guidRenameVSWindowTitle3PkgString)>
<ProvideOptionPage(GetType(OptionPageGrid),
    "Rename VS Window Title", "Rules", 0, 0, True)>
Public NotInheritable Class RenameVSWindowTitle
    Inherits Package
    ''' <summary>
    ''' Default constructor of the package.
    ''' Inside this method you can place any initialization code that does not require 
    ''' any Visual Studio service because at this point the package object is created but 
    ''' not sited yet inside Visual Studio environment. The place to do all the other 
    ''' initialization is the Initialize method.
    ''' </summary>
    Public Sub New()
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
#End Region

    Private dte As EnvDTE.DTE
    Public resetTitleTimer As Timer
    Private CurrentInstanceOriginalWindowTitle As String

    Private Function GetVSAppName(ByVal str As String) As String
        Try
            Dim m = New Regex("^(.*) - (Microsoft .*) \*$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (Microsoft.*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                'Return New Tuple(Of String, String)(m.Groups(2).Captures(0).Value, m.Groups(3).Captures(0).Value)
                Return m.Groups(2).Captures(0).Value
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSAppName not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSAppName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSName() As String
        Return Path.GetFileNameWithoutExtension(Me.dte.Solution.FullName)
    End Function

    Private Function GetVSName(ByVal str As String) As String
        Try
            Dim m = New Regex("^(.*)\\(.*) - (Microsoft.*) \*$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 4 Then
                Dim name = m.Groups(2).Captures(0).Value
                Dim state = GetVSState(str).ToString()
                Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
            Else
                m = New Regex("^(.*) - (Microsoft.*) \*$", RegexOptions.RightToLeft).Match(str)
                If (m.Success) AndAlso m.Groups.Count >= 3 Then
                    Dim name = m.Groups(1).Captures(0).Value
                    Dim state = GetVSState(str).ToString()
                    Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                Else
                    m = New Regex("^(.*) - (Microsoft.*)$", RegexOptions.RightToLeft).Match(str)
                    If (m.Success) AndAlso m.Groups.Count >= 3 Then
                        Dim name = m.Groups(1).Captures(0).Value
                        Dim state = GetVSState(str).ToString()
                        Return name.Substring(0, name.Length - If(String.IsNullOrEmpty(state), 0, state.Length + 3))
                    Else
                        If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSName not found: " & str & ".")
                        Return Nothing
                    End If
                End If
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSState(ByVal str As String) As String
        Try
            Dim m = New Regex(" \((.*)\) - (Microsoft.*) \*$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex(" \((.*)\) - (Microsoft.*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                'Return New Tuple(Of String, String)(m.Groups(2).Captures(0).Value, m.Groups(3).Captures(0).Value)
                Return m.Groups(1).Captures(0).Value
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSState not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("GetVSState Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Protected Sub DoInitialize()
        Me.dte = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        Me.resetTitleTimer = New Timer(New TimerCallback(AddressOf SetMainWindowTitle), "Hello world!", 0, 5000) 'Every 5 seconds, we check the window titles.
    End Sub

    Protected ReadOnly Property Settings() As OptionPageGrid
        Get
            Return CType(GetDialogPage(GetType(OptionPageGrid)), OptionPageGrid)
        End Get
    End Property

    Private SetMainWindowTitleLock As Object = New Object()

    Private Sub SetMainWindowTitle(ByVal state As Object)
        If (Not Monitor.TryEnter(SetMainWindowTitleLock)) Then Return
        Try
            Dim hWnd As IntPtr = New IntPtr(Me.dte.MainWindow.HWnd)
            If Me.dte Is Nothing OrElse Me.dte.Solution Is Nothing OrElse Me.dte.Solution.FullName = String.Empty Then Exit Sub
            Dim path = IO.Path.GetDirectoryName(Me.dte.Solution.FullName)
            Dim folders = path.Split(System.IO.Path.DirectorySeparatorChar)

            Dim currentInstance = Process.GetCurrentProcess()
            Dim currentInstanceWindowTitle = currentInstance.MainWindowTitle()
            If String.IsNullOrWhiteSpace(currentInstanceWindowTitle) Then 'Does not always work for some reason (e.g. sometimes on Windows Server 2008 R2).
                currentInstanceWindowTitle = GetWindowTitle(hWnd)
                If String.IsNullOrWhiteSpace(currentInstanceWindowTitle) Then
                    Return
                End If
            End If
            If (Me.Settings.EnableDebugMode) Then WriteOutput("Current instance window title: " & currentInstanceWindowTitle)

            'We append " *" when the window title has been improved
            If Not currentInstanceWindowTitle.EndsWith(" *") Then
                Me.CurrentInstanceOriginalWindowTitle = currentInstanceWindowTitle
            End If

            Dim vsInstances As Process() = Process.GetProcessesByName("devenv")
            Dim conflict = False

            'Here we should have more rules
            'So far, we just check if two or more instances of "devenv" were opened, and in that case, set conflict = True to improve the window title, unless RewriteOnlyIfConflict is true. 
            If vsInstances.Count >= Me.Settings.MinNumberOfInstances Then
                'Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                'The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well).
                Dim currentInstanceName = GetVSName()
                If Me.Settings.RewriteOnlyIfConflict AndAlso Not String.IsNullOrEmpty(currentInstanceName) Then
                    For Each vsInstance As Process In vsInstances
                        If vsInstance.Id = currentInstance.Id Then Continue For
                        Dim vsInstanceName = GetVSName(vsInstance.MainWindowTitle())
                        If (vsInstanceName IsNot Nothing) AndAlso (currentInstanceName = vsInstanceName) Then
                            conflict = True
                        Else
                            conflict = False
                        End If
                    Next
                Else
                    conflict = True
                End If
            End If
            If conflict Then 'Improve window title
                'TODO: here we should incorporate tag based rules, based on the current instance's solution characteristics.
                Dim tree = IO.Path.Combine(folders.Reverse().Skip(Me.Settings.ClosestParentDepth - 1).Take(Me.Settings.FarthestParentDepth - Me.Settings.ClosestParentDepth + 1).Reverse().ToArray())
                Dim vsstate = GetVSState(Me.CurrentInstanceOriginalWindowTitle)
                Dim appName = GetVSAppName(Me.CurrentInstanceOriginalWindowTitle)
                SetWindowText(hWnd, tree & System.IO.Path.DirectorySeparatorChar & Me.GetVSName() & If(Not String.IsNullOrEmpty(vsstate), " (" & vsstate & ")", "") & " - " & appName & " *")
            ElseIf currentInstanceWindowTitle.EndsWith(" *") Then 'Restore original window title
                SetWindowText(hWnd, Me.CurrentInstanceOriginalWindowTitle)
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("SetMainWindowTitle Exception: " + ex.ToString())
        Finally
            Monitor.Exit(SetMainWindowTitleLock)
        End Try
    End Sub

    Private Shared Sub WriteOutput(ByVal str As String)
        Try
            Dim outWindow As IVsOutputWindow = TryCast(Package.GetGlobalService(GetType(SVsOutputWindow)), IVsOutputWindow)

            Dim generalPaneGuid As Guid = VSConstants.OutputWindowPaneGuid.DebugPane_guid
            ' P.S. There's also the VSConstants.GUID_OutWindowDebugPane available.
            Dim generalPane As IVsOutputWindowPane = Nothing
            outWindow.GetPane(generalPaneGuid, generalPane)
            generalPane.OutputString("RenameVSWindowTitle: " & str & vbNewLine)
            generalPane.Activate()
        Catch
        End Try
    End Sub
    'Dim processes As Process() = Process.GetProcessesByName("MyApp")
    'For Each process As Process In processes
    'Dim windows As IDictionary(Of IntPtr, String) = GetOpenWindowsFromPID(Process.Id)
    'For Each kvp As KeyValuePair(Of IntPtr, String) In windows
    '' Do whatever you want here 
    'Next
    'Next
    Private Shared Function GetWindowTitle(ByVal hWnd As IntPtr) As String
        Const nChars As Integer = 256
        Dim Buff As New StringBuilder(nChars)
        If GetWindowText(hWnd, Buff, nChars) > 0 Then
            Return Buff.ToString()
        End If
        Return Nothing
    End Function

    Private Function GetActiveWindowTitle() As String
        Const nChars As Integer = 256
        Dim handle As IntPtr = IntPtr.Zero
        Dim Buff As New StringBuilder(nChars)
        handle = GetForegroundWindow()

        If GetWindowText(handle, Buff, nChars) > 0 Then
            Return Buff.ToString()
        End If
        Return Nothing
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function SetWindowText(ByVal hWnd As IntPtr, ByVal lpString As String) As Boolean
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetShellWindow() As IntPtr
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetWindowTextLength(ByVal hWnd As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)> _
    Private Shared Function GetWindowThreadProcessId(ByVal hWnd As IntPtr, <Out()> ByRef lpdwProcessId As UInt32) As UInt32
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function IsWindowVisible(ByVal hWnd As IntPtr) As Boolean
    End Function

    Private Delegate Function EnumWindowsProc(ByVal hWnd As IntPtr, ByVal lParam As Integer) As Boolean

    <DllImport("user32.dll")> _
    Private Shared Function EnumWindows(ByVal enumFunc As EnumWindowsProc, ByVal lParam As Integer) As Boolean
    End Function

    Private hShellWindow As IntPtr = GetShellWindow()
    Private dictWindows As New Dictionary(Of IntPtr, String)
    Private currentProcessID As Integer

    Public Function GetOpenWindowsFromPID(ByVal processID As Integer) As IDictionary(Of IntPtr, String)
        dictWindows.Clear()
        currentProcessID = processID
        EnumWindows(AddressOf enumWindowsInternal, 0)
        Return dictWindows
    End Function

    Private Function enumWindowsInternal(ByVal hWnd As IntPtr, ByVal lParam As Integer) As Boolean
        If (hWnd <> hShellWindow) Then
            Dim windowPid As UInt32
            If Not IsWindowVisible(hWnd) Then
                Return True
            End If
            Dim length As Integer = GetWindowTextLength(hWnd)
            If (length = 0) Then
                Return True
            End If
            GetWindowThreadProcessId(hWnd, windowPid)
            If (windowPid <> currentProcessID) Then
                Return True
            End If
            Dim stringBuilder As New Text.StringBuilder(length)
            GetWindowText(hWnd, stringBuilder, (length + 1))
            dictWindows.Add(hWnd, stringBuilder.ToString)
        End If
        Return True
    End Function
End Class