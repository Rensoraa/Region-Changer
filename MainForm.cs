using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Security.Principal;

namespace HostsEditor
{
    public class MainForm : Form
    {
        private Label lblTitle;
        private ComboBox cmbRegions;
        private Button btnApply;
        private Button btnRestore;

        private string hostsPath;

        private readonly (string Name, string? Id)[] regions = new (string, string?)[]
        {
            ("Default", null),
            ("N. Virginia", "us-east-1"),
            ("Ohio", "us-east-2"),
            ("N. California", "us-west-1"),
            ("Oregon", "us-west-2"),
            ("Frankfurt", "eu-central-1"),
            ("Ireland", "eu-west-1"),
            ("London", "eu-west-2"),
            ("South America", "sa-east-1"),
            ("Mumbai", "ap-south-1"),
            ("Seoul", "ap-northeast-2"),
            ("Singapore", "ap-southeast-1"),
            ("Sydney", "ap-southeast-2"),
            ("Tokyo", "ap-northeast-1"),
            ("Hong Kong", "ap-east-1"),
            ("Canada", "ca-central-1"),
            ("All Asia", "ALL_ASIA") // Custom paid option
        };

        private readonly string[] asiaRegionIds = new string[]
        {
            "ap-south-1",
            "ap-northeast-2",
            "ap-southeast-1",
            "ap-southeast-2",
            "ap-northeast-1",
            "ap-east-1"
        };

        public MainForm()
        {
            // === Form settings ===
            this.Text = "Region changer V0.1";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            // === Title ===
            lblTitle = new Label();
            lblTitle.Text = "Region Changer V0.1";
            lblTitle.ForeColor = Color.White;
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(80, 20);
            this.Controls.Add(lblTitle);

            // === Region dropdown ===
            cmbRegions = new ComboBox();
            foreach (var region in regions)
                cmbRegions.Items.Add(region.Name);

            cmbRegions.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRegions.Location = new Point(50, 70);
            cmbRegions.Size = new Size(300, 30);
            cmbRegions.BackColor = Color.FromArgb(50, 50, 50);
            cmbRegions.ForeColor = Color.White;
            this.Controls.Add(cmbRegions);

            // === Apply button ===
            btnApply = new Button();
            btnApply.Text = "Apply Region";
            btnApply.Size = new Size(140, 35);
            btnApply.Location = new Point(50, 120);
            btnApply.BackColor = Color.FromArgb(190, 20, 20);
            btnApply.ForeColor = Color.White;
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.Click += (s, e) => ApplyRegion();
            this.Controls.Add(btnApply);

            // === Restore button ===
            btnRestore = new Button();
            btnRestore.Text = "Restore Hosts";
            btnRestore.Size = new Size(140, 35);
            btnRestore.Location = new Point(210, 120);
            btnRestore.BackColor = Color.FromArgb(20, 190, 20);
            btnRestore.ForeColor = Color.White;
            btnRestore.FlatStyle = FlatStyle.Flat;
            btnRestore.Click += (s, e) => RestoreHosts();
            this.Controls.Add(btnRestore);

            // Determine hosts file location
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                hostsPath = @"C:\Windows\System32\drivers\etc\hosts";
            else
                hostsPath = "/etc/hosts";

            // Ensure admin/root access
            if (!IsAdministrator())
            {
                MessageBox.Show("You need to run this application as Administrator.", "Permission Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.Exit(0);
            }
        }

        private bool IsAdministrator()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            return false;
        }

        private void ApplyRegion()
        {
            string selectedRegionName = cmbRegions.SelectedItem?.ToString() ?? "Default";
            string? selectedRegionId = null;
            foreach (var r in regions)
                if (r.Name == selectedRegionName)
                    selectedRegionId = r.Id;

            RemoveAllOverrides();

            if (!string.IsNullOrEmpty(selectedRegionId))
                AppendHostsEntries(selectedRegionId);

            MessageBox.Show($"Region applied: {selectedRegionName}", "Hosts Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RestoreHosts()
        {
            RemoveAllOverrides();
            MessageBox.Show("Hosts file restored to default.", "Hosts Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AppendHostsEntries(string regionId)
        {
            var lines = File.ReadAllLines(hostsPath);
            var filteredLines = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                if (!IsGameliftLine(line))
                    filteredLines.Add(line);
            }

            if (regionId == "ALL_ASIA")
            {
                foreach (var r in regions)
                {
                    if (r.Id != null && Array.IndexOf(asiaRegionIds, r.Id) == -1) // Not Asia
                    {
                        filteredLines.Add($"0.0.0.0 gamelift.{r.Id}.amazonaws.com");
                        filteredLines.Add($"0.0.0.0 gamelift-ping.{r.Id}.api.aws");
                    }
                }
            }
            else
            {
                filteredLines.Add($"0.0.0.0 gamelift.{regionId}.amazonaws.com");
                filteredLines.Add($"0.0.0.0 gamelift-ping.{regionId}.api.aws");
            }

            File.WriteAllLines(hostsPath, filteredLines);
        }

        private void RemoveAllOverrides()
        {
            var lines = File.ReadAllLines(hostsPath);
            var filteredLines = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                if (!IsGameliftLine(line))
                    filteredLines.Add(line);
            }
            File.WriteAllLines(hostsPath, filteredLines);
        }

        private bool IsGameliftLine(string line)
        {
            return Regex.IsMatch(line, @".*\sgamelift\.[^.]+\.amazonaws\.com$") ||
                   Regex.IsMatch(line, @".*\sgamelift-ping\.[^.]+\.api\.aws$");
        }
    }
}
