using System.Text;

namespace RcloneEncrypt;

public abstract class FilenameEncoding
{
    public static readonly FilenameEncoding Base32 = new RcloneBase32Encoding();
    public static readonly FilenameEncoding Base64 = new UrlBase64Encoding();

    public abstract string Encode(byte[] value);
    public abstract byte[] Decode(string value);

    public static FilenameEncoding Parse(string value) => value.ToLowerInvariant() switch
    {
        "base32" => Base32,
        "base64" => Base64,
        _ => throw new ArgumentException("--filename-encoding must be base32 or base64.")
    };

    private sealed class RcloneBase32Encoding : FilenameEncoding
    {
        private static readonly char[] Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV".ToCharArray();
        private static readonly sbyte[] Lookup = CreateLookup();

        public override string Encode(byte[] value)
        {
            var output = new StringBuilder((value.Length * 8 + 4) / 5);
            var buffer = 0;
            var bits = 0;
            foreach (var item in value)
            {
                buffer = (buffer << 8) | item;
                bits += 8;
                while (bits >= 5)
                {
                    output.Append(char.ToLowerInvariant(Alphabet[(buffer >> (bits - 5)) & 31]));
                    bits -= 5;
                }
            }
            if (bits > 0) output.Append(char.ToLowerInvariant(Alphabet[(buffer << (5 - bits)) & 31]));
            return output.ToString();
        }

        public override byte[] Decode(string value)
        {
            if (value.Contains('=')) throw new ArgumentException("Invalid base32 filename encoding.");
            using var bytes = new MemoryStream();
            var buffer = 0;
            var bits = 0;
            foreach (var character in value)
            {
                if (character > 127 || Lookup[character] < 0) throw new ArgumentException("Invalid base32 filename encoding.");
                buffer = (buffer << 5) | (byte)Lookup[character];
                bits += 5;
                if (bits >= 8)
                {
                    bytes.WriteByte((byte)(buffer >> (bits - 8)));
                    bits -= 8;
                }
            }
            return bytes.ToArray();
        }

        private static sbyte[] CreateLookup()
        {
            var lookup = Enumerable.Repeat((sbyte)-1, 128).ToArray();
            for (var index = 0; index < Alphabet.Length; index++)
            {
                lookup[Alphabet[index]] = (sbyte)index;
                lookup[char.ToLowerInvariant(Alphabet[index])] = (sbyte)index;
            }
            return lookup;
        }
    }

    private sealed class UrlBase64Encoding : FilenameEncoding
    {
        public override string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public override byte[] Decode(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded);
        }
    }
}
