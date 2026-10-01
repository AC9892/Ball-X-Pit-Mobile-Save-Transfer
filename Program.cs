using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BallXPITSaveTransfer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TransferForm());
        }
    }

    internal sealed class BackupSource
    {
        public string Folder;
        public string Main;
        public string Backup;
        public string SlotInfo;
    }

    internal static class Transfer
    {
        private const string AppDomain = "AppDomain-com.devolverdigital.ballxpit";
        private static readonly Regex SlotName = new Regex(@"^meta([1-9][0-9]*)\.yankai$", RegexOptions.IgnoreCase);

        public static string DefaultBackupRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Apple", "MobileSync", "Backup");
        }

        public static string DefaultPcFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Kenny Sun", "BALL x PIT");
        }

        private static string BackupFile(string folder, string relativePath)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(AppDomain + "-" + relativePath);
            string id;
            using (SHA1 sha = SHA1.Create())
                id = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            return Path.Combine(folder, id.Substring(0, 2), id);
        }

        private static bool LooksLikeSave(string path)
        {
            if (!File.Exists(path)) return false;
            using (FileStream stream = File.OpenRead(path))
                return stream.Length >= 100 && stream.ReadByte() == 0x02 && stream.ReadByte() == 0x2f;
        }

        private static BackupSource CheckFolder(string folder)
        {
            string extractedMain = Path.Combine(folder, "meta1.yankai");
            string extractedBackup = Path.Combine(folder, "meta1_backup.yankai");
            string extractedSlotInfo = Path.Combine(folder, "saveslotinfo.balls");
            if (LooksLikeSave(extractedMain) && LooksLikeSave(extractedBackup) && LooksLikeSave(extractedSlotInfo))
            {
                return new BackupSource { Folder = folder, Main = extractedMain, Backup = extractedBackup, SlotInfo = extractedSlotInfo };
            }
            if (!File.Exists(Path.Combine(folder, "Manifest.db"))) return null;
            BackupSource source = new BackupSource();
            source.Folder = folder;
            source.Main = BackupFile(folder, "Documents/meta1.yankai");
            source.Backup = BackupFile(folder, "Documents/meta1_backup.yankai");
            source.SlotInfo = BackupFile(folder, "Documents/saveslotinfo.balls");
            return LooksLikeSave(source.Main) && LooksLikeSave(source.Backup) && LooksLikeSave(source.SlotInfo) ? source : null;
        }

        public static BackupSource FindBackup(string path)
        {
            if (String.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                throw new InvalidOperationException("Apple backup folder does not exist.");

            List<string> candidates = new List<string>();
            candidates.Add(path);
            string nestedBackup = Path.Combine(path, "Backup");
            if (Directory.Exists(nestedBackup)) candidates.Add(nestedBackup);
            foreach (string folder in candidates.ToArray())
            {
                try { candidates.AddRange(Directory.GetDirectories(folder)); }
                catch (UnauthorizedAccessException) { }
            }

            BackupSource newest = null;
            DateTime newestDate = DateTime.MinValue;
            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                BackupSource source;
                try { source = CheckFolder(candidate); }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }
                if (source == null) continue;
                string manifest = Path.Combine(candidate, "Manifest.db");
                DateTime date = File.GetLastWriteTimeUtc(File.Exists(manifest) ? manifest : source.Main);
                if (newest == null || date > newestDate) { newest = source; newestDate = date; }
            }
            if (newest == null)
                throw new InvalidOperationException("No BALL x PIT mobile save was found here. Select an unencrypted Apple Devices backup or a folder with extracted save files.");
            return newest;
        }

        public static int[] ExistingSlots(string pcFolder)
        {
            if (!Directory.Exists(pcFolder)) return new int[0];
            List<int> slots = new List<int>();
            foreach (string path in Directory.GetFiles(pcFolder, "meta*.yankai"))
            {
                Match match = SlotName.Match(Path.GetFileName(path));
                int number;
                if (match.Success && Int32.TryParse(match.Groups[1].Value, out number)) slots.Add(number);
            }
            slots.Sort();
            return slots.ToArray();
        }

        public static bool IsGameRunning()
        {
            return Process.GetProcessesByName("Balls").Length > 0 || Process.GetProcessesByName("BALL x PIT").Length > 0;
        }

        private static byte[] Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return sha.ComputeHash(stream);
        }

        private static bool SameFile(string first, string second)
        {
            return Hash(first).SequenceEqual(Hash(second));
        }

        private static void CopyVerified(string source, string destination)
        {
            string temporary = destination + ".transfer-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(source, temporary);
                if (!SameFile(source, temporary)) throw new IOException("Temporary copy did not match the source.");
                File.Copy(temporary, destination, true);
                if (!SameFile(source, destination)) throw new IOException("Destination copy did not match the source.");
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public static string Import(BackupSource source, string pcFolder, int slot)
        {
            string backupRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BALL x PIT Save Transfer", "Backups");
            return Import(source, pcFolder, slot, IsGameRunning, backupRoot);
        }

        internal static string Import(BackupSource source, string pcFolder, int slot, Func<bool> gameRunning, string backupRoot)
        {
            if (source == null || !LooksLikeSave(source.Main) || !LooksLikeSave(source.Backup))
                throw new InvalidOperationException("The mobile save files are missing or cannot be read.");
            if (!Directory.Exists(pcFolder)) throw new InvalidOperationException("PC save folder does not exist.");
            if (!ExistingSlots(pcFolder).Contains(slot))
                throw new InvalidOperationException("Create this save slot in BALL x PIT first, then quit the game.");
            if (gameRunning()) throw new InvalidOperationException("Quit BALL x PIT completely before importing a save.");

            string main = Path.Combine(pcFolder, "meta" + slot + ".yankai");
            string backup = Path.Combine(pcFolder, "meta" + slot + "_backup.yankai");
            string savedFolder = Path.Combine(backupRoot, DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(savedFolder);

            string[] saveFiles = Directory.GetFiles(pcFolder, "*.yankai");
            foreach (string path in saveFiles)
                CopyVerified(path, Path.Combine(savedFolder, Path.GetFileName(path)));
            string slotInfo = Path.Combine(pcFolder, "saveslotinfo.balls");
            if (File.Exists(slotInfo)) CopyVerified(slotInfo, Path.Combine(savedFolder, "saveslotinfo.balls"));
            if (gameRunning()) throw new InvalidOperationException("BALL x PIT started during the backup. No save was replaced.");

            bool backupExisted = File.Exists(backup);
            try
            {
                CopyVerified(source.Main, main);
                CopyVerified(source.Backup, backup);
            }
            catch
            {
                string savedMain = Path.Combine(savedFolder, Path.GetFileName(main));
                string savedBackup = Path.Combine(savedFolder, Path.GetFileName(backup));
                if (File.Exists(savedMain)) File.Copy(savedMain, main, true);
                if (backupExisted && File.Exists(savedBackup)) File.Copy(savedBackup, backup, true);
                else if (!backupExisted && File.Exists(backup)) File.Delete(backup);
                throw;
            }
            return savedFolder;
        }
    }

    internal sealed class TransferForm : Form
    {
        private readonly TextBox sourceBox = new TextBox();
        private readonly TextBox pcBox = new TextBox();
        private readonly ComboBox slotBox = new ComboBox();
        private readonly Label status = new Label();
        private readonly Button importButton = new Button();
        private BackupSource source;

        public TransferForm()
        {
            Text = "BALL x PIT Save Transfer";
            ClientSize = new Size(740, 350);
            MinimumSize = new Size(650, 390);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Segoe UI", 10f);

            Label intro = new Label();
            intro.Text = "Move an iPhone or Android save into an existing PC save slot.";
            intro.SetBounds(22, 18, 690, 28);
            intro.Font = new Font(Font, FontStyle.Bold);
            Controls.Add(intro);

            Label instructions = new Label();
            instructions.Text = "iPhone: use an unencrypted Apple Devices backup. Android: copy the game's three save files into a folder. To keep PC progress, create an empty PC slot and quit.";
            instructions.SetBounds(22, 49, 690, 45);
            Controls.Add(instructions);

            AddRow("Mobile save / backup", sourceBox, 102, delegate { Browse(sourceBox); });
            AddRow("PC save folder", pcBox, 154, delegate { Browse(pcBox); });

            Label slotLabel = new Label();
            slotLabel.Text = "Replace PC slot";
            slotLabel.SetBounds(22, 215, 145, 28);
            Controls.Add(slotLabel);
            slotBox.DropDownStyle = ComboBoxStyle.DropDownList;
            slotBox.SetBounds(174, 211, 175, 30);
            slotBox.SelectedIndexChanged += delegate { importButton.Enabled = source != null && slotBox.SelectedItem != null && !Transfer.IsGameRunning(); };
            Controls.Add(slotBox);

            Button refresh = new Button();
            refresh.Text = "Check folders";
            refresh.SetBounds(365, 209, 140, 34);
            refresh.Click += delegate { RefreshDetails(); };
            Controls.Add(refresh);

            importButton.Text = "Import mobile save";
            importButton.SetBounds(522, 266, 190, 43);
            importButton.Enabled = false;
            importButton.Click += ImportClicked;
            Controls.Add(importButton);

            status.SetBounds(22, 259, 485, 72);
            status.Text = "Checking folders...";
            Controls.Add(status);

            sourceBox.Text = Transfer.DefaultBackupRoot();
            pcBox.Text = Transfer.DefaultPcFolder();
            RefreshDetails();
        }

        private void AddRow(string title, TextBox box, int y, EventHandler browse)
        {
            Label label = new Label();
            label.Text = title;
            label.SetBounds(22, y, 145, 28);
            Controls.Add(label);
            box.SetBounds(174, y - 3, 430, 30);
            Controls.Add(box);
            Button button = new Button();
            button.Text = "Browse...";
            button.SetBounds(614, y - 5, 98, 34);
            button.Click += browse;
            Controls.Add(button);
        }

        private void Browse(TextBox box)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (Directory.Exists(box.Text)) dialog.SelectedPath = box.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    box.Text = dialog.SelectedPath;
                    RefreshDetails();
                }
            }
        }

        private void RefreshDetails()
        {
            source = null;
            slotBox.Items.Clear();
            importButton.Enabled = false;
            try
            {
                source = Transfer.FindBackup(sourceBox.Text.Trim());
                int[] slots = Transfer.ExistingSlots(pcBox.Text.Trim());
                foreach (int slot in slots) slotBox.Items.Add(slot);
                if (slots.Length == 0)
                    status.Text = "Mobile save found. Create a save slot in the PC game, quit, then click Check folders.";
                else if (Transfer.IsGameRunning())
                    status.Text = "Mobile save found. Quit BALL x PIT, then click Check folders and select a PC slot.";
                else
                    status.Text = "Mobile save found. " + slots.Length + " PC slot(s) found. Select the slot to replace.";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        private void ImportClicked(object sender, EventArgs e)
        {
            if (source == null || slotBox.SelectedItem == null) return;
            int slot = (int)slotBox.SelectedItem;
            DialogResult answer = MessageBox.Show(this,
                "Replace PC save slot " + slot + " with the mobile save?\n\nAll current PC save files will be backed up first.",
                "Confirm transfer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            try
            {
                importButton.Enabled = false;
                string backupFolder = Transfer.Import(source, pcBox.Text.Trim(), slot);
                status.Text = "Done. PC files backed up at:\n" + backupFolder;
                MessageBox.Show(this,
                    "Mobile save imported into PC slot " + slot + ".\n\nBackup: " + backupFolder + "\n\nLaunch BALL x PIT and select that slot.",
                    "Transfer complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Transfer stopped", MessageBoxButtons.OK, MessageBoxIcon.Error);
                status.Text = "No transfer completed: " + ex.Message;
            }
            finally
            {
                importButton.Enabled = source != null && slotBox.SelectedItem != null && !Transfer.IsGameRunning();
            }
        }
    }
}
