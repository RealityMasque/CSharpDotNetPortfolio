using System.Security.Cryptography;
using System.Text;

public static class CryptoHelper
{
    private static void ValidateRsa(RSA rsa)
    {
        if (rsa == null)
            throw new ArgumentNullException(nameof(rsa), "RSA object cannot be null.");
    }

    private static void ValidateNotNullOrEmpty(string data, string name)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException($"{name} cannot be null or empty.", name);
    }

    private static void ValidateNotNullOrEmpty(byte[] data, string name)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException($"{name} cannot be null or empty.", name);
    }

    private static void ValidateFileExists(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Input file not found.", filePath);
    }

    private static void ValidateFileDoesNotExist(string outputPath)
    {
        if (File.Exists(outputPath))
            throw new IOException($"Output file already exists: {outputPath}");
    }

    private static void LogOperation(string operation, string dataType, int dataSize, bool success, string extraInfo = "", LogLevel level = LogLevel.INFO, bool logToFile = false)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string status = success ? "success" : "failure";
        string logMessage = $"[{timestamp}] [{level}] {operation} ({dataType}) - size: {dataSize} - {status} {extraInfo}";
        Console.WriteLine(logMessage);

        if(logToFile)
        {
            string logFilePath = $"crypto_operations_{DateTime.Now:yyyyMMdd}.log";
            File.AppendAllText(logFilePath, $"{logMessage}{Environment.NewLine}");
        }
    }

    public static (RSA rsa, string publicKey, string privateKey) GenerateRsaKeys(int rsaKeySize)
    {
        RSA rsa = RSA.Create(rsaKeySize);
        string publicKey = rsa.ToXmlString(false);
        string privateKey = rsa.ToXmlString(true);
        return (rsa, publicKey, privateKey);
    }

    public static (byte[] encryptedMessage, byte[] encryptedAesKey, byte[] aesKey, byte[] aesIV) EncryptMessage(string message, RSA rsa, int aesKeySize, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(message, nameof(message));

        byte[] messageBytes = Encoding.UTF8.GetBytes(message);

        try
        {
            using Aes aes = Aes.Create();
            aes.KeySize = aesKeySize;
            byte[] aesKey = aes.Key;
            byte[] aesIV = aes.IV;

            ValidateNotNullOrEmpty(aesIV, nameof(aesIV));

            // Encrypt message with AES
            byte[] encryptedMessage;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                encryptedMessage = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
            }

            ValidateNotNullOrEmpty(encryptedMessage, nameof(encryptedMessage));

            // Encrypt AES key with RSA
            byte[] encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.Pkcs1);

            ValidateNotNullOrEmpty(encryptedAesKey, nameof(encryptedAesKey));

            LogOperation("EncryptMessage", "string", messageBytes.Length, true, level: LogLevel.INFO, logToFile: logToFile);

            return (encryptedMessage, encryptedAesKey, aesKey, aesIV);
        }
        catch(Exception exception)
        {
            LogOperation("EncryptMessage", "string", messageBytes.Length, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw;
        }
        
    }

    public static (byte[] encryptedMessage, byte[] encryptedAesKey, byte[] aesKey, byte[] aesIV) EncryptMessage(byte[] messageBytes, RSA rsa, int aesKeySize, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(messageBytes, nameof(messageBytes));

        try
        {
            using Aes aes = Aes.Create();
            aes.KeySize = aesKeySize;
            byte[] aesKey = aes.Key;
            byte[] aesIV = aes.IV;

            ValidateNotNullOrEmpty(aesIV, nameof(aesIV));

            byte[] encryptedMessage;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                encryptedMessage = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
            }

            ValidateNotNullOrEmpty(encryptedMessage, nameof(encryptedMessage));

            byte[] encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.Pkcs1);

            ValidateNotNullOrEmpty(encryptedAesKey, nameof(encryptedAesKey));

            LogOperation("EncryptMessage", "byte[]", messageBytes.Length, true, level: LogLevel.INFO, logToFile: logToFile);

            return (encryptedMessage, encryptedAesKey, aesKey, aesIV);
        }
        catch (Exception exception)
        {
            LogOperation("EncryptMessage", "byte[]", messageBytes.Length, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw;
        }
    }

    public static string DecryptMessage(byte[] encryptedMessage, byte[] encryptedAesKey, byte[] aesIV, RSA rsa, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(encryptedMessage, nameof(encryptedMessage));
        ValidateNotNullOrEmpty(aesIV, nameof(aesIV));
        ValidateNotNullOrEmpty(encryptedAesKey, nameof(encryptedAesKey));

        try
        {
            // Decrypt AES key with RSA
            byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.Pkcs1);

            // Decrypt message with AES
            byte[] decryptedMessage;
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decryptedMessage = decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);
                }
            }

            LogOperation("DecryptMessage", "byte[]", encryptedMessage.Length, true, level: LogLevel.INFO, logToFile: logToFile);

            return Encoding.UTF8.GetString(decryptedMessage);
        }
        catch (Exception exception)
        {
            LogOperation("DecryptMessage", "byte[]", encryptedMessage.Length, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw;
        }
    }

    public static byte[] DecryptMessage(byte[] encryptedMessage, byte[] encryptedAesKey, byte[] aesIV, RSA rsa, bool returnBytes, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(encryptedMessage, nameof(encryptedMessage));
        ValidateNotNullOrEmpty(aesIV, nameof(aesIV));
        ValidateNotNullOrEmpty(encryptedAesKey, nameof(encryptedAesKey));

        try
        {
            byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.Pkcs1);

            byte[] decryptedMessage;
            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIV;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decryptedMessage = decryptor.TransformFinalBlock(encryptedMessage, 0, encryptedMessage.Length);
                }
            }

            LogOperation("DecryptMessage", "byte[]", encryptedMessage.Length, true, level: LogLevel.INFO, logToFile: logToFile);

            return decryptedMessage; // raw bytes
        }
        catch (Exception exception)
        {
            LogOperation("DecryptMessage", "byte[]", encryptedMessage.Length, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw;
        }
    }

    public static (byte[] encryptedFile, byte[] encryptedAesKey, byte[] aesKey, byte[] aesIV) EncryptFile(string filePath, RSA rsa, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(filePath, nameof(filePath));
        ValidateFileExists(filePath);

        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            LogOperation("EncryptFile", "file", fileBytes.Length, true, $"file: {filePath}", level: LogLevel.INFO, logToFile: logToFile);

            return EncryptMessage(fileBytes, rsa, 256, logToFile); // reuse byte[] overload
        }
        catch (Exception exception)
        {
            LogOperation("EncryptFile", "file", 0, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw; // re-throw to let calling code handle if needed
        }
    }

    public static void DecryptFileToPath(byte[] encryptedFile, byte[] encryptedAesKey, byte[] aesIV, RSA rsa, string outputPath, bool logToFile)
    {
        ValidateRsa(rsa);
        ValidateNotNullOrEmpty(encryptedFile, nameof(encryptedFile));
        ValidateNotNullOrEmpty(aesIV, nameof(aesIV));
        ValidateNotNullOrEmpty(encryptedAesKey, nameof(encryptedAesKey));
        ValidateNotNullOrEmpty(outputPath, nameof(outputPath));
        ValidateFileDoesNotExist(outputPath);

        try
        {
            byte[] decryptedBytes = DecryptMessage(encryptedFile, encryptedAesKey, aesIV, rsa, true, logToFile);

            LogOperation("DecryptFileToPath", "byte[]", encryptedFile.Length, true, $"output file: {outputPath}", level: LogLevel.INFO, logToFile: logToFile);

            File.WriteAllBytes(outputPath, decryptedBytes);
        }
        catch (Exception exception)
        {
            LogOperation("DecryptFileToPath", "byte[]", encryptedFile.Length, false, exception.Message, level: LogLevel.ERROR, logToFile: logToFile);
            throw;
        }
    }
}