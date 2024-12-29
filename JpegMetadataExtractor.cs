/**********************************************************************************
 ***                         JpegMetadataExtractor                              ***
 **********************************************************************************
 *                                                                                *
 * Lightweight (?) library that can extract various types of metadata from        *
 * Jpeg files.                                                                    *
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
using System.IO;

namespace JpegMetadataExtractor
{
    /// <summary>
    /// Simplified data about an image. Constructed using a mixture of different metadata sources within the file.
    /// 
    /// For all retrieved image data RawImageMetadata instead.
    /// </summary>
    /// <see cref="JpegParser.GetMetadata(string)"/>
    /// <seealso cref="RawImageMetadata"/>
    public class ImageMetadata
    {
        // General
        public string Name;
        public string Artist;
        public string Copyright;
        public int Width;
        public int Height;
        public byte BitsPerSample;
        public OrientationType Orientation;
        public byte ColorComponents;
        public bool IsColor;
        public string Software = string.Empty;
        public string Encoding = string.Empty;
        public string CreatedDate = string.Empty;
        public string ModifiedDate = string.Empty;

        // Camera specific
        public string CameraMake = string.Empty;
        public string CameraModel = string.Empty;
        public ulong ISO;
        public URational ExposureTime;
        public double Aperture;
        public double MaxAperture;
        public int FocalLength;
        public ExposureProgram ExposureProgram;
        public string Lens = string.Empty;

        public override string ToString()
        {
            return string.Format(
                "Name: {0}\n" +
                "Width: {1}\n" +
                "Height: {2}\n" +
                "BitsPerSample: {3}\n" +
                "Orientation: {4}\n" +
                "IsColor: {5}\n" +
                "Color Components: {6}\n" +
                "Software: {7}\n" +
                "Encoding Process: {8}\n" +
                "Created: {9}\n" +
                "Modified: {10}\n" +
                "\n" +
                "Camera Make: {11}\n" +
                "Camera Model: {12}\n" +
                "ISO: {13}\n" +
                "Aperture: {14}\n" +
                "Max Aperture: {15}\n" +
                "Focal Length: {16}mm\n" +
                "Exposure Program: {17}\n" +
                "Lens: {18}\n",
                Name,
                Width,
                Height,
                BitsPerSample,
                Orientation,
                IsColor,
                ColorComponents,
                Software,
                Encoding,
                CreatedDate,
                ModifiedDate,
                CameraMake,
                CameraModel,
                ISO,
                Aperture,
                MaxAperture,
                FocalLength,
                ExposureProgram,
                Lens);
        }
    }

    /// <summary>
    /// All metadata extracted from an image. To be processed by the user however they want
    /// </summary>
    /// <see cref="JpegParser.GetRawMetadata(string)"/>
    public class RawImageMetadata
    {
        /// <summary>
        /// All parsed exif values for the main jpeg image found in the file.
        /// </summary>
        public Dictionary<ushort, ExifEntry> ExifImageEntries = new Dictionary<ushort, ExifEntry>();
        /// <summary>
        /// All parsed exif values for the embedded jpeg thumbnail
        /// </summary>
        public Dictionary<ushort, ExifEntry> ExifThumbnailEntries = new Dictionary<ushort, ExifEntry>();

        /// <summary>
        /// The raw AdobeXMP data found in the Jpeg file
        /// </summary>
        public byte[] AdobeXmpData = new byte[0];

        /// <summary>
        /// A subset of the data found in the Jpeg's StartOfFrame segmenet
        /// </summary>
        public StartOfFrameData FrameData;

        /// <summary>
        /// The Jfif data found in the Jpeg
        /// </summary>
        public byte[] JfifData = new byte[0];

        /// <summary>
        /// The thumbnail image data found in the Jpeg
        /// </summary>
        public byte[] ThumbnailData = new byte[0];
    }

    /// <summary>
    /// Utility class for different converting Apex (Additive System of Photographic Exposure) values to human readable numbers
    /// </summary>
    public static class ApexConverter
    {
        /// <summary>
        /// Converts the imputted Apex value to a readable double
        /// </summary>
        /// <param name="apexValue"></param>
        /// <returns></returns>
        public static double ApexToDouble(double apexValue) { return Math.Exp(apexValue) * Math.Log(2.0); }
    }

    /// <summary>
    /// A basic set of the data contains in StartOfFrame (SOF) segments 
    /// </summary>
    public struct StartOfFrameData
    {
        public byte BitPerSample;
        public ushort Height;
        public ushort Width;
        public byte ColorComponents;
        public bool IsColor;
        public string EncodingProcess;
    }

    /// <summary>
    /// Possible Exif orientation values for an image
    /// </summary>
    public enum OrientationType : ushort
    {
        Horizontal,
        MirroredHorizontal,
        Rotated180,
        MirroredVertical,
        MirroredHorizontalAndRotated270Clockwise,
        Rotated90Clockwise,
        MirroredHorizontalAndRotated90Clockwise,
        Rotated270Clockwise
    }

    /// <summary>
    /// Different exposure modes a camera could have been using when reading the ExposureProgram exif entry
    /// </summary>
    public enum ExposureProgram : byte
    {
        NotDefined,
        Manual,
        Program,
        ApeturePriority,
        ShutterSpeedPriority,
        Creative,
        Action,
        Portrait,
        Landscape
    }

    /// <summary>
    /// Just a few exif tags
    /// </summary>
    /// <remarks>
    /// You can find more tags here: https://exiftool.org/TagNames/EXIF.html
    /// </remarks>
    public class ExifTags
    {
        public const ushort Software = 0x0131;
        public const ushort Make = 0x010F;
        public const ushort Model = 0x0110;
        public const ushort Orientation = 0x0112;
        public const ushort ISO = 0x8827;
        public const ushort ExposureTime = 0x829A;
        public const ushort ApertureValue = 0x9202;
        public const ushort MaxAperture = 0x9205;
        public const ushort FocalLength = 0x920A;
        public const ushort FocalLengthIn35mmFormat = 0xA405;
        public const ushort ExposureProgram = 0x8822;
        public const ushort LensModel = 0xA434;
        public const ushort OriginalCreateDate = 0x9003;
        public const ushort ModifyDate = 0x0132;
        public const ushort Copyright = 0x8298;
        public const ushort Artist = 0x013B;
        public const ushort Compression = 0x0103;
        public const ushort ThumbnailLocation = 0x0201;
        public const ushort ThumbnailLength = 0x0202;
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

    public enum JfifDensityUnits : byte
    {
        NoUnits,
        PixelsPerInch,
        PixelsPerCM
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
        public static readonly ExifEntry Empty = default;

        /// <summary>
        /// Simple map that contains the byte length of each different Exif data types.
        /// </summary>
        public static readonly Dictionary<ExifType, byte> TypeSizeMap = new Dictionary<ExifType, byte>()
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
        /// <returns>Byte representation of Exif data or return 0 if exif type is not byte</returns>
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

        /// <summary>
        /// Returns the exif data as a ASCII string.
        /// </summary>
        /// <returns>String representation of Exif data or empty string if exif type is not a string</returns>
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

        /// <summary>
        /// Returns the exif data as a unsigned short.
        /// </summary>
        /// <returns>Unsigned short representation of Exif data or 0 if exif type is not ushort</returns>
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

        /// <summary>
        /// Returns the exif data as a unsigned long.
        /// </summary>
        /// <returns>Unsigned long representation of Exif data or 0 if the exif type is not ulong</returns>
        public uint GetValueAsULong()
        {
            if (Type != ExifType.ULong || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToUInt32(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a unsigned rational.
        /// </summary>
        /// <returns>Unsigned rational representation of Exif data or empty urational if type isn't urational</returns>
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

        /// <summary>
        /// Returns the exif data as a signed byte.
        /// </summary>
        /// <returns>Signed byte representation of Exif data or 0 if exif type isn't sbyte</returns>
        public sbyte GetValueAsSByte()
        {
            if (Type != ExifType.SByte || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return (sbyte)Value[0];
            }
        }

        /// <summary>
        /// Returns the exif data as a signed short.
        /// </summary>
        /// <returns>Signed short representation of Exif data or 0 if exif type isn't short</returns>
        public short GetValueAsShort()
        {
            if (Type != ExifType.Short || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToInt16(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a signed long.
        /// </summary>
        /// <returns>Signed long representation of Exif data or 0 if exif type isn't long</returns>
        public int GetValueAsLong()
        {
            if (Type != ExifType.Long || Value.Length == 0)
            {
                return 0;
            }
            else
            {
                return BitConverter.ToInt32(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a signed rational.
        /// </summary>
        /// <returns>Signed rational representation of Exif data or empty rational if exif type isn't rational</returns>
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

        /// <summary>
        /// Returns the exif data as a float.
        /// </summary>
        /// <returns>Float representation of Exif data or 0.0 if exif type is not a float</returns>
        public float GetValueAsFloat()
        {
            if (Type != ExifType.Float || Value.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                return BitConverter.ToSingle(Value, 0);
            }
        }

        /// <summary>
        /// Returns the exif data as a double.
        /// </summary>
        /// <returns>Double representation of Exif data or 0.0 if exif type isn't a double</returns>
        public double GetValueAsDouble()
        {
            if (Type != ExifType.Double || Value.Length == 0)
            {
                return 0.0;
            }
            else
            {
                return BitConverter.ToDouble(Value, 0);
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

        public readonly int Numerator;
        public readonly int Denominator;

        public Rational(byte[] bytes, int offset)
        {
            Numerator = BitConverter.ToInt32(bytes, offset);
            Denominator = BitConverter.ToInt32(bytes, offset + 4);
        }
        public Rational(int _numerator, int _denominator)
        {
            Numerator = _numerator;
            Denominator = _denominator;
        }

        public int ToInt32()
        {
            return Numerator / Denominator;
        }

        public double ToDouble()
        {
            return (double)Numerator / Denominator;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Numerator, Denominator);
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

        public readonly uint Numerator;
        public readonly uint Denominator;

        public URational(byte[] bytes, int offset)
        {
            Numerator = BitConverter.ToUInt32(bytes, offset);
            Denominator = BitConverter.ToUInt32(bytes, offset + 4);
        }
        public URational(uint _numerator, uint _denominator)
        {
            Numerator = _numerator;
            Denominator = _denominator;
        }

        public uint ToUInt32()
        {
            return Numerator / Denominator;
        }

        public double ToDouble()
        {
            return (double)Numerator / Denominator;
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Numerator, Denominator);
        }
    }

    /// <summary>
    /// The main Jpeg parsing class, capable of extracting different formats of metadata from a file.
    /// </summary>
    /// <see cref="JpegParser.GetMetadata(string)"/>
    /// <see cref="JpegParser.GetRawMetadata(string)"/>
    /// <see cref="JpegParser.GetExifTags(string)"/>
    public static class JpegParser
    {
        // Segment types
        private const byte kJpegStartOfImage = 0xD8;
        private const byte kJpegStartOfScan = 0xDA;
        private const byte kJpegEndOfImage = 0xD9;
        private const byte kJpegExifAppData = 0xE1;
        private const byte kJpegDefineRestartInterval = 0xDD;
        private const byte kJpegJfifAppData = 0xE0;

        // Different StartOfFrame segmenets
        // A different one of these is used per file depending on the type of DCT
        // used in the image (but the metadata stored in them is the same)
        private const byte kJpegStartOfFrame0 = 0xC0;
        private const byte kJpegStartOfFrame1 = 0xC1;
        private const byte kJpegStartOfFrame2 = 0xC2;
        private const byte kJpegStartOfFrame3 = 0xC3;
        private const byte kJpegStartOfFrame5 = 0xC5;
        private const byte kJpegStartOfFrame6 = 0xC6;
        private const byte kJpegStartOfFrame7 = 0xC7;
        private const byte kJpegStartOfFrame9 = 0xC9;
        private const byte kJpegStartOfFrame10 = 0xCA;
        private const byte kJpegStartOfFrame11 = 0xCB;
        private const byte kJpegStartOfFrame13 = 0xCD;
        private const byte kJpegStartOfFrame14 = 0xCE;
        private const byte kJpegStartOfFrame15 = 0xCF;

        // Dictionary of each encoding process.
        // Taken from https://github.com/Matthias-Wandel/jhead
        private static readonly Dictionary<byte, string> JpegEncodingProcesses = new Dictionary<byte, string>()
        {
            { kJpegStartOfFrame0, "Baseline" },
            { kJpegStartOfFrame1, "Extended Sequential" },
            { kJpegStartOfFrame2, "Progressive" },
            { kJpegStartOfFrame3, "Lossless" },
            { kJpegStartOfFrame5, "Differential Sequential" },
            { kJpegStartOfFrame6, "Differential Progressive" },
            { kJpegStartOfFrame7, "Differential Lossless" },
            { kJpegStartOfFrame9, "Extended Sequential, Arithmetic coding" },
            { kJpegStartOfFrame10, "Progressive, Arithmetic coding" },
            { kJpegStartOfFrame11, "Lossless, Arithmetic coding" },
            { kJpegStartOfFrame13, "Differential Sequential, Arithmetic coding" },
            { kJpegStartOfFrame14, "Differential Progressive, Arithmetic coding" },
            { kJpegStartOfFrame15, "Differential Lossless, Arithmetic coding" },
        };

        // Identifiers for different data that can live in the Exif data segment
        private const uint kExifAppIdentifier = 0x66697845;
        private const uint kAdobeXmpAppIdentifier = 0x70747468;

        // Tiff constants
        private const ushort kTiffIntelAligned = 0x4949;
        private const ushort kTiffMotorolaAligned = 0x4D4D;
        private const ushort kTiffExifSubIFDTag = 0x8769;

        // Signals that restart markers/blocks are present in the file. These need to be ignored while parsing.
        // These are blocks mark spots for parrellel processing, something we don't do.
        private static bool _restartMarkersPresent = false;


        /* Settings */

        // Specifies whether the extractor will parse through image data in a file
        // instead of skipping to the end of the file when it is encountered.
        // This will be marginally slower as there is no benefit in this case to parsing the data.
        public static bool ParseImageData = false;

        // If enabled, logging messages will be sent to STDIO
        public static bool LogMessages = false;

        /// <summary>
        /// Simple representation of a Tiff entry. Exif data is stored using tiff and so these entries are used to store references to Exif data.
        /// </summary>
        private struct TiffEntry
        {
            public ushort Tag;
            public ushort Type;
            public uint Count;
            public uint ValueOffset;
        }


        /* Cache controls */

        /// <summary>
        /// A fixed size cache of previously processed objects..
        /// </summary>
        private class ObjectCache<T>
        {
            // This could actually be done using an OrderedDictionary but hey
            private Queue<string> _keys = new Queue<string>();
            private Dictionary<string, T> _cache = new Dictionary<string, T>();

            private int _maxCacheSize = 1;

            public int Count => _cache.Count;

            public void Clear()
            {
                _keys.Clear();
                _cache.Clear();
            }

            public int Capacity {
                get => _maxCacheSize;
                set {
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

            public bool ContainsKey(string key)
            {
                return _cache.ContainsKey(key);
            }

            public T Retrieve(string key)
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                return default(T);
            }

            public void Add(string key, T @object)
            {
                if (_cache.Count == _maxCacheSize)
                {
                    _cache.Remove(_keys.Dequeue());
                }

                _cache.Add(key, @object);
                _keys.Enqueue(key);
            }
        }

        private static ObjectCache<RawImageMetadata> RawMetadataCache = new ObjectCache<RawImageMetadata>();
        private static ObjectCache<ImageMetadata> ImageMetadataCache = new ObjectCache<ImageMetadata>();

        /// <summary>
        /// Wether of not to use the extractor's internal cache of previously parsed metadata. This cache stores sets metadata for each file that has been successfully parsed.
        /// </summary>
        /// <seealso cref="SetCacheSize(int)"/>
        /// <seealso cref="ClearCache"/>
        public static bool UseInternalCache = false;

        /// <summary>
        /// Clears the contents of the extractor's internal metadata cache.
        /// </summary>
        /// <seealso cref="UseInternalCache"/>
        /// <seealso cref="SetCacheSize(int)"/>
        public static void ClearCache() { RawMetadataCache.Clear(); ImageMetadataCache.Clear(); }

        /// <summary>
        /// Sets the extractor's cache size. The cache stores sets of metadata for all the files that it has successfully parsed. 
        /// Setting the size controls how many file's metadata the cache will hold.
        /// 
        /// For example, setting the cache size to one will store a the metadata of 1 file at a time.
        /// </summary>
        /// <param name="size">How much metadata to cache (one per sucessfully parsed file)</param>
        /// <seealso cref="UseInternalCache"/>
        /// <seealso cref="ClearCache"/>
        public static int CacheSize { get { return RawMetadataCache.Capacity; } set { RawMetadataCache.Capacity = value; ImageMetadataCache.Capacity = value; } }


        /* Metadata retrieval methods */

        /// <summary>
        /// Returns a set of simplified, immediately accessible metadata from a file.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segmenet</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <remarks>
        /// Use GetExifTag or GetRawMetadata for accessible to all metadata that we were able to parse from the file.
        /// </remarks>
        public static ImageMetadata GetMetadata(string filePath)
        {
            return RetieveImageMetadata(filePath);
        }

        /// <summary>
        /// Retrieves a set of raw (sometimes unparsed) metadata found within a jpg file.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segmenet</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        public static RawImageMetadata GetRawMetadata(string filePath)
        {
            return RetrieveRawMetadata(filePath);
        }

        /// <summary>
        /// Tries to retrieve all image related Exif tags in a Jpeg file.
        /// </summary>
        /// <param name="filePath">The Jpeg file path to extract the tags from</param>
        /// <param name="entries">The structure used to store the found tags</param>
        /// <returns>If the tags were successfully extracted.</returns>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segmenet</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static bool TryGetExifTags(string filePath, out Dictionary<ushort, ExifEntry> entries)
        {
            RawImageMetadata metadata = RetrieveRawMetadata(filePath);
            entries = metadata.ExifImageEntries;

            return entries.Count > 0;
        }

        /// <summary>
        /// Tries to retrieve a specific Exif image tag in a Jpeg file.
        /// </summary>
        /// <param name="filePath">The path to the file to extract the tag from</param>
        /// <param name="tag">The tag type to look for</param>
        /// <param name="value">The found exif entry</param>
        /// <returns>If the entry with the specific type was found in the file</returns>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segmenet</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static bool TryGetExifTag(string filePath, ushort tag, out ExifEntry value)
        {
            value = new ExifEntry();

            RawImageMetadata metadata = RetrieveRawMetadata(filePath);
            Dictionary<ushort, ExifEntry> exifEntries = metadata.ExifImageEntries;
            if (exifEntries.Count == 0 || exifEntries.ContainsKey(tag) == false)
            {
                return false;
            }

            value = exifEntries[tag];
            return true;
        }

        /// <summary>
        /// Extracts any Exif image tags in a Jpeg file and returns them as a dictionary
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segmenet</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static Dictionary<ushort, ExifEntry> GetExifTags(string filePath)
        {
            Dictionary<ushort, ExifEntry> entries = new Dictionary<ushort, ExifEntry>();
            TryGetExifTags(filePath, out entries);
            return entries;
        }

        /// <summary>
        /// Extracts a specific Exif image tag from a Jpeg file
        /// </summary>
        /// <param name="filePath">The path to the file to extract the tag from</param>
        /// <param name="tag">The tag type to look for</param>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segment</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <remarks>
        /// The Exif entries for a file can be cached once the file is parsed, this is done to make retrieving entries quick. You can set the cache size, reset it's contents or disable it completely 
        /// via CacheSize, ResetCache and UseInternalCache respectively.
        /// </remarks>
        public static ExifEntry GetExifTag(string filePath, ushort tag)
        {
            ExifEntry exif = new ExifEntry();
            TryGetExifTag(filePath, tag, out exif);
            return exif;
        }

        /// <summary>
        /// Attempt to retrieve the thumbnail image data embedded in a given Jpeg. 
        /// </summary>
        /// <param name="filePath">The path to the Jpeg file</param>
        /// <param name="thumbnailImageData">The array to copy the found thumbnail image data into</param>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segment</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <returns>returns whether any thumbnail was found</returns>
        public static bool TryGetThumbnailData(string filePath, out byte[] thumbnailImageData)
        {
            thumbnailImageData = GetThumbnailData(filePath);
            return thumbnailImageData.Length != 0;
        }

        /// <summary>
        /// Extracts the embedded thumbnail of a Jpeg file at a given path.
        /// </summary>
        /// <param name="filePath">The path to the Jpeg file</param>
        /// <exception cref="ObjectDisposedException">Thrown if an object used during parsing is null. (whoops)</exception>
        /// <exception cref="IOException">Thrown if an error occurs while trying to read from the file stream.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the end of the file is unexpectedly reached.</exception>
        /// <exception cref="JpegParsingException">Thrown when an error occurs while navigating the Jpeg's structure</exception>
        /// <exception cref="JfifParsingException">Thrown if an error occurs while trying to pass the Jpeg Jfif segment</exception>
        /// <exception cref="ExifParsingException">Thrown if an error occurs while parsing the Jpeg Exif data</exception>
        /// <returns>The raw thumbnail image data</returns>
        public static byte[] GetThumbnailData(string filePath)
        {
            RawImageMetadata metadata = RetrieveRawMetadata(filePath);
            return metadata.ThumbnailData;
        }

        /// <summary>
        /// Parsed the provided file for metadata or retrieve previously parsed data from the internal cache.
        /// </summary>
        private static RawImageMetadata RetrieveRawMetadata(string filePath)
        {
            // Check cache first
            if (UseInternalCache && CacheSize != 0 && RawMetadataCache.ContainsKey(filePath))
            {
                return RawMetadataCache.Retrieve(filePath);
            }

            RawImageMetadata rawImageMetadata = ParseJpegFile(filePath);
            if (UseInternalCache && CacheSize != 0)
            {
                // Cache for later
                RawMetadataCache.Add(filePath, rawImageMetadata);
            }

            return rawImageMetadata;
        }

        /// <summary>
        /// Creates an ImageMetadata object from the RawImageMetadata of the image, or retrieve a previously created version.
        /// </summary>
        private static ImageMetadata RetieveImageMetadata(string filePath)
        {
            // Try and retrieve it from the cache
            if (UseInternalCache && CacheSize != 0 && ImageMetadataCache.ContainsKey(filePath))
            {
                return ImageMetadataCache.Retrieve(filePath);
            }

            // Alright we have to create it from the RawImageMetadata
            RawImageMetadata rawMetadata = RetrieveRawMetadata(filePath);
            ImageMetadata imageMetadata = new ImageMetadata();
            imageMetadata.Name = Path.GetFileName(filePath);

            // First get fill out the values that we can only get from exif
            // TODO: Try and get width here as well
            if (rawMetadata.ExifImageEntries.Count != 0)
            {
                ExifEntry entry;
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Software, out entry))
                {
                    imageMetadata.Software = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Make, out entry))
                {
                    imageMetadata.CameraMake = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Model, out entry))
                {
                    imageMetadata.CameraModel = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Orientation, out entry))
                {
                    imageMetadata.Orientation = (OrientationType)entry.GetValueAsUShort();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.ISO, out entry))
                {
                    imageMetadata.ISO = entry.GetValueAsUShort();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.ExposureTime, out entry))
                {
                    imageMetadata.ExposureTime = entry.GetValueAsURational();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.ApertureValue, out entry))
                {
                    imageMetadata.Aperture = ApexConverter.ApexToDouble(entry.GetValueAsURational().ToDouble()) * 0.5;
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.MaxAperture, out entry))
                {
                    imageMetadata.MaxAperture = ApexConverter.ApexToDouble(entry.GetValueAsURational().ToDouble()) * 0.5;
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.FocalLengthIn35mmFormat, out entry))
                {
                    imageMetadata.FocalLength = entry.GetValueAsUShort();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.ExposureProgram, out entry))
                {
                    imageMetadata.ExposureProgram = (ExposureProgram)entry.GetValueAsUShort();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.LensModel, out entry))
                {
                    imageMetadata.Lens = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.OriginalCreateDate, out entry))
                {
                    imageMetadata.CreatedDate = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.ModifyDate, out entry))
                {
                    imageMetadata.ModifiedDate = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Artist, out entry))
                {
                    imageMetadata.Artist = entry.GetValueAsString();
                }
                if (rawMetadata.ExifImageEntries.TryGetValue(ExifTags.Copyright, out entry))
                {
                    imageMetadata.Copyright = entry.GetValueAsString();
                }
            }

            // Next try and get some values from the SOF segment, this has the best basic image data
            imageMetadata.Width = rawMetadata.FrameData.Width;
            imageMetadata.Height = rawMetadata.FrameData.Height;
            imageMetadata.BitsPerSample = rawMetadata.FrameData.BitPerSample;
            imageMetadata.Encoding = rawMetadata.FrameData.EncodingProcess;
            imageMetadata.ColorComponents = rawMetadata.FrameData.ColorComponents;
            imageMetadata.IsColor = rawMetadata.FrameData.IsColor;

            if (UseInternalCache && CacheSize != 0)
            {
                // Cache for later
                ImageMetadataCache.Add(filePath, imageMetadata);
            }

            return imageMetadata;
        }


        /* Internal parsing code */

        /// <summary>
        /// Parses a JPG file, processing each relevant segment that contains metadata and returning an object containing all the 
        /// retrieved data.
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Return a RawImageMetadata object that contains all the metadata that was parsed</returns>
        private static RawImageMetadata ParseJpegFile(string filePath)
        {
            RawImageMetadata metadata = new RawImageMetadata();

            // Open file 
            FileStream stream = new FileStream(filePath, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);

            // Verify jpeg file, should always start with StartOfImage segment
            byte[] segment = reader.ReadBytes(2);
            if (segment[0] != 0xFF || segment[1] != kJpegStartOfImage)
            {
                throw new JpegParsingException($"Encountered incorrect starting segment, expected StartOfImage segment (0xFF 0xD8) instead found segment ({segment[0]} {segment[1]})");
            }

            // Loop through segments processing the appropriate ones as we go along
            byte marker, type;
            long pos;
            ushort size;
            bool parsing = true;
            while (parsing)
            {
                // Read segment type
                marker = reader.ReadByte();
                if (marker != 0xFF)
                {
                    throw new JpegParsingException($"Found incorrect segment marker ({marker}) @ 0x{(reader.BaseStream.Position - 1).ToString("X6")}");
                }
                type = reader.ReadByte();
                pos = reader.BaseStream.Position;

                // Check if we have reached the last segment
                if (type == kJpegEndOfImage)
                {
                    LogMessage("JPG", "Reached end of image");
                    break;
                }

                // Determine the size of the segment
                if (type != kJpegStartOfScan && type != kJpegEndOfImage)
                {
                    size = reader.ReadUInt16(true);
                }
                else
                {
                    // Segments that don't include the size are either just the marker (which we have already skipped)
                    // or require special parsing (like the StartOfScan segment). In the later case we have to
                    // assume that our parsing code will return us to just after the segment.
                    size = 0;
                }

                // Process segment (if we care about it or it's contents)
                switch (type)
                {
                    case kJpegDefineRestartInterval:
                        {
                            // This alerts future parsing code that we need to watch out for restart markers
                            _restartMarkersPresent = true;
                            break;
                        }

                    case kJpegExifAppData:
                        {
                            // Multiple different things use this segment
                            uint segmentSignature = BitConverter.ToUInt32(reader.PeakBytes(4), 0);
                            switch (segmentSignature)
                            {
                                case kExifAppIdentifier: ProcessExifSegment(reader, reader.BaseStream.Position, metadata); break;
                                case kAdobeXmpAppIdentifier: ProcessAdobeXmpSegment(reader, size, metadata); break;
                            }
                            break;
                        }

                    case kJpegStartOfFrame0:
                    case kJpegStartOfFrame1:
                    case kJpegStartOfFrame2:
                    case kJpegStartOfFrame3:
                    case kJpegStartOfFrame5:
                    case kJpegStartOfFrame6:
                    case kJpegStartOfFrame7:
                    case kJpegStartOfFrame9:
                    case kJpegStartOfFrame10:
                    case kJpegStartOfFrame11:
                    case kJpegStartOfFrame13:
                    case kJpegStartOfFrame14:
                    case kJpegStartOfFrame15:
                        {
                            ProcessStartOfFrameSegment(reader, size, type, metadata);
                            break;
                        }

                    case kJpegJfifAppData:
                        {
                            ProcessJfifSegment(reader, size, metadata);
                            break;
                        }

                    case kJpegStartOfScan:
                        {
                            if (ParseImageData)
                            {
                                ProcessStartOfScanSegment(reader);
                            }
                            else
                            {
                                // We basically bail once we hit image data. Parsing it does no benefit and it
                                // is normally at the end of the file so there is no more juicy metadata to read after it.
                                parsing = false;
                            }
                            break;
                        }
                }

                // Jump over the segment after we have processed it
                if (size != 0)
                {
                    reader.BaseStream.Seek(pos + size, SeekOrigin.Begin);
                }

                LogMessage("JPG", "[0x{2}] [Segment] Marker=0x{0} Type=0x{1} size={2}", marker.ToString("X"), type.ToString("X"), pos.ToString("X6"), size == 0 ? "-" : size.ToString());
            }

            // Cleanup
            stream.Close();
            reader.Close();

            return metadata;
        }

        /// <summary>
        /// Parse Exif segment in Jpeg file.
        /// 
        /// Exif data is stored in a Tiff structure. Entries referencing the data (it's position, type and length) are parsed first. Those are then resolved and the 
        /// Exif data is returned.
        /// </summary>
        private static void ProcessExifSegment(BinaryReader reader, long segmentOffset, RawImageMetadata outMetadata)
        {
            // Sanity check
            if (reader == null || reader.BaseStream == null)
            {
                return;
            }

            // Exif header
            string exifIdentifier = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (exifIdentifier != "Exif" || reader.ReadInt16() != 0)
            {
                throw new ExifParsingException("Could not parse Exif header");
            }

            // Tiff header
            long tiffOffset = reader.BaseStream.Position;
            ushort byteAlignment = reader.ReadUInt16();
            if (byteAlignment != kTiffIntelAligned && byteAlignment != kTiffMotorolaAligned)
            {
                throw new ExifParsingException("Did not recognise Tiff byte alignment");
            }
            ushort byteAlignmentTest = reader.ReadUInt16();
            if ((byteAlignment == kTiffIntelAligned && byteAlignmentTest != 0x2A00)
                && (byteAlignment == kTiffMotorolaAligned && byteAlignmentTest != 0x002A))
            {
                throw new ExifParsingException("Tiff byte alignment test failed");
            }
            bool reverseOrder = byteAlignment == kTiffMotorolaAligned;
            uint firstIfdOffset = reader.ReadUInt32(reverseOrder);

            // Parse tiff image file directory (IFD) structure
            uint ParseImageFileDirectory(long ifdOffset, ref List<TiffEntry> outEntries)
            {
                reader.BaseStream.Seek(tiffOffset + ifdOffset, SeekOrigin.Begin);
                ushort entries = reader.ReadUInt16(reverseOrder);
                LogMessage("EXIF|TIFF", "IFD: loc={1} count={0}", entries, ifdOffset.ToString("x8"));

                for (int i = 0; i < entries; i++)
                {
                    TiffEntry entry = new TiffEntry();
                    entry.Tag = reader.ReadUInt16(reverseOrder);
                    entry.Type = reader.ReadUInt16(reverseOrder);
                    entry.Count = reader.ReadUInt32(reverseOrder);
                    entry.ValueOffset = reader.ReadUInt32(reverseOrder);
                    LogMessage("EXIF|TIFF", "Entry: tag=0x{0} type={1} count={2} value_offset={3}", entry.Tag.ToString("X4"), entry.Type, entry.Count, entry.ValueOffset.ToString("X8"));

                    outEntries.Add(entry);

                    // Check if we found the Exif SubIFD, that contains those good good tags
                    // so recurse back into it to parse it's contents
                    if (outEntries[outEntries.Count - 1].Tag == kTiffExifSubIFDTag)
                    {
                        long subTagPosition = reader.BaseStream.Position;
                        ParseImageFileDirectory(outEntries[outEntries.Count - 1].ValueOffset, ref outEntries);
                        reader.BaseStream.Seek(subTagPosition, SeekOrigin.Begin);
                    }
                }

                // Return offset to next IFD
                return reader.ReadUInt32(reverseOrder);
            }

            // Exif only uses 2 image directories (and one exif sub directory) so we can unroll any loop that we would
            // normally need for parsing tiffs in this case
            List<TiffEntry> imageEntries = new List<TiffEntry>();
            List<TiffEntry> thumbnailEntries = new List<TiffEntry>();
            long thumbnailIfdOffset = ParseImageFileDirectory(firstIfdOffset, ref imageEntries);
            if (thumbnailIfdOffset != 0)
            {
                ParseImageFileDirectory(thumbnailIfdOffset, ref thumbnailEntries);
            }

            // Retrieve exif data from a list tiff entries
            Dictionary<ushort, ExifEntry> ResolveTiffEntries(List<TiffEntry> entries)
            {
                // Look up and store the image exif data
                Dictionary<ushort, ExifEntry> exifEntries = new Dictionary<ushort, ExifEntry>();
                foreach (var tiffEntry in entries)
                {
                    int exifDataLength = (int)tiffEntry.Count * ExifEntry.TypeSizeMap[(ExifType)tiffEntry.Type];

                    byte[] data;
                    if (exifDataLength <= 4)
                    {
                        // If the data is less than 4 bytes, the values offset IS the value
                        data = BitConverter.GetBytes(tiffEntry.ValueOffset);
                    }
                    else
                    {
                        // Else we have to go to specified offset to find the data
                        reader.BaseStream.Seek(tiffOffset + tiffEntry.ValueOffset, SeekOrigin.Begin);
                        data = reverseOrder ? reader.ReadBytes(exifDataLength).Reverse().ToArray() : reader.ReadBytes(exifDataLength);
                    }

                    exifEntries.Add(tiffEntry.Tag, new ExifEntry(tiffEntry.Tag, (ExifType)tiffEntry.Type, data));

                    // LogMessage() already checks for LogMessages but the formatting of this log can be pretty expensive so
                    // just don't do it if we aren't logging messages
                    if (LogMessages)
                    {
                        LogMessage("EXIF", "Tag=0x{0} Type={1} Count={2} ValueOffset=0x{3}: [Len={4} Offset={5}] ",
                            tiffEntry.Tag.ToString("X4"),
                            (ExifType)tiffEntry.Type,
                            tiffEntry.Count,
                            tiffEntry.ValueOffset.ToString("X8"),
                            exifDataLength,
                            tiffEntry.Count <= 4 ? "-" : (segmentOffset + tiffEntry.ValueOffset).ToString());
                    }
                }
                return exifEntries;
            }

            // Resolve and store the exif entries 
            outMetadata.ExifImageEntries = ResolveTiffEntries(imageEntries);
            outMetadata.ExifThumbnailEntries = ResolveTiffEntries(thumbnailEntries);

            // Whats this? I sniff an embedded preview thumbnail...
            if (outMetadata.ExifThumbnailEntries.ContainsKey(ExifTags.ThumbnailLocation) &&
                outMetadata.ExifThumbnailEntries.ContainsKey(ExifTags.ThumbnailLength))
            {
                long offset = outMetadata.ExifThumbnailEntries[ExifTags.ThumbnailLocation].GetValueAsULong();
                long length = outMetadata.ExifThumbnailEntries[ExifTags.ThumbnailLength].GetValueAsULong();

                outMetadata.ThumbnailData = new byte[length];
                reader.BaseStream.Seek(tiffOffset + offset, SeekOrigin.Begin);
                outMetadata.ThumbnailData = reader.ReadBytes((int)length);

                LogMessage("EXIF", "Read thumbnail data [Len={0} Offset={1}] ", length, offset);
            }
        }

        /// <summary>
        /// Processes the Adobe Xmp data segment.
        /// 
        /// Data is not parsed by is stored if it is required later.
        /// </summary>
        private static void ProcessAdobeXmpSegment(BinaryReader reader, ushort size, RawImageMetadata outMetadata)
        {
            // Sanity check
            if (reader == null || reader.BaseStream == null)
            {
                return;
            }

            // Copy the Adobe XMP chunk and save it for later
            outMetadata.AdobeXmpData = reader.ReadBytes(size);

            LogMessage("XMP", "Extracted XMP contents");
        }

        /// <summary>
        /// Partially processes Jfif data segment.
        /// 
        /// Stores basic image info, saving the rest to be parsed later if required.
        /// </summary>
        private static void ProcessJfifSegment(BinaryReader reader, ushort size, RawImageMetadata outMetadata)
        {
            // Sanity check
            if (reader == null || reader.BaseStream == null)
            {
                return;
            }

            // We don't need to parse it if we aren't logging out info, there are more reliable ways to get the
            // the data that is stored here
            if (LogMessages)
            {
                // Crudely read the first part of the APP0 segment (we don't care about the rest of the Jfif data)
                string jfifIdentifier = Encoding.ASCII.GetString(reader.ReadBytes(5));
                if (jfifIdentifier != "JFIF\0")
                {
                    throw new JfifParsingException("Could not parse Jfif data");
                }
                float version = reader.ReadByte();
                version += reader.ReadByte() / 100; // This is fine?
                JfifDensityUnits density = (JfifDensityUnits)reader.ReadByte();
                ushort densityX = reader.ReadUInt16(true);
                ushort densityY = reader.ReadUInt16(true);

                LogMessage("JFIF", "Parsed JFIF data: Version={0} DensityUnits={1} DensityX={2} DensityY={3}", version, density, densityX, densityY);
            }

            // Save the rest of the data
            outMetadata.JfifData = new byte[size];
            reader.BaseStream.Read(outMetadata.JfifData, 0, size);
        }

        /// <summary>
        /// Parses StartOfFrame segment (regardless of image type).
        /// 
        /// Stores basic image info (dimensions and color info).
        /// </summary>
        private static void ProcessStartOfFrameSegment(BinaryReader reader, ushort size, byte markerType, RawImageMetadata metadata)
        {
            if (reader == null || reader.BaseStream == null || size < 6)
            {
                return;
            }

            metadata.FrameData.BitPerSample = reader.ReadByte();
            metadata.FrameData.Height = reader.ReadUInt16(true);
            metadata.FrameData.Width = reader.ReadUInt16(true);
            metadata.FrameData.ColorComponents = reader.ReadByte();
            metadata.FrameData.IsColor = metadata.FrameData.ColorComponents == 3;

            string encoding = "Unknown";
            if (JpegEncodingProcesses.ContainsKey(markerType))
            {
                encoding = JpegEncodingProcesses[markerType];
            }
            metadata.FrameData.EncodingProcess = encoding;

            LogMessage("SOF", "StartOfFrame data: BitsPerSample={0} Height={1} Width={2} ColorComponent={3} Encoding=\"{4}\"",
                metadata.FrameData.BitPerSample,
                metadata.FrameData.Height,
                metadata.FrameData.Width,
                metadata.FrameData.ColorComponents,
                metadata.FrameData.EncodingProcess);
        }

        /// <summary>
        /// Parses StartOfScan segment, this segment contains the raw image data. The method will read to the end of the data, avoiding stuffed bytes and restart
        /// markers. It will set the BinaryReader.BaseStream.Position to be just before the next segment marker.
        /// </summary>
        /// <param name="reader"></param>
        private static void ProcessStartOfScanSegment(BinaryReader reader)
        {
            if (reader == null || reader.BaseStream == null)
            {
                return;
            }

            const long kMaxBufferSize = 8;

            long pos = reader.BaseStream.Position;
            long end = reader.BaseStream.Length;
            byte[] buffer = new byte[kMaxBufferSize];
            long bufferSize = 0;
            bool foundMarkerByte = false;

            while (true)
            {
                if (pos >= end)
                {
                    break;
                }

                bufferSize = Math.Min(kMaxBufferSize, end - pos);
                reader.BaseStream.Read(buffer, 0, (int)bufferSize);
                for (int i = 0; i < bufferSize; i++)
                {
                    if (foundMarkerByte && buffer[i] != 0x00 && IsRestartMarker(buffer[i]) == false)
                    {
                        // Found next segment
                        break;
                    }

                    foundMarkerByte = buffer[i] == 0xFF;
                }

                pos += bufferSize;
            }

            reader.BaseStream.Position = pos - 2; // We already overlapped the next marker so rewind a little bit
            LogMessage("SOS", "End of image data @ {0}", reader.BaseStream.Position.ToString());
        }

        /// <summary>
        /// A simple method to check if a marker type is a restart marker.
        /// We want to check for these as if they are used in the file, they should be skipped.
        /// </summary>
        /// <param name="marker">The marker type to check</param>
        /// <returns>If the marker type is a return marker</returns>
        private static bool IsRestartMarker(ushort marker)
        {
            return _restartMarkersPresent
                && marker == 0xD0
                || marker == 0XD1
                || marker == 0XD2
                || marker == 0XD3
                || marker == 0XD4
                || marker == 0XD5
                || marker == 0XD6
                || marker == 0XD7;
        }

        /// <summary>
        /// Small utility function that outputs formatted log message to standard out (if LogMessages is set to true)
        /// </summary>
        private static void LogMessage(string category, string format, params object[] args)
        {
            if (LogMessages)
            {
                Console.WriteLine(string.Format("[{0}] ", category) + format, args);
            }
        }
    }

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

        /// <summary>
        /// Reads the next bytes in the BinaryReader as a uint, reversing the byte order if specified
        /// </summary>
        public static uint ReadUInt32(this BinaryReader reader, bool reverseOrder)
        {
            uint value = reader.ReadUInt32();
            if (reverseOrder)
            {
                value = (value & 0x000000FFU) << 24 |
                        (value & 0x0000FF00U) << 8 |
                        (value & 0x00FF0000U) >> 8 |
                        (value & 0xFF000000U) >> 24;
            }
            return value;
        }

        /// <summary>
        /// Reads the next bytes in the BinaryReader as a ushort, reversing the byte order if specified
        /// </summary>
        public static ushort ReadUInt16(this BinaryReader reader, bool reverseOrder)
        {
            ushort value = reader.ReadUInt16();
            if (reverseOrder)
            {
                value = (ushort)((value & 0x00FFU) << 8 | (value & 0x0FF00U) >> 8);
            }
            return value;
        }
    }

    /// <summary>
    /// Exception representing errors encountered while traversing Jpeg file
    /// </summary>
    public class JpegParsingException : Exception
    {
        public JpegParsingException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception representing errors encountered while parsing Exif data
    /// </summary>
    public class ExifParsingException : Exception
    {
        public ExifParsingException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception representing errors encountered while parsing Jfif data
    /// </summary>
    public class JfifParsingException : Exception
    {
        public JfifParsingException(string message) : base(message) { }
    }
}