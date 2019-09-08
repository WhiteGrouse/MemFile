using System;

namespace MemFile
{
    public delegate void SegmentRequestHandler(Access access);

    public class Access : IAccess
    {
        private SegmentTable Table { get; }
        private SegmentRequestHandler RequestHandler { get; }

        public Access(Segment defaultSegment, SegmentRequestHandler requestHandler)
        {
            Table = new SegmentTable(System.IO.Path.GetTempFileName());
            Table.Add(defaultSegment);
            RequestHandler = requestHandler;
        }

        public Access(Segment defaultSegment) : this(defaultSegment, null) { }

        public void AddSegment(Segment segment) => Table.Add(segment);
        public long GetSegmentCount() => Table.Count;

        public byte[] Read(ulong ptr, uint size)
        {
            var segment = ptr.GetSegment();
            while (segment >= Table.Count)
            {
                uint prev = Table.Count;
                RequestHandler?.Invoke(this);
                if(RequestHandler == null || prev == Table.Count)
                {
                    throw new OutOfMemoryException();
                }
            }
            return Table.GetSegment(segment).Read((uint)ptr, size);
        }

        public void Write(ulong ptr, byte[] data)
        {
            var segment = ptr.GetSegment();
            while (segment >= Table.Count)
            {
                uint prev = Table.Count;
                RequestHandler?.Invoke(this);
                if(RequestHandler == null || prev == Table.Count)
                {
                    throw new OutOfMemoryException();
                }
            }
            Table.GetSegment(segment).Write((uint)ptr, data);
        }
    }
}