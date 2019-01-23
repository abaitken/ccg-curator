using System.IO;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace CCGCurator.Common
{
    public class pHash
    {
        public pHash()
        {
            temporaryFileManager = new TemporaryFileManager();
        }

        [DllImport("pHash.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ph_dct_imagehash(string file_name, ref ulong Hash);

        private static readonly ulong m1 = 0x5555555555555555;
        private static readonly ulong m2 = 0x3333333333333333;
        private static readonly ulong h01 = 0x0101010101010101;
        private static readonly ulong m4 = 0x0f0f0f0f0f0f0f0f;
        private TemporaryFileManager temporaryFileManager;

        public ulong ImageHash(Bitmap bitmap)
        {
            var filepath = temporaryFileManager.GetTemporaryFileName(".jpeg");
            bitmap.Save(filepath, ImageFormat.Jpeg);
            ulong phash = 0;
            ph_dct_imagehash(filepath, ref phash);
            return phash;
        }


        // Calculate the similarity between two hashes
        public int HammingDistance(ulong hash1, ulong hash2)
        {
            var x = hash1 ^ hash2;


            x -= (x >> 1) & m1;
            x = (x & m2) + ((x >> 2) & m2);
            x = (x + (x >> 4)) & m4;
            var res = (x * h01) >> 56;

            return (int)res;
        }
        
    }
}
