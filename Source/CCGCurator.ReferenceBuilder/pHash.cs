using System.IO;
using System.Runtime.InteropServices;

namespace CCGCurator.ReferenceBuilder
{
    class pHash
    {
        [DllImport("phash.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ph_dct_imagehash(string file_name, ref ulong Hash);

        public string ImageHash(string filename)
        {
            if (!File.Exists(filename))
                return string.Empty;
            ulong pHash = 0;
            ph_dct_imagehash(filename, ref pHash);
            return pHash.ToString();
        }
    }
}
