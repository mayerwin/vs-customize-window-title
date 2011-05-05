﻿Imports Microsoft.VisualBasic
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

    Protected Sub DoInitialize()
        Me.dte = DirectCast(GetGlobalService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
        Me.resetTitleTimer = New Timer(New TimerCallback(AddressOf SetMainWindowTitle), "Hello world!", 0, 10000)
    End Sub

    <DllImport("user32.dll")> _
    Private Shared Function SetWindowText(ByVal hWnd As IntPtr, ByVal lpString As String) As Boolean
    End Function
    Private Sub SetMainWindowTitle(ByVal state As Object)
        Dim hWnd As IntPtr = New IntPtr(Me.dte.MainWindow.HWnd)
        Dim path = IO.Path.GetDirectoryName(Me.dte.Solution.FullName)
        Dim folders = path.Split("\"c)

        Dim psList = Process.GetProcesses()
        If psList.Count(Function(ps) ps.ProcessName.StartsWith("devenv")) > 1 Then
            Dim title = folders(folders.Count - 2) & "\" & folders(folders.Count - 1) & " - Microsoft Visual Studio"
            SetWindowText(hWnd, title)
        Else
            Dim title = folders(folders.Count - 1) & " - Microsoft Visual Studio"
            SetWindowText(hWnd, title)
        End If
    End Sub

End Class
