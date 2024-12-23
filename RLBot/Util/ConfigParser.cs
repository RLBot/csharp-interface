using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RLBot.Flat;
using Tomlyn;
using Tomlyn.Model;

namespace RLBot.Util;

class ConfigParser
{
    private static readonly ILogger Logger = Logging.GetLogger("ConfigParser");

    private static TomlTable GetTable(string? path)
    {
        if (path == null)
        {
            Logger.LogError("Could not read Toml file, path is null");
            return [];
        }

        try
        {
            // TODO - catch any exceptions thrown by ToModel
            return Toml.ToModel(File.ReadAllText(path));
        }
        catch (FileNotFoundException)
        {
            Logger.LogError($"Could not find Toml file at '{path}'");
            return [];
        }
    }

    private static TomlTable ParseTable(TomlTable table, string key)
    {
        try
        {
            return (TomlTable)table[key];
        }
        catch (KeyNotFoundException)
        {
            return [];
        }
    }

    private static uint ParseUint(TomlTable table, string key, uint fallback)
    {
        try
        {
            return (uint)(long)table[key];
        }
        catch (KeyNotFoundException)
        {
            return fallback;
        }
    }

    private static string? ParseString(TomlTable table, string key)
    {
        try
        {
            return (string)table[key];
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static string? CombinePaths(string? parent, string? child)
    {
        if (parent == null || child == null)
            return null;

        return Path.Combine(parent, child);
    }

    private static bool ParseBool(TomlTable table, string key, bool fallback)
    {
        try
        {
            return (bool)table[key];
        }
        catch (KeyNotFoundException)
        {
            return fallback;
        }
    }

    private static string GetRunCommand(TomlTable runnableSettings)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ParseString(runnableSettings, "run_command") ?? "";

        return ParseString(runnableSettings, "run_command_linux") ?? "";
    }

    public static LoadoutPaintT ExtractLoadoutPaint(TomlTable config)
    {
        return new LoadoutPaintT
        {
            AntennaPaintId = ParseUint(config, "antenna_paint_id", 0),
            BoostPaintId = ParseUint(config, "boost_paint_id", 0),
            CarPaintId = ParseUint(config, "car_paint_id", 0),
            DecalPaintId = ParseUint(config, "decal_paint_id", 0),
            GoalExplosionPaintId = ParseUint(config, "goal_explosion_paint_id", 0),
            HatPaintId = ParseUint(config, "hat_paint_id", 0),
            TrailsPaintId = ParseUint(config, "trails_paint_id", 0),
            WheelsPaintId = ParseUint(config, "wheels_paint_id", 0),
        };
    }

    public static PlayerLoadoutT GetPlayerLoadout(string path, uint team)
    {
        TomlTable config = GetTable(path);

        string teamLoadoutString = team == 0 ? "blue_loadout" : "orange_loadout";
        TomlTable teamLoadout = ParseTable(config, teamLoadoutString);
        TomlTable teamPaint = ParseTable(teamLoadout, "paint");

        return new PlayerLoadoutT()
        {
            TeamColorId = ParseUint(teamLoadout, "team_color_id", 0),
            CustomColorId = ParseUint(teamLoadout, "custom_color_id", 0),
            CarId = ParseUint(teamLoadout, "car_id", 0),
            DecalId = ParseUint(teamLoadout, "decal_id", 0),
            WheelsId = ParseUint(teamLoadout, "wheels_id", 0),
            BoostId = ParseUint(teamLoadout, "boost_id", 0),
            AntennaId = ParseUint(teamLoadout, "antenna_id", 0),
            HatId = ParseUint(teamLoadout, "hat_id", 0),
            PaintFinishId = ParseUint(teamLoadout, "paint_finish_id", 0),
            CustomFinishId = ParseUint(teamLoadout, "custom_finish_id", 0),
            EngineAudioId = ParseUint(teamLoadout, "engine_audio_id", 0),
            TrailsId = ParseUint(teamLoadout, "trails_id", 0),
            GoalExplosionId = ParseUint(teamLoadout, "goal_explosion_id", 0),
            LoadoutPaint = ExtractLoadoutPaint(teamPaint),
        };
    }

    public static PlayerConfigurationT GetBotConfig(string tomlPath, uint team)
    {
        string tomlParent = Path.GetDirectoryName(tomlPath) ?? "";
        TomlTable playerToml = GetTable(tomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings");

        string rootDir = Path.Combine(
            tomlParent,
            ParseString(playerSettings, "root_dir") ?? ""
        );

        string? baseLoadoutPath = ParseString(playerSettings, "loadout_file");
        string? loadoutPath = CombinePaths(rootDir, baseLoadoutPath);

        string name = ParseString(playerSettings, "name") ?? "";
        string agentId = ParseString(playerSettings, "agent_id") ?? "";

        return new PlayerConfigurationT
        {
            Variety = PlayerClassUnion.FromCustomBot(new CustomBotT()),
            Team = team,
            Name = name,
            RootDir = CombinePaths(tomlParent, ParseString(playerSettings, "root_dir") ?? ""),
            RunCommand = GetRunCommand(playerSettings),
            Loadout = loadoutPath is not null ? GetPlayerLoadout(loadoutPath, team) : null,
            Hivemind = ParseBool(playerSettings, "hivemind", false),
            AgentId = agentId,
        };
    }

    public static PlayerConfigurationT GetPsyonixConfig(
        PsyonixT botSkill,
        string tomlPath,
        uint team
    )
    {
        string tomlParent = Path.GetDirectoryName(tomlPath) ?? "";
        TomlTable playerToml = GetTable(tomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings");

        string rootDir = Path.Combine(
            tomlParent,
            ParseString(playerSettings, "root_dir") ?? ""
        );

        string? baseLoadoutPath = ParseString(playerSettings, "loadout_file");
        string? loadoutPath = CombinePaths(rootDir, baseLoadoutPath);

        string name = ParseString(playerSettings, "name") ?? "";
        string agentId = ParseString(playerSettings, "agent_id") ?? "";

        return new PlayerConfigurationT
        {
            Variety = PlayerClassUnion.FromPsyonix(botSkill),
            Team = team,
            Name = name,
            RootDir = CombinePaths(tomlParent, ParseString(playerSettings, "root_dir") ?? ""),
            RunCommand = GetRunCommand(playerSettings),
            Loadout = loadoutPath is not null ? GetPlayerLoadout(loadoutPath, team) : null,
            Hivemind = ParseBool(playerSettings, "hivemind", false),
            AgentId = agentId,
        };
    }

    public static PlayerConfigurationT GetHumanConfig(uint team)
    {
        return new PlayerConfigurationT
        {
            Variety = PlayerClassUnion.FromHuman(new HumanT()),
            Team = team,
            Name = "Human",
            RootDir = "",
            RunCommand = "",
            AgentId = "",
        };
    }

    public static ScriptConfigurationT GetScriptConfig(string tomlPath)
    {
        string tomlParent = Path.GetDirectoryName(tomlPath) ?? "";
        TomlTable playerToml = GetTable(tomlPath);

        TomlTable playerSettings = ParseTable(playerToml, "settings");

        string rootDir = Path.Combine(
            tomlParent,
            ParseString(playerSettings, "root_dir") ?? ""
        );

        string baseLoadoutPath = ParseString(playerSettings, "loadout_file") ?? "";
        string loadoutPath = Path.Combine(rootDir, baseLoadoutPath);

        string name = ParseString(playerSettings, "name") ?? "";
        string agentId = ParseString(playerSettings, "agent_id") ?? "";

        return new ScriptConfigurationT
        {
            Name = name,
            RunCommand = GetRunCommand(playerSettings),
            AgentId = agentId,
        };
    }
}
