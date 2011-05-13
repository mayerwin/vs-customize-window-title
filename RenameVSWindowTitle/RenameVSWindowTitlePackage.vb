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

<PackageRegistration(UseManagedResourcesOnly:=True), _
InstalledProductRegistration("#110", "#112", "1.0", IconResourceID:=400), _
ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string), _
Guid(GuidList.guidRenameVSWindowTitle3PkgString)> _
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
        Trace.WriteLine(String.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", Me.GetType().Name))
    End Sub


    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    ' Overriden Package Implementation
#Region "Package Members"

    ''' <summary>
    ''' Initialization of the package; this method is called right after the package is sited, so this is the place
    ''' where you can put all the initilaization code that rely on services provided by VisualStudio.
    ''' </summary>
    Protected Overrides Sub Initialize()
        Trace.WriteLine(String.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", Me.GetType().Name))
        MyBase.Initialize()
        DoInitialize()
    End Sub
#End Region

    Private dte As EnvDTE.DTE
    Public resetTitleTimer As Timer
    Private currentInstanceOriginalWindowTitle As String

    Private Shared Function GetProjectOrSolutionNameAndStatus(ByVal str As String) As Tuple(Of String, String)
        Try
            Dim pattern = New Regex("^(.*)\\(.*) - (Microsoft .*) \*$", RegexOptions.RightToLeft)
            Dim m = pattern.Match(str)
            If (m.Success) AndAlso m.Groups.Count >= 4 Then
                Return New Tuple(Of String, String)(m.Groups(2).Captures(0).Value, m.Groups(3).Captures(0).Value)
            Else
                pattern = New Regex("^(.*) - (Microsoft.*)$", RegexOptions.RightToLeft)
                m = pattern.Match(str)
                If (m.Success) AndAlso m.Groups.Count >= 3 Then
                    Return New Tuple(Of String, String)(m.Groups(1).Captures(0).Value, m.Groups(2).Captures(0).Value)
                Else
                    Return Nothing
                End If
            End If
        Catch
            Return Nothing
        End Try
    End Function

    Protected Sub DoInitialize()
        Me.dte = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        Me.resetTitleTimer = New Timer(New TimerCallback(AddressOf SetMainWindowTitle), "Hello world!", 0, 10000) 'Every 10 seconds, we check the window titles.
    End Sub

    Private Sub SetMainWindowTitle(ByVal state As Object)
        Try
            Dim hWnd As IntPtr = New IntPtr(Me.dte.MainWindow.HWnd)
            If Me.dte Is Nothing OrElse Me.dte.Solution Is Nothing OrElse Me.dte.Solution.FullName = String.Empty Then Exit Sub
            Dim path = IO.Path.GetDirectoryName(Me.dte.Solution.FullName)
            Dim folders = path.Split("\"c)

            Dim currentInstance = Process.GetCurrentProcess()
            Dim currentInstanceWindowTitle = currentInstance.MainWindowTitle()

            'We append " *" when the window title has been improved
            If Not currentInstanceWindowTitle.EndsWith(" *") Then
                currentInstanceOriginalWindowTitle = currentInstanceWindowTitle
            End If

            'Check if multiple instances of devenv have identical original names. If so, then rewrite the title of current instance (normally the extension will run on each instance so no need to rewrite them as well).
            Dim conflict = False
            Dim vsInstances As Process() = Process.GetProcessesByName("devenv")
            Dim currentInstanceSolutionNameAndStatus = GetProjectOrSolutionNameAndStatus(currentInstanceWindowTitle)

            'TODO: right now, it will not consider as same project/solution instances that are running, debugging, or editing, because it changes the window title. We could add in the regex (Running) and (Debugging) but this would not be locale independent... 
            'The best would be to get the EnvDTE.DTE object of the other instances, and compare the solution or project names directly instead of relying on window titles (which may be hacked by third party software as well).
            'When this is done, the following block should be activated (remove "False").
            If False AndAlso currentInstanceSolutionNameAndStatus IsNot Nothing Then
                For Each vsInstance As Process In vsInstances
                    If vsInstance.Id = currentInstance.Id Then Continue For
                    Dim vsInstanceSolutionNameAndStatus = GetProjectOrSolutionNameAndStatus(vsInstance.MainWindowTitle())
                    If vsInstanceSolutionNameAndStatus IsNot Nothing _
                        AndAlso currentInstanceSolutionNameAndStatus.Item1 = vsInstanceSolutionNameAndStatus.Item1 Then
                        conflict = True
                    End If
                Next
            End If

            'So far, we just checked if two instances of "devenv" were opened, and in that case, set conflict = True to improve the window title. 
            'When the above TODO about the differentiation of execution modes is fixed, we should remove the following. Or simply add it as a rule that can be triggered upon the preference of the user.
            If vsInstances.Count >= 2 Then
                conflict = True
            End If
            If conflict Then 'Improve window title
                'TODO: here we should incorporate tag based rules, based on the current instance's solution characteristics.
                SetWindowText(hWnd, folders(folders.Count - 2) & "\" & currentInstanceSolutionNameAndStatus.Item1 & " - " & GetProjectOrSolutionNameAndStatus(Me.currentInstanceOriginalWindowTitle).Item2 & " *")
            ElseIf currentInstanceWindowTitle.EndsWith(" *") Then 'Restore original window title
                SetWindowText(hWnd, Me.currentInstanceOriginalWindowTitle)
            End If
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

    <DllImport("user32.dll")> _
    Private Shared Function SetWindowText(ByVal hWnd As IntPtr, ByVal lpString As String) As Boolean
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetShellWindow() As IntPtr
    End Function

    <DllImport("user32.dll")> _
    Private Shared Function GetWindowText(ByVal hWnd As IntPtr, ByVal lpString As Text.StringBuilder, ByVal nMaxCount As Integer) As Integer
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
