# OpenAI Desktop Profiles

A small Windows companion launcher for the installed Codex or ChatGPT desktop client.

It lets you create separate local profiles so you can sign into multiple accounts and open them in separate windows. Authentication happens inside the official OpenAI client. By default the launcher isolates each profile's browser directory, native app-data roots, temporary files, and `CODEX_HOME`.

## Build

Requires the .NET 8 SDK with Windows Desktop support.

```powershell
dotnet build .\OpenAIProfileSwitcher.csproj --configuration Release
dotnet run --project .\OpenAIProfileSwitcher.csproj
```

For a self-contained Windows x64 executable:

```powershell
dotnet publish .\OpenAIProfileSwitcher.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --output .\dist
```

## Appearance

The Windows launcher uses a permanent dark color scheme; there is **no light-mode toggle**. The main form, profile dialog, list headers/rows, buttons, inputs, and app confirmation dialogs are dark. Windows title bars request the native DWM dark-caption style on supported systems. The system's own file picker may still follow your Windows theme.

## Usage

1. Start the launcher.
2. Select the detected official Codex or ChatGPT executable.
3. Add profiles such as `Personal` and `Work`.
4. Launch a profile, then sign in through the official client.
5. In **default isolated mode**, repeat for each account. **Launch all** opens profiles sequentially.

Profiles and settings live under `%LOCALAPPDATA%\OpenAIProfileSwitcher`. The launcher itself does not read, copy, or export passwords or tokens, but the official client can save credentials in its corresponding profile state. Do not share these directories.

## Experimental shared Codex data mode

Check **Share Codex data across profiles (experimental; one profile at a time)** to point each launch's `CODEX_HOME` to your **existing** `%USERPROFILE%\.codex` directory rather than an isolated directory. The program asks for confirmation. By default, this option is **off**.

This can make locally stored Codex sessions, skills, config, and related Codex-home state available across launches **when the official desktop client honors `CODEX_HOME`**. Local projects outside the home folder are already accessible to any profile.

**Important limitations:**

- **Authentication is also shared in that directory.** Switching profile labels does not independently switch ChatGPT accounts; you may need to sign out and sign in to change account. The tool does not transfer subscriptions, quotas, or account ownership.
- **Cloud/account-bound conversation history is not portable by this setting.** Even if a prior thread appears locally, continuing it with another account is not guaranteed.
- The launcher **disables Launch all** and checks for running `ChatGPT`, `ChatGPT Classic`, and `Codex` processes when launching in shared mode. You must close **all** Codex/ChatGPT windows before switching. That check is a best-effort safeguard, not a system-wide file/database lock.
- Some desktop builds may ignore or partially honor `CODEX_HOME`. Isolation and history visibility must be verified on the specific Codex version you use.
- The `--user-data-dir` argument and redirected `APPDATA`/`LOCALAPPDATA` roots still isolate other native application data. Thus this is **shared Codex home**, *not* guaranteed full synchronization of every desktop-app cache, chat, or credential.
- **Back up `%USERPROFILE%\.codex` with all Codex processes closed** before enabling shared mode. Do not commit that directory, its tokens, or database files to GitHub.
- Enabling or disabling the option does not migrate or delete existing profile data.

For separate simultaneous signed-in windows, leave shared mode off. For the same local history and settings across account changes, try shared mode with one window at a time and expect to sign in again when changing accounts.

This is an independent companion project, not a modified or repackaged OpenAI client.
