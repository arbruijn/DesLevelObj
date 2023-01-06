using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Classic
{
    static class ClassicLevelExt
    {
        public static void Read(this Segment[] v, BinaryReader r)
        {
            for (int i = 0, l = v.Length; i < l; i++)
                (v[i] = new Segment()).Read(r);
        }
    }

    struct game_top_fileinfo
    {
        public ushort fileinfo_signature, fileinfo_version;
        public int fileinfo_sizeof;

        public void Read(BinaryReader r)
        {
            r.Read(out fileinfo_signature);
            r.Read(out fileinfo_version);
            r.Read(out fileinfo_sizeof);
        }
    }

    struct game_fileinfo {
        public ushort fileinfo_signature;
        public ushort fileinfo_version;
        public int fileinfo_sizeof;
        public string mine_filename;
        public int level;
        public int player_offset;
        public int player_sizeof;
        public int object_offset;
        public int object_howmany;
        public int object_sizeof;
        public int walls_offset;
        public int walls_howmany;
        public int walls_sizeof;
        public int doors_offset;
        public int doors_howmany;
        public int doors_sizeof;
        public int triggers_offset;
        public int triggers_howmany;
        public int triggers_sizeof;
        public int links_offset;
        public int links_howmany;
        public int links_sizeof;
        public int control_offset;
        public int control_howmany;
        public int control_sizeof;
        public int matcen_offset;
        public int matcen_howmany;
        public int matcen_sizeof;

        public void Read(BinaryReader r)
        {
            r.Read(out fileinfo_signature);
            r.Read(out fileinfo_version);
            r.Read(out fileinfo_sizeof);
            mine_filename = UTF8Encoding.UTF8.GetString(r.ReadBytes(15));
            r.Read(out level);
            r.Read(out level);
            r.Read(out player_offset);
            r.Read(out player_sizeof);
            r.Read(out object_offset);
            r.Read(out object_howmany);
            r.Read(out object_sizeof);
            r.Read(out walls_offset);
            r.Read(out walls_howmany);
            r.Read(out walls_sizeof);
            r.Read(out doors_offset);
            r.Read(out doors_howmany);
            r.Read(out doors_sizeof);
            r.Read(out triggers_offset);
            r.Read(out triggers_howmany);
            r.Read(out triggers_sizeof);
            r.Read(out links_offset);
            r.Read(out links_howmany);
            r.Read(out links_sizeof);
            r.Read(out control_offset);
            r.Read(out control_howmany);
            r.Read(out control_sizeof);
            r.Read(out matcen_offset);
            r.Read(out matcen_howmany);
            r.Read(out matcen_sizeof);
        }
    }

    public class ClassicObject
    {
        public void Read(BinaryReader r)
        {
        }
    }

    public struct Side
    {
        public short wall_num;
        public short tmap_num;
        public ushort tmap_num2;
        public g3s_uvl[] uvls;
    }

    public class Segment
    {
        public short[] children; // left, top, right, bottom, back, front
        public short[] verts;
        public byte[] wallIds;
        public byte special;
        public sbyte matcen_num;
        public short value;
        public Fix static_light;
        public Side[] sides;
        public const int NUM_SIDES = 6;
        public static readonly int[,] Side_to_verts = new int[NUM_SIDES,4] {
                        {7,6,2,3},                       // left
                        {0,4,7,3},                       // top
                        {0,1,5,4},                       // right
                        {2,6,5,1},                       // bottom
                        {4,5,6,7},                       // back
                        {3,2,1,0}};                      // front

        private short Conv255(byte b)
        {
            return b == 255 ? (short)-1 : (short)b;
        }

        public bool HasSide(int sidenum)
        {
            return children[sidenum] == -1 || sides[sidenum].wall_num != -1;
        }

        public void Read(BinaryReader r)
        {
            children = new short[6];
            byte mask = r.ReadByte();
            for (int i = 0; i < 6; i++)
                children[i] = (mask & (1 << i)) != 0 ? r.ReadInt16() : (short)-1;
            (verts = new short[8]).Read(r);
            if ((mask & 64) != 0)
            {
                r.Read(out special);
                r.Read(out matcen_num);
                r.Read(out value);
            }
            else
            {
                special = 0;
                matcen_num = -1;
                value = 0;
            }
            static_light.n = r.ReadUInt16() << 4;
            byte wall_mask = r.ReadByte();
            sides = new Side[NUM_SIDES];
            for (int sidenum = 0; sidenum < NUM_SIDES; sidenum++)
                sides[sidenum].wall_num = (wall_mask & (1 << sidenum)) != 0 ? Conv255(r.ReadByte()) : (short)-1;
            for (int sidenum = 0; sidenum < NUM_SIDES; sidenum++)
            {
                if (!HasSide(sidenum))
                    continue;
                int tmap = r.ReadUInt16();
                sides[sidenum].tmap_num = (short)(tmap & 0x7fff);
                sides[sidenum].tmap_num2 = (tmap & 0x8000) != 0 ? r.ReadUInt16() : (ushort)0;
                sides[sidenum].uvls = new g3s_uvl[4];
                for (int i = 0; i < 4; i++)
                {
                    sides[sidenum].uvls[i].u.n = r.ReadInt16() << 5;
                    sides[sidenum].uvls[i].v.n = r.ReadInt16() << 5;
                    sides[sidenum].uvls[i].l.n = r.ReadUInt16() << 1;
                }
                #if false
                {
                    sides[sidenum].tmap_num = sides[sidenum].tmap_num2 = 0;
                    for (int i = 0; i < 4; i++)
                        sides[sidenum].uvls[i].u.n = sides[sidenum].uvls[i].v.n = sides[sidenum].uvls[i].l.n = 0;
                }
                #endif
            }
        }
        public override string ToString()
        {
            return String.Format("v: {0} w: {1} l: {2} {3}",
                string.Join(",", Array.ConvertAll(verts, x => x.ToString())),
                string.Join(",", Array.ConvertAll(wallIds, x => x.ToString())),
                static_light,
                string.Join(",", Array.ConvertAll(sides, x => x.ToString())));
        }
    }

    public class ClassicLevelGame
    {
        game_fileinfo game_fileinfo;
        string level_name;
        ushort N_save_pof_names;
        string[] Save_pof_names;
        ClassicObject[] Objects;

        public void Read(BinaryReader r)
        {
            int i;
            game_fileinfo.Read(r);
            if (game_fileinfo.fileinfo_signature != 0x6705)
                throw new Exception("Invalid level file signature");
            if (game_fileinfo.fileinfo_version >= 14)
                level_name = r.ReadCString();
            if (game_fileinfo.fileinfo_version >= 19)
            {
                r.Read(out N_save_pof_names);
                Save_pof_names = new string[N_save_pof_names];
                for (i = 0; i < N_save_pof_names; i++)
                {
                    byte[] buf = new byte[13];
                    buf.Read(r);
                    Save_pof_names[i] = Encoding.UTF8.GetString(buf);
                }
            }
            if (game_fileinfo.object_offset > -1)
            {
                r.BaseStream.Position = game_fileinfo.object_offset;
                Objects = new ClassicObject[game_fileinfo.object_howmany];
                for (i = 0; i < game_fileinfo.object_howmany; i++)
                    Objects[i].Read(r);
            }
        }
    }

    public struct Mine
    {
        public vms_vector[] Vertices;
        public Segment[] Segments;

        public void Read(BinaryReader r)
        {
            byte version = r.ReadByte();
            if (version != 0)
                throw new Exception("wrong minedata version");
            int Num_vertices = r.ReadInt16();
            int Num_segments = r.ReadInt16();
            (Vertices = new vms_vector[Num_vertices]).Read(r);
            (Segments = new Segment[Num_segments]).Read(r);
        }        
    }

    public class ClassicLevel
    {
        public Mine mine;

        public void Read(BinaryReader r)
        {
            if (r.ReadInt32() != 0x504c564c)
                throw new Exception("wrong level signature");
            if (r.ReadInt32() != 1)
                throw new Exception("wrong level version");
            int minedata_ofs = r.ReadInt32();
            int gamedata_ofs = r.ReadInt32();
            int hostagetext_ofs = r.ReadInt32();
            r.BaseStream.Position = minedata_ofs;
            mine.Read(r);
        }
    }
}
