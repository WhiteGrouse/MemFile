using System;
using System.IO;
using System.Text;

namespace MemFile
{
    class SegmentTable
    {
        private string SegmentTableFile { get; }
        public uint Count { get; private set; }

        public SegmentTable(string tableFile)
        {
            SegmentTableFile = tableFile;
            Count = 0;//
        }

        public uint Add(Segment segment)
        {
            using (var stream = new FileStream(SegmentTableFile, FileMode.Open, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                stream.Position = 256 * Count;
                var bytes = Encoding.ASCII.GetBytes(segment.MemoryFile);
                writer.Write(bytes);
                writer.Write(new byte[256 - bytes.Length]);
                return Count++;
            }
        }

        private string GetFilePath(uint segment)
        {
            using (var stream = new FileStream(SegmentTableFile, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                stream.Position = 256 * segment;
                var bytes = reader.ReadBytes(256);
                int length = 0;
                for (; length < bytes.Length && bytes[length] != 0x00; ++length) ;
                return Encoding.ASCII.GetString(bytes).Substring(0, length);
            }
        }

        public Segment GetSegment(uint segment) => new Segment(GetFilePath(segment));
    }
}