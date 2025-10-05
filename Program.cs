using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace HostsEditor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                // Relaunch as admin
                var processInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas" // this triggers the admin prompt
                };

                try
                {
                    Process.Start(processInfo);
                }
                catch
                {
                    MessageBox.Show("Administrator privileges are required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
