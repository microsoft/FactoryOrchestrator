// From https://github.com/AndresTraks/pe-utility with minimal changes for simplification and compatibility with .NET Standard
// Copyright(c) 2014-2016 Andres Traks
// Licensed under the MIT license.
#pragma warning disable CA1051 // Do not declare visible instance fields
#pragma warning disable CA1815 // Override equals and operator equals on value types
using System;
using System.Runtime.InteropServices;

namespace PEUtility
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDosHeader
    {
        public UInt16 Signature;
        public UInt16 LastPageSize;
        public UInt16 PagesInFile;
        public UInt16 Relocations;
        public UInt16 HeaderSizePar;
        public UInt16 MinAlloc;
        public UInt16 MaxAlloc;
        public UInt16 Ss;
        public UInt16 Sp;
        public UInt16 Checksum;
        public UInt16 Ip;
        public UInt16 Cs;
        public UInt16 LfaRelocationTable;
        public UInt16 OverlayNumber;
        public UInt32 Reserved1;
        public UInt16 OemId;
        public UInt16 OemInfo;
        public UInt64 Reserved2;
        public UInt64 Reserved3;
        public UInt32 Reserved4;
        public Int32 LfaNewHeader;

        public bool IsValid
        {
            get { return Signature == 0x5a4d; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageFileHeader
    {
        public UInt16 Machine;
        public UInt16 NumberOfSections;
        public UInt32 TimeDateStamp;
        public UInt32 PointerToSymbolTable;
        public UInt32 NumberOfSymbols;
        public UInt16 SizeOfOptionalHeader;
        public UInt16 Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDataDirectory
    {
        public uint VirtualAddress;
        public uint Size;

        public override string ToString()
        {
            if (VirtualAddress == 0)
            {
                return "Directory: None";
            }
            return $"Directory: 0x{VirtualAddress:X} ({Size})";
        }
    }

    public enum Subsystem : ushort
    {
        Unknown = 0,
        Native = 1,
        WindowsGui = 2,
        WindowsCui = 3,
        PosixCui = 7,
        WindowsCeGui = 9,
        EfiApplication = 10,
        EfiBootServiceDriver = 11,
        EfiRuntimeDriver = 12,
        EfiRom = 13,
        Xbox = 14
    }

    [Flags]
    public enum DllCharacteristics : ushort
    {
        DynamicBase = 0x0040,
        ForceIntegrity = 0x0080,
        NxCompat = 0x0100,
        NoIsolation = 0x0200,
        NoSeh = 0x0400,
        NoBind = 0x0800,
        WdmDriver = 0x2000,
        TerminalServerAware = 0x8000
    }

    public enum ImageOptionalHeaderMagic : ushort
    {
        Header32 = 0x10b,
        Header64 = 0x20b,
        HeaderRom = 0x107
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageOptionalHeader32
    {
        public ImageOptionalHeaderMagic Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt32 BaseOfData;
        public UInt32 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public Subsystem Subsystem;
        public DllCharacteristics DllCharacteristics;
        public UInt32 SizeOfStackReserve;
        public UInt32 SizeOfStackCommit;
        public UInt32 SizeOfHeapReserve;
        public UInt32 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;
        public ImageDataDirectory ExportTable;
        public ImageDataDirectory ImportTable;
        public ImageDataDirectory ResourceTable;
        public ImageDataDirectory ExceptionTable;
        public ImageDataDirectory CertificateTable;
        public ImageDataDirectory BaseRelocationTable;
        public ImageDataDirectory Debug;
        public ImageDataDirectory Architecture;
        public ImageDataDirectory GlobalPtr;
        public ImageDataDirectory TLSTable;
        public ImageDataDirectory LoadConfigTable;
        public ImageDataDirectory BoundImport;
        public ImageDataDirectory IAT;
        public ImageDataDirectory DelayImportDescriptor;
        public ImageDataDirectory CLRRuntimeHeader;
        public ImageDataDirectory Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageOptionalHeader64
    {
        public ImageOptionalHeaderMagic Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt64 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public Subsystem Subsystem;
        public DllCharacteristics DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;
        public UInt32 LoaderFlags;
        public UInt32 NumberOfRvaAndSizes;
        public ImageDataDirectory ExportTable;
        public ImageDataDirectory ImportTable;
        public ImageDataDirectory ResourceTable;
        public ImageDataDirectory ExceptionTable;
        public ImageDataDirectory CertificateTable;
        public ImageDataDirectory BaseRelocationTable;
        public ImageDataDirectory Debug;
        public ImageDataDirectory Architecture;
        public ImageDataDirectory GlobalPtr;
        public ImageDataDirectory TLSTable;
        public ImageDataDirectory LoadConfigTable;
        public ImageDataDirectory BoundImport;
        public ImageDataDirectory IAT;
        public ImageDataDirectory DelayImportDescriptor;
        public ImageDataDirectory CLRRuntimeHeader;
        public ImageDataDirectory Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNtHeaders32
    {
        public UInt32 Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader32 OptionalHeader;

        public bool IsValid
        {
            get { return Signature == 0x4550 &&
                (OptionalHeader.Magic == ImageOptionalHeaderMagic.Header32 || OptionalHeader.Magic == ImageOptionalHeaderMagic.Header64);
            }
        }

        public bool Is64Bit
        {
            get { return OptionalHeader.Magic == ImageOptionalHeaderMagic.Header64; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNtHeaders64
    {
        public UInt32 Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader64 OptionalHeader;

        public bool IsValid
        {
            get
            {
                return Signature == 0x4550 &&
                    (OptionalHeader.Magic == ImageOptionalHeaderMagic.Header32 || OptionalHeader.Magic == ImageOptionalHeaderMagic.Header64);
            }
        }
    }

    public enum ImageDirectoryEntry
    {
        Export = 0,
        Import = 1,
        Resource = 2,
        Exception = 3,
        Security = 4,
        BaseReloc = 5,
        Debug = 6,
        Architecture = 7,
        GlobalPtr = 8,
        Tls = 9,
        LoadConfig = 10,
        BoundImport = 11,
        Iat = 12,
        DelayImport = 13,
        ComDescriptor = 14
    }

    [Flags]
    public enum ImageSectionCharacteristics : Int64
    {
        None = 0x0,
        TypeDSect = 0x1,
        TypeNoLoad = 0x2,
        TypeGroup = 0x4,
        TypeNoPad = 0x8,
        TypeCopy = 0x10,
        ContainsCode = 0x20,
        ContainsInitializedData = 0x40,
        ContainsUninitializedData = 0x80,
        LnkOther = 0x100,
        LnkInfo = 0x200,
        TypeOver = 0x400,
        LnkRemove = 0x800,
        LnkComdat = 0x1000,
        MemoryFarData = 0x8000,
        MemoryPurgeable = 0x20000,
        MemoryLocked = 0x40000,
        MemoryPreload = 0x80000,
        Align1Bytes = 0x100000,
        Align2Bytes = 0x200000,
        Align4Bytes = 0x300000,
        Align8Bytes = 0x400000,
        Align16Bytes = 0x500000,
        Align32Bytes = 0x600000,
        Align64Bytes = 0x700000,
        Align128Bytes = 0x800000,
        Align256Bytes = 0x900000,
        Align512Bytes = 0xA00000,
        Align1024Bytes = 0xB00000,
        Align2048Bytes = 0xC00000,
        Align4096Bytes = 0xD00000,
        Align8192Bytes = 0xE00000,
        LnkNRelocOvfl = 0x1000000,
        MemoryDiscardable = 0x2000000,
        MemoryNotCached = 0x4000000,
        MemoryNotPaged = 0x8000000,
        MemoryShared = 0x10000000,
        MemoryExecute = 0x20000000,
        MemoryRead = 0x40000000,
        MemoryWrite = 0x80000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageSectionHeader
    {
        public ulong Name;
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public ImageSectionCharacteristics Characteristics;

        public string Section
        {
            get
            {
                var name = new char[8];
                int i;
                for (i = 0; i < 8; i++)
                {
                    name[i] = (char)(Name >> (i << 3) & 0xffUL);
                    if (name[i] == 0)
                        break;
                }
                return new string(name, 0, i);
            }
        }

        public override string ToString()
        {
#pragma warning disable CA1305 // Specify IFormatProvider
            return Section + " RVA: " + string.Format("0x{0:X}", VirtualAddress);
#pragma warning restore CA1305 // Specify IFormatProvider
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ImageImportDescriptor
    {
        public UInt32 Characteristics;
        //public UInt32 OriginalFirstThunk;
        public UInt32 TimeDateStamp;
        public UInt32 ForwarderChain;
        public UInt32 Name;
        public UInt32 FirstThunk;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageExportDirectory
    {
        public UInt32 Characteristics;
        public UInt32 TimeDateStamp;
        public UInt16 MajorVersion;
        public UInt16 MinorVersion;
        public UInt32 Name;
        public UInt32 Base;
        public UInt32 NumberOfFunctions;
        public UInt32 NumberOfNames;
        public UInt32 AddressOfFunctions;
        public UInt32 AddressOfNames;
        public UInt32 AddressOfNameOrdinals;

        public string ExportDirectory
        {
            get
            {
                var name = new char[4];
                int i;
                for (i = 0; i < 4; i++)
                {
                    name[i] = (char)(Name >> (i << 3) & 0xffUL);
                    if (name[i] == 0)
                        break;
                }
                return new string(name, 0, i);
            }
        }
    }

#pragma warning disable CA1707 // Identifiers should not contain underscores
    [Flags]
    public enum ComImageFlags : uint
    {
        ILOnly = 1,
        _32BitRequired = 2,
        ILLibrary = 4,
        StrongNameSigned = 8,
        NativeEntryPoint = 0x10,
        TrackDebugData = 0x10000,
        _32BitPreferred = 0x20000,
    }
#pragma warning restore CA1707 // Identifiers should not contain underscores

    public struct ImageCor20Header
    {
        public UInt32 cb;
        public UInt16 MajorRuntimeVersion;
        public UInt16 MinorRuntimeVersion;
        public ImageDataDirectory MetaData;
        public ComImageFlags Flags;
        public uint EntryPoint;
        public ImageDataDirectory Resources;
        public ImageDataDirectory StrongNameSignature;
        public ImageDataDirectory CodeManagerTable;
        public ImageDataDirectory VTableFixups;
        public ImageDataDirectory ExportAddressTableJumps;
        public ImageDataDirectory ManagedNativeHeader;
    }
}
#pragma warning restore CA1051 // Do not declare visible instance fields
#pragma warning restore CA1815 // Override equals and operator equals on value types
