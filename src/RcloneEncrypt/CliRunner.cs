using System.Text;

namespace RcloneEncrypt;

public sealed class CliRunner
{
    public int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        try
        {
            if (args.Length == 1 && args[0] == "--version")
            {
                output.WriteLine($"cli-GPT-5.6-Terra-csharp {typeof(Program).Assembly.GetName().Version}");
                return 0;
            }
            var options = Parse(args);
            if (options.ShowHelp)
            {
                output.WriteLine(HelpText);
                return 0;
            }

            var password = options.Password;
            if (password is null)
            {
                password = Prompt(input, output, "Password: ");
            }
            else
            {
                error.WriteLine("Warning: --password exposes secrets through shell history and process listings. Prefer a prompt or an environment variable, and remove this command from terminal history.");
            }

            var salt = options.Salt ?? (options.Password is null ? Prompt(input, output, "Salt (optional, press Enter for none): ") : string.Empty);
            var cipher = new RcloneCipher(password, salt, options.Encoding);
            var destination = options.OutputFile ?? DefaultOutput(options, cipher);

            if (options.Operation == Operation.Encrypt)
            {
                cipher.EncryptFile(options.InputFile, destination);
            }
            else
            {
                cipher.DecryptFile(options.InputFile, destination);
            }

            output.WriteLine(destination);
            return 0;
        }
        catch (ArgumentException exception)
        {
            error.WriteLine($"Error: {exception.Message}");
            error.WriteLine("Run with --help for usage.");
            return 2;
        }
        catch (Exception exception)
        {
            error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static string Prompt(TextReader input, TextWriter output, string prompt)
    {
        output.Write(prompt);
        if (ReferenceEquals(input, Console.In) && !Console.IsInputRedirected)
        {
            var password = new StringBuilder();
            for (var key = Console.ReadKey(intercept: true); key.Key != ConsoleKey.Enter; key = Console.ReadKey(intercept: true))
            {
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                }
            }
            output.WriteLine();
            return password.ToString();
        }
        return input.ReadLine() ?? throw new ArgumentException("No value was provided for the prompt.");
    }

    private static string DefaultOutput(Options options, RcloneCipher cipher)
    {
        var inputPath = Path.GetFullPath(options.InputFile);
        var directory = Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory();
        var filename = Path.GetFileName(inputPath);
        var transformed = options.Operation == Operation.Encrypt
            ? cipher.EncryptFileName(filename)
            : cipher.DecryptFileName(filename);
        return Path.Combine(directory, transformed);
    }

    private static Options Parse(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            return new Options { ShowHelp = true };
        }

        var operation = args[0].ToLowerInvariant() switch
        {
            "encrypt" => Operation.Encrypt,
            "decrypt" => Operation.Decrypt,
            _ => throw new ArgumentException("The first argument must be 'encrypt' or 'decrypt'.")
        };

        string? inputFile = null;
        string? outputFile = null;
        string? password = null;
        string? salt = null;
        var encoding = FilenameEncoding.Base32;
        for (var index = 1; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument is "--help" or "-h") return new Options { ShowHelp = true };
            if (++index >= args.Length) throw new ArgumentException($"{argument} requires a value.");
            var value = args[index];
            switch (argument)
            {
                case "-i" or "--input-file": inputFile = value; break;
                case "-o" or "--output-file": outputFile = value; break;
                case "--password": password = value; break;
                case "--salt": salt = value; break;
                case "--filename-encoding": encoding = FilenameEncoding.Parse(value); break;
                default: throw new ArgumentException($"Unknown option '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(inputFile)) throw new ArgumentException("-i or --input-file is required.");
        if (!File.Exists(inputFile)) throw new ArgumentException($"Input file '{inputFile}' does not exist.");
        return new Options { Operation = operation, InputFile = inputFile, OutputFile = outputFile, Password = password, Salt = salt, Encoding = encoding };
    }

    private const string HelpText = """
Usage: cli-GPT-5.6-Terra-csharp <encrypt|decrypt> -i <input-file> [-o <output-file>] [options]

Options:
  -i, --input-file <path>         Required source file.
  -o, --output-file <path>        Destination file. Defaults to the rclone-encrypted/decrypted filename beside the input.
      --password <password>       Password. Warning: visible in shell history and process listings.
      --salt <salt>               Optional rclone crypt salt. Prompts when omitted.
      --filename-encoding <name>  base32 (default) or base64.
  -h, --help                      Show this help.
""";

    private sealed class Options
    {
        public bool ShowHelp { get; init; }
        public Operation Operation { get; init; }
        public string InputFile { get; init; } = string.Empty;
        public string? OutputFile { get; init; }
        public string? Password { get; init; }
        public string? Salt { get; init; }
        public FilenameEncoding Encoding { get; init; } = FilenameEncoding.Base32;
    }

    private enum Operation { Encrypt, Decrypt }
}
