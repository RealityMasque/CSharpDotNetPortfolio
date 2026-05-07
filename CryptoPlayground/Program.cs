using System.Security.Cryptography;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine();
        Console.WriteLine("Encrypt/Decrypt string messages using RSA and AES");
        Console.WriteLine("======================================================================");

        string message = "Hello, World!";
        Console.WriteLine($"Message: {message}");
        Console.WriteLine();
        Console.WriteLine();

        Console.WriteLine("Generate RSA keys, sign message, and verify signatures");
        Console.WriteLine("----------------------------------------------------------------------");

        RSA? rsa = null;

        try
        {
            var (theRsa, publicKey, privateKey) = CryptoHelper.GenerateRsaKeys(2048);
            rsa = theRsa;
            byte[] rsaMessageBytes = Encoding.UTF8.GetBytes(message);
            byte[] signature = rsa.SignData(rsaMessageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            bool isValid = rsa.VerifyData(rsaMessageBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            Console.WriteLine($"Public Key: {publicKey}");
            Console.WriteLine();

            Console.WriteLine($"Private Key: {privateKey}");
            Console.WriteLine();

            Console.WriteLine($"Signature: {Convert.ToBase64String(signature)}");
            Console.WriteLine();

            Console.WriteLine($"Is the signature valid? {isValid}");
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Unexpected error: {exception.Message}");
            return;
        }
        

        Console.WriteLine();
        Console.WriteLine("Generate AES key, encrypt/decrypt AES Key, and encrypt/decrypt message");
        Console.WriteLine("----------------------------------------------------------------------");

        try
        {
            var (encryptedMessage, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptMessage(message, rsa, 256, true);
            byte[] decryptedAesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.Pkcs1);
            string decryptedMessage = CryptoHelper.DecryptMessage(encryptedMessage, encryptedAesKey, aesIV, rsa, true);

            Console.WriteLine();
            Console.WriteLine($"AES Key: {Convert.ToBase64String(aesKey)}");

            Console.WriteLine();
            Console.WriteLine($"AES IV: {Convert.ToBase64String(aesIV)}");

            Console.WriteLine();
            Console.WriteLine($"Encrypted Message (AES): {Convert.ToBase64String(encryptedMessage)}");
            
            Console.WriteLine();
            Console.WriteLine($"Encrypted AES Key (RSA): {Convert.ToBase64String(encryptedAesKey)}");
            
            Console.WriteLine();
            Console.WriteLine($"Decrypted AES Key: {Convert.ToBase64String(decryptedAesKey)}");
            
            Console.WriteLine();
            Console.WriteLine($"Decrypted Message: {decryptedMessage}");
            Console.WriteLine();
            Console.WriteLine();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Unexpected error: {exception.Message}");
        }


        Console.WriteLine();
        Console.WriteLine("Encrypt/Decrypt file data using RSA and AES");
        Console.WriteLine("======================================================================");

        string inputFilePath = "test.txt";
        string outputFilePath = $"test_decrypted_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";

        try
        {
            var (encFile, encFileAesKey, fileAesKey, fileAesIV) = CryptoHelper.EncryptFile(inputFilePath, rsa, true);
            CryptoHelper.DecryptFileToPath(encFile, encFileAesKey, fileAesIV, rsa, outputFilePath, true);

            string inputFileData = File.ReadAllText(inputFilePath);
            string outputFileData = File.ReadAllText(outputFilePath);

            Console.WriteLine();
            Console.WriteLine($"File decrypted to {outputFilePath}");
            Console.WriteLine();
            Console.WriteLine($"Input File Data: {inputFileData}");
            Console.WriteLine();
            Console.WriteLine($"Output File Data: {outputFileData}");
            Console.WriteLine();
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Unexpected error: {exception.Message}");
        }
    }
}


