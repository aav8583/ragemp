using Konscious.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Text;


public class Crypto
{

    public string Ecnryption(string password, string salt)
    {
        using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
        {
           
            argon2.Salt = Encoding.UTF8.GetBytes(salt);
            argon2.DegreeOfParallelism = 8; // количество потоков
            argon2.MemorySize = 4096; // количество используемой памяти в килобайтах
            argon2.Iterations = 2; // количество итераций

            var hash = argon2.GetBytes(16); // размер генерируемого хеша в байтах
                                            // hash теперь содержит байтовое представление хеша
            return Convert.ToBase64String(hash);
        }
    }

    public static byte[] GenerateSalt(int size)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            var salt = new byte[size];
            rng.GetBytes(salt);
            return salt;
        }
    }

}
