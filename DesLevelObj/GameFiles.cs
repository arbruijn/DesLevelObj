using Classic;
using System;
using System.IO;

namespace DesLevelObj
{
    public class GameFiles
    {
        public ClassicData data;
        public Pig pig;
        public Hog hog;
        public byte[] pal;
        public Color[] pal32;
        public Classic.Version version;

        public GameFiles()
        {
        }

        public GameFiles(string pigFilename)
        {
            if (!Init(pigFilename))
                throw new Exception("Descent files not found in " + Path.GetDirectoryName(pigFilename));
        }

        public bool Init(string pigFilename)
        {
            var dir = Path.GetDirectoryName(pigFilename); // args.Length >= 1 ? args[0] : "";
            version = default(Classic.Version);
            string pigName = null;
            string palName = null;
            string hogName = null;

            if (File.Exists(Path.Combine(dir, "descent2.hog")) && File.Exists(Path.Combine(dir, "descent2.ham")) && File.Exists(Path.Combine(dir, "groupa.pig")))
            {
                version = Classic.Version.D2;
                hogName = "descent2.hog";
                pigName = "groupa.pig";
                palName = "groupa.256";
            }
            else if (File.Exists(Path.Combine(dir, "descent.hog")) && File.Exists(Path.Combine(dir, "descent.pig")))
            {
                version = Classic.Version.D1;
                hogName = "descent.hog";
                pigName = "descent.pig";
                palName = "palette.256";
            }
            if (version == Classic.Version.UNKNOWN)
            {
                return false;
            }

            hog = new Hog(Path.Combine(dir, hogName));

            byte[] vgaPal = hog.ItemData(palName);
            pal = ClassicLoader.VgaPalConv(vgaPal);
            pal32 = new Color[256];
            for (int i = 0; i < 256; i++)
                pal32[i] = new Color(pal[i * 3], pal[i * 3 + 1], pal[i * 3 + 2], 255);

            pig = new Pig(Path.Combine(dir, pigName));
            if (version == Classic.Version.D2)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(dir, "descent2.ham"));
                data = new ClassicData();
                data.Read(new BinaryReader(new MemoryStream(bytes, 8, bytes.Length - 8)), version);
            }
            else
            {
                pig.ReadTableData(out data);
            }
            return true;
        }
    }
}
