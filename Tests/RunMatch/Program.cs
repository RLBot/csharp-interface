using RLBot;

if (args.Length == 0)
{
    Console.WriteLine("Usage: RunMatch <full path to match config>");
    return;
}

RLBotInterface rlbot = new RLBotInterface();
rlbot.ConnectAsMatchHost();
rlbot.StartMatch(args[0]);

// Wait
Console.WriteLine("\nPress enter to end the match: ");
Console.ReadLine();

// End the match and disconnect
rlbot.StopMatch();
rlbot.Disconnect();
