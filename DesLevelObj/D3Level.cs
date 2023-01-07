using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace D3Level
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public float Mag()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
        public float Normalize()
        {
            float m = Mag();
            if (m > 0)
            {
                x /= m;
                y /= m;
                z /= m;
            }
            else
            {
                x = y = z = 0.5773502691896258f;
            }
            return m;
        }
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Vector3 operator /(Vector3 a, float b)
        {
            return new Vector3(a.x / b, a.y / b, a.z / b);
        }
        public static Vector3 CrossProduct(Vector3 a, Vector3 b)
        {
            return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }
        public static Vector3 GetPerp(Vector3 a, Vector3 b, Vector3 c)
        {
            return CrossProduct(b - a, c - b);
        }
        public static float GetNorm(Vector3 a, Vector3 b, Vector3 c, out Vector3 n)
        {
            n = GetPerp(a, b, c);
            return n.Normalize();
        }
        public override string ToString()
        {
            return x + "," + y + "," + z;
        }
    }

    static class LevelExt
    {
        public static Vector3 ReadVector3(this BinaryReader r)
        {
            return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }
        public static string ReadCString(this BinaryReader r)
        {
            byte b;
            var bs = new List<byte>();
            while ((b = r.ReadByte()) != 0)
                bs.Add(b);
            return Encoding.UTF8.GetString(bs.ToArray());
        }
    }

    public struct HogEntry
    {
        public string name;
        public uint flags;
        public uint len;
        public uint timestamp;
        public long offset;
    }
    public class Hog
    {
        const int PSFILENAME_LEN = 35;
        const int HOG_HDR_SIZE = 64;

        private Stream stream;
        public HogEntry[] Entries;
        private Dictionary<string, int> names;

        public static Hog OpenHog(string filename)
        {
            Stream stream = File.OpenRead(filename);
            stream = new BufferedStream(stream, 65536);
            BinaryReader r = new BinaryReader(stream);
            var id = r.ReadBytes(4);
            if (!id.SequenceEqual(new [] { (byte)'H', (byte)'O', (byte)'G', (byte)'2' }))
                throw new Exception("Invalid hog header");

            uint nfiles = r.ReadUInt32();
            long file_data_offset = r.ReadUInt32();
            var entries = new HogEntry[nfiles];
            var names = new Dictionary<string, int>();
            r.BaseStream.Position = 4 + HOG_HDR_SIZE;
            long offset = file_data_offset;
            for (int i = 0; i < nfiles; i++) {
                var name = r.ReadBytes(PSFILENAME_LEN + 1);
                var ni = Array.IndexOf<byte>(name, 0);
                entries[i].name = Encoding.UTF8.GetString(name, 0, ni >= 0 ? ni : name.Length);
                entries[i].flags = r.ReadUInt32();
                entries[i].len = r.ReadUInt32();
                entries[i].timestamp = r.ReadUInt32();
                entries[i].offset = offset;
                offset += entries[i].len;
                names.Add(entries[i].name.ToLowerInvariant(), i);
            }
            return new Hog() { stream = stream, Entries = entries, names = names };
        }

        public Stream Open(string name)
        {
            if (!names.TryGetValue(name.ToLowerInvariant(), out int idx))
                throw new FileNotFoundException(name);
            return new BufferedStream(new SubStreamRead(stream, Entries[idx].offset, Entries[idx].len), 65536);
        }
    }

    public class Bitmap
    {
        public const int NUM_MIP_LEVELS = 5;
        public const int OUTRAGE_4444_COMPRESSED_MIPPED = 121;
        public const int OUTRAGE_1555_COMPRESSED_MIPPED = 122;
        public const int OUTRAGE_NEW_COMPRESSED_MIPPED = 123;
        public const int OUTRAGE_COMPRESSED_MIPPED = 124;
        public const int OUTRAGE_COMPRESSED_OGF_8BIT = 125;
        public const int OUTRAGE_TGA_TYPE = 126;
        public const int OUTRAGE_COMPRESSED_OGF = 127;
        public const int BITMAP_NAME_LEN = 35;
        public const int BAD_BITMAP_HANDLE = 0;
        public const int BF_TRANSPARENT = 1;
        public const int BF_CHANGED = 2;
        public const int BF_MIPMAPPED = 4;
        public const int BF_NOT_RESIDENT = 8;
        public const int BF_WANTS_MIP = 16;
        public const int BF_WANTS_4444 = 32;
        public const int BF_BRAND_NEW = 64;
        public const int BF_COMPRESSABLE = 128;
        public const int BITMAP_FORMAT_STANDARD = 0;
        public const int BITMAP_FORMAT_1555 = 0;
        public const int BITMAP_FORMAT_4444 = 1;

        public static Bitmap Read(BinaryReader r)
        {
            byte image_id_len = r.ReadByte();
            byte color_map_type = r.ReadByte();
            byte image_type = r.ReadByte();

            if (color_map_type != 0 || (image_type != 10 && image_type != 2 && image_type != OUTRAGE_TGA_TYPE && image_type != OUTRAGE_COMPRESSED_OGF && image_type != OUTRAGE_COMPRESSED_MIPPED && image_type != OUTRAGE_NEW_COMPRESSED_MIPPED && image_type != OUTRAGE_1555_COMPRESSED_MIPPED && image_type != OUTRAGE_4444_COMPRESSED_MIPPED))
                throw new ArgumentException("unknown file format image_type " + image_type + " color_map_type " + color_map_type);

            string name = null;
            int num_mips = 1;
            //bool mipped = false;
            if (image_type == OUTRAGE_4444_COMPRESSED_MIPPED || image_type == OUTRAGE_1555_COMPRESSED_MIPPED || image_type == OUTRAGE_NEW_COMPRESSED_MIPPED || image_type == OUTRAGE_TGA_TYPE || image_type == OUTRAGE_COMPRESSED_MIPPED || image_type == OUTRAGE_COMPRESSED_OGF || image_type == OUTRAGE_COMPRESSED_OGF_8BIT)
            {
                if (image_type == OUTRAGE_4444_COMPRESSED_MIPPED || image_type == OUTRAGE_NEW_COMPRESSED_MIPPED || image_type == OUTRAGE_1555_COMPRESSED_MIPPED)
                {
                    name = r.ReadCString();
                }
                else
                {
                    name = Encoding.UTF8.GetString(r.ReadBytes(BITMAP_NAME_LEN));
                }
                if (image_type == OUTRAGE_4444_COMPRESSED_MIPPED || image_type == OUTRAGE_1555_COMPRESSED_MIPPED || image_type == OUTRAGE_COMPRESSED_MIPPED || image_type == OUTRAGE_NEW_COMPRESSED_MIPPED)
                    num_mips = r.ReadByte();
                else
                    num_mips = 1;

                //if (num_mips > 1)
                //    mipped = true;
            }

            for (int i = 0; i < 9; i++)
                r.ReadByte();

            int width = r.ReadInt16();
            int height = r.ReadInt16();
            int pixsize = r.ReadByte();

            if (pixsize != 32 && pixsize != 24)
                return null;

            int descriptor = r.ReadByte();
            if (((descriptor & 0x0F) != 8) && ((descriptor & 0x0F) != 0))
                return null;

            for (int i = 0; i < image_id_len; i++)
                r.ReadByte();

            bool upside_down = (descriptor & 0x20) == 0;

            int total = width * height;
            ushort[] dest = new ushort[total];
            if (image_type == OUTRAGE_4444_COMPRESSED_MIPPED || image_type == OUTRAGE_1555_COMPRESSED_MIPPED || image_type == OUTRAGE_NEW_COMPRESSED_MIPPED || image_type == OUTRAGE_COMPRESSED_MIPPED || image_type == OUTRAGE_COMPRESSED_OGF || image_type == OUTRAGE_COMPRESSED_OGF_8BIT)
            {
                int count = 0;
                while (count < total)
                {
                    int cmd = r.ReadByte();
                    ushort pixel = r.ReadUInt16();
                    if (cmd == 0)
                        dest[count++] = pixel;
                    else if (cmd >= 2 && cmd <= 250)
                        for (int i = 0; i < cmd; i++)
                            dest[count++] = pixel;
                    else
                        throw new Exception("Invalid compression command");
                }
            } else
                throw new Exception("Invalid image file type");
            return new Bitmap() { width = width, height = height, data = dest, type = image_type };
        }

        public int width, height, type;
        public ushort[] data;

        public static Bitmap Read(string filename)
        {
            using (var f = File.OpenRead(filename))
                return Read(new BinaryReader(f));
        }

        private int Conv5to8(int n) => (n << 3) | (n >> 2);

        public void WritePNG(string filename)
        {
            System.Drawing.Rectangle rect = new Rectangle(0, 0, width, height);
            PixelFormat fmt = PixelFormat.Format32bppArgb;

            int[] img = new int[width * height];

            int lastrow = (height - 1) * width;
            for (int ofs = 0; ofs <= lastrow; ofs += width)
            {
                if (type == OUTRAGE_4444_COMPRESSED_MIPPED)
                    for (int x = 0; x < width; x++)
                    {
                        ushort n = data[ofs + x];
                        img[ofs + x] =
                            ((n & 0xf000) * (0x11 << (24 - 12))) |
                            ((n & 0x0f00) * (0x11 << (16 - 8))) |
                            ((n & 0x00f0) * (0x11 << (8 - 4))) |
                            ((n & 0x000f) * (0x11 << 0));
                    }
                else
                    for (int x = 0; x < width; x++)
                    {
                        ushort n = data[ofs + x];
                        img[ofs + x] =
                            ((n & 0x8000) * 0x1fe00) |
                            (Conv5to8((n & 0x7c00) >> 10) << 16) |
                            (Conv5to8((n & 0x03e0) >> 5) << 8) |
                            (Conv5to8((n & 0x001f) >> 0) << 0);
                    }
            }

            using (System.Drawing.Bitmap b = new System.Drawing.Bitmap(width, height, fmt))
            {
                BitmapData d = b.LockBits(rect, ImageLockMode.ReadWrite, fmt);
                Marshal.Copy(img, 0, d.Scan0, img.Length);
                b.UnlockBits(d);
                b.Save(filename, ImageFormat.Png);
            }
        }
    }

    public class Level
    {
        private static uint MkTag(char a, char b, char c, char d) => a | (((uint)b) << 8) | (((uint)c) << 16) | (((uint)d) << 24);
        private static readonly uint LEVEL_FILE_TAG = MkTag('D', '3', 'L', 'V');
        private const int LEVEL_FILE_OLDEST_COMPATIBLE_VERSION  = 13;
        private const int LEVEL_FILE_VERSION = 132;
        public static readonly uint CHUNK_TEXTURE_NAMES = MkTag('T', 'X', 'N', 'M');
        public static readonly uint CHUNK_GENERIC_NAMES = MkTag('G', 'N', 'N', 'M');
        public static readonly uint CHUNK_ROBOT_NAMES = MkTag('R', 'B', 'N', 'M');
        public static readonly uint CHUNK_POWERUP_NAMES = MkTag('P', 'W', 'N', 'M');
        public static readonly uint CHUNK_DOOR_NAMES = MkTag('D', 'R', 'N', 'M');
        public static readonly uint CHUNK_ROOMS = MkTag('R', 'O', 'O', 'M');
        public static readonly uint CHUNK_ROOM_WIND = MkTag('R', 'W', 'N', 'D');
        public static readonly uint CHUNK_OBJECTS = MkTag('O', 'B', 'J', 'S');
        public static readonly uint CHUNK_TERRAIN = MkTag('T', 'E', 'R', 'R');
        public static readonly uint CHUNK_EDITOR_INFO = MkTag('E', 'D', 'I', 'T');
        public static readonly uint CHUNK_SCRIPT = MkTag('S', 'C', 'P', 'T');
        public static readonly uint CHUNK_TERRAIN_HEIGHT = MkTag('T', 'E', 'R', 'H');
        public static readonly uint CHUNK_TERRAIN_TMAPS_FLAGS = MkTag('T', 'E', 'T', 'M');
        public static readonly uint CHUNK_TERRAIN_LINKS = MkTag('T', 'L', 'N', 'K');
        public static readonly uint CHUNK_TERRAIN_SKY = MkTag('T', 'S', 'K', 'Y');
        public static readonly uint CHUNK_TERRAIN_END = MkTag('T', 'E', 'N', 'D');
        public static readonly uint CHUNK_SCRIPT_CODE = MkTag('C', 'O', 'D', 'E');
        public static readonly uint CHUNK_TRIGGERS = MkTag('T', 'R', 'I', 'G');
        public static readonly uint CHUNK_LIGHTMAPS = MkTag('L', 'M', 'A', 'P');
        public static readonly uint CHUNK_BSP = MkTag('C', 'B', 'S', 'P');
        public static readonly uint CHUNK_OBJECT_HANDLES = MkTag('O', 'H', 'N', 'D');
        public static readonly uint CHUNK_GAME_PATHS = MkTag('P', 'A', 'T', 'H');
        public static readonly uint CHUNK_BOA = MkTag('C', 'B', 'O', 'A');
        public static readonly uint CHUNK_NEW_BSP = MkTag('C', 'N', 'B', 'S');
        public static readonly uint CHUNK_LEVEL_INFO = MkTag('I', 'N', 'F', 'O');
        public static readonly uint CHUNK_PLAYER_STARTS = MkTag('P', 'S', 'T', 'R');
        public static readonly uint CHUNK_MATCEN_DATA = MkTag('M', 'T', 'C', 'N');
        public static readonly uint CHUNK_LEVEL_GOALS = MkTag('L', 'V', 'L', 'G');
        public static readonly uint CHUNK_ROOM_AABB = MkTag('A', 'A', 'B', 'B');
        public static readonly uint CHUNK_NEW_LIGHTMAPS = MkTag('N', 'L', 'M', 'P');
        public static readonly uint CHUNK_ALIFE_DATA = MkTag('L', 'I', 'F', 'E');
        public static readonly uint CHUNK_TERRAIN_SOUND = MkTag('T', 'S', 'N', 'D');
        public static readonly uint CHUNK_BNODES = MkTag('N', 'O', 'D', 'E');
        public static readonly uint CHUNK_OVERRIDE_SOUNDS = MkTag('O', 'S', 'N', 'D');
        public static readonly uint CHUNK_FFT_MOD = MkTag('F', 'F', 'T', 'M');

        private const int ROOM_NAME_LEN = 19;

        public const int FF_LIGHTMAP = 0x0001;
        public const int FF_VERTEX_ALPHA = 0x0002;
        public const int FF_CORONA = 0x0004;
        public const int FF_TEXTURE_CHANGED = 0x0008;
        public const int FF_HAS_TRIGGER = 0x0010;
        public const int FF_SPEC_INVISIBLE = 0x0020;
        public const int FF_FLOATING_TRIG = 0x0040;
        public const int FF_DESTROYED = 0x0080;
        public const int FF_VOLUMETRIC = 0x0100;
        public const int FF_TRIANGULATED = 0x0200;
        public const int FF_VISIBLE = 0x0400;
        public const int FF_NOT_SHELL = 0x0800;
        public const int FF_TOUCHED = 0x1000;
        public const int FF_GOALFACE = 0x2000;
        public const int FF_NOT_FACING = 0x4000;
        public const int FF_SCORCHED = 0x8000;

        public const int OLD_FF_PORTAL_TRIG = 0x0020;

        private static string GetCString(byte[] buf)
        {
            int i = Array.IndexOf<byte>(buf, 0);
            return Encoding.UTF8.GetString(buf, 0, i >= 0 ? i : buf.Length);
        }

        public struct roomUVL
        {
            public float u, v;
            public float u2, v2;
            public byte alpha;
        }

        private static byte Float_to_ubyte(float f)
        {
            return (byte)(f * 255);
        }

        public struct Texture
        {
            public string n;
        }

        public class Face
        {
            public short[] face_verts;
            public roomUVL[] face_uvls;
            public int flags;
            public int portal_num;
            //public Texture tmap;
            public int tmapIdx;
            public byte light_multiple;
        }

        private static Face ReadFace(BinaryReader r, int version, Texture[] texture_xlate)
        {
            Face face = new Face();
            int nverts = r.ReadByte();

            face.face_verts = new short[nverts];
            for (int i = 0; i < nverts; i++)
                face.face_verts[i] = r.ReadInt16();

            #if false
            Console.Write("Read " + nverts + " face verts: ");
            for (int i = 0; i < nverts; i++)
                Console.Write(" " + face.face_verts[i]);
            Console.WriteLine();
            #endif

            //Read uvls, and adjust alpha settings
            bool alphaed = false;

            face.face_uvls = new roomUVL[nverts];
            for (int i = 0; i < nverts; i++)
            {
                face.face_uvls[i].u = r.ReadSingle();
                face.face_uvls[i].v = r.ReadSingle();

                if (version < 56)
                {
                    // Read old lrgb stuff
                    r.ReadSingle();
                    r.ReadSingle();
                    r.ReadSingle();
                    r.ReadSingle();
                }

                if (version >= 21)
                {
                    if (version < 61)
                        face.face_uvls[i].alpha = Float_to_ubyte(r.ReadSingle());
                    else
                        face.face_uvls[i].alpha = r.ReadByte();
                }
                else
                    face.face_uvls[i].alpha = 255;

                if (face.face_uvls[i].alpha != 255)
                    alphaed = true;
            }

            //Read flags
            if (version < 27)
                face.flags = r.ReadByte();
            else
                face.flags = r.ReadInt16();

            //Kill old portal trigger flag
            if (version < 103)
                face.flags &= ~OLD_FF_PORTAL_TRIG;

            //Set vertex alpha flag
            if (alphaed)
                face.flags |= FF_VERTEX_ALPHA;
            else
                face.flags &= ~FF_VERTEX_ALPHA;

            //Read the portal number
            if (version >= 23)
                face.portal_num = r.ReadByte();
            else
                face.portal_num = r.ReadInt16();

            //Read and translate the texture number
            face.tmapIdx = r.ReadInt16();
            //face.tmap = texture_xlate[tmaplvl];

            //Check for failed xlate
            /*
            if (face.tmap.n == -1)
            {
                face.tmap.n = 0;
            }
            */

            // Check to see if there is a lightmap
            if ((face.flags & FF_LIGHTMAP) != 0 && (version >= 19))
            {
                if (version <= 29)
                {
                    int w = r.ReadByte();
                    int h = r.ReadByte();

                    for (int i = 0; i < w * h; i++)
                        r.ReadInt16();

                    face.flags &= ~FF_LIGHTMAP;
                }
                else
                {
                    // Read lightmap info handle
                    int lmi_handle = r.ReadUInt16();
                    /*
                    if (!Dedicated_server)
                    {
                        fp->lmi_handle = LightmapInfoRemap[lmi_handle];
                        LightmapInfo[fp->lmi_handle].used++;
                    }
                    */

                    if (version <= 88)
                    {
                        r.ReadByte();
                        r.ReadByte();
                        r.ReadByte();
                        r.ReadByte();
                    }
                }


                // Read UV2s
                for (int i = 0; i < nverts; i++)
                {
                    face.face_uvls[i].u2 = r.ReadSingle();
                    face.face_uvls[i].v2 = r.ReadSingle();

                    // Stupid fix for bad lightmap uvs
                    if (face.face_uvls[i].u2 < 0)
                        face.face_uvls[i].u2 = 0;
                    if (face.face_uvls[i].u2 > 1f)
                        face.face_uvls[i].u2 = 1.0f;

                    if (face.face_uvls[i].v2 < 0)
                        face.face_uvls[i].v2 = 0;
                    if (face.face_uvls[i].v2 > 1f)
                        face.face_uvls[i].v2 = 1.0f;

                }
            }

            if (version >= 22 && version <= 29)
            {
                r.ReadVector3();
            }

            if (version >= 40 && version <= 60) // was shadow room,face
            {
                r.ReadInt16();
                r.ReadInt16();
            }

            if (version >= 50)
            {
                face.light_multiple = r.ReadByte();

                if (face.light_multiple == 186)
                    face.light_multiple = 4; // Get Jason, I'm looking for this bug!  Its safe to go past it, but I'm just on the lookout


                if (version <= 52)
                {
                    //if (face.light_multiple >= 32)
                     //   Int3(); // Get Jason
                    face.light_multiple *= 4;
                }
            }
            else
                face.light_multiple = 4;

            if (version >= 71)
            {
                byte special = r.ReadByte();
                if (special != 0)
                {
                    if (version < 77)   // Ignore old specular data
                    {
                        r.ReadByte();
                        r.ReadVector3();
                        r.ReadInt16();
                    }
                    else
                    {
                        Vector3 center;

                        byte smooth = 0;
                        byte num_smooth_verts = 0;
                        byte type = r.ReadByte();
                        byte num = r.ReadByte();

                        if (version >= 117)
                        {
                            // Read if smoothed
                            smooth = r.ReadByte();
                            if (smooth != 0)
                            {
                                num_smooth_verts = r.ReadByte();
                                //fp->special_handle = AllocSpecialFace(type, num, true, num_smooth_verts);
                            }
                            else
                                //fp->special_handle = AllocSpecialFace(type, num);
                                smooth = 0;

                        }
                        else
                            //fp->special_handle = AllocSpecialFace(type, num);
                            smooth = 0;

                        //ASSERT(fp->special_handle != BAD_SPECIAL_FACE_INDEX);

                        for (int i = 0; i < num; i++)
                        {
                            center = r.ReadVector3();
                            ushort color = r.ReadUInt16();

                            //SpecialFaces[fp->special_handle].spec_instance[i].bright_center = center;
                            //SpecialFaces[fp->special_handle].spec_instance[i].bright_color = color;
                        }

                        if (smooth != 0)
                        {
                            for (int i = 0; i < num_smooth_verts; i++)
                            {
                                Vector3 vertnorm = r.ReadVector3();
                                //SpecialFaces[fp->special_handle].vertnorms[i] = vertnorm;
                            }
                        }

                    }
                }
            }

            return face;
        }

        public class Portal
        {
            public uint flags;
            public int portal_face;
            public int croom;
            public int cportal;
            public int combine_master;
            public int bnode_index;
            public Vector3 path_pnt;
        }

        public const uint OLD_PF_HAS_TRIGGER = 4;

        public static Portal ReadPortal(BinaryReader r, int version)
        {
            var portal = new Portal();
            
            portal.flags = r.ReadUInt32();

            if (version < 103)
                portal.flags &= ~OLD_PF_HAS_TRIGGER;

            if (version < 80)
            {
                int num_verts = r.ReadInt16();
                for (int i = 0; i < num_verts; i++)
                    r.ReadInt16();

                int num_faces = r.ReadInt16();
            }

            portal.portal_face = r.ReadInt16();

            portal.croom = r.ReadInt32();
            portal.cportal = r.ReadInt32();

            if (version >= 123)
                portal.bnode_index = r.ReadInt16();
            else
                portal.bnode_index = -1;

            if (version >= 63)
            {
                portal.path_pnt = r.ReadVector3();
                //Console.WriteLine(portal.path_pnt);
            }

            if (version >= 100)
                portal.combine_master = r.ReadInt32();

            return portal;
        }

        public class Room
        {
            public string name;
            public Vector3 path_pnt;
            public uint flags;
            public int pulse_time;
            public int pulse_offset;
            public Vector3[] verts;
            public Face[] faces;
            public Portal[] portals;
            public short mirror_face;
            public byte env_reverb;
            public float damage;
            public byte damage_type;
            public int ambient_sound;
            public float fog_depth;
            public float fog_r;
            public float fog_g;
            public float fog_b;
            public byte[] volume_lights;
            public int volume_width;
            public int volume_height;
            public int volume_depth;
        }

        public const int RF_FUELCEN = 1;
        public const int RF_DOOR = (1 << 1);
        public const int RF_EXTERNAL = (1 << 2);
        public const int RF_GOAL1 = (1 << 3);
        public const int RF_GOAL2 = (1 << 4);
        public const int RF_TOUCHES_TERRAIN = (1 << 5);
        public const int RF_SORTED_INC_Y = (1 << 6);
        public const int RF_GOAL3 = (1 << 7);
        public const int RF_GOAL4 = (1 << 8);
        public const int RF_FOG = (1 << 9);
        public const int RF_SPECIAL1 = (1 << 10);
        public const int RF_SPECIAL2 = (1 << 11);
        public const int RF_SPECIAL3 = (1 << 12);
        public const int RF_SPECIAL4 = (1 << 13);
        public const int RF_SPECIAL5 = (1 << 14);
        public const int RF_SPECIAL6 = (1 << 15);
        public const int RF_MIRROR_VISIBLE = (1 << 16);
        public const int RF_TRIANGULATE = (1 << 17);
        public const int RF_STROBE = (1 << 18);
        public const int RF_FLICKER = (1 << 19);
        public const int RFM_MINE = (0x1f << 20);
        public const int RF_INFORM_RELINK_TO_LG = (1 << 25);
        public const int RF_MANUAL_PATH_PNT = (1 << 26);
        public const int RF_WAYPOINT = (1 << 27);
        public const int RF_SECRET = (1 << 28);
        public const int RF_NO_LIGHT = (1 << 29);


        public const int DF_BLASTED = 1;
        public const int DF_AUTO = 2;
        public const int DF_LOCKED = 4;
        public const int DF_KEY_ONLY_ONE = 8;
        public const int DF_GB_IGNORE_LOCKED = 16;

        private static byte[] ReadCompressedBytes(BinaryReader r, int total)
        {
            byte compressed = r.ReadByte();
            if (compressed == 0)
                return r.ReadBytes(total);
            var vals = new byte[total];
            int ofs = 0;
            while (ofs < total)
            {
                int cmd = r.ReadByte();
                byte val = r.ReadByte();
                if (cmd == 0)
                    vals[ofs++] = val;
                else if (cmd >= 2 && cmd < 250)
                    for (int i = 0; i < cmd; i++)
                        vals[ofs++] = val;
                else
                    throw new Exception("Invalid compressed data");
            }
            return vals;
        }

        private static Room ReadRoom(BinaryReader r, int version, Texture[] texture_xlate)
        {
            var room = new Room();
            int nverts = r.ReadInt32();
            int nfaces = r.ReadInt32();
            int nportals = r.ReadInt32();
            if (version >= 96)
            {
                room.name = r.ReadCString();
                //Console.WriteLine("room name " + room.name);
            }
            if (version >= 63)
            {
                room.path_pnt = r.ReadVector3();
            }
            var verts = room.verts = new Vector3[nverts];
            //Console.Write("Reading " + nverts + " vertices");
            for (int i = 0; i < nverts; i++)
            {
                verts[i] = r.ReadVector3();
                //Console.Write(" " + verts[i]);
                if (version >= 52 && version <= 67)
                {
                    r.ReadInt16();
                }
                else if (version >= 68 && version < 71)
                {
                    r.ReadVector3();
                    r.ReadInt16();
                }
            }
            //Console.WriteLine();

            //Console.WriteLine("Reading " + nfaces + " faces");
            var faces = room.faces = new Face[nfaces];
            for (int i = 0; i < nfaces; i++)
                faces[i] = ReadFace(r, version, texture_xlate);

            //Console.WriteLine("Reading " + nportals + " portals");
            var portals = room.portals = new Portal[nportals];
            for (int i = 0; i < nportals; i++)
                portals[i] = ReadPortal(r, version);
            
            room.flags = r.ReadUInt32();

            if (version < 29)
                r.ReadSingle();
            
            if (version >= 68)
            {
                room.pulse_time = r.ReadByte();
                room.pulse_offset = r.ReadByte();
            }

            if (version >= 79)
                room.mirror_face = r.ReadInt16();
            else
                room.mirror_face = -1;
            
            if ((room.flags & RF_DOOR) != 0)
            {
                int doornum, keys;
                float position;
                if (version >= 28 && version <= 32)
                {
                    doornum = r.ReadInt32();
                }
                else if (version >= 33)
                {
                    if (version < 106)
                        r.ReadInt32();
                    int flags = r.ReadByte();
                    if (version < 106)
                        flags |= DF_AUTO;
                    if (version >= 36)
                        keys = r.ReadByte();
                    doornum = r.ReadInt32();
                    if (version >= 106)
                        position = r.ReadSingle();
                }
                if (version >= 28 && version < 106)
                {
                    r.ReadSingle();
                    r.ReadSingle();
                    r.ReadSingle();
                }

            }

            if (version >= 67)
            {
                // Read in volume lights
                if (r.ReadByte() == 1)
                {
                    int w = r.ReadInt32();
                    int h = r.ReadInt32();
                    int d = r.ReadInt32();

                    int size = w * h * d;

                    room.volume_lights = size != 0 ? ReadCompressedBytes(r, size) : null;

                    room.volume_width = w;
                    room.volume_height = h;
                    room.volume_depth = d;
                }
            }

            if (version >= 73)
            {
                // Read fog stuff
                room.fog_depth = r.ReadSingle();
                room.fog_r = r.ReadSingle();
                room.fog_g = r.ReadSingle();
                room.fog_b = r.ReadSingle();
            }

            //Read ambient sound pattern name
            if (version >= 78)
            {
                var sound_name = r.ReadCString();
                room.ambient_sound = -1;
                //FindAmbientSoundPattern(tbuf);
            }
            else
                room.ambient_sound = -1;

            //  read reverb value for room.
            room.env_reverb = (version >= 98) ? r.ReadByte() : (byte)0;

            //Read damage
            if (version >= 108)
            {
                room.damage = r.ReadSingle();
                room.damage_type = r.ReadByte();
            }

            return room;
        }

        private static Texture[] ReadTextureList(BinaryReader r)
        {
            int n = r.ReadInt32();
            var texs = new Texture[n];
            for (int i = 0; i < n; i++)
                texs[i].n = r.ReadCString();
            Debug.WriteLine("ReadTextureList " + n);
            return texs;
        }

        /*
        class TextureSet
        {
            public List<string> names;
            public Dictionary<string, int> index;
        }
        */

        public Room[] rooms;
        public Texture[] texture_xlate;

        public static Level Read(BinaryReader r)
        {
            if (r.ReadUInt32() != LEVEL_FILE_TAG)
                return null;
            var version = r.ReadInt32();
            if (version > LEVEL_FILE_VERSION)
                return null;
            if (version < LEVEL_FILE_OLDEST_COMPATIBLE_VERSION)
                return null;
            var len = r.BaseStream.Length;
            long pos;
            Texture[] texture_xlate = null;
            Room[] rooms = null;
            while ((pos = r.BaseStream.Position) < len)
            {
                var chunk_tag = r.ReadUInt32();
                var chunk_start = pos + 4;
                var chunk_size = r.ReadInt32();
                if (chunk_tag == CHUNK_TEXTURE_NAMES) {
                    texture_xlate = ReadTextureList(r);
                } else if (chunk_tag == CHUNK_ROOMS) {
                    int num_rooms = r.ReadInt32();
                    if (version >= 85)
                    {
                        int nverts = r.ReadInt32();
                        int nfaces = r.ReadInt32();
                        int nfaceverts = r.ReadInt32();
                        int nportals = r.ReadInt32();
                    }
                    //Console.WriteLine("Reading " + num_rooms + " rooms");
                    rooms = new Room[num_rooms];
                    for (int i = 0; i < num_rooms; i++)
                    {
                        int roomnum = (version >= 96) ? r.ReadInt16() : i;
                        //Console.WriteLine("Reading room " + roomnum);
                        rooms[roomnum] = ReadRoom(r, version, texture_xlate);
                    }
                }
                r.BaseStream.Position = chunk_start + chunk_size;
            }
            var level = new Level() { rooms = rooms, texture_xlate = texture_xlate };
            return level;
        }

        public static Level Read(string filename)
        {
            using (var f = File.OpenRead(filename))
                return Read(new BinaryReader(f));
        }
    }

    public class TextureInfo
    {
        public const int TF_VOLATILE = 1;
        public const int TF_WATER = (1 << 1);
        public const int TF_METAL = (1 << 2);
        public const int TF_MARBLE = (1 << 3);
        public const int TF_PLASTIC = (1 << 4);
        public const int TF_FORCEFIELD = (1 << 5);
        public const int TF_ANIMATED = (1 << 6);
        public const int TF_DESTROYABLE = (1 << 7);
        public const int TF_EFFECT = (1 << 8);
        public const int TF_HUD_COCKPIT = (1 << 9);
        public const int TF_MINE = (1 << 10);
        public const int TF_TERRAIN = (1 << 11);
        public const int TF_OBJECT = (1 << 12);
        public const int TF_TEXTURE_64 = (1 << 13);
        public const int TF_TMAP2 = (1 << 14);
        public const int TF_TEXTURE_32 = (1 << 15);
        public const int TF_FLY_THRU = (1 << 16);
        public const int TF_PASS_THRU = (1 << 17);
        public const int TF_PING_PONG = (1 << 18);
        public const int TF_LIGHT = (1 << 19);
        public const int TF_BREAKABLE = (1 << 20);
        public const int TF_SATURATE = (1 << 21);
        public const int TF_ALPHA = (1 << 22);
        public const int TF_DONTUSE = (1 << 23);
        public const int TF_PROCEDURAL = (1 << 24);
        public const int TF_WATER_PROCEDURAL = (1 << 25);
        public const int TF_FORCE_LIGHTMAP = (1 << 26);
        public const int TF_SATURATE_LIGHTMAP = (1 << 27);
        public const int TF_TEXTURE_256 = (1 << 28);
        public const int TF_LAVA = (1 << 29);
        public const int TF_RUBBLE = (1 << 30);
        public const int TF_SMOOTH_SPECULAR = (1 << 31);

        public string name;
        public string filename;
        public float r, g, b, alpha;
        public float speed, slide_u, slide_v, reflectivity;
        public int flags;
    }

    public class TableData
    {
        public const int PAGENAME_LEN = 35;
        public const int PAGETYPE_TEXTURE = 1;
        public const int PAGETYPE_DOOR = 5;
        public const int PAGETYPE_SOUND = 7;
        public const int PAGETYPE_GENERIC = 10;

        private const int KNOWN_TEXTURE_VERSION = 7;

        private static TextureInfo ReadTexturePage(BinaryReader r)
        {
            var version = r.ReadInt16();
            if (version > KNOWN_TEXTURE_VERSION)
                throw new Exception("Unsupported texture version");
            var tex = new TextureInfo();
            tex.name = r.ReadCString();
            tex.filename = r.ReadCString();
            r.ReadCString();
            tex.r = r.ReadSingle();
            tex.g = r.ReadSingle();
            tex.b = r.ReadSingle();
            tex.alpha = r.ReadSingle();

            tex.speed = r.ReadSingle();
            tex.slide_u = r.ReadSingle();
            tex.slide_v = r.ReadSingle();
            tex.reflectivity = r.ReadSingle();

            r.ReadByte();
            r.ReadInt32();

            tex.flags = r.ReadInt32();
            if ((tex.flags & TextureInfo.TF_PROCEDURAL) != 0)
            {
                for (int i = 0; i < 255; i++)
                    r.ReadInt16();
                r.ReadByte();
                r.ReadByte();
                r.ReadByte();
                r.ReadSingle();
                if (version >= 6)
                {
                    r.ReadSingle();
                    r.ReadByte();
                }
                int n = r.ReadInt16();
                for (int i = 0; i < n; i++)
                {
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                    r.ReadByte();
                }
            }

            if (version >= 5)
            {
                if (version < 7)
                    r.ReadInt16();
                else
                    r.ReadCString();
                r.ReadSingle();
            }
            return tex;
        }
        
        public List<TextureInfo> textures = new List<TextureInfo>();

        public static TableData Read(Stream stream)
        {
            var r = new BinaryReader(stream);
            var f = new TableData();
            var len = r.BaseStream.Length;
            long pos;
            while ((pos = r.BaseStream.Position) < len)
            {
                var pagetype = r.ReadByte();
                var length = r.ReadInt32();
                var next_chunk = pos + 1 + length;
                switch (pagetype)
                {
                    case PAGETYPE_TEXTURE:
                        f.textures.Add(ReadTexturePage(r));
                        break;
                    default:
                        r.BaseStream.Position = next_chunk;
                        break;
                }
            }
            return f;
        }

        public static TableData Read(string filename)
        {
            using (var stream = File.OpenRead(filename))
                return Read(stream);
        }
    }
}
