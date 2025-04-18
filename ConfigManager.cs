using DiscordBot;
using Newtonsoft.Json;

public static class ConfigManager
{
    private static BotConfig? _config;

    public static BotConfig Config
    {
        get
        {
            if (_config == null)
            {
                string json = File.ReadAllText("config.json");
                _config = JsonConvert.DeserializeObject<BotConfig>(json);
            }
            return _config!;
        }
    }
}
