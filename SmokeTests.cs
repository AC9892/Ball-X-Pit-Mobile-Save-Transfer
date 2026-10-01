using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace BallXPITSaveTransfer
{
    internal static class SmokeTests
    {
        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static byte[] Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path)) return sha.ComputeHash(stream);
        }

        private static bool Same(string first, string second)
        {
            return Hash(first).SequenceEqual(Hash(second));
        }

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length != 4) throw new ArgumentException("Expected: extracted-save-folder Apple-backup-root-or-dash PC-backup-folder test-output-folder");
            BackupSource extracted = Transfer.FindBackup(args[0]);
            if (args[1] != "-")
            {
                BackupSource hashed = Transfer.FindBackup(args[1]);
                Assert(Same(extracted.Main, hashed.Main), "Hashed backup lookup returned the wrong main save.");
                Assert(Same(extracted.Backup, hashed.Backup), "Hashed backup lookup returned the wrong game backup.");
                Assert(Same(extracted.SlotInfo, hashed.SlotInfo), "Hashed backup lookup returned the wrong slot info.");
            }

            string pc = Path.Combine(args[3], "pc");
            string backups = Path.Combine(args[3], "backups");
            Directory.CreateDirectory(pc);
            foreach (string name in new[] { "meta1.yankai", "meta1_backup.yankai", "saveslotinfo.balls" })
                File.Copy(Path.Combine(args[2], name), Path.Combine(pc, name));
            File.Copy(Path.Combine(args[2], "meta1.yankai"), Path.Combine(pc, "meta2.yankai"));
            File.Copy(Path.Combine(args[2], "meta1_backup.yankai"), Path.Combine(pc, "meta2_backup.yankai"));

            Assert(Transfer.ExistingSlots(pc).SequenceEqual(new[] { 1, 2 }), "Existing slots not detected.");
            bool refused = false;
            try { Transfer.Import(extracted, pc, 2, delegate { return true; }, backups); }
            catch (InvalidOperationException) { refused = true; }
            Assert(refused, "Import was not blocked while the game was running.");
            Assert(!Directory.Exists(backups), "A blocked import created a backup folder.");

            string saved = Transfer.Import(extracted, pc, 2, delegate { return false; }, backups);
            Assert(Same(Path.Combine(pc, "meta2.yankai"), extracted.Main), "Slot 2 main save mismatch.");
            Assert(Same(Path.Combine(pc, "meta2_backup.yankai"), extracted.Backup), "Slot 2 game backup mismatch.");
            Assert(Same(Path.Combine(pc, "meta1.yankai"), Path.Combine(args[2], "meta1.yankai")), "Slot 1 changed.");
            Assert(Same(Path.Combine(pc, "saveslotinfo.balls"), Path.Combine(args[2], "saveslotinfo.balls")), "Slot list changed.");
            Assert(Same(Path.Combine(saved, "meta2.yankai"), Path.Combine(args[2], "meta1.yankai")), "Original slot 2 was not backed up.");
            using (TransferForm form = new TransferForm())
                Assert(form.Text == "BALL x PIT Save Transfer", "The app window did not initialize.");
            Console.WriteLine("PASS: mobile save lookup, slot detection, running-game guard, import, backup, and slot isolation.");
        }
    }
}
