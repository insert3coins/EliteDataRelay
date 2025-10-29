using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using EliteDataRelay.Configuration;

namespace EliteDataRelay.UI
{
    public class DiagnosticsForm : Form
    {
        private readonly TextBox _text;
        private readonly System.Windows.Forms.Timer _timer;
        private long _lastLength;
        private readonly string _logPath;

        public DiagnosticsForm()
        {
            this.Text = Properties.Strings.Diagnostics_Title;
            this.Size = new System.Drawing.Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            _logPath = Path.Combine(AppConfiguration.AppDataPath, "debug_log.txt");

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            _text = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 9f)
            };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, FlowDirection = FlowDirection.LeftToRight };
            var btnOpenFolder = new Button { Text = Properties.Strings.Diagnostics_OpenFolder };
            var btnClear = new Button { Text = Properties.Strings.Diagnostics_ClearLog };
            var btnRefresh = new Button { Text = Properties.Strings.Diagnostics_Refresh };

            btnOpenFolder.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(Path.GetDirectoryName(_logPath)!) { UseShellExecute = true }); } catch { }
            };
            btnClear.Click += (s, e) => { try { File.WriteAllText(_logPath, string.Empty); _lastLength = 0; _text.Clear(); } catch { } };
            btnRefresh.Click += (s, e) => { try { _lastLength = 0; _text.Clear(); ReadTail(); } catch { } };

            btnPanel.Controls.Add(btnOpenFolder);
            btnPanel.Controls.Add(btnClear);
            btnPanel.Controls.Add(btnRefresh);

            panel.Controls.Add(_text);
            panel.Controls.Add(btnPanel);
            this.Controls.Add(panel);

            _timer = new System.Windows.Forms.Timer { Interval = 800 };
            _timer.Tick += (s, e) => ReadTail();
            this.Load += (s, e) => { _lastLength = 0; ReadTail(); _timer.Start(); };
            this.FormClosed += (s, e) => { try { _timer.Stop(); _timer.Dispose(); } catch { } };
        }

        private void ReadTail()
        {
            try
            {
                if (!File.Exists(_logPath)) return;
                using var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < _lastLength) _lastLength = 0; // rotated/cleared
                if (fs.Length == _lastLength) return;
                fs.Seek(_lastLength, SeekOrigin.Begin);
                using var sr = new StreamReader(fs, Encoding.UTF8, true, 4096, true);
                string chunk = sr.ReadToEnd();
                _lastLength = fs.Length;
                if (!string.IsNullOrEmpty(chunk))
                {
                    _text.AppendText(chunk);
                }
            }
            catch { }
        }
    }
}





