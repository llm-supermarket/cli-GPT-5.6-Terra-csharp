namespace RcloneEncrypt;

public static class Program
{
    public static int Main(string[] args) => new CliRunner().Run(args, Console.In, Console.Out, Console.Error);
}
