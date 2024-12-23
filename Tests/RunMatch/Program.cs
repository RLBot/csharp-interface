using RLBot.Manager;

if (args.Length == 0)
{
    Console.WriteLine("Usage: RunMatch <full path to match config>");
    return;
}

string matchConfigPath = args[0];
Match matchManager = new();
matchManager.StartMatch(matchConfigPath, false);

// wait
Console.WriteLine("\nPress enter to end the match: ");
Console.ReadLine();

// end the match and disconnect
matchManager.StopMatch();
matchManager.Disconnect();
