using System;
using System.Security.Cryptography;
using System.Text;

namespace RandomStringGenerator;

public class StringGenerator
{
    internal static readonly char[] chars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

    public static string GetUniqueString(int size)
    {
        var data = RandomNumberGenerator.GetBytes(4 * size);
        var result = new StringBuilder(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }

    public static string GetUniqueKeyOriginal_BIASED(int size)
    {
        var chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        var data = RandomNumberGenerator.GetBytes(size);
        var result = new StringBuilder(size);
        foreach (var b in data) result.Append(chars[b % chars.Length]);
        return result.ToString();
    }
}