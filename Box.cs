using System;
using System.Runtime.InteropServices;

namespace MemFile
{
    public class Box<T> where T : struct
    {
        public ulong Ptr { get; }
        public T Data { get; }

        public Box(ulong ptr)
        {
            Ptr = ptr;
            Data = Ptr.ToStructure<T>();
        }

        public void Write() => Ptr.Write<T>(Data);
    }
}