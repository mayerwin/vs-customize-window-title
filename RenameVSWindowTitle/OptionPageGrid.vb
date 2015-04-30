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

'http://stackoverflow.com/questions/24291249/dialogpage-string-array-not-persisted
'http://www.codeproject.com/Articles/351172/CodeStash-a-journey-into-the-dark-side-of-Visual-S

<ClassInterface(ClassInterfaceType.AutoDual)>
<CLSCompliant(False), ComVisible(True)>
Public Class OptionPageGrid
    Inherits DialogPage

    <Category("General")>
    <DisplayName("Always rewrite titles")>
    <Description("If set to true, will rewrite titles even if no conflict is detected. Default: true. Conflicts may not be detected if patterns have been changed from the default.")>
    Public Property AlwaysRewriteTitles As Boolean = True

    <Category("General")>
    <DisplayName("Activation threshold")>
    <Description("Min number of instances that need to be opened to rewrite titles, if they are in conflict or if ""Always rewrite titles"" is set to true. Default: 1.")>
    Public Property MinNumberOfInstances As Integer = 1

    <Category("General")>
    <DisplayName("Farthest parent folder depth")>
    <Description("Distance of the farthest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")>
    Public Property FarthestParentDepth As Integer = 1

    <Category("General")>
    <DisplayName("Closest parent folder depth")>
    <Description("Distance of the closest parent folder to be shown. 1 will only show the folder of the opened projet/solution file, before the project/folder name. Default: 1.")>
    Public Property ClosestParentDepth As Integer = 1

    <Category("Patterns")>
    <DisplayName("No document or solution open")>
    <Description("Default: [ideName]. See 'Supported Tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")>
    Public Property PatternIfNothingOpen As String = "[ideName]"

    <Category("Patterns")>
    <DisplayName("Document (no solution) open")>
    <Description("Default: [documentName] - [ideName]. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")>
    Public Property PatternIfDocumentButNoSolutionOpen As String = "[documentName] - [ideName]"

    <Category("Patterns")>
    <DisplayName("Solution in design mode")>
    <Description("Default: [parentPath]\[solutionName] - [ideName]. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")>
    Public Property PatternIfDesignMode As String = "[parentPath]\[solutionName] - [ideName]"

    <Category("Patterns")>
    <DisplayName("Solution in break mode")>
    <Description("Default: [parentPath]\[solutionName] (Debugging) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")>
    Public Property PatternIfBreakMode As String = "[parentPath]\[solutionName] (Debugging) - [ideName]"

    <Category("Patterns")>
    <DisplayName("Solution in running mode")>
    <Description("Default: [parentPath]\[solutionName] (Running) - [ideName]. The appended string parameter will be added at the end automatically to identify that the title is being improved. See 'Supported tags' section on the left or the documentation on the Visual Studio Gallery for more guidance.")>
    Public Property PatternIfRunningMode As String = "[parentPath]\[solutionName] (Running) - [ideName]"

    <Category("Patterns")>
    <DisplayName("Appended string")>
    <Description("String to be added at the end of the title to identify that it has been rewritten. If not default and Always rewrite titles, the detection of concurrent instances with the same title may not work. Default: '*'.")>
    Public Property AppendedString As String = "*"

    <Category("Debug")>
    <DisplayName("Enable debug mode")>
    <Description("Set to true to activate debug output to Output window.")>
    Public Property EnableDebugMode As Boolean = False

End Class