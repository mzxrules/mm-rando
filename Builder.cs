﻿using MMRando.Constants;
using MMRando.Models;
using MMRando.Models.Rom;
using MMRando.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MMRando
{

    public class Builder
    {
        private RandomizedResult _randomized;
        private Settings _settings;

        public Builder(RandomizedResult randomized)
        {
            _randomized = randomized;
            _settings = randomized.Settings;
        }

        private void WriteAudioSeq()
        {
            if (!_settings.RandomizeBGM)
            {
                return;
            }

            foreach (SequenceInfo s in RomData.SequenceList)
            {
                s.Name = Values.MusicDirectory + s.Name;
            }

            ResourceUtils.ApplyHack(Values.ModsDirectory + "fix-music");
            ResourceUtils.ApplyHack(Values.ModsDirectory + "inst24-swap-guitar");
            SequenceUtils.RebuildAudioSeq(RomData.SequenceList);
        }

        private void WriteLinkAppearance()
        {
            if (_settings.Character == Character.LinkMM)
            {
                WriteTunicColour();
            }

            else if (_settings.Character == Character.LinkOOT
                || _settings.Character == Character.AdultLink
                || _settings.Character == Character.Kafei)
            {
                int characterIndex = (int)_settings.Character;

                BinaryReader b = new BinaryReader(File.Open(Values.ObjsDirectory + "link-" + characterIndex.ToString(), FileMode.Open));
                byte[] obj = new byte[b.BaseStream.Length];
                b.Read(obj, 0, obj.Length);
                b.Close();

                if (_settings.Character != Character.Kafei)
                {
                    WriteTunicColour(obj, characterIndex);
                }

                ResourceUtils.ApplyHack(Values.ModsDirectory + "fix-link-" + characterIndex.ToString());
                ObjUtils.InsertObj(obj, 0x11);

                if (_settings.Character == Character.Kafei)
                {
                    b = new BinaryReader(File.Open(Values.ObjsDirectory + "kafei", FileMode.Open));
                    obj = new byte[b.BaseStream.Length];
                    b.Read(obj, 0, obj.Length);
                    b.Close();
                    WriteTunicColour(obj, characterIndex);
                    ObjUtils.InsertObj(obj, 0x1C);
                    ResourceUtils.ApplyHack(Values.ModsDirectory + "fix-kafei");
                }
            }
            List<int[]> Others = ResourceUtils.GetAddresses(Values.AddrsDirectory + "tunic-forms");
            TunicUtils.UpdateFormTunics(Others, _settings.TunicColor);
        }

        private void WriteTunicColour()
        {
            Color t = _settings.TunicColor;
            byte[] c = { t.R, t.G, t.B };
            List<int[]> locs = ResourceUtils.GetAddresses(Values.AddrsDirectory + "tunic-colour");
            for (int i = 0; i < locs.Count; i++)
            {
                ReadWriteUtils.WriteROMAddr(locs[i], c);
            }
        }

        private void WriteTunicColour(byte[] obj, int i)
        {
            Color t = _settings.TunicColor;
            byte[] c = { t.R, t.G, t.B };
            List<int[]> locs = ResourceUtils.GetAddresses(Values.AddrsDirectory + "tunic-" + i.ToString());
            for (int j = 0; j < locs.Count; j++)
            {
                ReadWriteUtils.WriteFileAddr(locs[j], c, obj);
            }
        }

        private void WriteTatlColour()
        {
            if (_settings.TatlColorSchema != TatlColorSchema.Random)
            {
                var selectedColorSchemaIndex = (int)_settings.TatlColorSchema;
                byte[] c = new byte[8];
                List<int[]> locs = ResourceUtils.GetAddresses(Values.AddrsDirectory + "tatl-colour");
                for (int i = 0; i < locs.Count; i++)
                {
                    ReadWriteUtils.Arr_WriteU32(c, 0, Values.TatlColours[selectedColorSchemaIndex, i << 1]);
                    ReadWriteUtils.Arr_WriteU32(c, 4, Values.TatlColours[selectedColorSchemaIndex, (i << 1) + 1]);
                    ReadWriteUtils.WriteROMAddr(locs[i], c);
                }
            }
            else
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "rainbow-tatl");
            }
        }

        private void WriteQuickText()
        {
            if (_settings.QuickTextEnabled)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "quick-text");
            }
        }

        private void WriteCutscenes()
        {
            if (_settings.ShortenCutscenes)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "short-cutscenes");
            }
        }

        private void WriteDungeons()
        {
            if ((_settings.LogicMode == LogicMode.Vanilla) || (!_settings.RandomizeDungeonEntrances))
            {
                return;
            }

            EntranceUtils.WriteEntrances(Values.OldEntrances.ToArray(), _randomized.NewEntrances);
            EntranceUtils.WriteEntrances(Values.OldExits.ToArray(), _randomized.NewExits);
            byte[] li = new byte[] { 0x24, 0x02, 0x00, 0x00 };
            List<int[]> addr = new List<int[]>();
            addr = ResourceUtils.GetAddresses(Values.AddrsDirectory + "d-check");
            for (int i = 0; i < addr.Count; i++)
            {
                li[3] = (byte)_randomized.NewExitIndices[i];
                ReadWriteUtils.WriteROMAddr(addr[i], li);
            }

            ResourceUtils.ApplyHack(Values.ModsDirectory + "fix-dungeons");
            addr = ResourceUtils.GetAddresses(Values.AddrsDirectory + "d-exit");

            for (int i = 0; i < addr.Count; i++)
            {
                if (i == 2)
                {
                    ReadWriteUtils.WriteROMAddr(addr[i], new byte[] {
                        (byte)((Values.OldExits[_randomized.NewEntranceIndices[i + 1]] & 0xFF00) >> 8),
                        (byte)(Values.OldExits[_randomized.NewEntranceIndices[i + 1]] & 0xFF) });
                }
                else
                {
                    ReadWriteUtils.WriteROMAddr(addr[i], new byte[] {
                        (byte)((Values.OldExits[_randomized.NewEntranceIndices[i]] & 0xFF00) >> 8),
                        (byte)(Values.OldExits[_randomized.NewEntranceIndices[i]] & 0xFF) });
                }
            }

            addr = ResourceUtils.GetAddresses(Values.AddrsDirectory + "dc-flagload");
            for (int i = 0; i < addr.Count; i++)
            {
                ReadWriteUtils.WriteROMAddr(addr[i], new byte[] { (byte)((_randomized.NewDCFlags[i] & 0xFF00) >> 8), (byte)(_randomized.NewDCFlags[i] & 0xFF) });
            }

            addr = ResourceUtils.GetAddresses(Values.AddrsDirectory + "dc-flagmask");
            for (int i = 0; i < addr.Count; i++)
            {
                ReadWriteUtils.WriteROMAddr(addr[i], new byte[] {
                    (byte)((_randomized.NewDCMasks[i] & 0xFF00) >> 8),
                    (byte)(_randomized.NewDCMasks[i] & 0xFF) });
            }
        }

        private void WriteGimmicks()
        {
            int damageMultiplier = (int)_settings.DamageMode;
            if (damageMultiplier > 0)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "dm-" + damageMultiplier.ToString());
            }

            int damageEffect = (int)_settings.DamageEffect;
            if (damageEffect > 0)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "de-" + damageEffect.ToString());
            }

            int gravityType = (int)_settings.MovementMode;
            if (gravityType > 0)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "movement-" + gravityType.ToString());
            }

            int floorType = (int)_settings.FloorType;
            if (floorType > 0)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "floor-" + floorType.ToString());
            }
        }

        private void WriteEnemies()
        {
            if (_settings.RandomizeEnemies)
            {
                Enemies.ShuffleEnemies(_randomized.Random);
            }
        }

        private void WriteFreeItem(int Item)
        {
            ReadWriteUtils.WriteToROM(Items.ITEM_ADDRS[Item], Items.ITEM_VALUES[Item]);
            switch (Item)
            {
                case 1: //bow
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x01);
                    break;
                case 5: //bomb bag
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x08);
                    break;
                case 19: //sword upgrade
                    ReadWriteUtils.WriteToROM(0xC5CE00, (byte)0x4E);
                    break;
                case 20:
                    ReadWriteUtils.WriteToROM(0xC5CE00, (byte)0x4F);
                    break;
                case 22: //quiver upgrade
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x02);
                    break;
                case 23:
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x03);
                    break;
                case 24://bomb bag upgrade
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x10);
                    break;
                case 25:
                    ReadWriteUtils.WriteToROM(0xC5CE6F, (byte)0x18);
                    break;
                default:
                    break;
            }
        }

        private void WriteItems()
        {
            if (_settings.LogicMode == LogicMode.Vanilla)
            {
                WriteFreeItem(Items.MaskDeku);

                if (_settings.ShortenCutscenes)
                {
                    //giants cs were removed
                    WriteFreeItem(Items.SongOath);
                }

                return;
            }

            //write free item (start item default = Deku Mask)
            var freeItemIndex = _randomized.ItemList.FindIndex(u => u.ReplacesItemId == Items.MaskDeku);
            WriteFreeItem(_randomized.ItemList[freeItemIndex].ID);

            //write everything else
            ItemSwapUtils.ReplaceGetItemTable(Values.ModsDirectory);
            ItemSwapUtils.InitItems();

            for (int i = 0; i < _randomized.ItemList.Count; i++)
            {
                var itemId = _randomized.ItemList[i].ID;

                // Unused item
                if (_randomized.ItemList[i].ReplacesItemId == -1)
                {
                    continue;
                };

                bool isRepeatable = Items.REPEATABLE.Contains(itemId);
                bool isCycleRepeatable = Items.CYCLE_REPEATABLE.Contains(itemId);
                int replacesItemId = _randomized.ItemList[i].ReplacesItemId;

                if (ItemUtils.IsItemDefinedPastAreas(itemId))
                {
                    // Subtract amount of entries describing areas and other
                    itemId -= Values.NumberOfAreasAndOther;
                }

                if (ItemUtils.IsItemDefinedPastAreas(replacesItemId))
                {
                    // Subtract amount of entries describing areas and other
                    replacesItemId -= Values.NumberOfAreasAndOther;
                }

                if (ItemUtils.IsBottleCatchContent(i))
                {
                    ItemSwapUtils.WriteNewBottle(replacesItemId, itemId);
                }
                else
                {
                    Debug.WriteLine($"Writing {Items.ITEM_NAMES[itemId]} --> {Items.ITEM_NAMES[replacesItemId]}");
                    ItemSwapUtils.WriteNewItem(replacesItemId, itemId, isRepeatable, isCycleRepeatable);
                }
            }

            if (_settings.AddShopItems)
            {
                ResourceUtils.ApplyHack(Values.ModsDirectory + "fix-shop-checks");
            }
        }

        private void WriteGossipQuotes()
        {
            if (_settings.LogicMode == LogicMode.Vanilla)
            {
                return;
            }

            if (_settings.EnableGossipHints)
            {
                MessageUtils.WriteGossipMessage(_randomized.GossipQuotes, _randomized.Random);
            }
        }

        private void WriteSpoilerLog()
        {
            if (_settings.LogicMode == LogicMode.Vanilla || !_settings.GenerateSpoilerLog)
            {
                return;
            }

            var settingsString = _settings.ToString();

            var directory = Path.GetDirectoryName(_settings.OutputROMFilename);
            var filename = $"{Path.GetFileNameWithoutExtension(_settings.OutputROMFilename)}_SpoilerLog.txt";

            using (var LogFile = new StreamWriter(Path.Combine(directory, filename)))
            {

                LogFile.WriteLine("Version: " + MainForm.AssemblyVersion.Substring(26));
                LogFile.WriteLine("Settings String: \"" + settingsString + "\"");
                LogFile.WriteLine("Seed: \"" + _settings.Seed + "\"\n");

                LogFile.WriteLine("------------TEST OF PLAYTHROUGH GUIDE-----------");

                if (_settings.RandomizeDungeonEntrances)
                {
                    LogFile.WriteLine("------------Entrance----------------------------Destination-----------");
                    string[] destinations = new string[] { "Woodfall", "Snowhead", "Inverted Stone Tower", "Great Bay" };
                    for (int i = 0; i < 4; i++)
                    {
                        LogFile.WriteLine(destinations[i].PadRight(32, '-')
                            + "---->>" + destinations[_randomized.NewEntranceIndices[i]].PadLeft(32, '-'));
                    };
                    LogFile.WriteLine("");
                };
                //
                // THIS SHOULDN'T BE HERE!? BUT DOESN'T WORK IF IT ISN'T WTF
                //
                _randomized.ItemList.RemoveAll(item => !item.ReplacesAnotherItem);

                LogFile.WriteLine("--------------Item------------------------------Destination-----------");
                for (int i = 0; i < _randomized.ItemList.Count; i++)
                {
                    LogFile.WriteLine(Items.ITEM_NAMES[_randomized.ItemList[i].ID].PadRight(32, '-') + "---->>" + Items.ITEM_NAMES[_randomized.ItemList[i].ReplacesItemId].PadLeft(32, '-'));
                };
                LogFile.WriteLine("");
                LogFile.WriteLine("-----------Destination------------------------------Item--------------");
                _randomized.ItemList.Sort((i, j) => i.ReplacesItemId.CompareTo(j.ReplacesItemId));
                for (int i = 0; i < _randomized.ItemList.Count; i++)
                {
                    LogFile.WriteLine(Items.ITEM_NAMES[_randomized.ItemList[i].ReplacesItemId].PadRight(32, '-') + "<<----" + Items.ITEM_NAMES[_randomized.ItemList[i].ID].PadLeft(32, '-'));
                };
            }
        }

        private void WriteFileSelect()
        {
            if (_settings.LogicMode == LogicMode.Vanilla)
            {
                return;
            }

            ResourceUtils.ApplyHack(Values.ModsDirectory + "file-select");
            byte[] SkyboxDefault = new byte[] { 0x91, 0x78, 0x9B, 0x28, 0x00, 0x28 };
            List<int[]> Addrs = ResourceUtils.GetAddresses(Values.AddrsDirectory + "skybox-init");
            Random R = new Random();
            int rot = R.Next(360);
            for (int i = 0; i < 2; i++)
            {
                Color c = Color.FromArgb(SkyboxDefault[i * 3], SkyboxDefault[i * 3 + 1], SkyboxDefault[i * 3 + 2]);
                float h = c.GetHue();
                h += rot;
                h %= 360f;
                c = Hue.FromAHSB(c.A, h, c.GetSaturation(), c.GetBrightness());
                SkyboxDefault[i * 3] = c.R;
                SkyboxDefault[i * 3 + 1] = c.G;
                SkyboxDefault[i * 3 + 2] = c.B;
            }

            for (int i = 0; i < 3; i++)
            {
                ReadWriteUtils.WriteROMAddr(Addrs[i], new byte[] { SkyboxDefault[i * 2], SkyboxDefault[i * 2 + 1] });
            }

            rot = R.Next(360);
            byte[] FSDefault = new byte[] { 0x64, 0x96, 0xFF, 0x96, 0xFF, 0xFF, 0x64, 0xFF, 0xFF };
            Addrs = ResourceUtils.GetAddresses(Values.AddrsDirectory + "fs-colour");
            for (int i = 0; i < 3; i++)
            {
                Color c = Color.FromArgb(FSDefault[i * 3], FSDefault[i * 3 + 1], FSDefault[i * 3 + 2]);
                float h = c.GetHue();
                h += rot;
                h %= 360f;
                c = Hue.FromAHSB(c.A, h, c.GetSaturation(), c.GetBrightness());
                FSDefault[i * 3] = c.R;
                FSDefault[i * 3 + 1] = c.G;
                FSDefault[i * 3 + 2] = c.B;
            }
            for (int i = 0; i < 9; i++)
            {
                if (i < 6)
                {
                    ReadWriteUtils.WriteROMAddr(Addrs[i], new byte[] { 0x00, FSDefault[i] });
                }
                else
                {
                    ReadWriteUtils.WriteROMAddr(Addrs[i], new byte[] { FSDefault[i] });
                }
            }
        }

        private void WriteStartupStrings()
        {
            if (_settings.LogicMode == LogicMode.Vanilla)
            {
                //ResourceUtils.ApplyHack(ModsDir + "postman-testing");
                return;
            }
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            RomUtils.SetStrings(Values.ModsDirectory + "logo-text", $"v{v}", _settings.ToString());
        }

        public void MakeROM(string InFile, string FileName, BackgroundWorker worker)
        {
            using (BinaryReader OldROM = new BinaryReader(File.Open(InFile, FileMode.Open, FileAccess.Read)))
            {
                RomUtils.ReadFileTable(OldROM);
            }

            worker.ReportProgress(55, "Writing Audio...");
            WriteAudioSeq();

            worker.ReportProgress(60, "Writing Character...");
            WriteLinkAppearance();
            if (_settings.LogicMode != LogicMode.Vanilla)
            {
                worker.ReportProgress(61, "Applying hacks...");
                ResourceUtils.ApplyHack(Values.ModsDirectory + "title-screen");
                ResourceUtils.ApplyHack(Values.ModsDirectory + "misc-changes");
                ResourceUtils.ApplyHack(Values.ModsDirectory + "cm-cs");
                WriteFileSelect();
            }
            ResourceUtils.ApplyHack(Values.ModsDirectory + "init-file");

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

            byte[] ROM = RomUtils.BuildROM(FileName);
            if (_settings.OutputVC)
            {
                worker.ReportProgress(98, "Building VC...");
                VCInjectionUtils.BuildVC(ROM, Values.VCDirectory, Path.ChangeExtension(FileName, "wad"));
            }
            worker.ReportProgress(100, "Done!");

        }

    }

}