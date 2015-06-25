using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using MovieGateway.Properties;

namespace MovieGateway
{


    internal class MovieGateway : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayIconContextMenu;
        private ToolStripMenuItem _closeMenuItem;
        private ToolStripMenuItem _listenMenuItem;
        private ToolStripMenuItem _autoStartMenuItem;

        public MovieGateway()
        {
            InitializeComponent();
            Application.ApplicationExit += OnApplicationExit;
            ClipboardNotification.ClipboardUpdate += ClipboardNotification_ClipboardUpdate;
            _trayIcon.Visible = true;


        }

        private void InitializeComponent()
        {
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.TrayIcon
            };

            _listenMenuItem = new ToolStripMenuItem
            {
                CheckOnClick = true,
                Checked = true,
                Text = "Check for Links"
            };

            _autoStartMenuItem = new ToolStripMenuItem
            {
                CheckOnClick = true,
                Checked = false,
                Text = "Start with Windows"
            };
            _autoStartMenuItem.CheckedChanged += _autoStartMenuItem_CheckedChanged;

            _autoStartMenuItem.Checked = IsStartupItem();

            _closeMenuItem = new ToolStripMenuItem {Text = "Close"};
            _closeMenuItem.Click += CloseMenuItem_Click;

            _trayIconContextMenu = new ContextMenuStrip();
            _trayIconContextMenu.SuspendLayout();
            _trayIconContextMenu.Items.AddRange(new ToolStripItem[]
            {
                _listenMenuItem,
                _autoStartMenuItem,
                _closeMenuItem
                
            });

            _trayIconContextMenu.ResumeLayout(false);
            _trayIcon.ContextMenuStrip = _trayIconContextMenu;
        }

        private async void ClipboardNotification_ClipboardUpdate(object sender, EventArgs e)
        {
            if (_listenMenuItem.Checked)
            {
                if (Clipboard.ContainsText())
                {
                    var clipboardtext = Clipboard.GetText();

                    if (clipboardtext.StartsWith("http://vivo.sx/"))
                    {
                        _trayIcon.ShowBalloonTip(50, "Found a viable Link", "Going to open: " + clipboardtext,
                            ToolTipIcon.Info);
                        Process.Start(await GetVivoDirectlink(clipboardtext));
                    }

                    if (clipboardtext.StartsWith("http://streamcloud.eu"))
                    {
                        _trayIcon.ShowBalloonTip(50, "Found a viable Link", "Going to open: " + clipboardtext,
                            ToolTipIcon.Info);
                        Process.Start(await GetStreamcloudDirectlink(clipboardtext));
                    }

                    if (clipboardtext.StartsWith("http://shared.sx"))
                    {
                        _trayIcon.ShowBalloonTip(50, "Found a viable Link", "Going to open: " + clipboardtext,
                            ToolTipIcon.Info);
                        Process.Start(await GetSharedDirectlink(clipboardtext));
                    }
                }
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
        }


        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to close me?", "Are you sure?", MessageBoxButtons.YesNo,
                MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            ClipboardNotification.ClipboardUpdate -= ClipboardNotification_ClipboardUpdate;
            Application.Exit();
        }


        private async Task<string> GetSharedDirectlink(string sharedLink)
        {
            using (HttpClient client = new HttpClient())
            {
                string sharedSource = await client.GetStringAsync(sharedLink);

                var regexHash = new Regex("\"hash\" value=\"(.*?)\"");
                var regexExpires = new Regex("\"expires\" value=\"(.*?)\"");
                var regexTimestamp = new Regex("\"timestamp\" value=\"(.*?)\"");
                Match matchHash = regexHash.Match(sharedSource); //matchVivo.Groups[1].Value)
                Match matchExpires = regexExpires.Match(sharedSource); //matchExpires.Groups[1].Value)
                Match matchTimestamp = regexTimestamp.Match(sharedSource); //matchTimestamp.Groups[1].Value)
                string hash = matchHash.Groups[1].Value;
                string expires = matchExpires.Groups[1].Value;
                string timestamp = matchTimestamp.Groups[1].Value;
                var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("hash", hash),
                        new KeyValuePair<string, string>("expires", expires),
                        new KeyValuePair<string, string>("timestamp", timestamp),
                    });
                await Task.Run(() =>
                {
                    Thread.Sleep(100);
                });
                var response = await client.PostAsync(sharedLink, formContent);
                var stringContent = await response.Content.ReadAsStringAsync();
                var regex = new Regex("data-url=\"(.*?)\"");
                Match match = regex.Match(stringContent);
                return match.Groups[1].Value;
            }
        }

        private async Task<string> GetStreamcloudDirectlink(string streamcloudLink)
        {
            using (HttpClient client = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("op", "download2"),
                    new KeyValuePair<string, string>("usr ", "login"),
                    new KeyValuePair<string, string>("id", streamcloudLink.Split('/')[3]),
                    new KeyValuePair<string, string>("fname", streamcloudLink.Split('/')[4]),
                    new KeyValuePair<string, string>("referer", ""),
                    new KeyValuePair<string, string>("hash", ""),
                    new KeyValuePair<string, string>("iamhuman", "Weiter zum Video")
                });
                var response = await client.PostAsync(streamcloudLink, formContent);
                var stringContent = await response.Content.ReadAsStringAsync();
                var regex = new Regex("file: \"(.*?)\",");
                Match match = regex.Match(stringContent);
                return match.Groups[1].Value;
            }

        }
        private async Task<string> GetVivoDirectlink(string vivoLink)
        {
            using (HttpClient client = new HttpClient())
            {
                string vivoSource = await client.GetStringAsync(vivoLink);
                var regexHash = new Regex("\"hash\" value=\"(.*?)\"");
                var regexExpires = new Regex("\"expires\" value=\"(.*?)\"");
                var regexTimestamp = new Regex("\"timestamp\" value=\"(.*?)\"");
                Match matchHash = regexHash.Match(vivoSource); //matchVivo.Groups[1].Value)
                Match matchExpires = regexExpires.Match(vivoSource); //matchExpires.Groups[1].Value)
                Match matchTimestamp = regexTimestamp.Match(vivoSource); //matchTimestamp.Groups[1].Value)
                string hash = matchHash.Groups[1].Value;
                string expires = matchExpires.Groups[1].Value;
                string timestamp = matchTimestamp.Groups[1].Value;
                var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("hash", hash),
                        new KeyValuePair<string, string>("expires", expires),
                        new KeyValuePair<string, string>("timestamp", timestamp),
                    });

                var response = await client.PostAsync(vivoLink, formContent);
                var stringContent = await response.Content.ReadAsStringAsync();
                var regex = new Regex("data-url=\"(.*?)\"");
                Match match = regex.Match(stringContent);
                return match.Groups[1].Value;
            }
        }


        private bool IsStartupItem()
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rkApp != null && rkApp.GetValue("MovieGateway") == null) return false;
            return true;
        }

        void _autoStartMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (_autoStartMenuItem.Checked)
            {
                // The path to the key where Windows looks for startup applications
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (!IsStartupItem())
                    if (rkApp != null) rkApp.SetValue("MovieGateway", Application.ExecutablePath);
            }
            if (_autoStartMenuItem.Checked == false)
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (IsStartupItem())
                {
                    if (rkApp != null) rkApp.DeleteValue("MovieGateway", false);
                }

            }
        }
    }

}
