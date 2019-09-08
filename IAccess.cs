using System;

namespace MemFile
{
    public interface IAccess
    {
        void Write(ulong ptr, byte[] data);
        byte[] Read(ulong ptr, uint size);
    }
}