# FolderSync

A console application that mirrors a *source* directory into a *replica* directory on a fixed interval, with structured logging.

---

## Build (single-file, self-contained)

Publish the app as a single-file, self-contained binary for your target platform:

```bash
dotnet publish FolderSync/Foldersync/src/FolderSync.App/FolderSync.App.csproj   -c Release   -p:PublishSingleFile=true   --self-contained   -r <RUNTIME_IDENTIFIER>   -o <OUTPUT_PATH>
```

**Common runtime identifiers (RID):**
- `win-x64`, `win-arm64`
- `linux-x64`, `linux-arm64`
- `osx-x64`, `osx-arm64`

**Examples:**
```bash
# Windows (x64)
dotnet publish FolderSync/Foldersync/src/FolderSync.App/FolderSync.App.csproj -c Release -p:PublishSingleFile=true --self-contained -r win-x64 -o ./publish/win-x64

# Linux (x64)
dotnet publish FolderSync/Foldersync/src/FolderSync.App/FolderSync.App.csproj -c Release -p:PublishSingleFile=true --self-contained -r linux-x64 -o ./publish/linux-x64

# macOS (Apple Silicon)
dotnet publish FolderSync/Foldersync/src/FolderSync.App/FolderSync.App.csproj -c Release -p:PublishSingleFile=true --self-contained -r osx-arm64 -o ./publish/osx-arm64
```

---

## Add to PATH (optional)

If you want to run the app directly from your system terminal (without typing the full path), add the publish directory to your `PATH`.

**Windows (PowerShell):**
```powershell
$env:Path += ";C:\path\to\publish\win-x64"
# Persist for your user:
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\path\to\publish\win-x64", "User")
```

**Windows (cmd.exe):**
```bat
set PATH=%PATH%;C:\path\to\publish\win-x64
```

**Linux/macOS (bash/zsh):**
```bash
export PATH="$PATH:/path/to/publish/linux-x64"
# To persist (bash):
echo 'export PATH="$PATH:/path/to/publish/linux-x64"' >> ~/.bashrc
# or (zsh):
echo 'export PATH="$PATH:/path/to/publish/linux-x64"' >> ~/.zshrc
```

---

## Usage

**Syntax (required flags):**
```bash
FolderSync --source <path> --replica <path> --log <path/app.log> --interval <seconds|HH:MM:SS> [--debug]
```

**Flags:**
- `--source <path>` — **required**. Absolute or relative path to the source directory.
- `--replica <path>` — **required**. Absolute or relative path to the replica directory.
- `--log <path/app.log>` — **required**. Path to the log file (will be created if missing).
- `--interval <value>` — **required**. Synchronization interval as:
  - **seconds** (e.g., `5`) **or**
  - **TimeSpan** format `HH:MM:SS` (e.g., `00:00:05`).
- `--debug` — **optional**. Enables more verbose logging (Debug level).

**Examples:**
```bash
# Run every 5 seconds, with explicit seconds:
FolderSync --source ./data/source --replica ./data/replica --log ./logs/app.log --interval 5

# Run every 5 seconds, with HH:MM:SS:
FolderSync --source /mnt/src --replica /mnt/replica --log /var/log/foldersync/app.log --interval 00:00:05

# With debug logging:
FolderSync --source C:\src --replica C:\replica --log C:\logs\app.log --interval 5 --debug
```

**Exit codes (summary):**
- `0` — success.
- non-zero — invalid arguments or unrecoverable error (e.g., invalid paths).

---

## Notes
- The application creates and updates the replica to match the source, logging each operation to console and file.
- Timestamps are preserved to avoid unnecessary re-syncs.
- Use Ctrl+C to gracefully stop the periodic synchronization loop.
