﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SidderApp
{
    internal class VHDXParser
    {
        private string _filename { get; set; }
        public ulong NativeDiskSize { get { return GetNativeDiskSize(this._filename); } }
        public ulong FirstPartitionSize { get { return GetFirstPartitionSize(this._filename); } }

        public VHDXParser(string filename)
        {
            _filename = filename;
        }

        private ulong GetNativeDiskSize(string fileName)
        {
            try
            {
                using (var fileVHDX = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var binVHDX = new BinaryReader(fileVHDX))
                    {
                        var regionTableMetadataHeaderEntry = SearchSignatur(binVHDX.BaseStream, new byte[] { 0x06, 0xA2, 0x7C, 0x8B, 0x90, 0x47, 0x9A, 0x4B, 0xB8, 0xFE, 0x57, 0x5F, 0x05, 0x0F, 0x88, 0x6E }) + 16;

                        binVHDX.BaseStream.Seek(regionTableMetadataHeaderEntry, SeekOrigin.Begin);
                        var posMetadataHeader = binVHDX.ReadUInt32();

                        var posVDSizeOffsetEntry = SearchSignatur(binVHDX.BaseStream, new byte[] { 0x24, 0x42, 0xA5, 0x2F, 0x1B, 0xCD, 0x76, 0x48, 0xB2, 0x11, 0x5D, 0xBE, 0xD8, 0x3B, 0xF4, 0xB8 }, posMetadataHeader) + 16;

                        binVHDX.BaseStream.Seek(posVDSizeOffsetEntry, SeekOrigin.Begin);
                        var posVDSizeEntry = posMetadataHeader + binVHDX.ReadUInt32();

                        binVHDX.BaseStream.Seek(posVDSizeEntry, SeekOrigin.Begin);
                        var virtualDiskSize = binVHDX.ReadUInt64();

                        return virtualDiskSize;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private ulong GetFirstPartitionSize(string fileName)
        {
            try
            {
                using (var fileVHDX = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var binVHDX = new BinaryReader(fileVHDX))
                    {
                        //var posFirstPartition = SearchSignatur(binVHDX.BaseStream, new byte[] { 0x33, 0xC0, 0x8E, 0xD0, 0xBC, 0x00, 0x7C }) + 446; // More accurate but way slower
                        var posFirstPartition = 0x400000 + 446; // Should work on all default VHDXs with normal master boot record
                        var posPartitionSectorCount = posFirstPartition + 12;

                        binVHDX.BaseStream.Seek(posPartitionSectorCount, SeekOrigin.Begin);
                        var sectorCount = binVHDX.ReadUInt32();

                        return (ulong)sectorCount * 512u;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private long SearchSignatur(Stream haystack, byte[] needle, long seekStart = 0)
        {
            haystack.Seek(seekStart, SeekOrigin.Begin);

            int b;
            long i = 0;
            while ((b = haystack.ReadByte()) != -1)
            {
                if (b == needle[i++])
                {
                    if (i == needle.Length)
                        return haystack.Position - needle.Length;
                }
                else
                    i = b == needle[0] ? 1 : 0;
            }

            return -1;
        }
    }
}
