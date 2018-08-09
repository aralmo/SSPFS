using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSPFS.DesktopHost
{
    class RSA
    {
        static public byte[] RSAEncrypt(byte[] byteEncrypt, RSAParameters RSAInfo, bool isOAEP)
        {
            try
            {
                byte[] encryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This only needs
                    //toinclude the public key information.
                    RSA.ImportParameters(RSAInfo);

                    //Encrypt the passed byte array and specify OAEP padding.
                    encryptedData = RSA.Encrypt(byteEncrypt, isOAEP);
                }
                return encryptedData;
            }
            //Catch and display a CryptographicException
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return null;
            }
        }
        static public byte[] RSADecrypt(byte[] byteDecrypt, RSAParameters RSAInfo, bool isOAEP)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(RSAInfo);

                    //Decrypt the passed byte array and specify OAEP padding.
                    decryptedData = RSA.Decrypt(byteDecrypt, isOAEP);
                }
                return decryptedData;
            }
            //Catch and display a CryptographicException
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }
        }
    }
}
