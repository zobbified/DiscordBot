using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SlashDMExample
{

    class Program
    {
        private DiscordSocketClient _client;
        private InteractionService _commands;
        private IServiceProvider _services;

        static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                AlwaysDownloadUsers = true
            });

            _commands = new InteractionService(_client.Rest);
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteraction;

            string token = "MTM1OTYzNTUyNzA5MDk2NjUzOA.GOl8Jl.mLsYN7V4Qrwm28rWGZWySLD109eYOAvV0hEkCE";
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task ReadyAsync()
        {
            // Register the commands
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Register to global commands (so they work in DMs too)
            await _commands.RegisterCommandsGloballyAsync(); // true = enable in DMs
            Console.WriteLine("✅ Slash commands registered globally.");
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _commands.ExecuteCommandAsync(context, _services);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

    }
}
