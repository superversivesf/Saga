using Murmur;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SagaDb
{
    public class KeyMaker
    {
        public KeyMaker()
        {

        }

        public string AuthorKey(string input)
        {
            // normalize author string then hash
            var _input = input.Replace(" ", String.Empty).Replace(".", String.Empty).ToLower();
            return MakeKey(_input);
        }

        public string BookKey(string input, IEnumerable<string> enumerable)
        {
            // normalize title string then add authors and hash
            var _input = input.Replace(" ", String.Empty).Replace(".", String.Empty).ToLower();
            _input = input + String.Join(String.Empty, enumerable);
            return MakeKey(_input);
        }

        public string SeriesKey(string input)
        {
            return MakeKey(input);
        }

        public string GenreKey(string input)
        {
            return MakeKey(input);
        }

        public string FileKey(string input)
        {
            return MakeKey(input);
        }

        private string MakeKeyFromBytes(byte[] input)
        {
            byte[] data = input;
            Murmur128 murmur128 = MurmurHash.Create128(managed: false); // returns a 128-bit algorithm using "unsafe" code with default seed
            byte[] hash = murmur128.ComputeHash(data);
            return string.Join(string.Empty, Array.ConvertAll(hash, b => b.ToString("X2")));
        }

        private string MakeKey(string input)
        {
            byte[] data = input.Select(c => (byte)c).ToArray();
            Murmur128 murmur128 = MurmurHash.Create128(managed: false); // returns a 128-bit algorithm using "unsafe" code with default seed
            byte[] hash = murmur128.ComputeHash(data);
            return string.Join(string.Empty, Array.ConvertAll(hash, b => b.ToString("X2")));
        }
    }
}
