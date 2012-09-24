Imports System.Runtime.InteropServices
Imports EnvDTE
Imports EnvDTE80
Imports EnvDTE90
Imports EnvDTE90a
Imports EnvDTE100
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
    ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string),
    ProvideMenuResource("Menus.ctmenu", 1),
    Guid(GuidList.guidRenameVSWindowTitle3PkgString)>
<ProvideOptionPage(GetType(OptionPageGrid),
                   "Rename VS Window Title", "Rules", 0, 0, True)>
Public NotInheritable Class RenameVSWindowTitle
    Inherits Package

    'Private dte As EnvDTE.DTE
    Private _dte2 As DTE2
    Private _addInInstance As AddIn
    Public ResetTitleTimer As Timer
    Private _currentInstanceOriginalTitle As String
    Private _currentInstanceOriginalState As String
    Private _currentInstanceOriginalAppName As String

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

    Private Sub DoInitialize()
        'Me.dte = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        Me._dte2 = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE80.DTE2) 'DirectCast(Marshal.GetActiveObject("VisualStudio.DTE.11.0"), DTE2)
        Me.ResetTitleTimer = New Timer(New TimerCallback(AddressOf SetMainWindowTitle), "Hello world!", 0, 5000)
        'Every 5 seconds, we check the window titles.
    End Sub

    Private ReadOnly Property Settings() As OptionPageGrid
        Get
            Return CType(GetDialogPage(GetType(OptionPageGrid)), OptionPageGrid)
        End Get
    End Property

    Private Function GetVSAppName(ByVal str As String) As String
        Try
            Dim m = New Regex("^(.*) - (Microsoft .*) \*$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex("^(.*) - (Microsoft.*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                'Return New Tuple(Of String, String)(m.Groups(2).Captures(0).Value, m.Groups(3).Captures(0).Value)
                Return m.Groups(2).Captures(0).Value
            Else
                If (Me.Settings.EnableDebugMode) Then WriteOutput("VSAppName not found: " & str & ".")
                Return Nothing
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("GetVSAppName Exception: " & str & ". Details: " + ex.ToString())
            Return Nothing
        End Try
    End Function

    Private Function GetVSSolutionName() As String
        Return Path.GetFileNameWithoutExtension(Me._dte2.Solution.FullName)
    End Function

    Private Function GetVSSolutionName(ByVal str As String) As String
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
            Dim m = New Regex(" \((.*)\) - (Microsoft.*) \*$", RegexOptions.RightToLeft).Match(str)
            If (Not m.Success) Then m = New Regex(" \((.*)\) - (Microsoft.*)$", RegexOptions.RightToLeft).Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 3 Then
                'Return New Tuple(Of String, String)(m.Groups(2).Captures(0).Value, m.Groups(3).Captures(0).Value)
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

    Private ReadOnly SetMainWindowTitleLock As Object = New Object()

    Private Sub SetMainWindowTitle(ByVal state As Object)
        If (Not Monitor.TryEnter(SetMainWindowTitleLock)) Then Return
        Try
            If Me._dte2 Is Nothing OrElse Me._dte2.Solution Is Nothing OrElse Me._dte2.Solution.FullName = String.Empty _
                Then Exit Sub
            Dim hWnd As IntPtr = New IntPtr(Me._dte2.MainWindow.HWnd)
            Dim path = IO.Path.GetDirectoryName(Me._dte2.Solution.FullName)
            Dim folders = path.Split(IO.Path.DirectorySeparatorChar)

            Dim currentInstance = Diagnostics.Process.GetCurrentProcess()
            Dim currentInstanceWindowTitle = Me._dte2.MainWindow.Caption
            ' 
            If String.IsNullOrWhiteSpace(currentInstanceWindowTitle) Then
                currentInstanceWindowTitle = currentInstance.MainWindowTitle()
                If String.IsNullOrWhiteSpace(currentInstanceWindowTitle) Then _
                    'Does not always work for some reason (e.g. sometimes on Windows Server 2008 R2).
                    currentInstanceWindowTitle = GetWindowTitle(hWnd)
                    If String.IsNullOrWhiteSpace(currentInstanceWindowTitle) Then
                        Return
                    End If
                End If
            End If
            If (String.IsNullOrWhiteSpace(GetVSAppName(currentInstanceWindowTitle))) Then Return
            If (Me.Settings.EnableDebugMode) Then _
                WriteOutput("Current instance window title: " & currentInstanceWindowTitle)

            'We append " *" when the window title has been improved
            If Not currentInstanceWindowTitle.EndsWith(" *") Then
                Me._currentInstanceOriginalTitle = currentInstanceWindowTitle
                Me._currentInstanceOriginalState = GetVSState(currentInstanceWindowTitle)
                Me._currentInstanceOriginalAppName = GetVSAppName(currentInstanceWindowTitle)
                If _
                    (String.IsNullOrWhiteSpace(Me._currentInstanceOriginalState) AndAlso
                     Me._dte2.Mode <> vsIDEMode.vsIDEModeDesign) Then
                    Exit Sub
                End If
            End If

            Dim vsInstances As Diagnostics.Process() = Diagnostics.Process.GetProcessesByName("devenv")
            Dim conflict = False

            'Here we should have more rules
            'So far, we just check if two or more instances of "devenv" were opened, and in that case, set conflict = True to improve the window title, unless RewriteOnlyIfConflict is true. 
            If vsInstances.Count >= Me.Settings.MinNumberOfInstances Then
                'Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well). Otherwise do not rewrite the title.
                'The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well).
                Dim currentInstanceName = GetVSSolutionName()
                If Me.Settings.RewriteOnlyIfConflict AndAlso Not String.IsNullOrEmpty(currentInstanceName) Then
                    For Each vsInstance In vsInstances
                        If vsInstance.Id = currentInstance.Id Then Continue For
                        Dim vsInstanceName = GetVSSolutionName(vsInstance.MainWindowTitle())
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
                Dim tree =
                        IO.Path.Combine(
                            folders.Reverse().Skip(Me.Settings.ClosestParentDepth - 1).Take(
                                Me.Settings.FarthestParentDepth - Me.Settings.ClosestParentDepth + 1).Reverse().ToArray())
                ChangeDteWindowTitle(Me._dte2, hWnd,
                                     tree & IO.Path.DirectorySeparatorChar & Me.GetVSSolutionName() &
                                     If _
                                         (Not String.IsNullOrEmpty(Me._currentInstanceOriginalState),
                                          " (" & Me._currentInstanceOriginalState & ")", "") & " - " &
                                     Me._currentInstanceOriginalAppName & " *")
            ElseIf currentInstanceWindowTitle.EndsWith(" *") Then 'Restore original window title
                ChangeDteWindowTitle(Me._dte2, hWnd, Me._currentInstanceOriginalTitle)
            End If
        Catch ex As Exception
            If (Me.Settings.EnableDebugMode) Then WriteOutput("SetMainWindowTitle Exception: " + ex.ToString())
        Finally
            Monitor.Exit(SetMainWindowTitleLock)
        End Try
    End Sub

    Private Shared Sub ChangeDteWindowTitle(ByVal dte2 As DTE2, ByVal hWnd As IntPtr, ByVal title As String)
        'If (dte2.MainWindow.Caption <> title) Then
        '    'SetWindowText no longer works with VS2012 (only the title in the taskbar is affected)
        '    SetWindowText(hWnd:=hWnd, lpString:=title)
        '    SetWindowText(hWnd:=New IntPtr(dte2.MainWindow.HWnd), lpString:=title)
        'End If
        Dim dispatcher = System.Windows.Application.Current.Dispatcher
        If (dispatcher IsNot Nothing) Then
            dispatcher.BeginInvoke((Sub()
                                        System.Windows.Application.Current.MainWindow.Title = title
                                    End Sub))
        End If
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

    'Dim processes As Process() = Process.GetProcessesByName("MyApp")
    'For Each process As Process In processes
    'Dim windows As IDictionary(Of IntPtr, String) = GetOpenWindowsFromPID(Process.Id)
    'For Each kvp As KeyValuePair(Of IntPtr, String) In windows
    '' Do whatever you want here 
    'Next
    'Next
    Private Shared Function GetWindowTitle(ByVal hWnd As IntPtr) As String
        Const nChars As Integer = 256
        Dim buff As New StringBuilder(nChars)
        If GetWindowText(hWnd, buff, nChars) > 0 Then
            Return buff.ToString()
        End If
        Return Nothing
    End Function

    Private Function GetActiveWindowTitle() As String
        Const nChars As Integer = 256
        Dim handle As IntPtr = IntPtr.Zero
        Dim buff As New StringBuilder(nChars)
        handle = GetForegroundWindow()

        If GetWindowText(handle, buff, nChars) > 0 Then
            Return buff.ToString()
        End If
        Return Nothing
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowText(ByVal hWnd As IntPtr, ByVal lpString As String) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetShellWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowTextLength(ByVal hWnd As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function GetWindowThreadProcessId(ByVal hWnd As IntPtr, <Out()> ByRef lpdwProcessId As UInt32) _
        As UInt32
    End Function

    <DllImport("user32.dll")>
    Private Shared Function IsWindowVisible(ByVal hWnd As IntPtr) As Boolean
    End Function

    Private Delegate Function EnumWindowsProc(ByVal hWnd As IntPtr, ByVal lParam As Integer) As Boolean

    <DllImport("user32.dll")>
    Private Shared Function EnumWindows(ByVal enumFunc As EnumWindowsProc, ByVal lParam As Integer) As Boolean
    End Function

    Private ReadOnly hShellWindow As IntPtr = GetShellWindow()
    Private ReadOnly dictWindows As New Dictionary(Of IntPtr, String)
    Private _currentPID As Integer

    Public Function GetOpenWindowsFromPID(ByVal pid As Integer) As IDictionary(Of IntPtr, String)
        dictWindows.Clear()
        _currentPID = pid
        EnumWindows(AddressOf enumWindowsInternal, 0)
        Return dictWindows
    End Function

    Private Function EnumWindowsInternal(ByVal hWnd As IntPtr, ByVal lParam As Integer) As Boolean
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
            If (windowPid <> _currentPID) Then
                Return True
            End If
            Dim stringBuilder As New StringBuilder(length)
            GetWindowText(hWnd, stringBuilder, (length + 1))
            dictWindows.Add(hWnd, stringBuilder.ToString)
        End If
        Return True
    End Function
End Class