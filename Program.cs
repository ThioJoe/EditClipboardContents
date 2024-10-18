using EditClipboardContents;
using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Configuration;

#nullable enable

namespace EditClipboardContents
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // -------- This code seems to be needed to actually get it to work with high DPI awareness even after adding app.manifest and app.config stuff --------
            // Also need to add reference to System.Configuration as dependency by right clicking Project > Add > Reference > Search: System.Configuration
            if (ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") is NameValueCollection section)
            {
                section["DpiAwareness"] = "PerMonitorV2";
            }
            //------------------------------------------------------------------------------------------------------------------------------------------------------

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}