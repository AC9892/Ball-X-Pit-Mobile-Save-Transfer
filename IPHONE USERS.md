# iPhone users: put Apple Devices backups on another drive

BALL x PIT Save Transfer can read an **unencrypted** iPhone backup made by Apple Devices on Windows. If your C: drive does not have enough space for that backup, you can point Apple Devices' backup folder to another drive with a Windows junction.

This guide uses the Apple Devices location that worked on our Windows setup: `%USERPROFILE%\Apple\MobileSync\Backup`. Check that this is the folder Apple Devices uses on your PC before running the script. Some older iTunes installations use `%APPDATA%\Apple Computer\MobileSync\Backup` instead; if yours does, change `$source` in the script.

## Before you start

1. Close Apple Devices and stop any backup in progress.
2. Choose a folder on another drive with enough free space. In the script below, replace **only** `D:\YOUR_FOLDER\Backup` with your chosen full path, such as `D:\iPhone Backups\Backup`.
3. If `C:\Users\YOUR_NAME\Apple\MobileSync\Backup` already contains backup files, move that **Backup** folder to your chosen destination first and verify the files are there. The script will stop while the C: folder contains files. Do not delete your only backup.

## Run in PowerShell

```powershell
# Fill in this path. It must point to the Backup folder itself.
$destination = 'D:\YOUR_FOLDER\Backup'

# Apple Devices backup path. Change only if your installation uses another path.
$source = Join-Path $env:USERPROFILE 'Apple\MobileSync\Backup'

if ($destination -eq 'D:\YOUR_FOLDER\Backup') {
    throw 'Replace D:\YOUR_FOLDER\Backup with your real destination path first.'
}

$source = [System.IO.Path]::GetFullPath($source)
$destination = [System.IO.Path]::GetFullPath($destination)

if ($source.TrimEnd('\') -ieq $destination.TrimEnd('\')) {
    throw 'The source and destination must be different folders.'
}

if (Test-Path -LiteralPath $source) {
    $existing = Get-Item -LiteralPath $source -Force
    if ($existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint) {
        throw "The source is already a link: $source"
    }
    if (-not $existing.PSIsContainer) {
        throw "The source is not a folder: $source"
    }
    if (Get-ChildItem -LiteralPath $source -Force | Select-Object -First 1) {
        throw "The source still contains files. Move and verify them before continuing: $source"
    }
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $source) -Force | Out-Null

# A junction needs the original path to be absent. At this point it can only be empty.
if (Test-Path -LiteralPath $source) {
    Remove-Item -LiteralPath $source
}

New-Item -ItemType Junction -Path $source -Target $destination
Get-Item -LiteralPath $source | Format-List FullName,LinkType,Target
```

The last command should show `LinkType : Junction` and your chosen destination under `Target`. Seeing a `Backup` entry under C: is normal: that entry is now a link to the other drive. Start a new **unencrypted** backup in Apple Devices, then check that files are appearing in the destination folder.

After the backup finishes, select its backup folder in BALL x PIT Save Transfer. Create an empty PC save slot in the game first, quit the game, and import the iPhone save into that slot.

Keep your iPhone backup and personal save files out of a public GitHub repository. This guide contains no personal backup data and is safe to publish as a template.
