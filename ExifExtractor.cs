/**********************************************************************************
 ***                           JpegExifExtractor                                ***    
 ********************************************************************************** 
 *                                                                                *  
 * Lightweight (?) library that can extract Exif image tags from Jpeg files.      *  
 *                                                                                *
 **********************************************************************************
 * MIT License                                                                    *
 *                                                                                *
 * Copyright (c) 2024 Matthew Carney                                              *
 *                                                                                *
 * Permission is hereby granted, free of charge, to any person obtaining a copy   *
 * of this software and associated documentation files (the "Software"), to deal  *
 * in the Software without restriction, including without limitation the rights   *
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell      *
 * copies of the Software, and to permit persons to whom the Software is          *
 * furnished to do so, subject to the following conditions:                       *
 *                                                                                *
 * The above copyright notice and this permission notice shall be included in all *
 * copies or substantial portions of the Software.                                *
 *                                                                                *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR     *
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,       *
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE    *
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER         *
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  *
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE  *
 * SOFTWARE.                                                                      *
 *********************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JpgTagExtractor
{
    /// <summary>
    /// Extension methods required by extraction code
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Attempt to look at the next byte without altering the BinaryReader's current position in the underly BaseStream.
        /// </summary>
        /// <returns>The next byte of the underlying stream</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the BinaryReader or BaseStream is null</exception>
        /// <exception cref="IOException">Thrown if the BaseStream is not readable for any reason</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the BaseStream is reached.</exception>
        public static byte PeakByte(this BinaryReader reader)
        {
            if (reader == null || reader.BaseStream == null)
            {
                throw new ObjectDisposedException(nameof(reader));
            }

            if (reader.BaseStream.CanSeek == false || reader.BaseStream.CanRead == false)
            {
                throw new IOException("Could not seek through base stream");
            }

            if (reader.BaseStream.Length < reader.BaseStream.Position + 1)
            {
                throw new EndOfStreamException();
            }

            long origin = reader.BaseStream.Position;
            byte next = reader.ReadByte();
            reader.BaseStream.Position = origin;

            return next;
        }

        /// <summary>
        /// Attempt to look at a series of bytes without altering the BinaryReader's current position in the underly BaseStream.
        /// </summary>
        /// <param name="count">The number of bytes to peak at</param>
        /// <returns>The specified number of bytes in the Stream</returns>
        /// <exception cref="ObjectDisposedException">Thrown if the BinaryReader or BaseStream is null</exception>
        /// <exception cref="IOException">Thrown if the BaseStream is not readable for any reason</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the BaseStream is reached.</exception>
        public static byte[] PeakBytes(this BinaryReader reader, int count)
        {
            if (reader == null || reader.BaseStream == null)
            {
                throw new ObjectDisposedException(nameof(reader));
            }

            if (reader.BaseStream.CanSeek == false || reader.BaseStream.CanRead == false)
            {
                throw new IOException("Could not seek through base stream");
            }

            if (reader.BaseStream.Length < reader.BaseStream.Position + count)
            {
                throw new EndOfStreamException();
            }

            long origin = reader.BaseStream.Position;
            byte[] next = reader.ReadBytes(count);
            reader.BaseStream.Position = origin;

            return next;
        }
    }

    /// <summary>
    /// Exception that is thrown when trying to access Exif Entry data as a type that is different to the ExifType the entry uses.
    /// </summary>
    public class ExifTypeMismatchException : Exception 
    {
        public ExifTypeMismatchException(string message) : base(message) { }
    }

    /// <summary>
    /// The Exif value types, defines what type of data an ExifEntry stores.
    /// </summary>
    /// <remarks>
    /// See TIFF 6.0 spec for more information,
    /// </remarks>
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

    /// <summary>
    /// Simple representation for an Exif data entry.
    /// 
    /// </summary>
    /// <remarks>
    /// The Exif data itself can be accessed in it's raw form as a byte array or using one of the accessor methods which 
    /// will attempt to blindly convert the byte array to the given data type.
    /// 
    /// The user is expected to read (or know ahead of time) the data type of the data and then can act accordingly on the raw 
    /// byte array or use one of the conversion methods as they see fit.
    /// 
    /// I have chosen to represent Exif data generically as one type to hopefully give the end user a bit more freedom of how they
    /// want to use and manipulate the exif data without having to abide by whatever system I might implement to best implement this data in a more OO way.
    /// </remarks>
    public struct ExifEntry
    {
        /// <summary>
        /// Simple map that contains the byte length of each different Exif data types.
        /// </summary>
        public static readonly Dictionary<ExifType, byte> TypeSizeMap = new()
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

        // Exif fields
        public readonly ushort Tag;
        public readonly ExifType Type;
        public readonly byte[] Value;

        public ExifEntry(ushort inTag, ExifType inType, byte[] inValue)
        {
            Tag = inTag;
            Type = inType;
            Value = inValue;
        }

        /// <summary>
        /// Returns the exif data as a byte.
        /// </summary>
        /// <returns>Byte representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public byte GetValueAsByte()
        {
            if (Type != ExifType.Byte || Value.Length == 0) 
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to Byte");
            }
            else
            {
                return Value[0];
            }
        }

        /// <summary>
        /// Returns the exif data as a ASCII string.
        /// </summary>
        /// <returns>String representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public string GetValueAsString()
        {
            if (Type != ExifType.String || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to string");
            }
            else
            {
                return Encoding.ASCII.GetString(Value);
            }
        }

        /// <summary>
        /// Returns the exif data as a unsigned short.
        /// </summary>
        /// <returns>Unsigned short representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public ushort GetValueAsUShort()
        {
            if (Type != ExifType.UShort || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to ushort");
            }
            else
            {
                return BitConverter.ToUInt16(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a unsigned long.
        /// </summary>
        /// <returns>Unsigned long representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public ulong GetValueAsULong()
        {
            if (Type != ExifType.ULong || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to ulong");
            }
            else
            {
                return BitConverter.ToUInt64(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a unsigned rational.
        /// </summary>
        /// <returns>Unsigned rational representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public URational GetValueAsURational()
        {
            if (Type != ExifType.URational || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to urational");
            }
            else
            {
                return new URational(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a signed byte.
        /// </summary>
        /// <returns>Signed byte representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public sbyte GetValueAsSByte()
        {
            if (Type != ExifType.SByte || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to sbyte");
            }
            else
            {
                return (sbyte) Value[0];
            }    
        }

        /// <summary>
        /// Returns the exif data as a signed short.
        /// </summary>
        /// <returns>Signed short representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public short GetValueAsShort()
        {
            if (Type != ExifType.Short || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to short");
            }
            else
            {
                return BitConverter.ToInt16(Value);
            }
        }

        /// <summary>
        /// Returns the exif data as a signed long.
        /// </summary>
        /// <returns>Signed long representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public long GetValueAsLong()
        {
            if (Type != ExifType.Long || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to long");
            }
            else
            {
                return BitConverter.ToInt64(Value);
            }
        }

        /// <summary>
        /// Returns the exif data as a signed rational.
        /// </summary>
        /// <returns>Signed rational representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public Rational GetValueAsRational()
        {
            if (Type != ExifType.Rational || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to rational");
            }
            else
            {
                return new Rational(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a float.
        /// </summary>
        /// <returns>Float representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public float GetValueAsFloat()
        {
            if (Type != ExifType.Float || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to float");
            }
            else
            {
                return BitConverter.ToSingle(Value);
            }
        }

        /// <summary>
        /// Returns the exif data as a double.
        /// </summary>
        /// <returns>Double representation of Exif data</returns>
        /// <exception cref="ExifTypeMismatchException">Thrown if the type does not match the ExifType of the entry</exception>
        public double GetValueAsDouble()
        {
            if (Type != ExifType.Double || Value.Length == 0)
            {
                throw new ExifTypeMismatchException($"Cannot convert Exif Entry with type {Type} to double");
            }
            else
            {
                return BitConverter.ToDouble(Value);
            }
        }

    }

    /// <summary>
    /// Simple representation of a signed rational value used in tiff/exif data to store a signed fixed point decimal number.
    /// </summary>
    /// <remarks>
    /// For more information on how Rational tiffs are defined please have a look at the Tiff 6.0 spec under IFD types.
    /// </remarks>
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

    /// <summary>
    /// Simple representation of a unsigned rational value used in tiff/exif data to store an unsigned fixed point decimal number.
    /// </summary>
    /// <remarks>
    /// For more information on how Rational tiffs are defined please have a look at the Tiff 6.0 spec under IFD types.
    /// </remarks>
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

    /// <summary>
    /// Class that can extract Exif Version 2.0 tags from Jpg files
    /// </summary>
    public static class ExifExtractor
    {
        // Some data constants that we will be looking out for as we parse Jpg and Tiff data
        private static readonly byte[] kJpgStartOfImage = { 0xFF, 0xD8 };
        private static readonly byte[] kJpgStartOfScan = { 0xFF, 0xDA };
        private static readonly byte[] kJpgEndOfImage = { 0xFF, 0xD9 };
        private static readonly byte[] kJpgExifAppData = { 0xFF, 0xE1 };
        private static ushort kTiffIntelAligned = 0x4949;
        private static ushort kTiffMotorolaAligned = 0x4D4D;
        private static ushort kTiffExifSubIFDTag = 0x8769;

        private static long kInvalidOffset = -1;


        /// <summary>
        /// Simple representation of a Tiff entry. Exif data is stored using tiff and so these entries are used to store references to Exif data.
        /// </summary>
        private struct TiffEntry
        {
            public ushort Tag = 0;
            public ushort Type = 0;
            public uint Count = 0;
            public uint ValueOffset = 0;

            public TiffEntry(BinaryReader? reader)
            {
                if (reader == null)
                {
                    return;
                }

                Tag = reader.ReadUInt16();
                Type = reader.ReadUInt16();
                Count = reader.ReadUInt32();
                ValueOffset = reader.ReadUInt32();
            }
        }


        /// <summary>
        /// A very simple fixed size cache of parsed image exif tags. 
        /// 
        /// Limited to store the last X file's worth of retrieved values, where the oldest entry is removed first.
        /// </summary>
        private static class ResolvedExifTagsCache
        {
            // This could actually be done using an OrderedDictionary but hey
            private static Queue<string> _keys = new();
            private static Dictionary<string, Dictionary<ushort, ExifEntry>> _cache = new();

            private static int _maxCacheSize = 1;

            public static int Count => _cache.Count;

            public static void Clear()
            {
                _keys.Clear();
                _cache.Clear();
            }

            public static int Capacity 
            { 
                get => _maxCacheSize; 
                set
                {
                    _maxCacheSize = value;

                    if (_maxCacheSize == 0)
                    {
                        Clear();
                    }
                    else if (_maxCacheSize < _cache.Count)
                    {
                        for (int i = _cache.Count - 1; i >= _maxCacheSize; i--)
                        {
                            _cache.Remove(_keys.Dequeue());
                        }
                    }
                }
            }

            public static bool ContainsKey(string key)
            {
                return _cache.ContainsKey(key);
            }

            public static Dictionary<ushort, ExifEntry> Retrieve(string key)
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                return new Dictionary<ushort, ExifEntry>();
            }

            public static void Add(string key, Dictionary<ushort, ExifEntry> ExifTags)
            {
                if (_cache.Count == _maxCacheSize)
                {
                    _cache.Remove(_keys.Dequeue());
                }

                _cache.Add(key, ExifTags);
                _keys.Enqueue(key);
            }
        }


        /* Cache controls */

        /// <summary>
        /// Wether of not to use the extractor's internal cache of exif tags. This cache stores sets of Exif tags that have been successfully parsed from files.
        /// </summary>
        /// <seealso cref="SetCacheSize(int)"/>
        /// <seealso cref="ClearCache"/>
        public static bool UseInternalCache = false;

        /// <summary>
        /// Clears the contents of the extractor's internal exif tag cache.
        /// </summary>
        /// <seealso cref="UseInternalCache"/>
        /// <seealso cref="SetCacheSize(int)"/>
        public static void ClearCache() => ResolvedExifTagsCache.Clear();

        /// <summary>
        /// Sets the extractor's cache size. The cache stores sets of exif tags for all the files that it has successfully parsed. 
        /// Setting the size controls how many files worth of exif tags the cache will hold.
        /// 
        /// For example, setting the cache size to one will store a maximum of 1 files worth of exif image tags.
        /// </summary>
        /// <param name="size">How many sets of Exif image tags to cache (one set per sucessfully parsed file)</param>
        /// <seealso cref="UseInternalCache"/>
        /// <seealso cref="ClearCache"/>
        public static int CacheSize { get => ResolvedExifTagsCache.Capacity; set => ResolvedExifTagsCache.Capacity = value; }


        /* Tag retrieval methods */

        /// <summary>
        /// Tries to retrieve all image related Exif tags in a Jpg file.
        /// </summary>
        /// <param name="filePath">The Jpg file path to extract the tags from</param>
        /// <param name="entries">The structure used to store the found tags</param>
        /// <returns>If the tags were successfully extracted.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static bool TryGetTags(string filePath, out Dictionary<ushort, ExifEntry> entries)
        {
            // Check cache first
            if (UseInternalCache && CacheSize != 0 && ResolvedExifTagsCache.ContainsKey(filePath))
            {
                entries = ResolvedExifTagsCache.Retrieve(filePath);
                return true;
            }

            entries = new Dictionary<ushort, ExifEntry>();

            BinaryReader? reader = OpenFile(filePath);
            if (reader == null)
            {
                return false;
            }

            using (reader)
            { 
                // First find the Exif segment in the Jpg
                long exifOffset = FindExifDataInJpg(reader);
                if (exifOffset == kInvalidOffset)
                {
                    return false;
                }

                // Next parse the tiff structure that houses the Exif data
                List<TiffEntry> imageEntries = new();
                List<TiffEntry> thumbnailEntries = new();
                if (TryParseTiffStructure(reader, exifOffset, out imageEntries, out thumbnailEntries) == false)
                {
                    return false;
                }

                // Finally resolve the Tiff entries to give us our actual Exif tags
                entries = ResolveTiffEntries(reader, exifOffset, imageEntries);

                if (UseInternalCache && CacheSize != 0)
                {
                    // Cache for later
                    ResolvedExifTagsCache.Add(filePath, entries);
                }

                return true;
            }
        }

        /// <summary>
        /// Tries to retrieve a specific Exif image tag in a Jpg file.
        /// </summary>
        /// <param name="filePath">The path to the file to extract the tag from</param>
        /// <param name="tag">The tag type to look for</param>
        /// <param name="value">The found exif entry</param>
        /// <returns>If the entry with the specific type was found in the file</returns>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static bool TryGetTag(string filePath, ushort tag, out ExifEntry value)
        {
            value = new ExifEntry();

            bool result = TryGetTags(filePath, out Dictionary<ushort, ExifEntry> tags);
            if (result == false || tags.ContainsKey(tag) == false)
            {
                return false;
            }

            value = tags[tag];
            return true;
        }

        /// <summary>
        /// Extracts any Exif image tags in a Jpg file and returns them as a dictionary
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static Dictionary<ushort, ExifEntry> GetTags(string filePath)
        {
            Dictionary<ushort, ExifEntry> entries = new();
            TryGetTags(filePath, out entries);
            return entries;
        }

        /// <summary>
        /// Extracts a specific Exif image tag from a Jpg file
        /// </summary>
        /// <param name="filePath">The path to the file to extract the tag from</param>
        /// <param name="tag">The tag type to look for</param>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static ExifEntry GetTag(string filePath, ushort tag)
        {
            ExifEntry exif = new();
            TryGetTag(filePath, tag, out exif);
            return exif;
        }

        // TODO
        private static bool TryGetThumbnail(string filePath, out byte[] imageData)
        {
            throw new NotSupportedException();
        }


        /* Internal parsing code */

        /// <summary>
        /// Basic method that has common code for verifying and opening a file
        /// </summary>
        private static BinaryReader? OpenFile(string filePath)
        {
            // Is it a jpeg file that exits
            if (File.Exists(filePath) == false || Path.GetExtension(filePath).ToLower() != ".jpg")
            {
                return null;
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
                return null;
            }

            return new BinaryReader(stream);
        }

        /// <summary>
        /// Parse a JPG file and returns the offset of the Exif data inside the file. 
        /// </summary>
        /// <param name="reader">BinaryReader of opened jpg file</param>
        /// <returns>offset of exif data within the jpg file</returns>
        /// <remarks>
        /// -1 is returned if an error occurs during parsing or if the exif data cannot be found.
        /// </remarks>
        private static long FindExifDataInJpg(BinaryReader? reader)
        {
            if (reader == null)
            {
                return kInvalidOffset;
            }

            // Verify jpeg file, should always start with StartOfImage segment
            byte[] segment = reader.ReadBytes(2);
            if (segment[0] != kJpgStartOfImage[0] || segment[1] != kJpgStartOfImage[1]) // TODO: Ew
            {
                Console.WriteLine("[JPG] Encountered incorrect starting segment");
                return kInvalidOffset;
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
                    return kInvalidOffset;
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
                return kInvalidOffset;
            }

            return reader.BaseStream.Position;
        }

        /// <summary>
        /// Parse Tiff data that contains our Exif data. Follows the Tiff 6.0 spec.
        /// </summary>
        /// <param name="reader">Our file reader</param>
        /// <param name="offset">Offset of the the tiff data within readers BaseStream</param>
        /// <param name="imageEntries">Entries related to the file's main image</param>
        /// <param name="thumbnailEntries">Entries related to the file's thumbail image</param>
        /// <returns></returns>
        private static bool TryParseTiffStructure(BinaryReader? reader, long offset, out List<TiffEntry> imageEntries, out List<TiffEntry> thumbnailEntries)
        {
            imageEntries = new List<TiffEntry>();
            thumbnailEntries = new List<TiffEntry>();

            // Sanity check
            if (reader == null)
            {
                return false;
            }

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

        /// <summary>
        /// Retrieves the data for a single tiff entry from within a file
        /// </summary>
        private static ExifEntry? ResolveTiffEntry(BinaryReader reader, long tiffOffset, in TiffEntry entry)
        {
            if (reader == null)
            {
                return null;
            }

            reader.BaseStream.Seek(tiffOffset + entry.ValueOffset, SeekOrigin.Begin);
            return new ExifEntry(entry.Tag, (ExifType)entry.Type, reader.ReadBytes((int)entry.Count * ExifEntry.TypeSizeMap[(ExifType)entry.Type]));
        }

        /// <summary>
        /// Retrieves the data for multiple tiff entries from within a file, at a given offset.
        /// </summary>
        private static Dictionary<ushort, ExifEntry>? ResolveTiffEntries(BinaryReader reader, long tiffOffset, List<TiffEntry> tiffEntries)
        {
            if (reader == null)
            {
                return null;
            }

            // TODO: Use out keyword to avoid initalizing again?
            Dictionary<ushort, ExifEntry> exifEntries = new();

            foreach (var tiffEntry in tiffEntries)
            {
                reader.BaseStream.Seek(tiffOffset + tiffEntry.ValueOffset, SeekOrigin.Begin);
                exifEntries.Add(tiffEntry.Tag, new(tiffEntry.Tag, (ExifType)tiffEntry.Type, reader.ReadBytes((int)tiffEntry.Count * ExifEntry.TypeSizeMap[(ExifType)tiffEntry.Type])));

                Console.WriteLine("Tag=0x{0} Type={1} Count={2} ValueOffset=0x{3}",
                    tiffEntry.Tag.ToString("X4"),
                    tiffEntry.Type,
                    tiffEntry.Count,
                    tiffEntry.ValueOffset.ToString("X8"));
            }

            return exifEntries;
        }
    }
}
