using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SQL;
using DiscordBot.Commands.AI;

namespace DiscordBot.Commands.Dating
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall)]

    [Group("date", "Anime Dating Sim Commands")]
    public class DatingCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly Helper _dbHelper = new();
        private static readonly AICommands ai = new();

        [SlashCommand("jelq", "Start jelqing.")]
        public async Task JelqCommand()
        {
            try
            {
                await RespondAsync("Commencing Jelq Session.");
                await Task.Delay(3000);

                Random rng = new Random();
                double amount = rng.NextDouble();
                amount /= 10;
                _dbHelper.SaveJelq(default, amount);
                await FollowupAsync($"Gained {Math.Round(amount, 2)} inches. \nTotal inches I've gained from jelqing: {Math.Round(_dbHelper.GetJelq(), 2)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await FollowupAsync(ex.Message);
            }
        }

        [SlashCommand("new", "Find a new person to go on a date with.")]
        public async Task FindDate()
        {
            await DeferAsync();

            string prompt = await ai.GenerateTextFromReplicateAsync("generate a short AI image prompt for a random woman from a typical anime dating sim"); 

            Embed image = await ai.GenerateImageFromStableDiffusionAsync(prompt);
            string imageUrl = image.Image?.Url;

            // 2. Download image
            var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            using var imageStream = new MemoryStream(imageBytes);

            // 3. Upload to Discord to get an Attachment
            var msg = await Context.Interaction.FollowupWithFileAsync(
        imageStream,
        "image.png",
        text: "Uploading image...",
        ephemeral: true // 👈 private upload
    );
            var attachment = msg.Attachments.First(); // Now you have a real Attachment object

            string info = await ai.GenerateTextFromReplicateAsync("generate a random description of this person from a anime dating sim. make sure to include their name, age, birthday, likes, dislikes, etc. keep it less than 1024 characters", attachment);

            _dbHelper.SaveGirl(Context.User.Id, info);
            string greeting = await ai.GenerateTextFromReplicateAsync(
        $"you are roleplaying as this fictional person: {info}. Generate a greeting for them"
    );
            // Build embed with image + greeting
            var embed = new EmbedBuilder()
                .WithTitle("🎀 Your New Date")
                .WithDescription($"{greeting}\n\n**Character Info:**\n{info}")
                .WithImageUrl("attachment://image.png")
                //.WithColor(Color.Magenta)
                .Build();

            // Reuse the image stream for followup
            using var imageStreamForFollowup = new MemoryStream(imageBytes);
            await FollowupWithFileAsync(imageStreamForFollowup, "image.png", embed: embed);

            // Optional: Clean up the temp upload message
            //await msg.DeleteAsync();
        }
        [SlashCommand("list", "See the list of girls you've met.")]
        public async Task DatingList()
        {
            await DeferAsync();
            var allGirls = _dbHelper.GetGirl(Context.User.Id);

            if (allGirls == null || allGirls.Count == 0)
            {
                await FollowupAsync("You haven't met any girls yet. Use `/new` to meet someone!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("💘 Your Dating History")
                .WithColor(Color.Magenta);

            int index = 1;
            foreach (var girl in allGirls)
            {
                string trimmed = girl.info.Length > 1024 ? girl.info.Substring(0, 1021) + "..." : girl.info;
                embed.AddField($"Girl #{index}", trimmed);
                index++;

                // Discord allows max 25 fields per embed
                if (index > 25) break;
            }

            await FollowupAsync(embed: embed.Build());
        }
        [SlashCommand("talk", "Talk to one of your hoes.")]
        public async Task TalkWithDate(string name)
        {
            await DeferAsync();

            var allGirls = _dbHelper.GetGirl(Context.User.Id);

            if (allGirls == null || allGirls.Count == 0)
            {
                await FollowupAsync("You haven't met anyone yet. Try `/new` to meet someone.");
                return;
            }

            // Search for the girl by name (case-insensitive)
            var matchedGirl = allGirls
                .FirstOrDefault(g => g.info.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (matchedGirl == default)
            {
                await FollowupAsync($"No one found with the name **{name}**. Use `/list` to see your dates.");
                return;
            }

            // Generate a response from that girl using AI
            string reply = await ai.GenerateTextFromReplicateAsync(
                $"You are roleplaying as this character: {matchedGirl}. Respond to the user saying something sweet or flirty."
            );

            await FollowupAsync(reply);
        }
    }
}
