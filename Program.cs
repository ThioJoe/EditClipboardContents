using EditClipboardContents;
using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Configuration;
using System.Runtime.InteropServices;

#nullable enable

namespace EditClipboardContents
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [STAThread]
        static void Main(string[] args)
        {
            bool showConsole = args.Length > 0 && (args[0].ToLower() == "-console");
            bool debugMode = args.Length > 0 && (args[0].ToLower() == "-debug");

            if (showConsole || debugMode)
            {
                AllocConsole();
                Console.WriteLine("Debug console attached.");
            }


            // -------- This code seems to be needed to actually get it to work with high DPI awareness even after adding app.manifest and app.config stuff --------
            // Also need to add reference to System.Configuration as dependency by right clicking Project > Add > Reference > Search: System.Configuration
            if (ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") is NameValueCollection section)
            {
                section["DpiAwareness"] = "PerMonitorV2";
            }
            //------------------------------------------------------------------------------------------------------------------------------------------------------

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(debugMode:debugMode));

            if (showConsole || debugMode)
            {
                FreeConsole();
            }
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}