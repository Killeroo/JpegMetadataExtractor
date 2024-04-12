using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace JpgExifExtractor
{
    public enum ExifType : ushort
    {
        Byte = 1,
        String = 2,
        UShort = 3,
        ULong = 4,
        URational = 5,
        SByte = 6,
        Undefined = 7,
        Short = 8,
        Long = 9,
        Rational = 10,
        Float = 11,
        Double = 12,
    }

    public struct ExifEntry
    {
        public readonly ushort Tag;
        public readonly ExifType Type;
        public readonly byte[] Value;

        public ExifEntry(ushort inTag, ExifType inType, byte[] inValue)
        {
            Tag = inTag;
            Type = inType;
            Value = inValue;
        }

        // TODO: Add some simple caching
        // TODO: Check specific size based on type?
        // TODO: Comment that is could return an exception
        public byte GetValueAsByte()
        {
            if (Type != ExifType.Byte || Value.Length == 0) 
            {
                return 0;
            }
            else
            {
                return Value[0];
            }
        }

        public string GetValueAsString()
        {
            if (Type != ExifType.String || Value.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return Encoding.ASCII.GetString(Value);
            }
        }

        public ushort GetValueAsUShort()
        {
            if (Type != ExifType.UShort || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToUInt16(Value, 0);
            }
        }

        public ulong GetValueAsULong()
        {
            if (Type != ExifType.ULong || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToUInt64(Value, 0);
            }
        }

        public URational GetValueAsURational()
        {
            if (Type != ExifType.URational || Value.Length == 0)
            {
                return URational.Empty;
            }
            else
            {
                return new URational(Value, 0);
            }
        }

        public sbyte GetValueAsSByte()
        {
            if (Type != ExifType.SByte || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return (sbyte) Value[0];
            }    
        }

        public short GetValueAsShort()
        {
            if (Type != ExifType.Short || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToInt16(Value);
            }
        }

        public long GetValueAsLong()
        {
            if (Type != ExifType.Long || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToInt64(Value);
            }
        }

        public Rational GetValueAsRational()
        {
            if (Type != ExifType.Rational || Value.Length == 0)
            {
                return Rational.Empty;
            }
            else
            {
                return new Rational(Value, 0);
            }
        }

        public float GetValueAsFloat()
        {
            if (Type != ExifType.Float || Value.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                return BitConverter.ToSingle(Value);
            }
        }

        public double GetValueAsDouble()
        {
            if (Type != ExifType.Double || Value.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                return BitConverter.ToDouble(Value);
            }
        }

    }

    public struct Rational
    {
        public static Rational Empty = new Rational(0, 0);

        public readonly long Numerator;
        public readonly long Denominator;

        public Rational(byte[] bytes, int offset)
        {
            Numerator = BitConverter.ToInt64(bytes, offset);
            Denominator = BitConverter.ToInt64(bytes, offset + 8);
        }
        public Rational(long _numerator, long _denominator)
        {
            Numerator = _numerator;
            Denominator = _denominator;
        }

        public override string ToString() 
        {
            return string.Format("{0}//{1}", Numerator, Denominator);
        }
    }

    public struct URational
    {
        public static URational Empty = new URational(0, 0);

        public readonly ulong Numerator;
        public readonly ulong Denominator;
        
        public URational(byte[] bytes, int offset)
        {
            Numerator = BitConverter.ToUInt64(bytes, offset);
            Denominator = BitConverter.ToUInt64(bytes, offset + 8);
        }
        public URational(ulong _numerator, ulong _denominator)
        {
            Numerator = _numerator;
            Denominator = _denominator;
        }

        public override string ToString()
        {
            return string.Format("{0}//{1}", Numerator, Denominator);
        }
    }


    public static class ExifExtractor
    {
        private static readonly byte[] kJpgStartOfImage = { 0xFF, 0xD8 };
        private static readonly byte[] kJpgStartOfScan = { 0xFF, 0xDA };
        private static readonly byte[] kJpgEndOfImage = { 0xFF, 0xD9 };
        private static readonly byte[] kJpgExifAppData = { 0xFF, 0xE1 };

        private static ushort kTiffIntelAligned = 0x4949;
        private static ushort kTiffMotorolaAligned = 0x4D4D;

        private static ushort kTiffExifSubIFDTag = 0x8769;

        // TODO: Not efficient lookups?
        public static readonly Dictionary<ExifType, byte> ExifTypeSizeMap = new()
        {
            { ExifType.Byte, 1 },
            { ExifType.String, 1 },
            { ExifType.UShort, 2 },
            { ExifType.ULong, 4 },
            { ExifType.URational, 8 },
            { ExifType.SByte, 1 },
            { ExifType.Undefined, 1 },
            { ExifType.Short, 2 },
            { ExifType.Long, 4 },
            { ExifType.Rational, 8 },
            { ExifType.Float, 4 },
            { ExifType.Double, 8 },
        };

        private struct TiffEntry
        {
            public ushort Tag;
            public ushort Type;
            public uint Count;
            public uint ValueOffset;

            public TiffEntry(BinaryReader reader)
            {
                Tag = reader.ReadUInt16();
                Type = reader.ReadUInt16();
                Count = reader.ReadUInt32();
                ValueOffset = reader.ReadUInt32();
            }
        }

        public static bool GetTags(string filePath, out List<ExifEntry> entries)
        {
            entries = new List<ExifEntry>();

            // Is it a jpeg file that exits
            if (File.Exists(filePath) == false || Path.GetExtension(filePath).ToLower() != ".jpg")
            {
                return false;
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
                return false;
            }

            using (BinaryReader reader = new(stream))
            {
                long exifOffset = FindExifDataInJpg(reader);
                if (exifOffset < 0)
                {
                    return false;
                }

                List<TiffEntry> imageEntries = new();
                List<TiffEntry> thumbnailEntries = new();
                TryParseTiffStructure(reader, exifOffset, out imageEntries, out thumbnailEntries);

                ResolveTiffEntries(reader, exifOffset, imageEntries);
                ResolveTiffEntry();

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
            imageData = new byte[0];
            return false;
        }

        /// <summary>
        /// Parse a JPG file and returns the offset of the Exif data inside the file. 
        /// </summary>
        /// <param name="reader">BinaryReader of opened jpg file</param>
        /// <returns>offset of exif data within the jpg file</returns>
        /// <remarks>
        /// -1 is returned if an error occurs during parsing or if the exif data cannot be found.
        /// </remarks>
        private static long FindExifDataInJpg(BinaryReader reader)
        {
            // Verify jpeg file, should always start with StartOfImage segment
            byte[] segment = reader.ReadBytes(2);
            if (segment[0] != kJpgStartOfImage[0] || segment[1] != kJpgStartOfImage[1]) // TODO: Ew
            {
                Console.WriteLine("[JPG] Encountered incorrect starting segment");
                return -1;
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
                    return -1;
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
                return -1;
            }

            return reader.BaseStream.Position;
        }

        private static bool TryParseTiffStructure(BinaryReader reader, long offset, out List<TiffEntry> imageEntries, out List<TiffEntry> thumbnailEntries)
        {
            imageEntries = new List<TiffEntry>();
            thumbnailEntries = new List<TiffEntry>();

            // Exif header
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            string exifMarker = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (exifMarker != "Exif" || reader.ReadInt16() != 0)
            {
                Console.WriteLine("Could not parse Exif header");
                return false;
            }

            // Tiff header
            long tiffPosition = reader.BaseStream.Position;
            ushort byteAlignment = reader.ReadUInt16();
            if (byteAlignment != kTiffIntelAligned && byteAlignment != kTiffMotorolaAligned)
            {
                Console.WriteLine("Could not recognise Tiff byte alignment");
                return false;
            }
            ushort byteAlignmentTest = reader.ReadUInt16();
            if ((byteAlignment == kTiffIntelAligned && byteAlignmentTest != 0x2A00)
                && (byteAlignment == kTiffMotorolaAligned && byteAlignmentTest != 0x002A))
            {
                Console.WriteLine("Tiff byte alignment test failed");
                return false;
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
            long thumbnailIfdOffset = ParseImageFileDirectory(firstIfdOffset, ref imageEntries);
            ParseImageFileDirectory(thumbnailIfdOffset, ref thumbnailEntries);

            return true;
        }

        private static ExifEntry ResolveTiffEntry(BinaryReader reader, in TiffEntry entry)
        {
            return new ExifEntry();
        }

        private static List<ExifEntry> ResolveTiffEntries(BinaryReader reader, long tiffOffset, List<TiffEntry> tiffEntries)
        {
            List<ExifEntry> exifEntries = new(); // Use out keyword to avoid initalizing again?

            foreach (var tiffEntry in tiffEntries)
            {
                reader.BaseStream.Seek(tiffOffset + tiffEntry.ValueOffset, SeekOrigin.Begin);

                ExifEntry newEntry = new (tiffEntry.Tag, (ExifType)tiffEntry.Type, reader.ReadBytes((int)tiffEntry.Count * ExifTypeSizeMap[(ExifType)tiffEntry.Type]));

                Console.WriteLine("Tag=0x{0} Type={1} Count={2} ValueOffset=0x{3}",
                    tiffEntry.Tag.ToString("X4"),
                    tiffEntry.Type,
                    tiffEntry.Count,
                    tiffEntry.ValueOffset.ToString("X8"));
            }

            return exifEntries;
        }

        private static byte PeakByte(this BinaryReader reader)
        {
            if (reader.BaseStream.CanSeek == false)
            {
                return 0; // TODO: Ooooh noooooooo
            }

            long origin = reader.BaseStream.Position;
            byte next = reader.ReadByte();
            reader.BaseStream.Position = origin;

            return next;
        }

        private static byte[] PeakBytes(this BinaryReader reader, int count)
        {
            if (reader.BaseStream.CanSeek == false)
            {
                return new byte[0];
            }

            long origin = reader.BaseStream.Position;
            byte[] next = reader.ReadBytes(count);
            reader.BaseStream.Position = origin;

            return next;
        }
    }
}
