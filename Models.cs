using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenAIProfileSwitcher;

public sealed class AccountProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "New account";
    public string UserDataDirectory { get; set; } = string.Empty;
    public DateTime? LastLaunchedUtc { get; set; }
}

public sealed class AppSettings
{
    public string? ClientPath { get; set; }
    public bool SharedCodexHome { get; set; } = false;
    public List<AccountProfile> Profiles { get; set; } = new();
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string RootDirectory { get; }
    public string SettingsPath { get; }
    public string ProfilesDirectory { get; }

    public SettingsStore()
    {
        RootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenAIProfileSwitcher");
        SettingsPath = Path.Combine(RootDirectory, "settings.json");
        ProfilesDirectory = Path.Combine(RootDirectory, "profiles");
    }

    public AppSettings Load()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ProfilesDirectory);

        if (!File.Exists(SettingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            settings.Profiles ??= new List<AccountProfile>();
            return settings;
        }
        catch (Exception)
        {
            var backupPath = SettingsPath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            try { File.Move(SettingsPath, backupPath, overwrite: false); } catch { }
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ProfilesDirectory);

        var temporaryPath = SettingsPath + ".tmp";
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}

public sealed class ClientOption
{
    public string Name { get; }
    public string Path { get; }

    public ClientOption(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public override string ToString() => Name;
}
