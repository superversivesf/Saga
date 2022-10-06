using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SagaUtil
{
    public class UserHelper
    {
        public static string GenerateSaltedHash(string plainText, string salt)
        {
            HashAlgorithm algorithm = SHA256.Create();
            var _password = Encoding.ASCII.GetBytes(plainText);
            var _salt = Encoding.ASCII.GetBytes(salt);

            byte[] plainTextWithSaltBytes =
              new byte[_password.Length + _salt.Length];

            for (int i = 0; i < _password.Length; i++)
            {
                plainTextWithSaltBytes[i] = _password[i];
            }
            for (int i = 0; i < _salt.Length; i++)
            {
                plainTextWithSaltBytes[_password.Length + i] = _salt[i];
            }

            return System.Text.Encoding.UTF8.GetString(algorithm.ComputeHash(plainTextWithSaltBytes));
        }

        public static bool ComparePasswords(string pass1, string pass2)
        {
            var _array1 = Encoding.ASCII.GetBytes(pass1);
            var _array2 = Encoding.ASCII.GetBytes(pass2);

            if (_array1.Length != _array2.Length)
            {
                return false;
            }

            for (int i = 0; i < _array1.Length; i++)
            {
                if (_array1[i] != _array2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
