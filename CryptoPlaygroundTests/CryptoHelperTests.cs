namespace CryptoPlaygroundTests;
using Xunit;
using System.Text;
using System.IO;

public class CryptoHelperTests
{
    public static IEnumerable<object[]> StringTestCases => new List<object[]>
    {
        new object[] { "Hello world!" },
        new object[] { "Short" },
        new object[] { "1234567890" },
        new object[] { "This is a longer string for testing encryption and decryption." },
        new object[] { "Special characters !@#$%^&*()" }
    };

    [Theory]
    [MemberData(nameof(StringTestCases))]
    public void EncryptDecryptString_MemberData_ShouldReturnOriginal(string input)
    {
        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);
        var (encrypted, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptMessage(input, rsa, 256, logToFile: false);
        string decrypted = CryptoHelper.DecryptMessage(encrypted, encryptedAesKey, aesIV, rsa, logToFile: false);
        Assert.Equal(input, decrypted);
    }

    [Theory]
    [InlineData("Byte array test 1")]
    [InlineData("Another test")]
    [InlineData("1234567890")]
    public void EncryptDecryptByteArray_Theory_ShouldReturnOriginalBytes(string input)
    {
        byte[] originalBytes = Encoding.UTF8.GetBytes(input);
        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

        var (encryptedMessage, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptMessage(originalBytes, rsa, 256, logToFile: false);
        byte[] decryptedBytes = CryptoHelper.DecryptMessage(encryptedMessage, encryptedAesKey, aesIV, rsa, true, logToFile: false);

        Assert.Equal(originalBytes, decryptedBytes);
    }

    [Theory]
    [InlineData("File test 1 content")]
    [InlineData("Another file content example")]
    [InlineData("1234567890 in a file")]
    public void EncryptDecryptFile_Theory_ShouldMatchOriginalContent(string fileContent)
    {
        string tempInputFile = $"temp_input_{Guid.NewGuid()}.txt";
        string tempOutputFile = $"temp_output_{Guid.NewGuid()}.txt";
        File.WriteAllText(tempInputFile, fileContent);

        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

        try
        {
            var (encryptedFile, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptFile(tempInputFile, rsa, logToFile: false);
            CryptoHelper.DecryptFileToPath(encryptedFile, encryptedAesKey, aesIV, rsa, tempOutputFile, logToFile: false);

            string decryptedContent = File.ReadAllText(tempOutputFile);

            Assert.Equal(fileContent, decryptedContent);
        }
        finally
        {
            File.Delete(tempInputFile);
            File.Delete(tempOutputFile);
        }
    }

    [Fact]
    public void EncryptMessage_NullRsa_ShouldThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => CryptoHelper.EncryptMessage("test", null!, 256, logToFile: false));
    }

    [Fact]
    public void EncryptMessage_EmptyString_ShouldThrowException()
    {
        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);
        Assert.Throws<ArgumentException>(() => CryptoHelper.EncryptMessage("", rsa, 256, logToFile: false));
    }

    [Fact]
    public void EncryptFile_NonexistentFile_ShouldThrowException()
    {
        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);
        Assert.Throws<FileNotFoundException>(() => CryptoHelper.EncryptFile("no_such_file.txt", rsa, logToFile: false));
    }

    [Theory]
    [InlineData(10)] // 10 MB
    [InlineData(50)] // 50 MB
    public void EncryptDecrypt_LargeByteArray_ShouldSucceed(int sizeMB)
    {
        byte[] largeArray = new byte[sizeMB * 1024 * 1024];
        new Random().NextBytes(largeArray);

        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

        var (encMsg, encKey, aesKey, aesIV) = CryptoHelper.EncryptMessage(largeArray, rsa, 256, logToFile: false);
        byte[] decrypted = CryptoHelper.DecryptMessage(encMsg, encKey, aesIV, rsa, true, logToFile: false);

        Assert.True(largeArray.SequenceEqual(decrypted));
    }

    [Theory]
    [InlineData(10)] // 10 MB
    [InlineData(20)] // 20 MB
    public void EncryptDecrypt_LargeFile_ShouldSucceed(int sizeMB)
    {
        string tempInputFile = $"temp_input_{Guid.NewGuid()}.txt";
        string tempOutputFile = $"temp_output_{Guid.NewGuid()}.txt";

        try
        {
            using (var fs = new FileStream(tempInputFile, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[1024 * 1024];
                Random rnd = new Random();
                for (int i = 0; i < sizeMB; i++)
                {
                    rnd.NextBytes(buffer);
                    fs.Write(buffer, 0, buffer.Length);
                }
            }

            var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

            var (encryptedFile, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptFile(tempInputFile, rsa, logToFile: false);
            CryptoHelper.DecryptFileToPath(encryptedFile, encryptedAesKey, aesIV, rsa, tempOutputFile, logToFile: false);

            byte[] original = File.ReadAllBytes(tempInputFile);
            byte[] decrypted = File.ReadAllBytes(tempOutputFile);
            Assert.True(original.SequenceEqual(decrypted));
        }
        finally
        {
            if (File.Exists(tempInputFile)) File.Delete(tempInputFile);
            if (File.Exists(tempOutputFile)) File.Delete(tempOutputFile);
        }
    }

    private string GetExpectedLogFileName() => $"crypto_operations_{DateTime.Now:yyyyMMdd}.log";

    [Fact]
    public void LoggingToFile_StringEncrypt_ShouldCreateLogFile()
    {
        string logFile = GetExpectedLogFileName();
        if (File.Exists(logFile)) File.Delete(logFile);

        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

        CryptoHelper.EncryptMessage("Test logging message", rsa, 256, logToFile: true);

        Assert.True(File.Exists(logFile));

        string[] lines = File.ReadAllLines(logFile);
        Assert.Contains(lines, line => line.Contains("EncryptMessage") && line.Contains("INFO"));

        File.Delete(logFile);
    }

    [Fact]
    public void LoggingToFile_ByteArrayEncrypt_ShouldCreateLogFile()
    {
        string logFile = GetExpectedLogFileName();
        if (File.Exists(logFile)) File.Delete(logFile);

        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);
        byte[] data = Encoding.UTF8.GetBytes("Test byte array logging");

        CryptoHelper.EncryptMessage(data, rsa, 256, logToFile: true);

        Assert.True(File.Exists(logFile));

        string[] lines = File.ReadAllLines(logFile);
        Assert.Contains(lines, line => line.Contains("EncryptMessage") && line.Contains("INFO"));

        File.Delete(logFile);
    }

    [Fact]
    public void LoggingToFile_FileEncrypt_ShouldCreateLogFile()
    {
        string logFile = GetExpectedLogFileName();
        if (File.Exists(logFile)) File.Delete(logFile);

        var (rsa, _, _) = CryptoHelper.GenerateRsaKeys(2048);

        string tempInputFile = Path.GetTempFileName();
        string tempOutputFile = Path.GetTempFileName();
        File.WriteAllText(tempInputFile, "Test file logging content");

        try
        {
            var (encryptedFile, encryptedAesKey, aesKey, aesIV) = CryptoHelper.EncryptFile(tempInputFile, rsa, logToFile: true);

            Assert.True(File.Exists(logFile));

            string[] lines = File.ReadAllLines(logFile);
            Assert.Contains(lines, line => line.Contains("EncryptFile") && line.Contains("INFO"));
        }
        finally
        {
            File.Delete(tempInputFile);
            File.Delete(tempOutputFile);
            if (File.Exists(logFile)) File.Delete(logFile);
        }
    }
}