using System.Security.Cryptography;
using System.Text;

using RSA rsa = RSA.Create(2048);

string publicKey = rsa.ToXmlString(false);
string privateKey = rsa.ToXmlString(true);

Console.WriteLine($"Public Key: {publicKey}");
Console.WriteLine();
Console.WriteLine($"Private Key: {privateKey}");
Console.WriteLine();

string message = "Hello, World!";
byte[] messageBytes = Encoding.UTF8.GetBytes(message);
byte[] signature = rsa.SignData(messageBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

Console.WriteLine($"Signature: {Convert.ToBase64String(signature)}");
Console.WriteLine();

bool isValid = rsa.VerifyData(messageBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
Console.WriteLine($"Is the signature valid? {isValid}");
Console.WriteLine();

