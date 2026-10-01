# BALL x PIT Save Transfer 
--- Back up your PC save file this program replaces your save --

A small Windows app for copying a BALL x PIT iPhone or Android save into an existing PC save slot. It copies the save files directly; it does not edit their contents or merge two saves.

## Use it

1. For iPhone, finish an **unencrypted** backup in Apple Devices. For Android, copy `meta1.yankai`, `meta1_backup.yankai`, and `saveslotinfo.balls` from `Android/data/com.devolverdigital.ballxpit/files/` into a folder on your PC. You can also choose a folder containing those three files from another source.
2. To keep your current PC progress, open BALL x PIT on PC, choose **Create New Save** in an empty slot, then quit the game completely.
3. Open **BALL x PIT Save Transfer.exe**. It fills in the usual Apple backup and PC save locations. For Android, browse to the folder holding the copied files.
4. Click **Check folders**, select the newly created PC slot, and click **Import mobile save**.
5. Launch BALL x PIT and select that slot. Its summary may initially show the temporary new save until the game loads and saves the imported progress.

The app refuses to import while `Balls.exe` is running. It backs up all existing `.yankai` files and `saveslotinfo.balls` under `%LOCALAPPDATA%\BALL x PIT Save Transfer\Backups\` before replacing the selected slot. It leaves other slots and the live slot list untouched. If you need to restore, quit the game and copy the files from the dated backup folder back into the PC save folder.

The Apple backup reader recognizes the BALL x PIT iOS app domain `com.devolverdigital.ballxpit` and the save files in its `Documents` folder. Encrypted Apple backups are not supported. Android saves are read from a normal folder containing the three files.

If Apple Devices runs out of space on C:, see [IPHONE USERS.md](IPHONE%20USERS.md) for a fill-in-the-blank PowerShell guide to place backups on another drive.

## Build from source

`Program.cs` is a Windows Forms app targeting the .NET Framework included with Windows. On a Windows machine with the C# compiler, build it with:

```powershell
& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:winexe "/out:BALL x PIT Save Transfer.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll Program.cs
```

`SmokeTests.cs` contains a fixture test for backup lookup, copying, the running-game guard, and slot isolation.
