using System.Collections.Generic;
using System.IO;
using CommandLine;
using Microsoft.AspNetCore.Hosting;
using RandomStringGenerator;

namespace SagaUtil;

internal class Options
{
    [Option('a', "AudiobookDb", Required = true, HelpText = "The audiobook database")]
    public string audiobookDb { get; set; }

    [Option('u', "UserDb", Required = true, HelpText = "The user database")]
    public string userDb { get; set; }

    [Option('p', "Port", Required = false, HelpText = "The port to bind", Default = 4040)]
    public int port { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }

    private static void RunOptions(Options options)
    {
        var _jwtSigningKey = StringGenerator.GetUniqueString(256);
        var _userDb = options.userDb;
        var _bookDb = options.audiobookDb;
        var _port = options.port;

        SystemVariables.Instance.UpdateState(_userDb, _bookDb, _jwtSigningKey);

        var host = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>()
            .UseUrls($"http://*:{_port}")
            .Build();

        host.Run();
    }

    private static void HandleParseError(IEnumerable<Error> errors)
    {
    }
}