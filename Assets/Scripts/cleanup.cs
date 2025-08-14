using System;
using System.Linq;
using UnityEngine;

public class CmdLineDiag : MonoBehaviour
{
    public static bool LeakValidationEnabled { get; private set; }

    const string Flag = "-diag-temp-memory-leak-validation";

    void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        LeakValidationEnabled = args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase));
        if (LeakValidationEnabled)
        {
            Debug.Log("[CmdLineDiag] Leak validation flag detected.");

            // (Editor/Dev only) turn on stricter checks you want behind the flag:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 1) Make logs as verbose as possible
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);

            // 2) (Editor) enable native container leak checks via the menu:
            // Jobs > Leak Detection > Enabled With Stack Trace
            // (You can’t flip that via code reliably across versions.)
#endif
        }
    }
}
