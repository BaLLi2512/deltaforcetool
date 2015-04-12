﻿
using System;
using System.Diagnostics;
using System.IO;
using Singular.Settings;
using Styx.Common;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Color = System.Drawing.Color;
using Styx.Helpers;

using LogLevel = Styx.Common.LogLevel;
using Singular.Helpers;
using Styx;

namespace Singular
{
    public static class LogColor
    {
        public static Color Normal = Color.Green;
        public static Color Hilite = Color.White;
        public static Color SpellHeal = Color.LightGreen;
        public static Color SpellNonHeal = Color.DodgerBlue;
        public static Color Debug = Color.Orange;
        public static Color Diagnostic = Color.Yellow;
        public static Color Cancel = Color.OrangeRed;
        public static Color Init = Color.Cyan;
        public static Color Targeting = Color.LightCoral;
    }

    public static class Logger
    {
        static int lineNo = 0;

        /// <summary>
        /// write message to log window and file
        /// </summary>
        /// <param name="message">message text</param>
        public static void Write(string message)
        {
            Write(Color.Green, message);
        }

        /// <summary>
        /// write message to log window and file
        /// </summary>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void Write(string message, params object[] args)
        {
            Write(Color.Green, message, args);
        }

        /// <summary>
        /// write message to log window and file.  overrides log windows duplicate
        /// line suppression by ensuring adjoining lines differ
        /// </summary>
        /// <param name="clr">color of message in window</param>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void Write(Color clr, string message, params object[] args)
        {
            string sUniqueChar = (lineNo++ & 1) == 0 ? "" : " ";
            System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
            if (GlobalSettings.Instance.LogLevel >= LogLevel.Normal)
                Logging.Write(newColor, "[Singular] " + message + sUniqueChar, args);
            else if (GlobalSettings.Instance.LogLevel == LogLevel.Quiet)
                Logging.WriteToFileSync(LogLevel.Normal, "[Singular] " + message + sUniqueChar, args);
        }

        /// <summary>
        /// write message to log window if Singular Debug Enabled setting true
        /// </summary>
        /// <param name="message">message text</param>
        public static void WriteDebug(string message)
        {
            WriteDebug( LogColor.Debug, message);
        }

        /// <summary>
        /// write message to log window if Singular Debug Enabled setting true
        /// </summary>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteDebug(string message, params object[] args)
        {
            WriteDebug( LogColor.Debug, message, args);
        }

        /// <summary>
        /// write message to log window if Singular Debug Enabled setting true
        /// </summary>
        /// <param name="clr">color of message in window</param>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteDebug(Color clr, string message, params object[] args)
        {
            if (SingularSettings.Debug)
            {
                if (SingularSettings.Instance.DebugOutput == DebugOutputDest.FileOnly)
                    Logging.WriteToFileSync(LogLevel.Normal, "(Singular) " + message, args);

                else // if (SingularSettings.Instance.DebugOutput == DebugOutputDest.WindowAndFile)
                {
                    System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B);                    
                    Logging.Write(newColor, "(Singular) " + message, args);
                }
            }
        }

        /// <summary>
        /// write message to log file
        /// </summary>
        /// <param name="message">message text</param>
        public static void WriteFile(string message)
        {
            WriteFile(LogLevel.Normal, message);
        }

        /// <summary>
        /// write message to log file
        /// </summary>
        /// <param name="message">message text with replaceable parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteFile(string message, params object[] args)
        {
            WriteFile(LogLevel.Normal, message, args);
        }

        /// <summary>
        /// write message to log file
        /// </summary>
        /// <param name="ll">level to code entry with (doesn't control if written)</param>
        /// <param name="message">message text with replaceable parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteFile( LogLevel ll, string message, params object[] args)
        {
            if ( GlobalSettings.Instance.LogLevel >= LogLevel.Quiet)
                Logging.WriteToFileSync( ll, "(Singular) " + message, args);
        }

        /// <summary>
        /// write message to log window if Singular Debug Enabled setting true
        /// </summary>
        /// <param name="message">message text</param>
        public static void WriteDiagnostic(string message)
        {
            WriteDiagnostic( LogColor.Debug, message);
        }

        /// <summary>
        /// write message to log window if Singular Debug Enabled setting true
        /// </summary>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteDiagnostic(string message, params object[] args)
        {
            WriteDiagnostic( LogColor.Debug, message, args);
        }

        /// <summary>
        /// output a diagnostic message.  message is always written to log file, but is also written
        /// to log window if Debug enabled
        /// </summary>
        /// <param name="clr">color of message in window</param>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteDiagnostic(Color clr, string message, params object[] args)
        {
            if (SingularSettings.Instance != null && SingularSettings.Instance.DebugOutput == DebugOutputDest.WindowAndFile)
            {
                System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
                Logging.Write(newColor, "(Singular) " + message, args);
            }
            else
            {
                WriteFile("(Singular) " + message, args);
            }
        }

        /// <summary>
        /// output a diagnostic message.  message is always written to log file, but is also written
        /// to log window if Debug enabled
        /// </summary>
        /// <param name="clr">color of message in window</param>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteTrace(Color clr, string message, params object[] args)
        {
            if (SingularSettings.Instance != null && SingularSettings.Instance.DebugOutput == DebugOutputDest.WindowAndFile)
            {
                System.Windows.Media.Color newColor = System.Windows.Media.Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
                Logging.Write(newColor, "|Singular| " + message, args);
            }
            else
            {
                WriteFile("|Singular| " + message, args);
            }
        }

        public static void PrintStackTrace(string reason = "Debug")
        {
            // add +1 to level to begin with caller
            PrintStackTrace(1, 10, reason);
        }

        public static void PrintStackTrace(int levelsUp, int levelsCnt, string reason)
        {
            WriteDebug("Stack trace for " + reason);
            var stackTrace = new StackTrace(true);
            StackFrame[] stackFrames = stackTrace.GetFrames();
            // Start at frame 1 (just before this method entrance)
            for (int i = 1 + levelsUp; i < Math.Min(stackFrames.Length, levelsCnt); i++)
            {
                StackFrame frame = stackFrames[i];
                WriteDebug(string.Format("\tCaller {0}: {1} in {2} line {3}", i, frame.GetMethod().Name, Path.GetFileName(frame.GetFileName()), frame.GetFileLineNumber()));
            }
        }


        /// <summary>
        /// write behavior creation message to log window and file
        /// </summary>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteInBehaviorCreate(string message, params object[] args)
        {
            if (!Dynamics.CompositeBuilder.SilentBehaviorCreation)
                Write(message, args);
        }

        public static void WriteInBehaviorCreate(Color clr, string message, params object[] args)
        {
            if (!Dynamics.CompositeBuilder.SilentBehaviorCreation)
                Write(clr, message, args);
        }

        /// <summary>
        /// write behavior creation message to log window and file
        /// </summary>
        /// <param name="message">message text with embedded parameters</param>
        /// <param name="args">replacement parameter values</param>
        public static void WriteDebugInBehaviorCreate(string message, params object[] args)
        {
            if (!Dynamics.CompositeBuilder.SilentBehaviorCreation)
                WriteDebug(message, args);
        }

        public static void WriteDebugInBehaviorCreate(Color clr, string message, params object[] args)
        {
            if (!Dynamics.CompositeBuilder.SilentBehaviorCreation)
                WriteDebug(clr, message, args);
        }


        #region Helpers

        public static void TellUser(string template, params object[] args)
        {
            string msg = string.Format(template, args);
            Logger.Write(Color.Yellow, msg);
            if (SingularSettings.Instance.Hotkeys().ChatFrameMessage)
            {
                StyxWoW.Overlay.AddToast(
                    () => { return msg; },
                    TimeSpan.FromMilliseconds(SingularSettings.Instance.Hotkeys().ChatFrameMessageDuration),
                    System.Windows.Media.Colors.LightYellow,
                    System.Windows.Media.Colors.Blue,
                    new System.Windows.Media.FontFamily("Consolas")
                    );
            }
        }

        #endregion

    }

    public class LogMessage : Action
    {
        private readonly string message;

        public LogMessage(string message)
        {
            this.message = message;
        }

        protected override RunStatus Run(object context)
        {
            Logger.Write(message);

            if (Parent is Selector)
                return RunStatus.Failure;
            return RunStatus.Success;
        }
    }

    public class SeqLog : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public SeqLog(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Success, new Action(r => { Logger.Write(msg(r)); return RunStatus.Success; }))
        {
        }
        public SeqLog(double secs, Color clr, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Success, new Action(r => { Logger.Write(clr, msg(r)); return RunStatus.Success; }))
        {
        }
    }

    public class SeqDbg : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public SeqDbg(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Success, new Action(r => { if (SingularSettings.Debug) Logger.WriteDebug(msg(r)); return RunStatus.Success; }))
        {
        }
    }

    public class SeqDiag : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public SeqDiag(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Success, new Action(r => { Logger.WriteDiagnostic(msg(r)); return RunStatus.Success; }))
        {
        }
    }

    public class PriLog : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public PriLog(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Failure, new Action(r => { Logger.Write(msg(r)); return RunStatus.Failure; }))
        {
        }
        public PriLog(double secs, Color clr, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Failure, new Action(r => { Logger.Write(clr, msg(r)); return RunStatus.Failure; }))
        {
        }
    }

    public class PriDbg : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public PriDbg(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Failure, new Action(r => { if (SingularSettings.Debug) Logger.WriteDebug(msg(r)); return RunStatus.Failure; }))
        {
        }
    }

    public class PriDiag : ThrottlePasses
    {
        SimpleStringDelegate msg;

        public PriDiag(double secs, SimpleStringDelegate msg)
            : base(1, TimeSpan.FromSeconds(secs), RunStatus.Failure, new Action(r => { Logger.WriteDiagnostic(msg(r)); return RunStatus.Failure; }))
        {
        }
    }

}