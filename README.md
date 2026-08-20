# OpenAI Desktop Profiles

A small Windows companion launcher for the installed Codex or ChatGPT desktop client.

It lets you create separate local profiles so you can sign into multiple accounts and open them in separate windows at the same time. Authentication always happens inside the official OpenAI client. Each launch isolates the browser profile, native app-data roots, temporary files, and visible window, so one account cannot overwrite another profile's local session.

## Build

Requires the .NET 8 SDK with Windows Desktop support.

```powershell
dotnet build .\OpenAIProfileSwitcher.csproj --configuration Release
dotnet run --project .\OpenAIProfileSwitcher.csproj
```

For a self-contained executable:

```powershell
dotnet publish .\OpenAIProfileSwitcher.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output .\dist
```

## Usage

1. Start the launcher.
2. Select the detected official Codex or ChatGPT executable.
3. Add a profile such as `Personal` or `Work`.
4. Launch it and sign in through the official client.
5. Repeat for each account. Launch profiles again to open their separate windows. **Launch all** starts profiles sequentially and waits for each window to initialize, avoiding the desktop client's startup race.

Profile data is stored under `%LOCALAPPDATA%\OpenAIProfileSwitcher\profiles`. Each profile also receives isolated native app-data roots and a `CODEX_HOME`, where the Codex app-server keeps its own local state. The launcher stores labels and local paths only; it does not read or copy passwords, cookies, access tokens, or refresh tokens. Do not share profile directories.

This is an independent companion project, not a modified or repackaged OpenAI client.
