using System.Runtime.InteropServices;
using System.Text;

namespace JpgExifExtractor
{
    internal class Program
    {






        static void Main(string[] args)
        {
            // Do we have any arguments
            if (args.Length == 0)
            {
                return;
            }


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