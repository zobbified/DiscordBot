using Discord.Interactions;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SQL;
using DiscordBot.Commands.AI;
using Discord.WebSocket;

namespace DiscordBot.Commands.Dating
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall)]

    [Group("date", "Anime Dating Sim Commands")]
    public class DatingCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly Helper _dbHelper = new();
        private static readonly AICommands ai = new();
        private static readonly Dictionary<ulong, List<Embed>> openPackCards = [];

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

            string prompt = await ai.GenerateTextFromReplicateAsync("generate a short AI image prompt for a random woman from a typical anime dating sim. give them a unique occupation or hobby. do not make librarians or bee girls or girls who only love books. no cherry blossoms. only return the prompt");

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

            string name = await ai.GenerateTextFromReplicateAsync("generate a random name from a random nationality for this person (only return their name)", attachment);
            string info = await ai.GenerateTextFromReplicateAsync($"generate info for this dating sim character named {name}, make sure their age is at least 18");
            string img = attachment.Url;
            _dbHelper.SaveGirl(Context.User.Id, name, info, img);
            string greeting = await ai.GenerateTextFromReplicateAsync(
        $"you are roleplaying as this fictional person named {name}, their background: {info}. Generate a very short greeting for them introducing their name"
    );
            // Build embed with image + greeting
            var embed = new EmbedBuilder()
                .WithTitle(name)
                .WithDescription(greeting)
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

            // Build a single embed listing all girls
            var description = string.Join("\n", allGirls.Select((g, i) => $"**{i + 1}.** {g.name}"));

            var embed = new EmbedBuilder()
                .WithTitle("💘 Girls You've Met")
                .WithDescription(description)
                .WithColor(Color.Magenta)
                .WithFooter($"Total: {allGirls.Count} girl{(allGirls.Count == 1 ? "" : "s")}")
                .Build();

            await FollowupAsync(embed: embed);
        }

        public static IEnumerable<string> SplitIntoChunks(string str, int maxChunkSize = 1024)
        {
            int offset = 0;
            while (offset < str.Length)
            {
                int length = Math.Min(maxChunkSize, str.Length - offset);
                // Try to break on newline or space within the chunk for better formatting
                int lastNewline = str.LastIndexOf('\n', offset + length - 1, length);
                int lastSpace = str.LastIndexOf(' ', offset + length - 1, length);

                int breakPos = Math.Max(lastNewline, lastSpace);
                if (breakPos >= offset && breakPos < offset + length && breakPos != -1)
                {
                    length = breakPos - offset + 1;
                }

                yield return str.Substring(offset, length).Trim();
                offset += length;
            }
        }


        [SlashCommand("talk", "Talk to one of your hoes.")]
        public async Task TalkWithDate(string name, string prompt, Attachment? image = null)
        {
            await DeferAsync();

            var allGirls = _dbHelper.GetGirl(Context.User.Id);
            if (allGirls == null || allGirls.Count == 0)
            {
                await FollowupAsync("You haven't met anyone yet. Try `/new` to meet someone.");
                return;
            }

            var matchedGirl = allGirls
                .FirstOrDefault(g => g.name.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (matchedGirl == default)
            {
                await FollowupAsync($"No one found with the name **{name}**. Use `/list` to see your dates.");
                return;
            }

            string reply = await ai.GenerateTextFromReplicateAsync(
                $"You are roleplaying as this character: {matchedGirl}. Respond to the user saying this: {prompt}.", image
            );

            // Truncate reply to max embed description length (4096 chars)
            if (reply.Length > 4096)
                reply = reply.Substring(0, 4093) + "...";

            var embed = new EmbedBuilder()
                .WithTitle(matchedGirl.name)
                .WithImageUrl(matchedGirl.img)
                .WithDescription(reply)
                .WithColor(Color.Magenta)
                .Build();

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("create", "Create a new person to go on a date with.")]
        public async Task CreateDate(string prompt, string? name = null)
        {
            await DeferAsync();

            // 1. Generate the image prompt based on user input
            await FollowupAsync("🧠 Thinking about your dream date...");
            string aiPrompt = await ai.GenerateTextFromReplicateAsync(
                $"Generate a short AI image prompt for an anime dating sim character based on this description: {prompt}. " +
                "Give them a unique occupation or hobby. Do not make them a librarian or a schoolgirl or anything typical. Only return the prompt."
            );

            // 2. Generate the image
            await FollowupAsync("🎨 Drawing your date...");
            Embed image = await ai.GenerateImageFromStableDiffusionAsync(aiPrompt);
            string imageUrl = image.Image?.Url;

            var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            using var imageStream = new MemoryStream(imageBytes);

            // 3. Upload to Discord for a permanent attachment URL
            var tempUploadMsg = await Context.Interaction.FollowupWithFileAsync(
                imageStream,
                "image.png",
                text: "📤 Uploading your date..."
            );
            var attachment = tempUploadMsg.Attachments.First();
            string img = attachment.Url;

            // 4. Generate name, info, and greeting — in parallel
            
            string Name = name ?? await ai.GenerateTextFromReplicateAsync("generate a random name from a random nationality for this person (only return their name)", attachment);


            var infoTask = ai.GenerateTextFromReplicateAsync($"Generate info for this dating sim character named {name}, make sure their age is at least 18");
            var greetingTask = ai.GenerateTextFromReplicateAsync(
                $"You are roleplaying as this fictional person named {name}. Generate a very short, friendly greeting introducing themselves. Keep it under 300 characters."
            );

            await Task.WhenAll(infoTask, greetingTask);
            string info = infoTask.Result;
            string greeting = greetingTask.Result;

            // 5. Save to DB
            _dbHelper.SaveGirl(Context.User.Id, name, info, img);

            // 6. Final response with image + greeting
            using var finalStream = new MemoryStream(imageBytes); // reuse image
            var embed = new EmbedBuilder()
                .WithTitle(name)
                .WithDescription(greeting)
                .WithImageUrl("attachment://image.png")
                .WithColor(Color.Magenta)
                .Build();

            await FollowupWithFileAsync(finalStream, "image.png", embed: embed);
        }

    }

}
