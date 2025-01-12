using RLBot.Flat;

namespace RLBot.Util;

static class GameStateExt
{
    public static DesiredGameStateT FillDesiredGameState(
        Dictionary<int, DesiredBallStateT>? balls = null,
        Dictionary<int, DesiredCarStateT>? cars = null,
        Dictionary<int, DesiredBoostStateT>? boostPads = null,
        DesiredMatchInfoT? matchInfo = null,
        List<ConsoleCommandT>? commands = null
    )
    {
        var gameState = new DesiredGameStateT
        {
            MatchInfo = matchInfo,
            ConsoleCommands = commands ?? new List<ConsoleCommandT>(),
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
        else
        {
            gameState.BallStates = new List<DesiredBallStateT>();
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
        else
        {
            gameState.CarStates = new List<DesiredCarStateT>();
        }

        if (boostPads != null)
        {
            var maxEntry = boostPads.Keys.Max();
            gameState.BoostStates = Enumerable
                .Range(0, maxEntry + 1)
                .Select(i => boostPads.TryGetValue(i, out var boostPad) ? boostPad : new DesiredBoostStateT())
                .ToList();
        }
        else
        {
            gameState.BoostStates = new List<DesiredBoostStateT>();
        }

        return gameState;
    }
}
