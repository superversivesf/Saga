using System;
using System.Collections.Generic;
using System.Linq;
using Murmur;

namespace SagaDb;

public class KeyMaker
{
    public string AuthorKey(string input)
    {
        // normalize author string then hash
        var _input = input.Replace(" ", string.Empty).Replace(".", string.Empty).ToLower();
        return MakeKey(_input);
    }

    public string BookKey(string input, IEnumerable<string> enumerable)
    {
        // normalize title string then add authors and hash
        var _input = input.Replace(" ", string.Empty).Replace(".", string.Empty).ToLower();
        _input = input + string.Join(string.Empty, enumerable);
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
        var data = input;
        var murmur128 =
            MurmurHash.Create128(managed: false); // returns a 128-bit algorithm using "unsafe" code with default seed
        var hash = murmur128.ComputeHash(data);
        return string.Join(string.Empty, Array.ConvertAll(hash, b => b.ToString("X2")));
    }

    private string MakeKey(string input)
    {
        var data = input.Select(c => (byte)c).ToArray();
        var murmur128 =
            MurmurHash.Create128(managed: false); // returns a 128-bit algorithm using "unsafe" code with default seed
        var hash = murmur128.ComputeHash(data);
        return string.Join(string.Empty, Array.ConvertAll(hash, b => b.ToString("X2")));
    }
}