using RLBot.Util;

class Version
{
    const string BoldRed = "\x1b[31;1m";
    const string Cyan = "\x1b[36;20m";
    const string BoldGreen = "\x1b[32;1m";
    const string Reset = "\x1b[0m";

    private static readonly Dictionary<string, string> RELEASE_NOTES = new()
    {
        ["5.0.0-alpha1"] =
            @"
    Initial iteration of the C# interface for RLBot.
    ",
    };

    private static readonly string RELEASE_BANNER =
        BoldGreen
        + "           ______ _     ______       _\n"
        + "     10100 | ___ \\ |    | ___ \\     | |   00101\n"
        + "    110011 | |_/ / |    | |_/ / ___ | |_  110011\n"
        + "  00110110 |    /| |    | ___ \\/ _ \\| __| 01101100\n"
        + "    010010 | |\\ \\| |____| |_/ / (_) | |_  010010\n"
        + "     10010 \\_| \\_\\_____/\\____/ \\___/ \\__| 01001\n"
        + Reset
        + "\n\n\n";

    public static string GetCurrentReleaseNotes() =>
        RELEASE_NOTES.GetValueOrDefault(VersionInfo.PackageVersion, "");

    public static string GetHelpText() =>
        $"{BoldRed}Trouble?{Reset} Ask on Discord at {Cyan}https://discord.gg/5cNbXgG{Reset} "
        + $"or report an issue at {Cyan}https://github.com/RLBot/core/issues{Reset}";

    public static void PrintCurrentReleaseNotes()
    {
        Console.WriteLine(RELEASE_BANNER);
        Console.WriteLine($"Version {VersionInfo.PackageVersion}");
        Console.WriteLine(GetCurrentReleaseNotes());
        Console.WriteLine(GetHelpText());
        Console.WriteLine("");
    }
}
