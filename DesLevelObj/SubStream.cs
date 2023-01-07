using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace D3Level
{
    class SubStreamRead : Stream
    {
        private Stream baseStream;
        private long baseOffset;
        private long size;
        private long pos;

        public SubStreamRead(Stream baseStream, long baseOffset, long size)
        {
            this.baseStream = baseStream;
            this.baseOffset = baseOffset;
            this.size = size;
            this.pos = 0;
        }

        public override long Seek(long ofs, SeekOrigin org = SeekOrigin.Begin)
        {
            switch (org)
            {
                case SeekOrigin.Begin:
                    pos = ofs;
                    break;
                case SeekOrigin.Current:
                    pos += ofs;
                    break;
                case SeekOrigin.End:
                    pos = size;
                    break;
            }
            return pos;
        }

        public override int Read(byte[] buf, int bufOfs, int count)
        {
            if (pos >= size)
                return 0;
            if (count > size - pos)
                count = (int)(size - pos);
            baseStream.Position = pos + baseOffset;
            int n = baseStream.Read(buf, bufOfs, count);
            pos += n;
            return n;
        }
        public override long Length { get { return size; } }
        public override long Position { get { return pos; } set { Seek(value); } }
        public override bool CanSeek { get { return baseStream.CanSeek; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanRead { get { return baseStream.CanRead; } }
        public override void Flush() { throw new NotImplementedException(); }
        public override void SetLength(long len) { throw new NotImplementedException(); }
        public override void Write(byte[] buf, int bufOfs, int count) { throw new NotImplementedException(); }
    }
}
