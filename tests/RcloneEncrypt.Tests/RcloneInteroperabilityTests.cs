using RcloneEncrypt;
using Xunit;

namespace RcloneEncrypt.Tests;

public sealed class RcloneInteroperabilityTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public RcloneInteroperabilityTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("kr9tu4e1da4u3nifdd99g9tf5o", "base32", "TEST_FILE.txt")]
    [InlineData("Iyxcijgc9bp3o5Y0npW6xqUvwWNcc3MA4SadB0sR6cY", "base64", "TEST_FILE BASE64.txt")]
    public void DecryptsSuppliedRcloneFixtures(string encryptedName, string encoding, string plaintextName)
    {
        var cipher = new RcloneCipher("Testpassword1", null, FilenameEncoding.Parse(encoding));
        Assert.Equal(plaintextName, cipher.DecryptFileName(encryptedName));
        var source = Path.Combine(AppContext.BaseDirectory, encryptedName);
        var destination = Path.Combine(_directory, $"{encoding}.txt");
        cipher.DecryptFile(source, destination);
        Assert.Contains("umbrella", File.ReadAllText(destination));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
