using Classic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DesLevelObj
{
    public static class D3LevelToObj
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

        public static void Convert(MainForm mainForm, GameFiles gameFiles, TextureRemapRoot textureRemap, D3Level.Level lvl, string outName, bool dumpTex)
        {
            var rooms = lvl.rooms;
            var lvlTexSet = new HashSet<int>();

            var total = new D3Level.Vector3();
            foreach (var room in rooms)
            {
                var roomTotal = new D3Level.Vector3();
                foreach (var vert in room.verts)
                    roomTotal += vert;
                total += roomTotal / room.verts.Length;
            }
            var center = total / rooms.Length;

            var texData = new Dictionary<string, D3Level.TextureInfo>(StringComparer.OrdinalIgnoreCase);
            if (gameFiles != null && gameFiles.tableData != null)
                foreach (var texInfo in gameFiles.tableData.textures)
                {
                    try
                    {
                        texData.Add(texInfo.name, texInfo);
                    }
                    catch (ArgumentException) // dup?
                    {
                    }
                }

            string outDir = Path.GetDirectoryName(outName);
            string mtlName = Path.ChangeExtension(outName, ".mtl");
            using (var f = new StreamWriter(outName))
            using (var fmtl = new StreamWriter(mtlName))
            {
                f.WriteLine("mtllib " + Path.GetFileName(mtlName));
                //f.WriteLine("o " + Path.GetFileNameWithoutExtension(outName).Replace(' ', '_'));

                var vertOfs = 1;
                var texCoordOfs = 1;
                var roomTex = new HashSet<int>();
                for (int roomIdx = 0; roomIdx < rooms.Count(); roomIdx++)
                {
                    f.WriteLine("o room" + (roomIdx + 1000).ToString().Substring(1));
                    var room = rooms[roomIdx];
                    foreach (var vert in room.verts)
                        f.WriteLine("v " + -(vert.x - center.x) + " " + (vert.y - center.y) + " " +
                            (vert.z - center.z));
                    roomTex.Clear();
                    foreach (var face in room.faces)
                        roomTex.Add(face.tmapIdx);
                    var texList = roomTex.ToList();
                    texList.Sort();
                    foreach (var tex in texList)
                    {
                        f.WriteLine("usemtl " + lvl.texture_xlate[tex].n.Replace(' ', '_'));
                        lvlTexSet.Add(tex);
                        foreach (var face in room.faces)
                        {
                            if (face.tmapIdx != tex)
                                continue;
                            int n = face.face_verts.Length;
                            var uvls = face.face_uvls;
                            for (int i = 0; i < n; i++)
                                f.WriteLine("vt " + uvls[i].u + " " + -uvls[i].v);

                            var line = new StringBuilder("f");
                            for (int i = n - 1; i >= 0; i--)
                            {
                                line.Append(' ');
                                line.Append(vertOfs + face.face_verts[i]);
                                line.Append('/');
                                line.Append(texCoordOfs + i);
                            }
                            f.WriteLine(line);
                            texCoordOfs += n;
                        }
                    }
                    vertOfs += room.verts.Count();
                }

                var lvlTex = lvlTexSet.ToList();
                lvlTex.Sort();

                foreach (var tex in lvlTex)
                {
                    var matName = lvl.texture_xlate[tex].n.Replace(' ', '_');
                    var textureName = matName;

                    if (textureRemap != null && textureRemap.TextureRemap.Count > 0)
                    {
                        var foundRemap = textureRemap.TextureRemap.FirstOrDefault(x => x.Textures.Contains(matName, StringComparer.InvariantCultureIgnoreCase));

                        if (foundRemap != null)
                        {
                            mainForm.Log($"Renaming texture '{matName}' to '{foundRemap.RemapTo.Material}', '{foundRemap.RemapTo.Texture}'...");
                            matName = foundRemap.RemapTo.Material;
                            textureName = foundRemap.RemapTo.Texture;
                        }
                    }

                    fmtl.WriteLine("newmtl " + matName);
                    fmtl.WriteLine("illum 2");
                    fmtl.WriteLine("Kd 1.00 1.00 1.00");
                    fmtl.WriteLine("Ka 0.00 0.00 0.00");
                    fmtl.WriteLine("Ks 0.00 0.00 0.00");
                    fmtl.WriteLine("d 1.0");
                    fmtl.WriteLine("map_Kd " + textureName + ".png");
                    if (dumpTex && texData.TryGetValue(lvl.texture_xlate[tex].n, out var texInfo))
                    {
                        bool vclip = texInfo.filename.EndsWith(".oaf", StringComparison.OrdinalIgnoreCase);
                        try
                        {
                            using (var s = gameFiles.hog3.Open(texInfo.filename))
                            {
                                var r = new BinaryReader(s);
                                if (vclip)
                                    r.BaseStream.Position += 7; // skip vclip header
                                D3Level.Bitmap.Read(r).WritePNG(Path.Combine(outDir, textureName + ".png"));
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }
    }
}
