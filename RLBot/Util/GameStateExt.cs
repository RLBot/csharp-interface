using RLBot.Flat;

namespace RLBot.Util;

static class GameStateExt
{
    public static DesiredGameStateT FillDesiredGameState(
        Dictionary<int, DesiredBallStateT>? balls = null,
        Dictionary<int, DesiredCarStateT>? cars = null,
        DesiredGameInfoStateT? gameInfo = null,
        List<ConsoleCommandT>? commands = null
    )
    {
        var gameState = new DesiredGameStateT
        {
            GameInfoState = gameInfo,
            ConsoleCommands = commands,
        };

        if (balls != null)
        {
            var maxEntry = balls.Keys.Max();
            gameState.BallStates = Enumerable
                .Range(0, maxEntry + 1)
                .Select(i =>
                    balls.TryGetValue(i, out var ballState)
                        ? ballState
                        : new DesiredBallStateT()
                )
                .ToList();
        }

        if (cars != null)
        {
            var maxEntry = cars.Keys.Max();
            gameState.CarStates = Enumerable
                .Range(0, maxEntry + 1)
                .Select(i =>
                    cars.TryGetValue(i, out var carState) ? carState : new DesiredCarStateT()
                )
                .ToList();
        }

        return gameState;
    }
}
