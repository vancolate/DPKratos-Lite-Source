﻿using Extension.Ext;
using PatcherYRpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Extension.Utilities
{
    public static class ContainerHelper
    {
        public static uint Write(this IStream stream, byte[] buffer)
        {
            uint written = 0;
            stream.Write(buffer, buffer.Length, Pointer<uint>.AsPointer(ref written));
            return written;
        }
        public static uint Write<T>(this IStream stream, T obj)
        {
            var ptr = Pointer<T>.AsPointer(ref obj);
            byte[] buffer = new byte[Pointer<T>.TypeSize()];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            return stream.Write(buffer);
        }
        public static uint WriteObject(this IStream stream, object obj)
        {
            uint written = 0;

            bool isNull = obj == null;
            written += stream.Write(isNull);

            if (isNull == false)
            {
                MemoryStream memory = new MemoryStream();
                Serialization.Serialize(memory, obj);

                byte[] buffer = memory.ToArray();
                written += stream.Write(buffer.Length);
                written += stream.Write(buffer);
            }

            return written;
        }

        public static uint Read(this IStream stream, byte[] buffer)
        {
            uint written = 0;
            stream.Read(buffer, buffer.Length, Pointer<uint>.AsPointer(ref written));
            return written;
        }
        public static uint Read<T>(this IStream stream, ref T obj)
        {
            var ptr = Pointer<T>.AsPointer(ref obj);
            byte[] buffer = new byte[Pointer<T>.TypeSize()];
            uint written = stream.Read(buffer);
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            return written;
        }
        public static uint ReadObject<T>(this IStream stream, out T obj) where T : class
        {
            uint written = 0;

            bool isNull = false;
            written += stream.Read(ref isNull);

            if (isNull)
            {
                obj = null;
            }
            else
            {
                int length = 0;
                written += stream.Read(ref length);
                byte[] buffer = new byte[length];
                written += stream.Read(buffer);

                MemoryStream memory = new MemoryStream(buffer);
                obj = Serialization.Deserialize<T>(memory);
            }

            return written;
        }

        public static void Swizzle<T>(this SwizzleManagerClass @this, ref T obj)
        {
            SwizzleManagerClass.Instance.Swizzle(Pointer<T>.AsPointer(ref obj).Convert<IntPtr>());
        }
        public static void Here_I_Am<T>(this SwizzleManagerClass @this, Pointer<T> oldPtr, ref T obj)
        {
            SwizzleManagerClass.Instance.Here_I_Am((int)oldPtr, Pointer<T>.AsPointer(ref obj).Convert<IntPtr>());
        }

        public static void Swizzle<TBase, T>(this Extension<TBase> ext, ref T obj)
        {
            SwizzleManagerClass.Instance.Swizzle(ref obj);
        }
        public static void Here_I_Am<TBase, T>(this Extension<TBase> ext, Pointer<T> oldPtr, ref T obj)
        {
            SwizzleManagerClass.Instance.Here_I_Am(oldPtr, ref obj);
        }
        public static void Here_I_Am<TBase, T>(this Extension<TBase> ext, IStream stream, ref T obj)
        {
            Pointer<T> oldPtr = Pointer<T>.Zero;
            stream.Read(ref oldPtr);
            ext.Here_I_Am(oldPtr, ref obj);
        }

        public static void Save<TBase, T>(this Extension<TBase> ext, IStream stream, PointerHandle<T> ptr)
        {
            stream.Write(ptr.Pointer);
        }
        public static void Load<TBase, T>(this Extension<TBase> ext, IStream stream, ref PointerHandle<T> ptr)
        {
            if (ptr == null)
            {
                ptr = new PointerHandle<T>();
            }
            stream.Read(ref ptr.Pointer);
            ext.Swizzle(ref ptr.Pointer);
        }


        public static IContainer GetContainer<TExt>()
        {
            Type type = typeof(TExt);
            FieldInfo member = type.GetField("ExtMap");
            return (IContainer)member.GetValue(null);
        }
    }
}