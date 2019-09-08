using System;
using System.Runtime.InteropServices;

namespace MemFile
{
    public static class PtrMethods
    {
        public static uint GetSegment(this ulong ptr) => (uint)(ptr >> 32);
        
        public static void Write<T>(this ulong ptr, T data) where T : struct
        {
            int size = Marshal.SizeOf(data);
            IntPtr ptr_ = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr_, true);
            var bytes = new byte[size];
            Marshal.Copy(ptr_, bytes, 0, size);
            Marshal.FreeHGlobal(ptr_);
            Allocator.GetAccess().Write(ptr, bytes);
        }

        public static T ToStructure<T>(this ulong ptr) where T : struct
        {
            T Data = default;
            int size = Marshal.SizeOf(Data);
            var bytes = Allocator.GetAccess().Read(ptr, (uint)size);
            IntPtr ptr_ = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, 0, ptr_, size);
            Data = Marshal.PtrToStructure<T>(ptr_);
            Marshal.FreeHGlobal(ptr_);
            return Data;
        }

        public static Box<T> Wrap<T>(this ulong ptr) where T : struct => new Box<T>(ptr);
    }
}