using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace RcloneEncrypt;

public sealed class RcloneCipher
{
    private static readonly byte[] DefaultSalt = [0xA8, 0x0D, 0xF4, 0x3A, 0x8F, 0xBD, 0x03, 0x08, 0xA7, 0xCA, 0xB8, 0x3E, 0x58, 0x1F, 0x86, 0xB1];
    private static readonly byte[] Magic = "RCLONE\0\0"u8.ToArray();
    private readonly byte[] _dataKey;
    private readonly byte[] _nameKey;
    private readonly byte[] _nameTweak;
    private readonly FilenameEncoding _filenameEncoding;

    public RcloneCipher(string password, string? salt, FilenameEncoding filenameEncoding)
    {
        ArgumentNullException.ThrowIfNull(password);
        _filenameEncoding = filenameEncoding;
        var material = password.Length == 0
            ? new byte[80]
            : SCrypt.Generate(Encoding.UTF8.GetBytes(password), string.IsNullOrEmpty(salt) ? DefaultSalt : Encoding.UTF8.GetBytes(salt), 16384, 8, 1, 80);
        _dataKey = material[..32];
        _nameKey = material[32..64];
        _nameTweak = material[64..80];
        CryptographicOperations.ZeroMemory(material);
    }

    public string EncryptFileName(string name)
    {
        var plaintext = Encoding.UTF8.GetBytes(name);
        var padded = AddPkcs7Padding(plaintext);
        return _filenameEncoding.Encode(EmeTransform(padded, encrypt: true));
    }

    public string DecryptFileName(string name)
    {
        var padded = EmeTransform(_filenameEncoding.Decode(name), encrypt: false);
        return Encoding.UTF8.GetString(RemovePkcs7Padding(padded));
    }

    public void EncryptFile(string source, string destination)
    {
        EnsureDistinct(source, destination);
        using var input = File.OpenRead(source);
        using var output = File.Create(destination);
        var nonce = new byte[24];
        RandomNumberGenerator.Fill(nonce);
        output.Write(Magic);
        output.Write(nonce);
        var plaintext = new byte[65536];
        for (var read = input.Read(plaintext); read > 0; read = input.Read(plaintext))
        {
            output.Write(Seal(plaintext.AsSpan(0, read), nonce));
            IncrementNonce(nonce);
        }
    }

    public void DecryptFile(string source, string destination)
    {
        EnsureDistinct(source, destination);
        using var input = File.OpenRead(source);
        var header = ReadExactly(input, 32);
        if (!header.AsSpan(0, 8).SequenceEqual(Magic)) throw new InvalidOperationException("Input is not an rclone-encrypted file (bad magic).");
        var nonce = header[8..32];
        using var output = File.Create(destination);
        while (input.Position < input.Length)
        {
            var remaining = input.Length - input.Position;
            if (remaining <= 16) throw new InvalidOperationException("Encrypted file has a truncated block header.");
            var encrypted = ReadExactly(input, (int)Math.Min(65552, remaining));
            output.Write(Open(encrypted, nonce));
            IncrementNonce(nonce);
        }
    }

    private byte[] Seal(ReadOnlySpan<byte> plaintext, byte[] nonce)
    {
        var stream = CreateStream(nonce);
        var zeroes = new byte[32];
        var polyKey = new byte[32];
        stream.ProcessBytes(zeroes, 0, zeroes.Length, polyKey, 0);
        var ciphertext = new byte[plaintext.Length];
        stream.ProcessBytes(plaintext.ToArray(), 0, plaintext.Length, ciphertext, 0);
        var tag = Poly1305(polyKey, ciphertext);
        return [.. tag, .. ciphertext];
    }

    private byte[] Open(byte[] encrypted, byte[] nonce)
    {
        var tag = encrypted[..16];
        var ciphertext = encrypted[16..];
        var stream = CreateStream(nonce);
        var zeroes = new byte[32];
        var polyKey = new byte[32];
        stream.ProcessBytes(zeroes, 0, zeroes.Length, polyKey, 0);
        var expectedTag = Poly1305(polyKey, ciphertext);
        if (!CryptographicOperations.FixedTimeEquals(tag, expectedTag)) throw new InvalidOperationException("Failed to authenticate encrypted block. The password or salt may be incorrect.");
        var plaintext = new byte[ciphertext.Length];
        stream.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
        return plaintext;
    }

    private XSalsa20Engine CreateStream(byte[] nonce)
    {
        var stream = new XSalsa20Engine();
        stream.Init(true, new ParametersWithIV(new KeyParameter(_dataKey), nonce));
        return stream;
    }

    private static byte[] Poly1305(byte[] key, byte[] data)
    {
        var mac = new Poly1305();
        mac.Init(new KeyParameter(key));
        mac.BlockUpdate(data, 0, data.Length);
        var tag = new byte[16];
        mac.DoFinal(tag, 0);
        return tag;
    }

    private byte[] EmeTransform(byte[] input, bool encrypt)
    {
        if (input.Length == 0 || input.Length % 16 != 0 || input.Length > 2048) throw new ArgumentException("Invalid encrypted filename length.");
        var blocks = input.Length / 16;
        var output = new byte[input.Length];
        var l = EncryptBlock(new byte[16]);
        var table = new byte[blocks][];
        for (var index = 0; index < blocks; index++)
        {
            l = MultiplyByTwo(l);
            table[index] = l;
            Xor(input.AsSpan(index * 16, 16), l).CopyTo(output, index * 16);
            TransformBlock(output, index * 16, output, index * 16, encrypt);
        }

        var mp = Xor(output.AsSpan(0, 16), _nameTweak);
        for (var index = 1; index < blocks; index++) XorInPlace(mp, output.AsSpan(index * 16, 16));
        var mc = TransformSingleBlock(mp, encrypt);
        var m = Xor(mp, mc);
        for (var index = 1; index < blocks; index++)
        {
            m = MultiplyByTwo(m);
            Xor(output.AsSpan(index * 16, 16), m).CopyTo(output, index * 16);
        }

        var first = Xor(mc, _nameTweak);
        for (var index = 1; index < blocks; index++) XorInPlace(first, output.AsSpan(index * 16, 16));
        first.CopyTo(output, 0);
        for (var index = 0; index < blocks; index++)
        {
            TransformBlock(output, index * 16, output, index * 16, encrypt);
            XorInPlace(output.AsSpan(index * 16, 16), table[index]);
        }
        return output;
    }

    private byte[] EncryptBlock(byte[] input) => TransformSingleBlock(input, true);

    private byte[] TransformSingleBlock(byte[] input, bool encrypt)
    {
        var output = new byte[16];
        TransformBlock(input, 0, output, 0, encrypt);
        return output;
    }

    private void TransformBlock(byte[] input, int inputOffset, byte[] output, int outputOffset, bool encrypt)
    {
        using var aes = Aes.Create();
        aes.Key = _nameKey;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        using var transform = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        _ = transform.TransformBlock(input, inputOffset, 16, output, outputOffset);
    }

    private static byte[] AddPkcs7Padding(byte[] input)
    {
        var padding = 16 - input.Length % 16;
        return [.. input, .. Enumerable.Repeat((byte)padding, padding)];
    }

    private static byte[] RemovePkcs7Padding(byte[] input)
    {
        var padding = input[^1];
        if (padding is 0 or > 16 || input[^padding..].Any(value => value != padding)) throw new InvalidOperationException("Invalid filename padding. The password, salt, or encoding may be incorrect.");
        return input[..^padding];
    }

    private static byte[] MultiplyByTwo(byte[] input)
    {
        var output = new byte[16];
        output[0] = (byte)(2 * input[0] ^ (135 & -(input[15] >> 7)));
        for (var index = 1; index < 16; index++) output[index] = (byte)(2 * input[index] + (input[index - 1] >> 7));
        return output;
    }

    private static byte[] Xor(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var output = new byte[left.Length];
        for (var index = 0; index < output.Length; index++) output[index] = (byte)(left[index] ^ right[index]);
        return output;
    }

    private static void XorInPlace(Span<byte> left, ReadOnlySpan<byte> right)
    {
        for (var index = 0; index < left.Length; index++) left[index] ^= right[index];
    }

    private static void IncrementNonce(byte[] nonce)
    {
        for (var index = 0; index < nonce.Length; index++)
        {
            nonce[index]++;
            if (nonce[index] != 0) break;
        }
    }

    private static byte[] ReadExactly(Stream stream, int count)
    {
        var buffer = new byte[count];
        stream.ReadExactly(buffer);
        return buffer;
    }

    private static void EnsureDistinct(string source, string destination)
    {
        if (Path.GetFullPath(source).Equals(Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Input and output files must be different.");
    }
}
