// From https://github.com/AndresTraks/pe-utility with minimal changes for simplification and compatibility with .NET Standard
// Copyright(c) 2014-2016 Andres Traks
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace PEUtility
{
    public class ImportEntry
    {
        public string Name { get; set; }
        public List<string> Entries { get; set; }

        public ImportEntry(string name)
        {
            Name = name;
            Entries = new List<string>();
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class Executable : IDisposable
    {
        public ImageNtHeaders32 NtHeaders32Bit { get; private set; }
        public ImageNtHeaders64 NtHeaders64Bit { get; private set; }
        public ImageCor20Header ClrHeader { get; private set; }

        public bool IsValid { get; }
        public string Filename { get; }
        public ExecutableReader Reader { get; }
        public ImageSectionHeader[] Sections { get; private set; }

        public string Type
        {
            get
            {
                if ((ClrHeader.Flags & ComImageFlags.ILOnly) != 0)
                {
                    return "Any CPU";
                }
                return Is64Bit ? "64-bit" : "32-bit";
            }
        }

        public Subsystem Subsystem
        {
            get
            {
                if (Is64Bit && NtHeaders64Bit.IsValid)
                {
                    return NtHeaders64Bit.OptionalHeader.Subsystem;
                }
                else if (NtHeaders32Bit.IsValid)
                {
                    return NtHeaders32Bit.OptionalHeader.Subsystem;
                }
                else
                {
                    return Subsystem.Unknown;
                }
            }
        }

        public bool Is64Bit => NtHeaders32Bit.Is64Bit;

        public MemoryMappedViewAccessor GetSectionAccessor(int section)
        {
            return Reader.GetAccessor(Sections[section].PointerToRawData, Sections[section].SizeOfRawData);
        }

        public string ReadStringFromFile(UnmanagedMemoryAccessor accessor, long position)
        {
            var bytes = new List<char>();
            while (true)
            {
                var b = accessor.ReadByte(position);
                if (b == 0)
                {
                    break;
                }
                bytes.Add((char)b);
                position++;
            }
            return new string(bytes.ToArray());
        }

        public Executable(string fileName)
        {
            Filename = fileName;
            Reader = new ExecutableReader(fileName);

            var header = Reader.ReadStruct<ImageDosHeader>(0);
            if (!header.IsValid)
            {
                throw new InvalidDataException("Invalid PE header for " + fileName);
            }

            NtHeaders32Bit = Reader.ReadStruct<ImageNtHeaders32>(header.LfaNewHeader);
            if (!NtHeaders32Bit.IsValid)
            {
                throw new InvalidDataException("Invalid PE header for " + fileName);
            }

            if (NtHeaders32Bit.Is64Bit)
            {
                NtHeaders64Bit = Reader.ReadStruct<ImageNtHeaders64>(header.LfaNewHeader);
            }

            // Read sections
            if (Is64Bit)
            {
                Sections = Reader.ReadStructArray<ImageSectionHeader>(
                    header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders64)),
                    NtHeaders64Bit.FileHeader.NumberOfSections);
            }
            else
            {
                Sections = Reader.ReadStructArray<ImageSectionHeader>(
                    header.LfaNewHeader + Marshal.SizeOf(typeof(ImageNtHeaders32)),
                    NtHeaders32Bit.FileHeader.NumberOfSections);
            }

            ReadCorHeader();

            Reader.Close();
            IsValid = true;
        }

        private void ReadCorHeader()
        {
            long clrRuntimeHeader;
            if (Is64Bit)
            {
                clrRuntimeHeader = NtHeaders64Bit.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
            }
            else
            {
                clrRuntimeHeader = NtHeaders32Bit.OptionalHeader.CLRRuntimeHeader.VirtualAddress;
            }
            if (clrRuntimeHeader == 0)
            {
                return;
            }

            clrRuntimeHeader = DecodeRva(clrRuntimeHeader);
            ClrHeader = Reader.ReadStruct<ImageCor20Header>(clrRuntimeHeader);
        }

        public int GetRvaSection(long rva)
        {
            for (int i = 0; i < Sections.Length; i++)
            {
                if (rva >= Sections[i].VirtualAddress &&
                    rva < Sections[i].VirtualAddress + Sections[i].SizeOfRawData)
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException($"Unknown relative virtual address: {rva}");
        }

        // Decode Relative Virtual Address
        public long DecodeRva(long rva)
        {
            int section = GetRvaSection(rva);
            return DecodeRva(rva, section);
        }

        public long DecodeRva(long rva, int section)
        {
            return Sections[section].PointerToRawData + (rva - Sections[section].VirtualAddress);
        }

        public void Dispose()
        {
            Reader.Dispose();
        }
    }
}
