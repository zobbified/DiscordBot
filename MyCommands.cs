using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using System.Collections.Generic;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)] // Allow these commands in DMs

[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands() : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("hello", "Say hello back!")]
    public async Task HelloCommand()
    {
        await RespondAsync($"👋 Hello, {Context.User.Username}!");
    }

    [SlashCommand("ping", "Test the bot latency.")]
    public async Task PingCommand()
    {
        await RespondAsync("🏓 Pong!");
    }

    [SlashCommand("jelq", "Start jelqing.")]
    public async Task JelqCommand()
    {

        await RespondAsync("Jelqing...");
    }

}
