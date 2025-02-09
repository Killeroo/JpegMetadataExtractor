# JpegMetadataExtractor

A small C# library for extracting metadata from JPEG images.

## What does it do?
- Easily and quickly extract thumbnail data and other basic image details.
- Parses and extracts data from image files in milliseconds.
- Almost non existent memory footprint.
- Retrieve and decode [Exif metadata](https://en.wikipedia.org/wiki/Exif).
- Extract raw [Adobe XMP](https://en.wikipedia.org/wiki/Extensible_Metadata_Platform) and [JFIF](https://en.wikipedia.org/wiki/JPEG_File_Interchange_Format) data.

## Remarks
The Jpeg file format is over 30 years old, it has a lot of skeletons in it's closet and there will be files that this library struggles to read. I have done my best to battleharden this library and will continue to update it as I see fit but if you do encounter anything that is struggles to parse please [open an issue](https://github.com/Killeroo/JpegMetadataExtractor/issues) and share the image if at all possible.

_Please bear this in mind when using this library._

## How do I use it?
`JpegMetadataExtractor` is all contained within one source file, [JpegMetadataExtractor.cs](https://github.com/Killeroo/JpgMetadataExtractor/blob/main/JpegMetadataExtractor.cs), which can be grabbed straight from github and be dropped into any C# project. Or you can also download it as a [precompiled dll](https://github.com/Killeroo/JpegMetadataExtractor/releases).

The code is implemented as a netstandard 2.0 compatible library, this was done to ensure maximum compatibility amoung old and new C# projects.

## Getting started
The main component of the library is the `JpegParser` class, this provides all the necessary methods for extracting metadata. 

### Basics
There are a few different ways to use the library. To get some basic information easily accessible information about a file you can use the following method:
```csharp
using JpegMetadataExtractor;

JpegParser.GetSimpleMetadata(@"C:\Path\To\Your\Image.jpg");
```
This will return a small `ImageMetadata` class that contains some basic image information; Image dimensions, Encoding information and other pieces of metadata derived from Exif tags (if they were present in the file).

For a structure containing all metadata that `JpegMetadataExtractor` was able to find you can use the following:
```csharp
JpegParser.GetRawMetadata(@"C:\Path\To\Your\Image.jpg");
```
This will return a class, `RawImageMetadata`, this contains:
- A dictionaries containing Image and Thumbnail Exif data.
- The raw JFIF data (if it was found in the file)
- The raw Adobe XMP data (if it was found in the file)
- Basic image data information found in the `StartOfFrame` segment.

### Embedded thumbnail extraction
It's trivial to extract the thumbnail image embedded in most Jpegs: 
```csharp
byte[] thumbnailData = JpegParser.GetThumbnailData(imagePath);
```
From here it can either be saved directly to a file and opened. Or it can be converted to something more convienient like C#'s `Image` class to be processed or used further:
```
byte[] thumbnailData = JpegParser.GetThumbnailData(imagePath);
if (thumbnailData.Length != 0)
{
    using (MemoryStream stream = new MemoryStream(thumbnailData))
    {
        return Image.FromStream(stream);
    }
}
```
Bear in mind that not all Jpegs have an embedded thumbnail image. In cases like this, the `GetThumbnailData()` will return an empty array.


### Exif specific functionality
The library was primarily intended to extract and decode Exif data so the libaray includes a few methods for doing just that.

You can retrieve a specific tag from the file:
```csharp
ExifEntry entry = JpegParser.GetExifTag("test.jpg", 0x013B /* Artist Tag */);

Console.WriteLine(entry.GetValueAsString());
```

Or you can retrieve all Exif tags found the file:
```csharp
Dictionary<ushort, ExifEntry> exifEntries = JpegParser.GetExifTags("test.jpg");

Console.WriteLine($"Image created on {exifEntries[0x9003 /* Original Creation Date Tag */].GetValueAsString()}"); 
Console.WriteLine($"Last modified {exifEntries[0x0132 /* Modified Date Tag */].GetValueAsString()}");
```

There are also equivalent `TryGetTag()` and `TryGetTags()` methods for handling tags not being present in a file.

#### Exif Entries

All Exif data is returned as individual `ExifEntry` classes. These classes contain the Exif Type and the raw value represented as a byte array.

When working with the `ExifEntry` class you can choose to retrieve the data as it's corresponding type via helper methods (such as `GetValueAsString()`). It should be noted that these methods will only return a valid value if the method that corresponds with the `ExifEntry`'s type is called. For example a tag with a `UShort` exif type (expressed numerically as `2`) will have a value that can be retrieved using `GetValueAsUShort()` and not via a similar numeric method like `GetValueAsULong()`. You can check the type of the ExifEntry using `ExifEntry.Type` or you can look up the type before hand (all exif tags have a pre-determined type). You can also look at the `JpgMetadataExtract.ExifType` enum for the different type values.

### JFIF and Adobe XMP data
As previously mentioned, JpgMetadataExtractor is able to extract JFIF and Adobe XMP metadata. It is not designed to parse it however, the raw data will have to be processed further but can be extracted using the following code:
```csharp
byte[] jfifData = JpegParser.GetRawMetadata("test.jpg").JfifData;
string adobeXmp = JpegParser.GetRawMetadata("text.jpg").AdobeXmpData;
```
It is worth noting that if adobe or jfif data is not present in a file then an empty array and string will be returned respectively.

### Caching 
The library contains a caching system which will retain previously parsed file metadata. This be used if enabled (caching is disabled by default) and if the file has been previously parsed by the library.

Caching can be enabled using the following code:
```csharp
JpegParser.UseInternalCache = true;
JpegParser.CacheSize = 5; /* How many previously parsed RawImageMetadata objects to cache */
```

Cached entries will be automatically used when using the normal parsing methods (`GetExifTag()`, `GetSimpleMetadata()` etc). The cache can be manually cleared via `JpgParser.ClearCache()`.

### Debugging
Some simple debug logging is included in the library, this will print out information as the library traverses the file to STDIO. This helps diagnose potential parsing issues that may occur. It can be enabled by using the following variable:
```
JpegParser.LogMessages = true;
```

### Other
The library has a few other little features that can help with extracting and dealing with Jpg metadata:
- Simple `ApexConverter` for dealing with [APEX](https://en.wikipedia.org/wiki/APEX_system) values in Exif data.
- Implementation of `Rational` and `URational` numbers found in Exif data.
- Ability to walk through image data in file.

# License
```
MIT License

Copyright (c) 2024 Matthew Carney

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
