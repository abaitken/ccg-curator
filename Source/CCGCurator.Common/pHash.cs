using System.IO;
using System.Drawing;
using Shipwreck.Phash;
using Shipwreck.Phash.Bitmaps;
using System;

namespace CCGCurator.Common
{
    public class pHash
    {
        private static readonly ulong m1 = 0x5555555555555555;
        private static readonly ulong m2 = 0x3333333333333333;
        private static readonly ulong h01 = 0x0101010101010101;
        private static readonly ulong m4 = 0x0f0f0f0f0f0f0f0f;

        public ulong ImageHash(string filename)
        {
            var bitmap = (Bitmap)Image.FromFile(filename);
            var hash = ImagePhash.ComputeDctHash(bitmap.ToLuminanceImage());
            return hash;
        }

        public ulong ImageHash(Bitmap bitmap)
        {
            var hash = ImagePhash.ComputeDctHash(bitmap.ToLuminanceImage());
            return hash;
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
