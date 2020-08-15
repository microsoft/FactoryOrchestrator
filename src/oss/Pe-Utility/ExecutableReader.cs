// From https://github.com/AndresTraks/pe-utility with minimal changes for simplification and compatibility with .NET Standard
// Copyright (c) 2014-2016 Andres Traks
// Licensed under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace PEUtility
{
    public sealed class ExecutableReader : IDisposable
    {
        private readonly string _fileName;
        private MemoryMappedFile _file;

        public ExecutableReader(string fileName)
        {
            _fileName = fileName;
        }

        private MemoryMappedFile File
        {
            get
            {
                if (_file == null)
                {
                    _file = MemoryMappedFile.CreateFromFile(_fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                }
                return _file;
            }
        }

        public MemoryMappedViewAccessor GetAccessor(long offset, long size)
        {
            return File.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read);
        }

        public T ReadStruct<T>(long offset) where T : struct
        {
            T structure;
            using (var accessor = GetAccessor(offset, Marshal.SizeOf(typeof(T))))
            {
                accessor.Read(0, out structure);
            }
            return structure;
        }

        public T[] ReadStructArray<T>(long offset, int count) where T : struct
        {
            T[] structArray = new T[count];
            using (var accessor = GetAccessor(offset, count * Marshal.SizeOf(typeof(T))))
            {
                accessor.ReadArray(0, structArray, 0, count);
            }
            return structArray;
        }

        public ushort ReadUInt16(long address)
        {
            using (var accessor = GetAccessor(address, sizeof(short)))
            {
                return accessor.ReadUInt16(0);
            }
        }

        public uint ReadUInt32(long address)
        {
            using (var accessor = GetAccessor(address, sizeof(uint)))
            {
                return accessor.ReadUInt32(0);
            }
        }

        public ulong ReadUInt64(long address)
        {
            using (var accessor = GetAccessor(address, sizeof(ulong)))
            {
                return accessor.ReadUInt64(0);
            }
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            if (_file != null)
            {
                _file.Dispose();
                _file = null;
            }
        }
    }
}
