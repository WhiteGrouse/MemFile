using System;
using System.IO;

namespace MemFile
{
    public class Segment
    {
        public long Size { get; }
        public string MemoryFile { get; }

        public Segment(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException("filename");
            if (!File.Exists(filePath)) throw new FileNotFoundException($"memory file not found ({filePath})");

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 1024) throw new IOException("not enough segment size(<1KB)");
            if (fileInfo.Length > (uint.MaxValue + 1L)) throw new IOException("too large segment size(>4GB)");
            Size = fileInfo.Length;

            MemoryFile = filePath;
        }

        public void Write(uint ptr, byte[] data)
        {
            if ((ptr + data.Length) >= Size) throw new OverflowException();
            using (var stream = new FileStream(MemoryFile, FileMode.Open, FileAccess.Write))
            {
                stream.Position = ptr;
                stream.Write(data, 0, data.Length);
            }
        }

        public byte[] Read(uint ptr, uint size)
        {
            if ((ptr + size) >= Size) throw new OverflowException();
            var buffer = new byte[size];
            using (var stream = new FileStream(MemoryFile, FileMode.Open, FileAccess.Read))
            {
                stream.Position = ptr;
                stream.Read(buffer, 0, buffer.Length);//
            }
            return buffer;
        }
    }
}