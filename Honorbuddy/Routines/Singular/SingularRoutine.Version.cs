﻿using System.Reflection;
using System.Runtime.InteropServices;

using System;
using Styx.CommonBot.Routines;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Singular")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("ApocDev & Co")]
[assembly: AssemblyProduct("Singular")]
[assembly: AssemblyCopyright("Copyright © ApocDev & Co 2011")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("fe33d5f5-c00e-40ae-bb8e-7ea7dd836929")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("4.0.0.4631")]
[assembly: AssemblyFileVersion("4.0.0.4631")]

namespace Singular
{
    /// <summary>
    /// Template File:  SingularRoutine.Version.tmpl
    /// Generated File: SingularRoutine.Version.cs
    /// 
    /// These files are the source and output for SubWCRev.exe included
    /// with TortoiseSVN.  The purpose is to provide a real Build #
    /// automatically updated with each release.
    /// 
    /// To make changes, be sure to edit SingularRoutine.Version.tmpl
    /// as the .cs version gets overwritten each build
    /// 
    /// Singular SVN Information
    /// -------------------------
    /// Revision 4631
    /// Date     2015/04/13 12:12:26
    /// Range    4604:4631
    /// 
    /// </summary>
    public partial class SingularRoutine : CombatRoutine
    {
        // HB Build Process is overwriting AssemblyInfo.cs contents,
        // ... so manage version here instead of reading assembly
        // --------------------------------------------------------

        // return Assembly.GetExecutingAssembly().GetName().Version;
        public static Version GetSingularVersion()
        {
            return new Version("4.0.0.4631");
        }
        public static string GetSingularBuildTime()
        {
            return "2015/04/13 12:13:44";
        }
    }
}
