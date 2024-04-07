using System;
using System.Collections.Generic;
using System.Linq;
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
        public string Tag;
        public ExifType Type;
        public object Value;
    }
}
