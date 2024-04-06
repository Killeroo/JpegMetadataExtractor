namespace JpgExifExtractor
{
    namespace JPG
    {
        // Jpeg section header
        struct SectionHeader : IEquatable<SectionHeader>
        {
            public byte Marker;
            public byte Type;
            public ushort Size;

            public SectionHeader(byte marker, byte type) : this(marker, type, 0) { }
            public SectionHeader(byte marker, byte type, ushort size)
            {
                this.Marker = marker;
                this.Type = type;
                this.Size = size;
            }

            public static bool operator ==(SectionHeader rhs, SectionHeader lhs)
            {
                return rhs.Equals(lhs);
            }
            public static bool operator !=(SectionHeader rhs, SectionHeader lhs)
            {
                return !rhs.Equals(lhs);
            }

            public override bool Equals(object obj)
            {
                return obj is SectionHeader && Equals((SectionHeader)obj);
            }

            public bool Equals(SectionHeader other)
            {
                return Marker == other.Marker && Type == other.Type;
            }
        }
    }

    namespace Tiff
    {
        enum ByteOrder : byte
        {
            LittleEndian,
            BigEndian,
        }

        enum EntryType
        {

        }

        struct Header
        {
            ByteOrder ByteOrder;
            ushort MagicNumber; // Should always be 42
            uint StartOffset;
        }

        struct Directory
        {

        }

        
        struct Entry<T>
        {
            byte Tag;
            ushort Type;
            T Value;
        }
    }

    internal class Program
    {
        private static readonly byte[] kJPGStartOfImage = { 0xFF, 0xD8 };
        private static readonly byte[] kJPGStartOfScan = { 0xFF, 0xDA };
        private static readonly byte[] kJPGEndOfImage = { 0xFF, 0xD9 };
        private static readonly byte[] kJPGExifAppData = { 0xFF, 0xE1 };

        // JPEG section headers we need to watch out for
        private static readonly JPG.SectionHeader kStartOfImageSection = new(0xFF, 0xD8);
        private static readonly JPG.SectionHeader kStartOfScanSection = new(0xFF, 0xDA);
        private static readonly JPG.SectionHeader kStartOfEndSection = new(0xFF, 0xD9);
        private static readonly JPG.SectionHeader kExifAppDataSection = new(0xFF, 0xE1);

        static void Main(string[] args)
        {
            // Do we have any arguments
            if (args.Length == 0)
            {
                return;
            }

            // Is it a jpeg file that exits
            string filePath = args[0];
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

            using (BinaryReader reader = new (stream)) 
            {
                // Verify jpeg file, should always start with StartOfImage segment
                byte[] segment = reader.ReadBytes(2);
                if (segment[0] != kJPGStartOfImage[0] || segment[1] != kJPGStartOfImage[1]) // TODO: Ew
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

                    if (marker == kJPGExifAppData[0] && type == kJPGExifAppData[1])
                    {
                        exifSegmentSize = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray());
                        foundExifSegment = true;
                    }
                    if (marker == kJPGEndOfImage[0] && type == kJPGEndOfImage[1])
                    {
                        Console.WriteLine("[JPG] Reached end of image");
                        break;
                    }
                    else if (marker == kJPGStartOfScan[0] && type == kJPGStartOfScan[1])
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
                    else
                    {
                        // Treat segment as if it was a normal variable length segment and skip over data
                        ushort size = BitConverter.ToUInt16(reader.ReadBytes(2).Reverse().ToArray());
                        reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
                    }

                    Console.WriteLine("[0x{2}] [Segment] Marker=0x{0} Type=0x{1}", marker.ToString("X"), type.ToString("X"), pos.ToString("X6"));
                }
                while (!foundExifSegment);

                if (foundExifSegment == false || exifSegmentSize == 0)
                {
                    Console.WriteLine("Could not find Exif segment");
                    return;
                }

                // Parse Tiff

            }


            stream.Close();
        }
    }

    static class Extensions
    {
        public static byte PeakByte(this BinaryReader reader)
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

        public static byte[] PeakBytes(this BinaryReader reader, int count)
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