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

            ExifExtractor.GetTags(args[0], out var tags);

        }
    }
}