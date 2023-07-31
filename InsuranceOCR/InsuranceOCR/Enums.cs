using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceOCR
{
    public class Mappings
    {
        public static Dictionary<string, int> monthMappings = new Dictionary<string, int>()
        {
            { "JAN", 1 },
            { "FEB", 2 },
            { "MAR", 3 },
            { "APR", 4 },
            { "MAY", 5 },
            { "JUN", 6 },
            { "JUL", 7 },
            { "AUG", 8 },
            { "SEP", 9 },
            { "OCT", 10 },
            { "NOV", 11 },
            { "DEC", 12 }
        };
    }
    public static class FileExtensions
    {
        public const string Jpg = "jpg";
        public const string Png = "png";
        public const string Tiff = "tiff";
        public const string Pdf = "pdf";
    }
    public class Enums
    {
        public enum HttpMethod
        {
            GET = 0,
            POST = 1
        }
    }
}
