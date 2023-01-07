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
        public string dir;
        public D3Level.TableData tableData;
        public D3Level.Hog hog3;
        public string curPalName;

        public GameFiles()
        {
        }

        public GameFiles(string dir)
        {
            if (!Init(dir))
                throw new Exception("Descent files not found in " + dir);
        }

        public bool Init(string dir)
        {
            version = default(Classic.Version);
            string hogName = null;

            if (File.Exists(Path.Combine(dir, "descent2.hog")) && File.Exists(Path.Combine(dir, "descent2.ham")) && File.Exists(Path.Combine(dir, "groupa.pig")))
            {
                version = Classic.Version.D2;
                hogName = "descent2.hog";
            }
            else if (File.Exists(Path.Combine(dir, "descent.hog")) && File.Exists(Path.Combine(dir, "descent.pig")))
            {
                version = Classic.Version.D1;
                hogName = "descent.hog";
            }
            else if (File.Exists(Path.Combine(dir, "d3.hog")))
            {
                version = Classic.Version.D3;
                this.dir = dir;
                hog = null;
                pig = null;
                hog3 = D3Level.Hog.OpenHog(Path.Combine(dir, "d3.hog"));
                using (var s = hog3.Open("Table.gam"))
                    tableData = D3Level.TableData.Read(s);
                return true;
            }
            else
            {
                return false;
            }

            tableData = null;
            hog3 = null;

            this.dir = dir;
            hog = new Hog(Path.Combine(dir, hogName));

            if (version == Classic.Version.D2)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(dir, "descent2.ham"));
                data = new ClassicData();
                data.Read(new BinaryReader(new MemoryStream(bytes, 8, bytes.Length - 8)), version);
            }
            else
            {
                LoadPalette("palette.256");
                pig = new Pig(Path.Combine(dir, "descent.pig"));
                pig.ReadTableData(out data);
            }
            return true;
        }

        private void LoadPalette(string palName)
        {
            byte[] vgaPal = hog.ItemData(palName);
            pal = ClassicLoader.VgaPalConv(vgaPal);
            pal32 = new Color[256];
            for (int i = 0; i < 256; i++)
                pal32[i] = new Color(pal[i * 3], pal[i * 3 + 1], pal[i * 3 + 2], 255);
        }

        public void SelectPalette(string name)
        {
            if (version != Classic.Version.D2)
                return;
            name = name.ToLowerInvariant();
            if (name == curPalName)
                return;
            curPalName = name;
            LoadPalette(name);
            pig = new Pig(Path.Combine(dir, Path.ChangeExtension(name, ".pig")));
        }
    }
}
