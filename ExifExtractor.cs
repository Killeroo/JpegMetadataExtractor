﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JpgExifExtractor
{
    public static class ExifExtractor
    {
        private static readonly byte[] kJpgStartOfImage = { 0xFF, 0xD8 };
        private static readonly byte[] kJpgStartOfScan = { 0xFF, 0xDA };
        private static readonly byte[] kJpgEndOfImage = { 0xFF, 0xD9 };
        private static readonly byte[] kJpgExifAppData = { 0xFF, 0xE1 };

        private static ushort kTiffIntelAligned = 0x4949;
        private static ushort kTiffMotorolaAligned = 0x4D4D;

        private static ushort kTiffExifSubIFDTag = 0x8769;

        private struct TiffEntry
        {
            public ushort Tag;
            public ushort Type;
            public uint Count;
            public uint ValueOffset;
            //public long BaseOffset;

            public TiffEntry(BinaryReader reader)
            {
                Tag = reader.ReadUInt16();
                Type = reader.ReadUInt16();
                Count = reader.ReadUInt32();
                ValueOffset = reader.ReadUInt32();
            }
        }

        public static bool GetTags(string filePath, out ExifEntry[] entries)
        {
            // Is it a jpeg file that exits
            if (File.Exists(filePath) == false || Path.GetExtension(filePath).ToLower() != ".jpg")
            {
                return;
            }

            // Open file stream
            FileStream stream;
            try
            {
                stream = new FileStream(filePath, FileMode.Open);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open file. [{0}]", e.GetType().ToString());
                return;
            }

            using (BinaryReader reader = new(stream))
            {

            }

            stream.Close();

            return false;
        }

        public static bool GetTag(string filePath, ushort tag, out object value)
        {
            value = 2;
            return false;
        }

        public static bool GetThumbnail(string filePath, out byte[] imageData)
        {

        }

        private static long ParseFile(BinaryReader reader, out List<TiffEntry> imageEntries, out List<TiffEntry> thumbnailEntries)
        {
            // Verify jpeg file, should always start with StartOfImage segment
            byte[] segment = reader.ReadBytes(2);
            if (segment[0] != kJpgStartOfImage[0] || segment[1] != kJpgStartOfImage[1]) // TODO: Ew
            {
                Console.WriteLine("[JPG] Encountered incorrect starting segment");
                return;
            }

            // Loop through segments until we find Exif file in segment APP1 
            bool foundExifSegment = false;
            int exifSegmentSize = 0;
            do
            {
                byte marker = reader.ReadByte();
                if (marker != 0xFF)
                {
                    Console.WriteLine("[JPG] Found incorrect starting marker @ 0x{0}", (reader.BaseStream.Position - 1).ToString("X6"));
                    return;
                }
                byte type = reader.ReadByte();
                long pos = reader.BaseStream.Position;

                // Skip next byte appears to be a marker
                if (type == 0xFF || reader.PeakByte() == 0xFF)
                {
                    continue;
                }

                if (marker == kJpgExifAppData[0] && type == kJpgExifAppData[1])
                {
                    exifSegmentSize = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray());
                    foundExifSegment = true;
                }
                if (marker == kJpgEndOfImage[0] && type == kJpgEndOfImage[1])
                {
                    Console.WriteLine("[JPG] Reached end of image");
                    break;
                }
                else if (marker == kJpgStartOfScan[0] && type == kJpgStartOfScan[1])
                {
                    // StartOfScan segments go straight into data with no length (how naughty)
                    // Read the data until we hit another marker
                    byte[] next;
                    do
                    {
                        byte read = reader.ReadByte();
                        next = reader.PeakBytes(2);
                    }
                    while (next[0] != 0xFF || next[1] == 0x0); // (Ignore 'stuffed bytes' aka fake markers in data)
                }
                else if (!foundExifSegment)
                {
                    // Treat segment as if it was a normal variable length segment and skip over data
                    ushort size = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray());
                    reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
                }

                //Console.WriteLine("[0x{2}] [Segment] Marker=0x{0} Type=0x{1}", marker.ToString("X"), type.ToString("X"), pos.ToString("X6"));
            }
            while (!foundExifSegment);

            if (foundExifSegment == false || exifSegmentSize == 0)
            {
                Console.WriteLine("Could not find Exif segment");
                return;
            }

            // Exif header
            string exifMarker = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (exifMarker != "Exif" || reader.ReadInt16() != 0)
            {
                Console.WriteLine("Could not parse Exif header");
                return;
            }

            // Tiff header
            long tiffPosition = reader.BaseStream.Position;
            ushort byteAlignment = reader.ReadUInt16();
            if (byteAlignment != kTiffIntelAligned && byteAlignment != kTiffMotorolaAligned)
            {
                Console.WriteLine("Could not recognise Tiff byte alignment");
                return;
            }
            ushort byteAlignmentTest = reader.ReadUInt16();
            if ((byteAlignment == kTiffIntelAligned && byteAlignmentTest != 0x2A00)
                && (byteAlignment == kTiffMotorolaAligned && byteAlignmentTest != 0x002A))
            {
                Console.WriteLine("Tiff byte alignment test failed");
                return;
            }
            uint firstIfdOffset = reader.ReadUInt32();

            // Parse tiff structure
            uint ParseImageFileDirectory(long offset, ref List<TiffEntry> outEntries)
            {
                reader.BaseStream.Seek(tiffPosition + offset, SeekOrigin.Begin);
                ushort entries = reader.ReadUInt16();
                //Console.WriteLine("IFD: loc={1} count={0}", entries, offset.ToString("x8"));

                for (int i = 0; i < entries; i++)
                {
                    outEntries.Add(new TiffEntry(reader));

                    // Check if we found the Exif SubIFD, that contains those good good tags
                    // so recurse back into it to parse it's contents
                    if (outEntries[^1].Tag == kTiffExifSubIFDTag)
                    {
                        long subTagPosition = reader.BaseStream.Position;
                        ParseImageFileDirectory(outEntries[^1].ValueOffset, ref outEntries);
                        reader.BaseStream.Seek(subTagPosition, SeekOrigin.Begin);
                    }
                }

                // Return offset to next IFD
                return reader.ReadUInt32();
            }

            // Exif only uses 2 image directories (and one exif sub directory) so we can unroll any loop that we would
            // normally need for parsing tiffs in this case
            List<TiffEntry> imageEntries = new();
            List<TiffEntry> thumbnailEntries = new();
            long thumbnailIfdOffset = ParseImageFileDirectory(firstIfdOffset, ref imageEntries);
            ParseImageFileDirectory(thumbnailIfdOffset, ref thumbnailEntries);

            // Resolve exif values
            foreach (var entry in imageEntries)
            {
                Console.WriteLine("Tag=0x{0} Type={1} Count={2} ValueOffset=0x{3}",
                    entry.Tag.ToString("X4"),
                    entry.Type,
                    entry.Count,
                    entry.ValueOffset.ToString("X8"));
            }
        }

        ExifEntry ResolveTiffEntry(BinaryReader reader, in TiffEntry entry)
        {

        }

        List<ExifEntry> ResolveTiffEntries(BinaryReader reader, List<TiffEntry> entries)
        {

        }
    }
}
