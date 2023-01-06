using Classic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DesLevelObj
{
    public static class LevelToObj
    {
        // img is in argb format
        public static void WritePng(string fn, byte[] img, int w, int h)
        {
            var fmt = PixelFormat.Format32bppArgb;
            Rectangle rect = new Rectangle(0, 0, w, h);
            using (Bitmap b = new Bitmap(w, h, fmt))
            {
                BitmapData d = b.LockBits(rect, ImageLockMode.ReadWrite, fmt);
                Marshal.Copy(img, 0, d.Scan0, img.Length);
                b.UnlockBits(d);
                b.Save(fn, ImageFormat.Png);
            }
        }

        public static void WritePigBitmapToPng(string fn, byte[] pal, Pig pig, PigBitmap bmp)
        {
            if (File.Exists(fn))
                return;

            int w = bmp.width, h = bmp.height;
            byte[] img = new byte[w * h * 4];
            int dstOfs = 0;

            byte[] img8 = pig.GetBitmap(bmp);
            int srcOfs = 0;
            int th = bmp.height;
            for (int y = 0; y < th; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int colorIdx = img8[srcOfs++] * 3;
                    img[dstOfs++] = pal[colorIdx + 2];
                    img[dstOfs++] = pal[colorIdx + 1];
                    img[dstOfs++] = pal[colorIdx];
                    img[dstOfs++] = (colorIdx >= 254 * 3 ? (byte)0 : (byte)255);
                }
            }
            WritePng(fn, img, w, h);
        }

        public static void Convert(GameFiles gameFiles, ClassicLevel lvl, string outName, bool dumpTex)
        {
            var mine = lvl.mine;
            var sides = new List<Side>();
            var sideSegs = new List<int>();
            var sideNums = new List<int>();
            var segments = mine.Segments;
            for (int segIdx = 0; segIdx < segments.Count(); segIdx++)
                for (int i = 0; i < 6; i++)
                {
                    var side = segments[segIdx].sides[i];
                    if (segments[segIdx].children[i] != -1 && side.wall_num == -1)
                        continue;
                    sides.Add(side);
                    sideNums.Add(i);
                    sideSegs.Add(segIdx);
                }
            var lvlTexSet = new HashSet<int>();
            foreach (var side in sides)
                lvlTexSet.Add(side.tmap_num);
            var lvlTex = lvlTexSet.ToList();
            lvlTex.Sort();

            int texCount = lvlTex.Count();
            int matCount = texCount;

            string outDir = Path.GetDirectoryName(outName);
            string mtlName = Path.ChangeExtension(outName, ".mtl");
            using (var f = new StreamWriter(outName))
            using (var fmtl = new StreamWriter(mtlName))
            {
                f.WriteLine("mtllib " + Path.GetFileName(mtlName));
                f.WriteLine("o " + Path.GetFileNameWithoutExtension(outName).Replace(' ', '_'));
                foreach (var vert in mine.Vertices)
                    f.WriteLine("v " + -vert.x.ToFloat() + " " + vert.y + " " + vert.z);
                //foreach (var norm in modelReader.Norms.ItemList)
                //    f.WriteLine("vn " + -norm.x.ToFloat() + " " + norm.y + " " + norm.z);
                foreach (var side in sides)
                    foreach (var uvl in side.uvls)
                        f.WriteLine("vt " + (uvl.u.ToFloat()) + " " + (-uvl.v.ToFloat()));
                foreach (var tex in lvlTex)
                {
                    var bmpIdx = gameFiles.data.Textures[tex].index - 1;
                    var bmp = gameFiles.pig.bitmaps[bmpIdx];
                    var matName = bmp.name;
                    fmtl.WriteLine("newmtl " + matName);
                    fmtl.WriteLine("illum 2");
                    fmtl.WriteLine("Kd 1.00 1.00 1.00");
                    fmtl.WriteLine("Ka 0.00 0.00 0.00");
                    fmtl.WriteLine("Ks 0.00 0.00 0.00");
                    fmtl.WriteLine("d 1.0");
                    fmtl.WriteLine("map_Kd " + matName + ".png");
                    if (dumpTex)
                        WritePigBitmapToPng(Path.Combine(outDir, matName + ".png"), gameFiles.pal, gameFiles.pig, bmp);

                    f.WriteLine("usemtl " + matName);
                    f.WriteLine("s off");
                    for (int i = 0; i < sides.Count(); i++)
                    {
                        if (sides[i].tmap_num != tex)
                            continue;
                        var segIdx = sideSegs[i];
                        var sideNum = sideNums[i];
                        var verts = new int[4][];
                        for (int j = 0; j < 4; j++)
                        {
                            verts[j] = new int[2];
                            verts[j][0] = segments[segIdx].verts[Segment.Side_to_verts[sideNum, j]];
                            verts[j][1] = i * 4 + j;
                        }
                        f.WriteLine("f " + string.Join(" ", verts.Reverse().Select(v => (v[0] + 1) + "/" + (v[1] + 1))));
                    }
                }
            }
        }
    }
}
