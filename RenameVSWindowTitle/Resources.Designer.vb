﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.0
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System


'This class was auto-generated by the StronglyTypedResourceBuilder
'class via a tool like ResGen or Visual Studio.
'To add or remove a member, edit your .ResX file then rerun ResGen
'with the /str option, or rebuild your VS project.
'''<summary>
'''  A strongly-typed resource class, for looking up localized strings, etc.
'''</summary>
<Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0"),  _
 Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
 Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
Friend Class Resources
    
    Private Shared resourceMan As Global.System.Resources.ResourceManager
    
    Private Shared resourceCulture As Global.System.Globalization.CultureInfo
    
    <Global.System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")>  _
    Friend Sub New()
        MyBase.New
    End Sub
    
    '''<summary>
    '''  Returns the cached ResourceManager instance used by this class.
    '''</summary>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
    Friend Shared ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
        Get
            If Object.ReferenceEquals(resourceMan, Nothing) Then
                Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("ErwinMayerLabs.RenameVSWindowTitle.Resources", GetType(Resources).Assembly)
                resourceMan = temp
            End If
            Return resourceMan
        End Get
    End Property
    
    '''<summary>
    '''  Overrides the current thread's CurrentUICulture property for all
    '''  resource lookups using this strongly typed resource class.
    '''</summary>
    <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
    Friend Shared Property Culture() As Global.System.Globalization.CultureInfo
        Get
            Return resourceCulture
        End Get
        Set
            resourceCulture = value
        End Set
    End Property
    
    '''<summary>
    '''  Looks up a localized string similar to //http://stackoverflow.com/questions/200162/allow-only-copy-paste-context-menu-in-system-windows-forms-webbrowser-control/200194#200194
    '''//&lt;div id=&apos;ContextMenu&apos; style=&apos;display: none; z-index: 1000; padding: 1px 10px; background-color: white; border: 2px solid lightgreen; position: absolute;&apos;&gt;&lt;a href=&apos;#&apos; id=&apos;CopyBtn&apos; style=&apos;display: block; color: black; text-decoration: none;&apos;&gt;Copy&lt;/a&gt;&lt;/div&gt;
    '''
    '''function selectText(obj) { // adapted from Denis Sadowski (via StackOverflow.com)
    '''    var range;
    '''    if (document [rest of string was truncated]&quot;;.
    '''</summary>
    Friend Shared ReadOnly Property script() As String
        Get
            Return ResourceManager.GetString("script", resourceCulture)
        End Get
    End Property
    
    '''<summary>
    '''  Looks up a localized string similar to body {
    '''	background: #fafafa;
    '''	color: #444;
    '''	font: 100%/30px &apos;Helvetica Neue&apos;, helvetica, arial, sans-serif;
    '''}
    '''
    '''h1 {
    '''  font-size: 15pt;
    '''  margin-bottom: 10px;
    '''  font-weight: bold;
    '''}
    '''
    '''strong {
    '''	font-weight: bold; 
    '''}
    '''
    '''em {
    '''	font-style: italic; 
    '''}
    '''
    '''table {
    '''	background: #f5f5f5;
    '''	font-size: 10pt;
    '''	line-height: 24px;
    '''	text-align: left;
    '''}	
    '''
    '''th {
    '''	background: #333;
    '''	border-left: 1px solid #555;
    '''	border-right: 1px solid #777;
    '''	border-top: 1px solid #555;
    '''	border-bottom: 1px solid #333 [rest of string was truncated]&quot;;.
    '''</summary>
    Friend Shared ReadOnly Property style() As String
        Get
            Return ResourceManager.GetString("style", resourceCulture)
        End Get
    End Property
End Class
