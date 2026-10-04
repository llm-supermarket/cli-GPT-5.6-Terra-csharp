using RcloneEncrypt;
using Xunit;

namespace RcloneEncrypt.Tests;

public sealed class CliRunnerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public CliRunnerTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void EncryptsAndDecryptsWithPasswordWithoutSalt()
    {
        RoundTrip("without-salt.txt", ["--password", "Testpassword1"]);
    }

    [Fact]
    public void EncryptsAndDecryptsWithPasswordAndSalt()
    {
        RoundTrip("with-salt.txt", ["--password", "Testpassword1", "--salt", "second secret"]);
    }

    [Fact]
    public void SupportsBase64FilenameEncoding()
    {
        var source = WriteFile("TEST_FILE.txt");
        var runner = new CliRunner();
        Assert.Equal(0, runner.Run(["encrypt", "-i", source, "--password", "Testpassword1", "--filename-encoding", "base64"], new StringReader(string.Empty), TextWriter.Null, new StringWriter()));
        var encrypted = Directory.EnumerateFiles(_directory).Single(path => path != source);
        Assert.DoesNotContain(Path.GetFileName(encrypted), "=");
        Assert.Equal(0, runner.Run(["decrypt", "-i", encrypted, "--password", "Testpassword1", "--filename-encoding", "base64"], new StringReader(string.Empty), TextWriter.Null, new StringWriter()));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(Path.Combine(_directory, "TEST_FILE.txt")));
    }

    [Fact]
    public void PromptsForPasswordAndSaltWhenNotProvided()
    {
        var source = WriteFile("prompted.txt");
        var encrypted = Path.Combine(_directory, "encrypted.bin");
        var decrypted = Path.Combine(_directory, "decrypted.txt");
        var runner = new CliRunner();
        var prompts = new StringWriter();
        Assert.Equal(0, runner.Run(["encrypt", "-i", source, "-o", encrypted], new StringReader("Testpassword1\nprompt salt\n"), prompts, new StringWriter()));
        Assert.Contains("Password:", prompts.ToString());
        Assert.Contains("Salt", prompts.ToString());
        Assert.Equal(0, runner.Run(["decrypt", "-i", encrypted, "-o", decrypted], new StringReader("Testpassword1\nprompt salt\n"), TextWriter.Null, new StringWriter()));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(decrypted));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private void RoundTrip(string filename, string[] credentials)
    {
        var source = WriteFile(filename);
        var encrypted = Path.Combine(_directory, "encrypted.bin");
        var decrypted = Path.Combine(_directory, "decrypted.txt");
        var runner = new CliRunner();
        var encryptArguments = new List<string> { "encrypt", "-i", source, "-o", encrypted };
        encryptArguments.AddRange(credentials);
        var encryptError = new StringWriter();
        Assert.Equal(0, runner.Run([.. encryptArguments], new StringReader(string.Empty), TextWriter.Null, encryptError));
        Assert.Contains("Warning: --password", encryptError.ToString());
        var decryptArguments = new List<string> { "decrypt", "-i", encrypted, "-o", decrypted };
        decryptArguments.AddRange(credentials);
        Assert.Equal(0, runner.Run([.. decryptArguments], new StringReader(string.Empty), TextWriter.Null, new StringWriter()));
        Assert.Equal(File.ReadAllText(source), File.ReadAllText(decrypted));
    }

    private string WriteFile(string filename)
    {
        var path = Path.Combine(_directory, filename);
        File.WriteAllText(path, "abandon ability able about above absent absorb abstract absurd abuse access accident");
        return path;
    }
}
