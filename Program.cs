using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Reflection;

namespace SlashDMExample
{
    class Program
    {
        private DiscordSocketClient? _client;
        private InteractionService? _commands;
        private IServiceProvider? _services;

        static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            // Load config.json and handle potential issues
            BotConfig? config = new();
            try
            {
                string json = File.ReadAllText("config.json");
                config = JsonConvert.DeserializeObject<BotConfig>(json);
                if (config == null)
                {
                    Console.WriteLine("❌ Failed to load configuration: config.json is missing or invalid.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading config.json: {ex.Message}");
                return;
            }

            // Initialize the Discord client and command services
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers,
                AlwaysDownloadUsers = true
            });

            _commands = new InteractionService(_client.Rest);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            // Event handlers
            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteraction;

            // Log in and start the bot
            string token = config.DiscordToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("❌ Discord token is missing or invalid in config.json.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block the program until it is closed
            await Task.Delay(-1);
        }

        private async Task ReadyAsync()
        {
            try
            {
                // Register the commands
                await _commands!.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

                // Register global commands (this will enable the commands to work in DMs too)
                await _commands.RegisterCommandsGloballyAsync();

                // Register guild commands
                await _commands.RegisterCommandsToGuildAsync(ulong.Parse(ConfigManager.Config.ServerID));
                Console.WriteLine("Slash commands registered globally.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error registering commands: {ex.Message}");
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client!, interaction); // Using '!' to assert non-null
                await _commands!.ExecuteCommandAsync(context, _services!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error executing interaction: {ex.Message}");
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
