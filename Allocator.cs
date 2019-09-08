using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MemFile
{
    public class Allocator
    {
        protected static bool Initialized = false;
        public static string WorkDir;
        private static Access Access;

        private static uint[] SizeGroup = {
            1 << 4,
            1 << 5,
            1 << 6,
            1 << 7,
            1 << 8,
            1 << 10,
            1 << 20,
            uint.MaxValue
        };

        public static void Bootstrap(string workDir)
        {
            if(!Directory.Exists(workDir)) new DirectoryNotFoundException(workDir);
            WorkDir = workDir;

            Segment CreateSegment()
            {
                long count = Access?.GetSegmentCount() ?? 0;
                string file = $"{workDir}/{count}";
                using(var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
                {
                    stream.Position = uint.MaxValue;
                    stream.WriteByte(0x00);
                }
                return new Segment(file);
            }

            void SegmentInit(Segment segment)
            {
                var zero = new byte[SizeGroup.Length * 4];
                segment.Write(0, zero);
                var free_ptr = (uint)(SizeGroup.Length * 4);
                var free_size = (uint)(segment.Size - zero.Length);
                uint group = SizeToGroup(free_size);
                segment.Write(group * 4, BitConverter.GetBytes(free_ptr));
                var buf = new byte[8];
                Array.Copy(BitConverter.GetBytes(free_size), 0, buf, 4, 4);
                segment.Write(free_ptr, buf);
            }

            void RequestHandler(Access access)
            {
                var segment = CreateSegment();
                SegmentInit(segment);
                access.AddSegment(segment);
            }

            if (Initialized) throw new InvalidOperationException("already initialized");
            Initialized = true;
            var seg = CreateSegment();
            SegmentInit(seg);
            Access = new Access(seg, RequestHandler);
        }

        public static Access GetAccess()
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            return Access;
        }

        private static uint SizeToGroup(uint size)
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            for (uint i = 0; i < SizeGroup.Length; ++i)
            {
                if (size <= SizeGroup[i])
                {
                    return i;
                }
            }
            return SizeGroup[SizeGroup.Length - 1];//
        }

        public static ulong Allocate(uint size)
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            if (size < 8) throw new ArgumentException("size < 8");

            uint group = SizeToGroup(size);
            ulong segment = 0;
            while (true)
            {
                ulong segment_mask = segment << 32;
                for (uint i = group; i < SizeGroup.Length; ++i)
                {
                    uint top_ptr = BitConverter.ToUInt32(Access.Read(segment_mask | (i * 4), 4));
                    if(top_ptr == 0) continue;
                    uint free_size = BitConverter.ToUInt32(Access.Read((segment_mask | top_ptr) + 4, 4));
                    if(free_size - size < 8 && free_size - size > 0) continue;
                    uint next_ptr = BitConverter.ToUInt32(Access.Read(segment_mask | top_ptr, 4));
                    Access.Write(segment_mask | (i * 4), BitConverter.GetBytes(next_ptr));
                    if(next_ptr != 0)
                    {
                        Access.Write(segment_mask | next_ptr, new byte[4]);
                    }
                    if(free_size - size > 0)
                    {
                        Free((segment_mask | top_ptr) + size, free_size - size);
                    }
                    return segment_mask | top_ptr;
                }
                ++segment;
            }
        }

        public static Box<T> Allocate<T>() where T : struct
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            uint size = (uint)Marshal.SizeOf<T>();
            return Allocate(Math.Max(size, 8)).Wrap<T>();
        }

        public static Box<T> AllocateArray<T>(uint length) where T : struct
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            uint size = (uint)Marshal.SizeOf<T>();
            return Allocate(Math.Max(size * length, 8)).Wrap<T>();
        }

        public static void Free(ulong ptr, uint size)
        {
            if (!Initialized) throw new InvalidOperationException("not initialized");
            if (size < 8) throw new ArgumentException("size < 8");

            ulong segment_mask = ptr & ((~0UL) << 32);

            var buf = new byte[8];
            Array.Copy(BitConverter.GetBytes(size), 0, buf, 4, 4);
            Access.Write(ptr, buf);

            uint group = SizeToGroup(size);
            uint top_ptr = BitConverter.ToUInt32(Access.Read(segment_mask | (group * 4), 4));
            Access.Write(segment_mask | (group * 4), BitConverter.GetBytes((uint)ptr));
            if (top_ptr != 0)
            {
                Access.Write(segment_mask | top_ptr, BitConverter.GetBytes((uint)ptr));
            }
        }

        public static void Free<T>(ulong ptr) where T : struct => Free(ptr, Math.Max((uint)Marshal.SizeOf<T>(), 8));

        public static void Free<T>(Box<T> boxed) where T : struct => Free<T>(boxed.Ptr);

        public static void FreeArray<T>(ulong ptr, uint length) where T : struct => Free(ptr, Math.Max((uint)Marshal.SizeOf<T>() * length, 8));

        public static void FreeArray<T>(Box<T> boxed, uint length) where T : struct => FreeArray<T>(boxed.Ptr, length);
    }
}