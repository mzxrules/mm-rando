using MMRando.Constants;
using MMRando.Models.Rom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace MMRando.Utils
{
    public class SequenceUtils
    {
        const int AUDIO_SEQ_TABLE = 0xC77B80;
        const int INSTRUMENT_SET_MAP = 0xC77A60;

        public static void ReadSequenceInfo()
        {
            var sequenceList = new List<SequenceInfo>();
            var targetSequences = new List<SequenceInfo>();

            var json = Resources.GetTextFile("sequences.json");
            List<SequenceInfo> sequences = JsonConvert.DeserializeObject<List<SequenceInfo>>(json);

            foreach(var item in sequences)
            {
                SequenceInfo sourceSequence = new SequenceInfo
                {
                    Name = item.Name,
                    Type = item.Type,
                    Instrument = item.Instrument,
                    Replaceable = item.Replaceable
                };

                SequenceInfo targetSequence = new SequenceInfo
                {
                    Name = item.Name,
                    Type = item.Type,
                    Instrument = item.Instrument
                };

                if (item.Replaceable)
                {
                    sourceSequence.SequenceId = item.SequenceId;
                    targetSequence.Replaces = item.SequenceId;
                    targetSequences.Add(targetSequence);
                }

                if (sourceSequence.Name == "mmr-f-sot")
                {
                    sourceSequence.Replaces = 0x33;
                }

                if (sourceSequence.SequenceId != 0x18) //Fairy Fountain Song, File Select
                {
                    sequenceList.Add(sourceSequence);
                }
            }

            RomData.SequenceList = sequenceList;
            RomData.TargetSequences = targetSequences;
        }


        public static void RebuildAudioSeq(List<SequenceInfo> SequenceList)
        {
            List<MMSequence> OldSeq = new List<MMSequence>();
            int f = RomUtils.GetFileIndexForWriting(AUDIO_SEQ_TABLE);
            int basea = RomData.MMFileList[f].Addr;

            for (int i = 0; i < 128; i++)
            {
                MMSequence entry = new MMSequence();
                if (i == 0x1E)
                {
                    entry.Addr = 2;
                    entry.Size = 0;
                    OldSeq.Add(entry);
                    continue;
                }

                int entryaddr = AUDIO_SEQ_TABLE + (i * 16);
                entry.Addr = (int)ReadWriteUtils.Arr_ReadU32(RomData.MMFileList[f].Data, entryaddr - basea);
                entry.Size = (int)ReadWriteUtils.Arr_ReadU32(RomData.MMFileList[f].Data, (entryaddr - basea) + 4);
                if (entry.Size > 0)
                {
                    entry.Data = new byte[entry.Size];
                    Array.Copy(RomData.MMFileList[4].Data, entry.Addr, entry.Data, 0, entry.Size);
                }
                else
                {
                    int j = SequenceList.FindIndex(u => u.Replaces == i);
                    if (j != -1)
                    {
                        if ((entry.Addr > 0) && (entry.Addr < 128))
                        {
                            if (SequenceList[j].Replaces != 0x28)
                            {
                                SequenceList[j].Replaces = entry.Addr;
                            }
                            else
                            {
                                entry.Data = OldSeq[0x18].Data;
                                entry.Size = OldSeq[0x18].Size;
                            }
                        }
                    }
                }
                OldSeq.Add(entry);
            }

            List<MMSequence> NewSeq = new List<MMSequence>();
            int addr = 0;
            byte[] NewAudioSeq = new byte[0];
            for (int i = 0; i < 128; i++)
            {
                MMSequence newentry = new MMSequence();
                if (OldSeq[i].Size == 0)
                {
                    newentry.Addr = OldSeq[i].Addr;
                }
                else
                {
                    newentry.Addr = addr;
                }

                int j = SequenceList.FindIndex(u => u.Replaces == i);
                if (j != -1)
                {
                    if (SequenceList[j].SequenceId != -1)
                    {
                        newentry.Size = OldSeq[SequenceList[j].SequenceId].Size;
                        newentry.Data = OldSeq[SequenceList[j].SequenceId].Data;
                    }
                    else
                    {
                        BinaryReader sequence = new BinaryReader(File.Open(SequenceList[j].Name, FileMode.Open));
                        int len = (int)sequence.BaseStream.Length;
                        byte[] data = new byte[len];
                        sequence.Read(data, 0, len);
                        sequence.Close();

                        if (data[1] != 0x20)
                        {
                            data[1] = 0x20;
                        }

                        newentry.Size = len;
                        newentry.Data = data;
                    }
                }
                else
                {
                    newentry.Size = OldSeq[i].Size;
                    newentry.Data = OldSeq[i].Data;
                }
                NewSeq.Add(newentry);

                if (newentry.Data != null)
                {
                    NewAudioSeq = NewAudioSeq.Concat(newentry.Data).ToArray();
                }

                addr += newentry.Size;
            }

            if (addr > (RomData.MMFileList[4].End - RomData.MMFileList[4].Addr))
            {
                MMFile newa = new MMFile();
                newa.Addr = RomData.MMFileList[RomData.MMFileList.Count - 1].End;
                newa.End = newa.Addr + addr;
                newa.IsCompressed = false;
                newa.Data = NewAudioSeq;
                RomData.MMFileList.Add(newa);
                ResourceUtils.ApplyHack(Values.ModsDirectory + "reloc-audio");
                RomData.MMFileList[4].Data = new byte[0];
                RomData.MMFileList[4].Cmp_Addr = -1;
                RomData.MMFileList[4].Cmp_End = -1;
            }
            else
            {
                RomData.MMFileList[4].Data = NewAudioSeq;
            }

            //update pointer table
            f = RomUtils.GetFileIndexForWriting(AUDIO_SEQ_TABLE);
            for (int i = 0; i < 128; i++)
            {
                ReadWriteUtils.Arr_WriteU32(RomData.MMFileList[f].Data, (AUDIO_SEQ_TABLE + (i * 16)) - basea, (uint)NewSeq[i].Addr);
                ReadWriteUtils.Arr_WriteU32(RomData.MMFileList[f].Data, 4 + (AUDIO_SEQ_TABLE + (i * 16)) - basea, (uint)NewSeq[i].Size);
            }

            //update inst sets
            f = RomUtils.GetFileIndexForWriting(INSTRUMENT_SET_MAP);
            basea = RomData.MMFileList[f].Addr;
            for (int i = 0; i < 128; i++)
            {
                int paddr = (INSTRUMENT_SET_MAP - basea) + (i * 2) + 2;
                int j = -1;

                if (NewSeq[i].Size == 0)
                {
                    j = SequenceList.FindIndex(u => u.Replaces == NewSeq[i].Addr);
                }
                else
                {
                    j = SequenceList.FindIndex(u => u.Replaces == i);
                }

                if (j != -1)
                {
                    RomData.MMFileList[f].Data[paddr] = (byte)SequenceList[j].Instrument;
                }

            }
        }

    }
}
