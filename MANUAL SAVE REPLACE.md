# Manual iPhone or Android save transfer to PC

Use this guide if you want to copy a BALL x PIT mobile save into the Windows PC version **without the transfer app**. The `.yankai` save contents do not need to be decrypted or converted. These steps replace one PC save slot; they do not merge progress from two slots.

## 1. Get the mobile files

You need `meta1.yankai` and `meta1_backup.yankai` from the mobile game. Keep the two files together; the backup file may contain different data. `saveslotinfo.balls` is useful to keep with your extracted copy, but do **not** replace the PC copy of that file when importing into an existing PC slot.

### Android

Close BALL x PIT on the phone. Connect it to the PC in **File transfer** mode, then copy these files from `Android/data/com.devolverdigital.ballxpit/files/` into a new folder on the PC:

- `meta1.yankai`
- `meta1_backup.yankai`
- `saveslotinfo.balls`

Android may restrict access to `Android/data` in some file managers. Use a connection or file manager that can read that folder, and work from copies of the files.

### iPhone

Finish an **unencrypted** backup using Apple Devices on Windows. Its usual backup root is `%USERPROFILE%\Apple\MobileSync\Backup`; open the device's backup folder inside it. If you moved backups to another drive using [IPHONE USERS.md](IPHONE%20USERS.md), you can also open the destination folder there. The device backup folder contains `Manifest.db` and folders named with two hexadecimal characters.

The iPhone backup stores the BALL x PIT files under hashed names. **These IDs are calculated from the app domain and file path, not from the individual iPhone.** They should be the same on another iPhone *if* BALL x PIT uses the same app domain and `Documents` paths there. We have verified these mappings on one real iPhone backup; check `Manifest.db` on other backups rather than assuming the files are present.

Expected files to copy into a **new** folder on the PC (rename only the *copies*):

| File inside the device backup folder | Name for your extracted copy |
| --- | --- |
| `b5\b5bbf27adf4b18b334dfacd2978bd052f2dabecc` | `meta1.yankai` |
| `e0\e06149a3696bc715467ac551cac4a6c918a95864` | `meta1_backup.yankai` |
| `5a\5a1cf4960a7185d21cb020d5824506caf1a8580a` | `saveslotinfo.balls` |

To verify a different iPhone backup, open its `Manifest.db` with a [SQLite viewer](https://sqliteviewer.app/) and run:

```sql
SELECT relativePath, fileID
FROM Files
WHERE domain = 'AppDomain-com.devolverdigital.ballxpit'
  AND relativePath IN (
    'Documents/meta1.yankai',
    'Documents/meta1_backup.yankai',
    'Documents/saveslotinfo.balls'
  );
```

Use the `fileID` values returned by **that** backup. Each physical file is under the folder named by its first two ID characters. If a row or file is absent, check that you opened the correct completed, unencrypted backup; a game update could also change the app's storage paths. Do not rename or move files *inside* the Apple backup.

## 2. Prepare a PC slot

1. Open BALL x PIT on PC and choose **Create New Save** in an empty slot. Remember its number, such as slot 2.
2. Quit the game completely. Check Task Manager if needed: `Balls.exe` must not still be running.
3. In File Explorer, open `%USERPROFILE%\AppData\LocalLow\Kenny Sun\BALL x PIT`.
4. Copy the **entire** PC save folder to a separate backup location before replacing anything. Keep this copy until you have loaded and saved successfully in game.

## 3. Replace only that slot's files

For **slot 2**, copy the mobile `meta1.yankai` into the PC save folder and name the new copy `meta2.yankai`. Copy mobile `meta1_backup.yankai` into the same PC folder and name it `meta2_backup.yankai`. Approve replacement of those two slot 2 files only.

For a different slot, use its number in both names: `metaN.yankai` and `metaN_backup.yankai`. Do not rename the mobile originals. Leave the PC `saveslotinfo.balls` alone, and leave every other PC slot alone.

Launch the PC game and open the chosen slot. Its selection-screen summary may show the temporary new slot until the imported save has loaded and the game saves again. Confirm the progress in game before removing your separate PC backup.

The iPhone-to-PC copy was tested in game. Android uses the same save format and its files were tested in an isolated PC save folder, but the public Android sample has not been launched in the PC game here.
