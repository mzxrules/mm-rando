using MMRando.Models;
using MMRando.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MMRando
{

    public partial class MainRandomizerForm
    {

        private void WriteAudioSeq()
        {
            if (!Settings.RandomizeBGM) {
                return;
            };

            foreach (SequenceInfo s in SequenceList)
            {
                s.Name = MusicDirectory + s.Name;
            };
            ROMFuncs.ApplyHack(ModsDirectory + "fix-music");
            ROMFuncs.ApplyHack(ModsDirectory + "inst24-swap-guitar");
            ROMFuncs.RebuildAudioSeq(SequenceList);
        }

        private void WriteLinkAppearance()
        {
            if (Settings.Character == Character.LinkMM)
            {
                WriteTunicColour();
            }
            else if (Settings.Character == Character.LinkOOT
                || Settings.Character == Character.AdultLink
                || Settings.Character == Character.Kafei)
            {
                int characterIndex = (int)Settings.Character;
                BinaryReader b = new BinaryReader(File.Open(ObjsDirectory + "link-" + characterIndex.ToString(), FileMode.Open));
                byte[] obj = new byte[b.BaseStream.Length];
                b.Read(obj, 0, obj.Length);
                b.Close();
                if (characterIndex < 3)
                {
                    WriteTunicColour(obj, characterIndex);
                }

                ROMFuncs.ApplyHack(ModsDirectory + "fix-link-" + characterIndex.ToString());
                ROMFuncs.InsertObj(obj, 0x11);
                if (characterIndex == 3)
                {
                    b = new BinaryReader(File.Open(ObjsDirectory + "kafei", FileMode.Open));
                    obj = new byte[b.BaseStream.Length];
                    b.Read(obj, 0, obj.Length);
                    b.Close();
                    WriteTunicColour(obj, characterIndex);
                    ROMFuncs.InsertObj(obj, 0x1C);
                    ROMFuncs.ApplyHack(ModsDirectory + "fix-kafei");
                }
            }
            List<int[]> Others = ROMFuncs.GetAddresses(AddrsDirectory + "tunic-forms");
            ROMFuncs.UpdateFormTunics(Others, Settings.TunicColor);
        }

        private void WriteTunicColour()
        {
            Color t = Settings.TunicColor;
            byte[] c = { t.R, t.G, t.B };
            List<int[]> locs = ROMFuncs.GetAddresses(AddrsDirectory + "tunic-colour");
            for (int i = 0; i < locs.Count; i++)
            {
                ROMFuncs.WriteROMAddr(locs[i], c);
            }
        }

        private void WriteTunicColour(byte[] obj, int i)
        {
            Color t = Settings.TunicColor;
            byte[] c = { t.R, t.G, t.B };
            List<int[]> locs = ROMFuncs.GetAddresses(AddrsDirectory + "tunic-" + i.ToString());
            for (int j = 0; j < locs.Count; j++)
            {
                ROMFuncs.WriteFileAddr(locs[j], c, obj);
            }
        }

        private void WriteTatlColour()
        {
            if (Settings.TatlColorSchema != TatlColorSchema.Random)
            {
                var selectedColorSchemaIndex = (int)Settings.TatlColorSchema;
                byte[] c = new byte[8];
                List<int[]> locs = ROMFuncs.GetAddresses(AddrsDirectory + "tatl-colour");
                for (int i = 0; i < locs.Count; i++)
                {
                    ROMFuncs.Arr_WriteU32(c, 0, Values.TatlColours[selectedColorSchemaIndex, i << 1]);
                    ROMFuncs.Arr_WriteU32(c, 4, Values.TatlColours[selectedColorSchemaIndex, (i << 1) + 1]);
                    ROMFuncs.WriteROMAddr(locs[i], c);
                }
            }
            else
            {
                ROMFuncs.ApplyHack(ModsDirectory + "rainbow-tatl");
            }
        }

        private void WriteQuickText()
        {
            if (Settings.QuickTextEnabled)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "quick-text");
            }
        }

        private void WriteCutscenes()
        {
            if (Settings.ShortenCutscenes)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "short-cutscenes");
            }
        }

        private void WriteDungeons()
        {
            if ((Settings.LogicMode == LogicMode.Vanilla) || (!Settings.RandomizeDungeonEntrances))
            {
                return;
            }

            ROMFuncs.WriteEntrances(Values.OldEntrances.ToArray(), _newEntrances);
            ROMFuncs.WriteEntrances(Values.OldExits.ToArray(), _newExits);
            byte[] li = new byte[] { 0x24, 0x02, 0x00, 0x00 };
            List<int[]> addr = new List<int[]>();
            addr = ROMFuncs.GetAddresses(AddrsDirectory + "d-check");
            for (int i = 0; i < addr.Count; i++)
            {
                li[3] = (byte)_newExts[i];
                ROMFuncs.WriteROMAddr(addr[i], li);
            }

            ROMFuncs.ApplyHack(ModsDirectory + "fix-dungeons");
            addr = ROMFuncs.GetAddresses(AddrsDirectory + "d-exit");

            for (int i = 0; i < addr.Count; i++)
            {
                if (i == 2)
                {
                    ROMFuncs.WriteROMAddr(addr[i], new byte[] { (byte)((Values.OldExits[_newEnts[i + 1]] & 0xFF00) >> 8), (byte)(Values.OldExits[_newEnts[i + 1]] & 0xFF) });
                }
                else
                {
                    ROMFuncs.WriteROMAddr(addr[i], new byte[] { (byte)((Values.OldExits[_newEnts[i]] & 0xFF00) >> 8), (byte)(Values.OldExits[_newEnts[i]] & 0xFF) });
                }
            }

            addr = ROMFuncs.GetAddresses(AddrsDirectory + "dc-flagload");
            for (int i = 0; i < addr.Count; i++)
            {
                ROMFuncs.WriteROMAddr(addr[i], new byte[] { (byte)((_newDCFlags[i] & 0xFF00) >> 8), (byte)(_newDCFlags[i] & 0xFF) });
            }

            addr = ROMFuncs.GetAddresses(AddrsDirectory + "dc-flagmask");
            for (int i = 0; i < addr.Count; i++)
            {
                ROMFuncs.WriteROMAddr(addr[i], new byte[] { (byte)((_newDCMasks[i] & 0xFF00) >> 8), (byte)(_newDCMasks[i] & 0xFF) });
            }
        }

        private void WriteGimmicks()
        {
            int damageMultiplier = (int)Settings.DamageMode;
            if (damageMultiplier > 0)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "dm-" + damageMultiplier.ToString());
            }

            int damageEffect = (int) Settings.DamageEffect;
            if (damageEffect > 0)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "de-" + damageEffect.ToString());
            }

            int gravityType = (int)Settings.MovementMode;
            if (gravityType > 0)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "movement-" + gravityType.ToString());
            }

            int floorType = (int)Settings.FloorType;
            if (floorType > 0)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "floor-" + floorType.ToString());
            }
        }

        private void WriteEnemies()
        {
            if (Settings.RandomizeEnemies)
            {
                SeedRNG();
                ROMFuncs.ShuffleEnemies(RNG);
            }
        }

        private void WriteFreeItem(int Item)
        {
            ROMFuncs.WriteToROM(Items.ITEM_ADDRS[Item], Items.ITEM_VALUES[Item]);
            switch (Item)
            {
                case 1: //bow
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x01);
                    break;
                case 5: //bomb bag
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x08);
                    break;
                case 19: //sword upgrade
                    ROMFuncs.WriteToROM(0xC5CE00, (byte)0x4E);
                    break;
                case 20:
                    ROMFuncs.WriteToROM(0xC5CE00, (byte)0x4F);
                    break;
                case 22: //quiver upgrade
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x02);
                    break;
                case 23:
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x03);
                    break;
                case 24://bomb bag upgrade
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x10);
                    break;
                case 25:
                    ROMFuncs.WriteToROM(0xC5CE6F, (byte)0x18);
                    break;
                default:
                    break;
            }
        }

        private void WriteItems()
        {
            if (Settings.LogicMode == LogicMode.Vanilla)
            {
                WriteFreeItem(Items.MaskDeku);

                if (Settings.ShortenCutscenes)
                {
                    //giants cs were removed
                    WriteFreeItem(Items.SongOath);
                }

                return;
            }

            //write free item
            int itemId = ItemList.FindIndex(u => u.ReplacesItemId == 0);
            WriteFreeItem(ItemList[itemId].ID);

            //write everything else
            ROMFuncs.ReplaceGetItemTable(ModsDirectory);
            ROMFuncs.InitItems();

            for (int i = 0; i < ItemList.Count; i++)
            {
                itemId = ItemList[i].ID;

                // Unused item
                if (ItemList[i].ReplacesItemId == -1)
                {
                    continue;
                };

                bool isRepeatable = Items.REPEATABLE.Contains(itemId);
                bool isCycleRepeatable = Items.CYCLE_REPEATABLE.Contains(itemId);
                int replacesItemId = ItemList[i].ReplacesItemId;

                if (ItemUtils.IsItemDefinedPastAreas(itemId)) {
                    // Subtract amount of entries describing areas and other
                    itemId -= Values.NumberOfAreasAndOther;
                }

                if (ItemUtils.IsItemDefinedPastAreas(replacesItemId)) {
                    // Subtract amount of entries describing areas and other
                    replacesItemId -= Values.NumberOfAreasAndOther;
                }

                if (ItemUtils.IsBottleCatchContent(i))
                {
                    ROMFuncs.WriteNewBottle(replacesItemId, itemId);
                }
                else
                {
                    ROMFuncs.WriteNewItem(replacesItemId, itemId, isRepeatable, isCycleRepeatable);
                }
            }

            if (Settings.AddShopItems)
            {
                ROMFuncs.ApplyHack(ModsDirectory + "fix-shop-checks");
            }
        }

        private void WriteGossipQuotes()
        {
            if (Settings.LogicMode == LogicMode.Vanilla)
            {
                return;
            }

            if (Settings.EnableGossipHints)
            {
                SeedRNG();
                ROMFuncs.WriteGossipMessage(GossipQuotes, RNG);
            }
        }

        private void WriteSpoilerLog()
        {
            if (Settings.LogicMode == LogicMode.Vanilla)
            {
                return;
            }

            if (Settings.GenerateSpoilerLog)
            {
                MakeSpoilerLog();
            }
        }

        private void WriteFileSelect()
        {
            if (Settings.LogicMode == LogicMode.Vanilla)
            {
                return;
            }

            ROMFuncs.ApplyHack(ModsDirectory + "file-select");
            byte[] SkyboxDefault = new byte[] { 0x91, 0x78, 0x9B, 0x28, 0x00, 0x28 };
            List<int[]> Addrs = ROMFuncs.GetAddresses(AddrsDirectory + "skybox-init");
            Random R = new Random();
            int rot = R.Next(360);
            for (int i = 0; i < 2; i++)
            {
                Color c = Color.FromArgb(SkyboxDefault[i * 3], SkyboxDefault[i * 3 + 1], SkyboxDefault[i * 3 + 2]);
                float h = c.GetHue();
                h += rot;
                h %= 360f;
                c = ROMFuncs.FromAHSB(c.A, h, c.GetSaturation(), c.GetBrightness());
                SkyboxDefault[i * 3] = c.R;
                SkyboxDefault[i * 3 + 1] = c.G;
                SkyboxDefault[i * 3 + 2] = c.B;
            }
            for (int i = 0; i < 3; i++)
            {
                ROMFuncs.WriteROMAddr(Addrs[i], new byte[] { SkyboxDefault[i * 2], SkyboxDefault[i * 2 + 1] });
            }
            rot = R.Next(360);
            byte[] FSDefault = new byte[] { 0x64, 0x96, 0xFF, 0x96, 0xFF, 0xFF, 0x64, 0xFF, 0xFF };
            Addrs = ROMFuncs.GetAddresses(AddrsDirectory + "fs-colour");
            for (int i = 0; i < 3; i++)
            {
                Color c = Color.FromArgb(FSDefault[i * 3], FSDefault[i * 3 + 1], FSDefault[i * 3 + 2]);
                float h = c.GetHue();
                h += rot;
                h %= 360f;
                c = ROMFuncs.FromAHSB(c.A, h, c.GetSaturation(), c.GetBrightness());
                FSDefault[i * 3] = c.R;
                FSDefault[i * 3 + 1] = c.G;
                FSDefault[i * 3 + 2] = c.B;
            }
            for (int i = 0; i < 9; i++)
            {
                if (i < 6)
                {
                    ROMFuncs.WriteROMAddr(Addrs[i], new byte[] { 0x00, FSDefault[i]});
                }
                else
                {
                    ROMFuncs.WriteROMAddr(Addrs[i], new byte[] { FSDefault[i] });
                }
            }
        }

        private void WriteStartupStrings()
        {
            if (Settings.LogicMode == LogicMode.Vanilla)
            {
                //ROMFuncs.ApplyHack(ModsDir + "postman-testing");
                return;
            }
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            ROMFuncs.SetStrings(ModsDirectory + "logo-text", $"v{v.Major}.{v.Minor}b", tSString.Text);
        }

        //Added by Spectre, using ProbablyButter's code (Thoughout lines 400 - 491)
        private BinaryReader readROM(string FileName)
        {
           BinaryReader ROM = new BinaryReader(File.Open(FileName, FileMode.Open));
            // check if this is a .z64 (32-bit big endian), .v64 (16-bit little endian), or .n64 (32-bit big endian) file
            // v64: 0x37, 0x80, 0x40, 0x12
            // z64: 0x80, 0x37, 0x12, 0x40
            // n64: 0x40, 0x12, 0x37, 0x80
            // assume little endian machine
            uint header = ROM.ReadUInt32();
            ROM.BaseStream.Seek(0, 0);
            if (header == 0x40123780u)
            {
                // z64 format
                // do nothing
                return ROM;
            }
            else if (header == 0x80371240)
            {
                // n64 format
                byte[] data = new byte[ROM.BaseStream.Length];
                ROM.Read(data, 0, data.Length);
                ROM.Close();
                // 32-bit little endian
                for (int i = 0; i < data.Length; i += 4)
                {
                    byte tmp = data[i];
                    data[i] = data[i + 3];
                    data[i + 3] = tmp;
                    tmp = data[i + 1];
                    data[i + 1] = data[i + 2];
                    data[i + 2] = tmp;
                }
                // technically not necessary to recalculate CRC unless you just want a sanity check
                //ROMFuncs.FixCRC(data);
                BinaryReader fixedRom = new BinaryReader(new MemoryStream(data));
                return fixedRom;
            }
            else if (header == 0x12408037)
            {
                // v64 format
                byte[] data = new byte[ROM.BaseStream.Length];
                ROM.Read(data, 0, data.Length);
                ROM.Close();
                // 16-bit little endian
                for (int i = 0; i < data.Length; i += 2)
                {
                    byte tmp = data[i];
                    data[i] = data[i + 1];
                    data[i + 1] = tmp;
                }
                // technically not necessary to recalculate CRC unless you just want a sanity check
                //ROMFuncs.FixCRC(data);
                BinaryReader fixedRom = new BinaryReader(new MemoryStream(data));
                return fixedRom;
            }
            else
            {
                // is this even a valid ROM?
                return null;
            }
        }
       

        private bool ValidateROM(string FileName)
       //private bool ValidateROM(BinaryReader ROM) //Added/Altered by Spectre, using ProbablyButter's Code
        {
            bool res = false;
            using (BinaryReader ROM = new BinaryReader(File.Open(FileName, FileMode.Open, FileAccess.Read)))
            {
                if (ROM.BaseStream.Length == 0x2000000)
                {
                    res = ROMFuncs.CheckOldCRC(ROM);
                }
                ROM.Close(); //Added/Altered by Spectre, using ProbablyButter's Code
            }
            return res;
        }

        //private void MakeROM(string InFile, string FileName, BackgroundWorker worker)
        private void MakeRom(BinaryReader OldROM, string FileName, BackgroundWorker worker) //Added/Altered by Spectre, using ProbablyButter's Code
        {
            //using (BinaryReader OldROM = new BinaryReader(File.Open(InFile, FileMode.Open, FileAccess.Read)))
            //{
                //ROMFuncs.ReadFileTable(OldROM);
                OldROM.BaseStream.Seek(0, 0);
                ROMFuncs.ReadFileTable(OldROM);
                OldROM.Close();
            //}
         //Commented out by Spectre, using ProbablyButter's code
             //Added by Spectre, using ProbablyButter's Code (Lines 488 - 491 )
           

            worker.ReportProgress(55, "Writing Audio...");
            WriteAudioSeq();
            
            worker.ReportProgress(60, "Writing Character...");
            WriteLinkAppearance();
            if (Settings.LogicMode != LogicMode.Vanilla)
            {
                worker.ReportProgress(61, "Applying hacks...");
                ROMFuncs.ApplyHack(ModsDirectory + "title-screen");
                ROMFuncs.ApplyHack(ModsDirectory + "misc-changes");
                ROMFuncs.ApplyHack(ModsDirectory + "cm-cs");
                WriteFileSelect();
            }
            ROMFuncs.ApplyHack(ModsDirectory + "init-file");

            worker.ReportProgress(62, "Writing quick text...");
            WriteQuickText();

            worker.ReportProgress(64, "Writing cutscenes...");
            WriteCutscenes();

            worker.ReportProgress(66, "Writing Tatl...");
            WriteTatlColour();

            worker.ReportProgress(68, "Writing dungeons...");
            WriteDungeons();

            worker.ReportProgress(70, "Writing gimmicks...");
            WriteGimmicks();

            worker.ReportProgress(72, "Writing enemies...");
            WriteEnemies();

            worker.ReportProgress(75, "Writing items...");
            WriteItems();

            worker.ReportProgress(85, "Writing gossip...");
            WriteGossipQuotes();

            worker.ReportProgress(87, "Writing startup...");
            WriteStartupStrings();

            worker.ReportProgress(89, "Writing spoiler log...");
            WriteSpoilerLog();

            worker.ReportProgress(90, "Building ROM...");

            byte[] ROM = ROMFuncs.BuildROM(FileName);
            if (_outputVC)
            {
                worker.ReportProgress(98, "Building VC...");
                ROMFuncs.BuildVC(ROM, VCDirectory, Path.ChangeExtension(FileName, "wad"));
            }
            worker.ReportProgress(100, "Done!");

        }

    }

}