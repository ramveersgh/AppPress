using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace AppPressFramework
{
    public class APCrypto
    {
        enum EmcryptionTypes
        {
            AES = 1
        }
        //This is the master key which is use to generate all encryption keys.
        private static byte[] _secretsalt = Encoding.ASCII.GetBytes("RPRYANVIKASRAMCOMMISSIONPROJECTOR@9891779179");

        static Dictionary<string, RijndaelManaged> aesAlg = new Dictionary<string, RijndaelManaged>();

        internal static string EncryptStringAES(string plainText, string newEncryptionKey = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;
            if (AppPress.Settings.encryptionKey == null && newEncryptionKey == null)
                throw new Exception("Cannot EncryptStringAES as EncryptionKey is not passed to InitAppPress");
            if (newEncryptionKey == null)
                newEncryptionKey = AppPress.Settings.encryptionKey;
            RijndaelManaged aesAlgLocal;
            if (!aesAlg.TryGetValue(newEncryptionKey, out aesAlgLocal))
            {
                string sharedSecret = newEncryptionKey;
                var AESkey = new Rfc2898DeriveBytes(sharedSecret, _secretsalt);
                aesAlgLocal = aesAlg[newEncryptionKey] = new RijndaelManaged();
                aesAlg[newEncryptionKey].Key = AESkey.GetBytes(aesAlg[newEncryptionKey].KeySize / 8);
            }
            string outStr = null;                       // Encrypted string to return
                                                        // generate the key from the shared secret and the salt

            // Create a decryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlgLocal.CreateEncryptor(aesAlgLocal.Key, aesAlgLocal.IV);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // prepend the IV
                msEncrypt.Write(BitConverter.GetBytes(aesAlgLocal.IV.Length), 0, sizeof(int));
                msEncrypt.Write(aesAlgLocal.IV, 0, aesAlgLocal.IV.Length);
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                }
                outStr = Convert.ToBase64String(msEncrypt.ToArray());
            }

            // Return the encrypted bytes from the memory stream.
            return outStr;
        }

        internal static string DecryptStringAES(string encryptrdText, string newEncryptionKey = null)
        {
            if (string.IsNullOrEmpty(encryptrdText))
                throw new ArgumentNullException("cipherText");
            if (AppPress.Settings.encryptionKey == null)
                throw new Exception("Cannot DecryptStringAES as EncryptionKey is not passed to InitAppPress");
            try
            {
                if (newEncryptionKey == null)
                    newEncryptionKey = AppPress.Settings.encryptionKey+ "hfdfwe8yur98w4";
                RijndaelManaged aesAlgLocal;
                if (!aesAlg.TryGetValue(newEncryptionKey, out aesAlgLocal))
                {
                    string sharedSecret = newEncryptionKey;
                    var AESkey = new Rfc2898DeriveBytes(sharedSecret, _secretsalt);
                    aesAlgLocal = aesAlg[newEncryptionKey] = new RijndaelManaged();
                    aesAlg[newEncryptionKey].Key = AESkey.GetBytes(aesAlg[newEncryptionKey].KeySize / 8);
                }

                // Declare the string used to hold
                // the decrypted text.
                string plaintext = null;

                // Create the streams used for decryption.                
                byte[] bytes = Convert.FromBase64String(encryptrdText);
                using (MemoryStream msDecrypt = new MemoryStream(bytes))
                {
                    // Create a RijndaelManaged object
                    // with the specified key and IV.

                    // Get the initialization vector from the encrypted stream
                    aesAlgLocal.IV = ReadByteArray(msDecrypt);
                    // Create a decrytor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlgLocal.CreateDecryptor(aesAlgLocal.Key, aesAlgLocal.IV);
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                    }
                }

                return plaintext;
            }
            catch
            {
                throw new Exception("Error in AES Decrypt of " + encryptrdText);
            }
        }

        private static byte[] ReadByteArray(Stream s)
        {
            byte[] rawLength = new byte[sizeof(int)];
            if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
            {
                throw new SystemException("Stream did not contain properly formatted byte array");
            }

            byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
            if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new SystemException("Did not read byte array properly");
            }
            return buffer;
        }
    }
}