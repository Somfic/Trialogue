using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Trialogue.Importer
{
    public static class BinaryExtensions
    {
        public static unsafe T ReadEnum<T>(this BinaryReader reader)
        {
            var i32 = reader.ReadInt32();
            return Unsafe.Read<T>(&i32);
        }

        public static void WriteEnum<T>(this BinaryWriter writer, T value)
        {
            var i32 = Convert.ToInt32(value);
            writer.Write(i32);
        }

        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            var byteCount = reader.ReadInt32();
            return reader.ReadBytes(byteCount);
        }

        public static void WriteByteArray(this BinaryWriter writer, byte[] array)
        {
            writer.Write(array.Length);
            writer.Write(array);
        }

        public static void WriteObjectArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeFunc)
        {
            writer.Write(array.Length);
            foreach (var item in array)
            {
                writeFunc(writer, item);
            }
        }

        public static T[] ReadObjectArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
        {
            var length = reader.ReadInt32();
            var ret = new T[length];
            for (var i = 0; i < length; i++)
            {
                ret[i] = readFunc(reader);
            }

            return ret;
        }

        public static unsafe void WriteBlittableArray<T>(this BinaryWriter writer, T[] array)
        {
            var sizeofT = Unsafe.SizeOf<T>();
            var totalBytes = array.Length * sizeofT;

            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var ptr = (byte*)handle.AddrOfPinnedObject();

            writer.Write(array.Length);
            for (var i = 0; i < totalBytes; i++)
            {
                writer.Write(ptr[i]);
            }

            handle.Free();
        }

        public static unsafe T[] ReadBlittableArray<T>(this BinaryReader reader)
        {
            var sizeofT = Unsafe.SizeOf<T>();
            var length = reader.ReadInt32();
            var ret = new T[length];
            var handle = GCHandle.Alloc(ret, GCHandleType.Pinned);

            var totalBytes = length * sizeofT;
            var ptr = (byte*)handle.AddrOfPinnedObject();
            for (var i = 0; i < totalBytes; i++)
            {
                ptr[i] = reader.ReadByte();
            }

            handle.Free();

            return ret;
        }
    }
}
