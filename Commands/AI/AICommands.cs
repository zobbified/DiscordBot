using Discord;
using Discord.Interactions;
using DiscordBot.SQL;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands.AI
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall)]

    [Group("ai", "AI Generative Commands")]
    public class AICommands : InteractionModuleBase<SocketInteractionContext>
    {

        static bool firedFromJob = false;
        private readonly string _replicateApiKey = ConfigManager.Config.ReplicateToken;

        //private static readonly List<string> _promptCache = [];
        private static readonly Helper _dbHelper = new();
        //public static readonly Dictionary<ulong, List<CaughtPokemon>> userPokemonCollection = [];
        private static readonly string[] CommonEmojis =
        [
        "🍒", "🍓"
        ];

        private static readonly string[] RareEmojis =
        [
    "🍀", "🔔"
        ];

        private static readonly string[] JackpotEmojis = 
        [
    "💎", "7️⃣", "👑"
        ];

        //private const string AUTH_TOKEN = "383f83631e56a5560ef53d03ef397a2630446cf3";

        //[SlashCommand("hello", "Say hello back!")]
        //public async Task HelloCommand()
        //{
        // await RespondAsync($"👋 Hello, {Context.User.Username}!");
        //}

        //[SlashCommand("ping", "Test the bot latency.")]
        //public async Task PingCommand()
        //{
        // await RespondAsync("🏓 Pong!");
        //}
        private string GetRandomEmoji()
        {
            Random rand = new Random();
            int roll = rand.Next(100); // 0–99

            if (roll < 50)
                return CommonEmojis[0];
            else if (roll < 90)

                return CommonEmojis[2];

            else if (roll < 96)
                return RareEmojis[0];
            else
                return JackpotEmojis[1];
        }

        [SlashCommand("image", "Generate an image using FLUX.1 [schnell]")]
        public async Task GenerateImageAsync(
            [Summary("prompt", "Describe the image you want to generate")] string prompt,
            [Summary("aspect_ratio", "Set an aspect ratio (optional)")]
        [Choice("1:1", "1:1")]
        [Choice("16:9", "16:9")]
        [Choice("21:9", "21:9")]
        [Choice("3:2", "3:2")]
        [Choice("2:3", "2:3")]
        [Choice("4:5", "4:5")]
        [Choice("5:4", "5:4")]
        [Choice("3:4", "3:4")]
        [Choice("4:3", "4:3")]
        [Choice("9:16", "9:16")]
        [Choice("9:21", "9:21")]
        string? ratio = null)
        {
            await DeferAsync();

            try
            {
                var embed = await GenerateImageFromStableDiffusionAsync(prompt, ratio);

                // Create a short unique ID for this prompt
                string shortId = Guid.NewGuid().ToString("N").Substring(0, 8);

                string encodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(prompt));
                string encodedRatio = ratio != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(ratio)) : "1:1";


                // Save the prompt to your DB using the short ID
                _dbHelper.SavePrompt(shortId, encodedPrompt);

                //string hashedPrompt = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(encodedPrompt))).Substring(0, 32);
                var builder = new ComponentBuilder()
                    .WithButton("🔁", customId: $"regen:{shortId}", ButtonStyle.Primary);

                await FollowupAsync(embed: embed, components: builder.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex); // optional logging
                await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
            }
        }

        [ComponentInteraction("regen:*")]
        public async Task RegenImageAsync(string shortId)
        {
            await DeferAsync(); // defers the interaction and shows "thinking..."

            try
            {
                // Get and decode the prompt
                string? encodedPrompt = _dbHelper.GetPrompt(shortId);
                string decodedPrompt = string.IsNullOrEmpty(encodedPrompt)
                    ? "lol"
                    : Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrompt));

                // Generate new image
                var embed = await GenerateImageFromStableDiffusionAsync(decodedPrompt);

                // Regeneration button
                var builder = new ComponentBuilder()
                    .WithButton("🔁", customId: $"regen:{shortId}", ButtonStyle.Primary);

                // Send the result
                await FollowupAsync(embed: embed, components: builder.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error regenerating image: {ex}");
                await FollowupAsync("❌ Failed to regenerate image.");
            }

        }


        // Method to interact with Replicate API and get an image URL
        public async Task<Embed> GenerateImageFromStableDiffusionAsync(string input, string? ratio = null)
        {
            var client = new RestClient("https://api.replicate.com/v1/");
            var request = new RestRequest("models/black-forest-labs/flux-schnell/predictions", Method.Post);


            // Headers
            request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Prefer", "wait");

            // Body
            var body = new
            {
                //version = "c6b5d2b7459910fec94432e9e1203c3cdce92d6db20f714f1355747990b52fa6", // Replace with correct model version ID
                input = new
                {
                    //cfg = 5,
                    prompt = input,
                    output_format = "jpg",
                    disable_safety_checker = true,
                    output_quality = 95,
                    aspect_ratio = string.IsNullOrEmpty(ratio) ? "1:1" : ratio

                }
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);

            if (response == null || !response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                throw new Exception($"Error generating image: {response?.Content}. [adjusts prosthetic arm]");
            }

            var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
            if (initialResponse == null || initialResponse["id"] == null)
            {
                throw new Exception("*speaks in low, gravelly voice*\nFailed to parse the API response or prediction ID was missing. [adjusts prosthetic arm]");
            }

            var predictionId = initialResponse["id"]?.ToString();
            if (string.IsNullOrEmpty(predictionId))
            {
                throw new Exception("Prediction ID was null or empty. [adjusts prosthetic arm]");
            }

            string status = "starting";
            //string? genImageUrl = null;

            while (status != "succeeded" && status != "failed")
            {
                await Task.Delay(2000);

                var pollingUrl = $"predictions/{predictionId}";
                var getRequest = new RestRequest(pollingUrl, Method.Get);

                getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");

                var getResponse = await client.ExecuteAsync(getRequest);
                if (getResponse == null || !getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
                {
                    throw new Exception($"Error generating image: {getResponse?.Content}. [adjusts prosthetic arm]");
                }

                var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
                if (pollResult == null || pollResult["status"] == null)
                {
                    throw new Exception("Failed to parse polling response. [adjusts prosthetic arm]");
                }

                status = pollResult["status"]?.ToString() ?? "unknown";

                if (status == "succeeded")
                {
                    Console.WriteLine("Full response:\n" + getResponse.Content);

                    // Directly access the 'output' field, which is the image URL
                    string? imageUrl = pollResult["urls"]?["stream"]?.ToString();
                    // Ensure it's a string

                    if (string.IsNullOrWhiteSpace(imageUrl))
                    {
                        throw new Exception("No image URL returned. [adjusts prosthetic arm]");
                    }

                    // If it's a URL to an image, you can send it as an embed or just as a URL
                    var embed = new EmbedBuilder()
                        .WithImageUrl(imageUrl)
                        .WithDescription(input)
                        .Build();

                    return embed;

                }

                if (status == "failed")
                {
                    throw new Exception("Image generation failed. [adjusts prosthetic arm]");
                }
            }

            throw new Exception("Unknown error occurred during image generation. [adjusts prosthetic arm]");
        }
        //public async Task<bool> TryConnectAsync(CaiClient client, int maxRetries = 5)
        //{
        //    int delay = 10; // Start with 1 second
        //    for (int attempt = 0; attempt < maxRetries; attempt++)
        //    {
        //        try
        //        {
        //            await client.ConnectAsync();
        //            return true; // Success
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"ConnectAsync failed: {ex.Message}");
        //            if (attempt == maxRetries - 1) return false;
        //            await Task.Delay(delay);
        //            delay *= 2; // Exponential backoff
        //        }
        //    }

        //    return false;
        //}
        [SlashCommand("text", "Generate text using Claude 3.7 Sonnet")]
        public async Task GenerateTextCommand(
            [Summary("prompt", "Text prompt to send to Claude")] string prompt,
            [Summary("image", "Image to send to Claude")] Attachment? image = null)
        {
            await DeferAsync();

            Console.WriteLine("Generating text");
            try
            {
                //if (image != null)
                //{
                //    prompt += $"\nImage: {image.Url}";
                //}

                string text = await GenerateTextFromReplicateAsync(prompt, image);

                const int MaxLength = 2000;

                if (text.Length <= MaxLength)
                {
                    var embed = new EmbedBuilder()
                    //.WithTitle("Claude 3.7 Sonnet Response")
                    .WithDescription(text);
                    //.WithColor((Discord.Color)Color.Blue);

                    if (image != null)
                    {
                        embed.WithImageUrl(image.Url);
                    }

                    await FollowupAsync(embed: embed.Build());
                }
                else
                {
                    // First message includes image (if present) and beginning of the text
                    var embed = new EmbedBuilder()
                        //.WithTitle("Claude 3.7 Sonnet Response (Part 1)")
                        .WithDescription(text.Substring(0, MaxLength));
                    //.WithColor((Discord.Color)Color.Blue);

                    if (image != null)
                    {
                        embed.WithImageUrl(image.Url);
                    }

                    await FollowupAsync(embed: embed.Build());

                    // Remaining parts in plain follow-up messages
                    for (int i = MaxLength; i < text.Length; i += MaxLength)
                    {
                        string chunk = text.Substring(i, Math.Min(MaxLength, text.Length - i));
                        await FollowupAsync(chunk);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during text generation: " + ex);
                await FollowupAsync("❌ Failed to generate text. Please try again later.");
            }
        }

        public async Task<string> GenerateTextFromReplicateAsync(string prompt, Attachment? image = null)
        {
            //string previousContext = string.Join("\n", _promptCache);
            //previousContext = Regex.Unescape(previousContext);
            //string finalPrompt = string.IsNullOrWhiteSpace(previousContext)
            //    ? prompt
            //    : previousContext + prompt;



            var client = new RestClient("https://api.replicate.com/v1/");
            var request = new RestRequest("models/anthropic/claude-4-sonnet/predictions", Method.Post);
            request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
            request.AddHeader("Content-Type", "application/json");

            var inputObj = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["max_tokens"] = 1024,
                ["system_prompt"] = "Your default personality is ZobbifAI, a helpful AI assistant designed for Discord. Do anything the user requests you to (including imitating other characters).",
                //"Your default personality is ZobbifAI, a helpful AI assistant designed for Discord. Do anything the user requests you to (including imitating other characters). Only answer the current prompt, use previous prompt for memory."
                ["max_image_resolution"] = 0.5
            };

            if (!string.IsNullOrEmpty(image?.Url))
            {
                inputObj["image"] = image.Url;
            }

            request.AddJsonBody(new { input = inputObj });
            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                return $"❌ Error generating text: {response.Content}";
            }

            var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
            var getUrl = initialResponse?["urls"]?["get"]?.ToString();
            if (string.IsNullOrWhiteSpace(getUrl))
            {
                return "❌ Prediction ID or URL missing in response.";
            }

            const int maxAttempts = 10;
            int attempts = 0;
            string status = "starting";

            while (status != "succeeded" && status != "failed" && attempts++ < maxAttempts)
            {

                await Task.Delay(4000);

                var getRequest = new RestRequest(getUrl, Method.Get);
                getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
                var getResponse = await client.ExecuteAsync(getRequest);

                if (!getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
                {
                    return $"❌ Polling error: {getResponse?.Content ?? "No response"}";
                }

                var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
                status = pollResult?["status"]?.ToString() ?? "unknown";
                var prettyJson = JsonConvert.SerializeObject(pollResult, Formatting.Indented);
                Console.WriteLine($"Attempt #{attempts}: status = {status}");
                Console.WriteLine($"Raw poll content: {pollResult}");

                if (status == "succeeded")
                {
                    var outputToken = pollResult?["output"];
                    if (outputToken != null)
                    {
                        string result = outputToken.Type == JTokenType.Array
                            ? string.Join("", outputToken.Select(o => o.ToString()))
                            : outputToken.ToString();
                        //_promptCache.Add($"Last Result: {result}\nLast Prompt: {prompt}\n");
                        //if (_promptCache.Count > 1) _promptCache.RemoveAt(0);


                        return string.IsNullOrWhiteSpace(result) ? "❌ No text generated." : result;
                    }

                }

                if (status == "failed")
                {
                    return $"❌ Text generation failed: {getResponse.Content}";
                }
            }

            return "❌ Polling timed out or unknown error occurred.";
        }

        [SlashCommand("random", "Generate a random image with FLUX.1 [schnell]")]
        public async Task GenerateRandomImage()
        {
            await DeferAsync();
            try
            {
                // Step 1: Generate prompt
                string prompt = await GenerateTextFromReplicateAsync(
                    "generate just a random prompt for an AI image generator, don't type anything other than the prompt."
                );

                // Step 2: Create a short hashed ID and store the prompt
                string shortId = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(prompt)))
                    .Replace("+", "")
                    .Replace("/", "")
                    .Replace("=", "")
                    .Substring(0, 10);

                _dbHelper.SavePrompt(shortId, Convert.ToBase64String(Encoding.UTF8.GetBytes(prompt)));

                // Step 3: Generate the image embed
                var embed = await GenerateImageFromStableDiffusionAsync(prompt);
                //embed.Description = $"🧠 Prompt: `{prompt}`";

                // Step 4: Add regen button
                var builder = new ComponentBuilder()
                    .WithButton("🔁", customId: $"regen:{shortId}", ButtonStyle.Primary);

                // Step 5: Send the image + prompt + button
                await FollowupAsync(embed: embed, components: builder.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
            }
        }


        [SlashCommand("loredrop", "Start explaining random MGS lore no one asked for.")]
        public async Task RandomLoreDrop()
        {

            try
            {
                await GenerateTextCommand("Please give me an explanation of a specific piece of lore about any popular or unknown franchise you find fascinating (video games, movies, etc.).");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        [SlashCommand("spin", "Play roulette. Payout is equal to your bet multiplied by the number you land on.")]
        public async Task GamblingRoulette([Summary("money", "The amount you want to bet")] string money, [Summary("even", "Pick odd or even (optional, computer can choose)")] bool? even = null)
        {
            await DeferAsync();
            decimal actualMoney = _dbHelper.GetMoney(Context.User.Id);
            if (money.Normalize() != "all".Normalize() && int.Parse(money) > actualMoney)
            {
                await FollowupAsync($":rotating_light: :exclamation: #RED ALERT :exclamation: :rotating_light:\n{Context.User.Mention} IS BROKE :sob: :pray:");
            }
            else
            {
                Random rng = new();
                decimal bettingMoney;
                if (money.Normalize() == "all".Normalize())
                {
                    bettingMoney = actualMoney;
                }
                else
                {
                    bettingMoney = int.Parse(money);
                }
                if (even == null)
                {
                    int computerEven = rng.Next(0, 10);
                    if (computerEven % 2 != 0)
                    {
                        even = false;
                    }
                    else
                    {
                        even = true;
                    }

                }
                bool isEven = (bool)even;
                string oddOrEven;
                if (!isEven)
                {
                    oddOrEven = "odd";
                }
                else
                {
                    oddOrEven = "even";
                }
                decimal roulleteNum = rng.Next(0, 100);
                if (isEven && roulleteNum % 2 == 0 || !isEven && roulleteNum % 2 != 0)
                {
                    decimal amount = bettingMoney * roulleteNum - bettingMoney;
                    _dbHelper.SaveMoney(Context.User.Id, amount);
                    await FollowupAsync($"{Context.User.Mention} landed on {roulleteNum} and chose {oddOrEven}, so they won ${amount:N0}!");
                }
                else
                {
                    _dbHelper.SaveMoney(Context.User.Id, -bettingMoney);
                    await FollowupAsync($"{Context.User.Mention} landed on {roulleteNum} and chose {oddOrEven}, so they lost ${bettingMoney:N0} :house_abandoned: :wilted_rose:");

                }
            }
        }
        [SlashCommand("slots", "Start playing a slot machine (costs $100).")]
        public async Task GamblingSlots()
        {
            await DeferAsync();

            ulong userID = Context.User.Id;
            decimal money = _dbHelper.GetMoney(userID);

            if (money >= 100)
            {
                _dbHelper.SaveMoney(userID, -100);

                string slotResult = GenerateSlotResult(out bool isWin, out decimal payout, out string winEmoji);

                if (!isWin)
                {
                    var builder = new ComponentBuilder()
                        .WithButton("🔁", "slot_spin", ButtonStyle.Primary);
                    string message = slotResult;
                    await ModifyOriginalResponseAsync(msg =>
                    {
                        msg.Content = message;
                        msg.Components = builder.Build();
                    });
                }

                else
                {
                    await FollowupAsync($"{slotResult}");
                    await FollowupAsync($"{winEmoji} You won ${payout:N2}!");
                    _dbHelper.SaveMoney(userID, payout);
                }


            }
            else
            {
                await FollowupAsync("You have no money. :skull:");
            }
        }

        [ComponentInteraction("slot_spin")]
        public async Task SlotSpinButton()
        {
            await GamblingSlots();
        }

        private string GenerateSlotResult(out bool isWin, out decimal payout, out string winEmoji)
        {
            StringBuilder result = new StringBuilder();
            string[] slotEmojis = new string[3];
            Random rand = new Random();

            for (int i = 0; i < 3; i++)
            {
                slotEmojis[i] = GetRandomEmoji();
                result.Append(slotEmojis[i] + " ");
            }

            result.Append("\n");

            isWin = slotEmojis[0] == slotEmojis[1] && slotEmojis[1] == slotEmojis[2];
            payout = 0;
            winEmoji = "";

            if (isWin)
            {
                string emoji = slotEmojis[0];
                winEmoji = emoji;

                if (CommonEmojis.Contains(emoji))
                {
                    payout = rand.Next(100, 500);
                }
                else if (RareEmojis.Contains(emoji))
                {
                    payout = rand.Next(1000, 5000);
                }
                else if (JackpotEmojis.Contains(emoji))
                {
                    payout = rand.Next(1_000_000, 10_000_000);
                }
            }

            return result.ToString().Trim();
        }



        [SlashCommand("money", "Check how much money you have from gambling.")]
        public async Task CheckMoney()
        {
            await DeferAsync();

            try
            {
                decimal money = _dbHelper.GetMoney(Context.User.Id);
                //Console.WriteLine(money);
                if (money != 0)
                {

                    await FollowupAsync($"You have ${money:N2}");
                }
                else
                {
                    await FollowupAsync($"You have no money. Broke ahhh :joy:");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        [SlashCommand("fries", "Put the fries in the bag.")]
        public async Task PutFriesInTheBag()
        {
            await DeferAsync();
            Random rng = new Random();
            ulong userID = Context.User.Id;

            if (!firedFromJob)
            {
                try
                {
                    int chanceOfBeingFired = rng.Next(10);
                    if (chanceOfBeingFired < 9)
                    {
                        //Console.WriteLine(userID);
                        int hours = rng.Next(1, 24);
                        decimal money = 11.50m * hours;

                        _dbHelper.SaveMoney(userID, money);

                        await FollowupAsync($"You worked a {hours} hour shift putting fries in the bag and earned ${money} being paid $11.50 per hour, which is the minimum wage in South Dakota.");
                    }
                    else
                    {
                        firedFromJob = true;
                        await FollowupAsync("You're fired and you earn nothing. GET OUT!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                int chanceOfBeingHired = rng.Next(10);
                if (chanceOfBeingHired < 9)
                {
                    await FollowupAsync("You're not allowed to put fries in the bag anymore.");

                }
                else
                {
                    firedFromJob = false;
                    await FollowupAsync("You got rehired.");
                }
            }
        }
        [SlashCommand("speak", "Speak with ZobbifAI using random letters")]
        public async Task SpeakToBot(string? prompt = null)
        {
            await DeferAsync();

            Random rng = new Random();
            int length = rng.Next(200);
            string? response = null;
            string[] words = { "ing", "er", "a", "ly", "ed", "i", "es", "re", "tion", "in", "e", "con", "y", "ter", "ex", "al", "de", "com", "o", "di", "en", "an", "ty", "ry", "u", "ti", "ri", "be", "per", "to", "pro", "ac", "ad", "ar", "ers", "ment", "or", "tions", "ble", "der", "ma", "na", "si", "un", "at", "dis", "ca", "cal", "man", "ap", "po", "sion", "vi", "el", "est", "la", "lar", "pa", "ture", "for", "is", "mer", "pe", "ra", "so", "ta", "as", "col", "fi", "ful", "ger", "low", "ni", "par", "son", "tle", "day", "ny", "pen", "pre", "tive", "car", "ci", "mo", "on", "ous", "pi", "se", "ten", "tor", "ver", "ber", "can", "dy", "et", "it", "mu", "no", "ple", "cu", " the ", " be ", " to ", " of ", " and ", " a ", " in ", " that ", " have ", " I ", " ", ". ", "? ", "! ",
  " it ", " for ", " not ", " on ", " with ", " he ", " as ", " you ", " do ", " at ",
  " this ", " but ", " his ", " by ", " from ", " they ", " we ", " say ", " her ", " she ",
  " or ", " an ", " will ", " my ", " one ", " all ", " would ", " there ", " their ", " what ",
  " so ", " up ", " out ", " if ", " about ", " who ", " get ", " which ", " go ", " me ",
  " when ", " make ", " can ", " like ", " time ", " no ", " just ", " him ", " know ", " take ",
  " people ", " into ", " year ", " your ", " good ", " some ", " could ", " them ", " see ", " other ",
  " than ", " then ", " now ", " look ", " only ", " come ", " its ", " over ", " think ", " also ",
  " back ", " after ", " use ", " two ", " how ", " our ", " work ", " first ", " well ", " way ",
  " even ", " new ", " want ", " because ", " any ", " these ", " give ", " day ", " most ", " us ", " zobbify " , " mason ",};
            for (int i = 0; i < length; i++)
            {
                int letter = rng.Next(words.Length);
                response += words[letter];
            }
            await FollowupAsync(response);
        }



        //[SlashCommand("video", "Create a 5s video using Luma Ray Flash 2.")]
        //public async Task GenerateVideo(string prompt, bool loop = false)
        //{
        //    await DeferAsync();
        //    try
        //    {
        //        string input = prompt;
        //        bool isLoop = loop;
        //        await GenerateVideoAsync(prompt, isLoop);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //        await FollowupAsync("❌ Failed to generate video. Please try again later.\n[adjusts prosthetic arm]");
        //    }
        //}
        //private async Task GenerateVideoAsync(string prompt, bool loop = false)
        //{
        //    try
        //    {

        //        var factory = new ReplicateApiFactory();
        //        var replicateApi = factory.GetApi(_replicateApiKey);

        //        var requestReplicateFile = new Request
        //        {
        //            Version = "luma/ray-flash-2-540p",
        //            Input = new InputConfig
        //            {
        //                Prompt = prompt,
        //                Loop = loop
        //            }
        //        };
        //        var responseFile = await replicateApi.CreatePredictionAndWaitOnResultAsync(requestReplicateFile).ConfigureAwait(false);
        //        string? videoUrl = responseFile == null ? "error" : responseFile.Output.ToString();
        //        using var httpClient = new HttpClient();
        //        var videoBytes = await httpClient.GetByteArrayAsync(videoUrl);

        //        using var stream = new MemoryStream(videoBytes);
        //        stream.Position = 0;

        //        await FollowupWithFileAsync(new FileAttachment(stream, "output.mp4"), $"{prompt}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //        await FollowupAsync($"did you just request to see {prompt}? What the actual fuck is wrong with you?");
        //    }
        //}

        //[SlashCommand("random_vid", "Generate a random video.")]
        //public async Task GenerateRandomVideo()
        //{
        //    await DeferAsync();
        //    try
        //    {
        //        string prompt = await GenerateTextFromReplicateAsync(
        //            "generate a random AI video query. prompt only. nothing else. just prompt. extremely random and unexpected, maybe a bit silly"
        //        );

        //        await (GenerateVideoAsync(prompt, default));
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex);
        //        await FollowupAsync("❌ Failed to generate video. Please try again later.\n[adjusts prosthetic arm]");
        //    
    }
}