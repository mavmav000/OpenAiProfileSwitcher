using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenAIProfileSwitcher;

public sealed class MainForm : Form
{
    private readonly SettingsStore _store = new();
    private AppSettings _settings;
    private readonly ComboBox _clientPicker = new();
    private readonly CheckBox _sharedCodexCheck = new();
    private readonly TextBox _clientPath = new();
    private readonly ListView _profileList = new();
    private readonly Label _status = new();
    private Button _launchButton = new();
    private Button _launchAllButton = new();
    private List<ClientOption> _clients = new();

    public MainForm()
    {
        _settings = _store.Load();
        Text = "OpenAI Desktop Profiles";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 480);
        Size = new Size(980, 620);
        Font = new Font("Segoe UI", 9F);
        DarkTheme.StyleForm(this);

        BuildUi();
        RefreshClients();
        RefreshProfiles();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            BackColor = DarkTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var title = new Label
        {
            Text = "OpenAI Desktop Profiles",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = DarkTheme.Text,
            Margin = new Padding(0, 0, 0, 4)
        };
        root.Controls.Add(title, 0, 0);

        var explanation = new Label
        {
            Text = "Launch separate local profiles for the installed Codex or ChatGPT desktop client.\n" +
                   "Sign in through the official client; this manager never reads or stores passwords, cookies, or tokens.",
            AutoSize = true,
            ForeColor = DarkTheme.SecondaryText,
            Margin = new Padding(0, 0, 0, 14)
        };
        root.Controls.Add(explanation, 0, 1);

        var clientPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            RowCount = 3,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        clientPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        clientPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        clientPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        clientPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(clientPanel, 0, 2);

        var clientLabel = new Label
        {
            Text = "Client:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 7, 10, 0)
        };
        clientPanel.Controls.Add(clientLabel, 0, 0);

        _clientPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        DarkTheme.StyleComboBox(_clientPicker);
        _clientPicker.Dock = DockStyle.Fill;
        _clientPicker.Margin = new Padding(0, 0, 8, 0);
        _clientPicker.SelectedIndexChanged += (_, _) => UseSelectedClient();
        clientPanel.Controls.Add(_clientPicker, 1, 0);

        var browseButton = MakeButton("Browse…", 90);
        browseButton.Click += (_, _) => BrowseForClient();
        clientPanel.Controls.Add(browseButton, 2, 0);

        var refreshButton = MakeButton("Refresh", 80);
        refreshButton.Click += (_, _) => RefreshClients();
        clientPanel.Controls.Add(refreshButton, 3, 0);

        _clientPath.ReadOnly = true;
        _clientPath.BorderStyle = BorderStyle.None;
        _clientPath.BackColor = DarkTheme.Background;
        _clientPath.ForeColor = DarkTheme.SecondaryText;
        _clientPath.Dock = DockStyle.Fill;
        _clientPath.Margin = new Padding(0, 4, 0, 0);
        clientPanel.Controls.Add(_clientPath, 1, 1);
        clientPanel.SetColumnSpan(_clientPath, 3);

        _sharedCodexCheck.AutoSize = true;
        DarkTheme.StyleCheckBox(_sharedCodexCheck);
        _sharedCodexCheck.Text = "Share Codex data across profiles (experimental; one profile at a time)";
        _sharedCodexCheck.Checked = _settings.SharedCodexHome;
        _sharedCodexCheck.Margin = new Padding(0, 12, 0, 0);
        _sharedCodexCheck.CheckedChanged += SharedCodexCheckChanged;
        clientPanel.Controls.Add(_sharedCodexCheck, 0, 2);
        clientPanel.SetColumnSpan(_sharedCodexCheck, 4);

        _profileList.Dock = DockStyle.Fill;
        _profileList.View = View.Details;
        DarkTheme.StyleListView(_profileList);
        _profileList.FullRowSelect = true;
        _profileList.GridLines = false;
        _profileList.HideSelection = false;
        _profileList.MultiSelect = false;
        _profileList.Columns.Add("Profile", 190);
        _profileList.Columns.Add("Saved session", 130);
        _profileList.Columns.Add("Last opened", 155);
        _profileList.Columns.Add("Local profile directory", 430);
        _profileList.DoubleClick += (_, _) => LaunchSelected();
        root.Controls.Add(_profileList, 0, 3);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 12, 0, 0)
        };
        root.Controls.Add(actionPanel, 0, 4);

        var addButton = MakeButton("Add profile", 110);
        addButton.Click += (_, _) => AddProfile();
        actionPanel.Controls.Add(addButton);

        _launchButton = MakeButton("Launch selected", 125);
        DarkTheme.StyleButton(_launchButton, primary: true);
        _launchButton.Click += (_, _) => LaunchSelected();
        actionPanel.Controls.Add(_launchButton);

        _launchAllButton = MakeButton("Launch all", 100);
        _launchAllButton.Click += async (_, _) => await LaunchAllAsync();
        actionPanel.Controls.Add(_launchAllButton);

        var openFolderButton = MakeButton("Open profile folder", 145);
        openFolderButton.Click += (_, _) => OpenSelectedFolder();
        actionPanel.Controls.Add(openFolderButton);

        var forgetButton = MakeButton("Forget profile", 110);
        forgetButton.Click += (_, _) => ForgetSelected();
        actionPanel.Controls.Add(forgetButton);

        _status.AutoSize = true;
        _status.ForeColor = DarkTheme.SecondaryText;
        _status.Margin = new Padding(8, 8, 0, 0);
        actionPanel.Controls.Add(_status);
    }

    private static Button MakeButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = width,
            Height = 32,
            Margin = new Padding(0, 0, 8, 0)
        };
        DarkTheme.StyleButton(button);
        return button;
    }

    private void RefreshClients()
    {
        _clients = DiscoverClients();
        if (!string.IsNullOrWhiteSpace(_settings.ClientPath) && File.Exists(_settings.ClientPath) &&
            _clients.All(client => !PathsEqual(client.Path, _settings.ClientPath)))
        {
            _clients.Insert(0, new ClientOption("Configured client", _settings.ClientPath));
        }

        _clientPicker.Items.Clear();
        foreach (var client in _clients)
        {
            _clientPicker.Items.Add(client);
        }

        if (_clients.Count == 0)
        {
            _clientPath.Text = "No installed client found. Use Browse… to select ChatGPT.exe or ChatGPT Classic.exe.";
            SetStatus("Choose the official desktop client executable.");
            return;
        }

        var preferredIndex = _settings.ClientPath is null
            ? 0
            : Math.Max(0, _clients.FindIndex(client => PathsEqual(client.Path, _settings.ClientPath)));
        _clientPicker.SelectedIndex = preferredIndex;
        UseSelectedClient();
    }

    private void UseSelectedClient()
    {
        if (_clientPicker.SelectedItem is not ClientOption client)
        {
            return;
        }

        _settings.ClientPath = client.Path;
        SaveSettings();
        _clientPath.Text = client.Path;
    }

    private void BrowseForClient()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select the official Codex or ChatGPT desktop executable",
            Filter = "OpenAI desktop executable|ChatGPT.exe;ChatGPT Classic.exe|Executable files|*.exe|All files|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var option = new ClientOption("Manually selected client", dialog.FileName);
        _clients.Insert(0, option);
        _clientPicker.Items.Insert(0, option);
        _clientPicker.SelectedIndex = 0;
        SetStatus("Client selected.");
    }

    private void SharedCodexCheckChanged(object? sender, EventArgs e)
    {
        if (_sharedCodexCheck.Checked && !_settings.SharedCodexHome)
        {
            var answer = DarkTheme.ShowMessage(this,
                "Shared mode uses your existing %USERPROFILE%\\.codex directory for every profile, " +
                "including Codex credentials and session history. It does NOT keep account logins separate.\n\n" +
                "Back up that directory before enabling shared mode. Close all Codex/ChatGPT desktop windows " +
                "before switching accounts. Only one profile can be launched at a time.\n\n" +
                "Existing conversations may still be account-bound and might not resume under another login.\n\n" +
                "Enable experimental shared mode?",
                "Enable shared Codex data?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                _sharedCodexCheck.Checked = false;
                return;
            }
        }

        _settings.SharedCodexHome = _sharedCodexCheck.Checked;
        SaveSettings();
        RefreshProfiles();
        SetStatus(_settings.SharedCodexHome
            ? "Shared mode enabled: close all Codex windows before launching or switching profiles."
            : "Isolated profile mode enabled. Existing shared data was not moved or deleted.");
    }

    private void AddProfile()
    {
        using var dialog = new ProfileDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var profile = new AccountProfile
        {
            Label = dialog.ProfileLabel,
            Id = Guid.NewGuid().ToString("N")
        };
        profile.UserDataDirectory = Path.Combine(_store.ProfilesDirectory, profile.Id);
        Directory.CreateDirectory(profile.UserDataDirectory);
        _settings.Profiles.Add(profile);
        SaveSettings();
        RefreshProfiles();
        SelectProfile(profile.Id);
        SetStatus($"Added “{profile.Label}”. Launch it to sign in.");
    }

    private AccountProfile? SelectedProfile()
    {
        if (_profileList.SelectedItems.Count == 0)
        {
            return null;
        }

        var id = _profileList.SelectedItems[0].Tag as string;
        return _settings.Profiles.FirstOrDefault(profile => profile.Id == id);
    }

    private void LaunchSelected()
    {
        var profile = SelectedProfile();
        if (profile is null)
        {
            SetStatus("Select a profile first.");
            return;
        }

        LaunchProfile(profile);
    }

    private async Task LaunchAllAsync()
    {
        if (_settings.SharedCodexHome)
        {
            SetStatus("Launch all is disabled in shared mode to prevent concurrent writes to Codex state.");
            return;
        }

        if (_settings.Profiles.Count == 0)
        {
            SetStatus("Add a profile first.");
            return;
        }

        var profiles = _settings.Profiles.ToArray();
        _launchButton.Enabled = false;
        _launchAllButton.Enabled = false;
        SaveSettings();

        var launched = 0;
            try
            {
                foreach (var profile in profiles)
                {
                var process = LaunchProfile(profile, showErrors: false);
                if (process is null)
                {
                    continue;
                }

                await WaitForVisibleWindowAsync(process);
                await Task.Delay(1000);
                launched++;
            }

            SetStatus($"Launched {launched} profile(s) one at a time. Sign in once in each window.");
        }
        finally
        {
            RefreshProfiles();
            _launchButton.Enabled = _profileList.Items.Count > 0;
            _launchAllButton.Enabled = _settings.Profiles.Count > 0 && !_settings.SharedCodexHome;
        }
    }

    private Process? LaunchProfile(AccountProfile profile, bool showErrors = true)
    {
        var clientPath = _settings.ClientPath;
        if (string.IsNullOrWhiteSpace(clientPath) || !File.Exists(clientPath))
        {
            if (showErrors)
            {
                DarkTheme.ShowMessage(this,
                    "Select the official Codex or ChatGPT desktop executable before launching.",
                    "Client not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        // Shared Codex homes include an authentication file and a writable session database.
        // Refuse to launch another Codex window while a recognized desktop process is running.
        if (_settings.SharedCodexHome && IsOpenAiDesktopRunning())
        {
            const string message = "Close every Codex/ChatGPT desktop window before launching a shared profile. " +
                "Shared mode cannot safely run multiple account windows at once.";
            if (showErrors)
            {
                DarkTheme.ShowMessage(this, message, "Codex is already running",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            SetStatus(message);
            return null;
        }

        try
        {
            Directory.CreateDirectory(profile.UserDataDirectory);
            var startInfo = new ProcessStartInfo
            {
                FileName = clientPath,
                WorkingDirectory = Path.GetDirectoryName(clientPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add($"--user-data-dir={profile.UserDataDirectory}");
            startInfo.ArgumentList.Add("--new-window");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-default-browser-check");
            ConfigureProfileEnvironment(startInfo, profile.UserDataDirectory, _settings.SharedCodexHome);
            var process = Process.Start(startInfo);
            if (process is null)
            {
                throw new InvalidOperationException("The official client did not start.");
            }
            profile.LastLaunchedUtc = DateTime.UtcNow;
            SaveSettings();
            SetStatus($"Launched “{profile.Label}”. Complete sign-in in the official client if needed.");
            return process;
        }
        catch (Exception ex)
        {
            if (showErrors)
            {
                DarkTheme.ShowMessage(this, ex.Message, "Could not launch client", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }
    }

    private static async Task WaitForVisibleWindowAsync(Process process)
    {
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(15);

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                if (process.HasExited)
                {
                    return;
                }

                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            await Task.Delay(250);
        }
    }

    private static bool IsOpenAiDesktopRunning()
    {
        foreach (var processName in new[] { "ChatGPT", "ChatGPT Classic", "Codex" })
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { return true; } // Fail closed if processes cannot be inspected.

            try
            {
                foreach (var process in processes)
                {
                    try { if (!process.HasExited) return true; }
                    catch { return true; }
                }
            }
            finally
            {
                foreach (var process in processes) process.Dispose();
            }
        }
        return false;
    }

    private static void ConfigureProfileEnvironment(
        ProcessStartInfo startInfo, string profileDirectory, bool sharedCodexHome)
    {
        var runtimeDirectory = Path.Combine(profileDirectory, "runtime");
        var roamingDirectory = Path.Combine(runtimeDirectory, "AppData", "Roaming");
        var localDirectory = Path.Combine(runtimeDirectory, "AppData", "Local");
        var tempDirectory = Path.Combine(runtimeDirectory, "Temp");
        // Sharing the default home retains the existing Codex sessions/settings rather than
        // creating an empty shared directory. Authentication in this home is ALSO shared.
        var codexHomeDirectory = sharedCodexHome
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex")
            : Path.Combine(runtimeDirectory, "CodexHome");

        Directory.CreateDirectory(roamingDirectory);
        Directory.CreateDirectory(localDirectory);
        Directory.CreateDirectory(tempDirectory);
        Directory.CreateDirectory(codexHomeDirectory);

        startInfo.Environment["APPDATA"] = roamingDirectory;
        startInfo.Environment["LOCALAPPDATA"] = localDirectory;
        startInfo.Environment["TEMP"] = tempDirectory;
        startInfo.Environment["TMP"] = tempDirectory;
        startInfo.Environment["CODEX_HOME"] = codexHomeDirectory;
    }

    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private void OpenSelectedFolder()
    {
        var profile = SelectedProfile();
        if (profile is null)
        {
            SetStatus("Select a profile first.");
            return;
        }

        Directory.CreateDirectory(profile.UserDataDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = QuoteArgument(profile.UserDataDirectory),
            UseShellExecute = true
        });
    }

    private void ForgetSelected()
    {
        var profile = SelectedProfile();
        if (profile is null)
        {
            SetStatus("Select a profile first.");
            return;
        }

        var result = DarkTheme.ShowMessage(this,
            $"Remove “{profile.Label}” from this manager?\n\nThe local profile directory will be kept, so its saved session is not deleted.",
            "Forget profile", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (result != DialogResult.OK)
        {
            return;
        }

        _settings.Profiles.Remove(profile);
        SaveSettings();
        RefreshProfiles();
        SetStatus($"Forgot “{profile.Label}”. Its local session directory was kept.");
    }

    private void RefreshProfiles()
    {
        _profileList.BeginUpdate();
        try
        {
            _profileList.Items.Clear();
            foreach (var profile in _settings.Profiles.OrderBy(profile => profile.Label, StringComparer.OrdinalIgnoreCase))
            {
                var saved = Directory.Exists(profile.UserDataDirectory) ? "Available" : "Not created";
                var lastOpened = profile.LastLaunchedUtc.HasValue
                    ? profile.LastLaunchedUtc.Value.ToLocalTime().ToString("g")
                    : "Never";
                var item = new ListViewItem(profile.Label);
                item.SubItems.Add(saved);
                item.SubItems.Add(lastOpened);
                item.SubItems.Add(profile.UserDataDirectory);
                item.Tag = profile.Id;
                _profileList.Items.Add(item);
            }
        }
        finally
        {
            _profileList.EndUpdate();
        }

        _launchButton.Enabled = _profileList.Items.Count > 0;
        _launchAllButton.Enabled = _profileList.Items.Count > 0 && !_settings.SharedCodexHome;
    }

    private void SelectProfile(string id)
    {
        foreach (ListViewItem item in _profileList.Items)
        {
            if (item.Tag as string == id)
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                break;
            }
        }
    }

    private void SaveSettings()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception ex)
        {
            DarkTheme.ShowMessage(this, ex.Message, "Could not save local settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text) => _status.Text = text;

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

    private static List<ClientOption> DiscoverClients()
    {
        var results = new List<ClientOption>();
        var windowsApps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");

        AddCandidates(results, windowsApps, "OpenAI.Codex_*", new[] { ("Codex desktop", "app\\ChatGPT.exe") });
        AddCandidates(results, windowsApps, "OpenAI.ChatGPT-Desktop_*", new[]
        {
            ("ChatGPT desktop", "app\\ChatGPT Classic.exe"),
            ("ChatGPT desktop", "app\\ChatGPT.exe")
        });

        try
        {
            foreach (var process in Process.GetProcessesByName("ChatGPT"))
            {
                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        AddUnique(results, new ClientOption("Running desktop client", path));
                    }
                }
                catch { }
                finally { process.Dispose(); }
            }
        }
        catch { }

        return results
            .OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(option => option.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddCandidates(List<ClientOption> results, string root, string directoryPattern, (string Name, string RelativePath)[] candidates)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(root, directoryPattern, SearchOption.TopDirectoryOnly))
            {
                foreach (var candidate in candidates)
                {
                    var path = Path.Combine(directory, candidate.RelativePath);
                    if (File.Exists(path))
                    {
                        AddUnique(results, new ClientOption(candidate.Name, path));
                    }
                }
            }
        }
        catch { }
    }

    private static void AddUnique(List<ClientOption> results, ClientOption option)
    {
        if (results.All(existing => !PathsEqual(existing.Path, option.Path)))
        {
            results.Add(option);
        }
    }
}

internal sealed class ProfileDialog : Form
{
    private readonly TextBox _labelBox = new();
    public string ProfileLabel => _labelBox.Text.Trim();

    public ProfileDialog()
    {
        Text = "Add local profile";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 145);
        Font = new Font("Segoe UI", 9F);
        DarkTheme.StyleForm(this);

        var label = new Label
        {
            Text = "Profile label (for example, Personal or Work):",
            AutoSize = true,
            Location = new Point(16, 18)
        };
        Controls.Add(label);

        _labelBox.Location = new Point(16, 45);
        _labelBox.Width = 388;
        _labelBox.MaxLength = 80;
        DarkTheme.StyleTextBox(_labelBox);
        _labelBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { DialogResult = DialogResult.OK; Close(); }
        };
        Controls.Add(_labelBox);

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(238, 91),
            Width = 80
        };
        DarkTheme.StyleButton(cancel);
        Controls.Add(cancel);

        var add = new Button
        {
            Text = "Add",
            DialogResult = DialogResult.OK,
            Location = new Point(324, 91),
            Width = 80
        };
        DarkTheme.StyleButton(add, primary: true);
        Controls.Add(add);
        AcceptButton = add;
        CancelButton = cancel;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK && string.IsNullOrWhiteSpace(ProfileLabel))
        {
            DarkTheme.ShowMessage(this, "Enter a label for this profile.", "Label required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }
}
