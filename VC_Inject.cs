using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MMRando
{

    public static class VC_Inject
    {

        private static void GetApp5(byte[] ROM, string VCDir)
        {
            byte[] a50;
            using (var a50stream = new BinaryReader(File.Open(VCDir + "5-0", FileMode.Open)))
            {
                a50 = new byte[a50stream.BaseStream.Length];
                a50stream.Read(a50, 0, a50.Length);
            }
            byte[] a51;
            using (var a51stream = new BinaryReader(File.Open(VCDir + "5-1", FileMode.Open)))
            {
                a51 = new byte[a51stream.BaseStream.Length];
                a51stream.Read(a51, 0, a51.Length);
            }
            using (var app5 = new BinaryWriter(File.Open(VCDir + "00000005.app", FileMode.Create)))
            {
                app5.Write(a50);
                app5.Write(ROM);
                app5.Write(a51);
            }
        }

        private static byte[] AddVCHeader(byte[] ROM)
        {
            byte[] Header = new byte[] { 0x08, 0x00, 0x00, 0x00 };
            return Header.Concat(ROM).ToArray();
        }

        public static void BuildVC(byte[] ROM, string VCDir, string FileName)
        {
            ROM = AddVCHeader(ROM);
            GetApp5(ROM, VCDir);
            ProcessStartInfo p = new ProcessStartInfo
            {
                FileName = "wadpacker.exe",
                Arguments = "mm.tik mm.tmd mm.cert \"" + FileName + "\" -i NMRE",
                WorkingDirectory = VCDir
            };
            Process.Start(p);
        }

    }

}