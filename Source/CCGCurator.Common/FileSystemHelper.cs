using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCGCurator.Common
{
    public class FileSystemHelper
    {
        private readonly List<string> invalidFileNames = new List<string>
        {
            "CON",
"PRN",
"AUX",
"NUL",
"COM1",
"COM2",
"COM3",
"COM4",
"COM5",
"COM6",
"COM7",
"COM8",
"COM9",
"LPT1",
"LPT2",
"LPT3",
"LPT4",
"LPT5",
"LPT6",
"LPT7",
"LPT8",
"LPT9"
        };

        public bool IsInvalidFileName(string filename)
        {
            return invalidFileNames.Any(i => i.Equals(filename));
        }
    }
}
