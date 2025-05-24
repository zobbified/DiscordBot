using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Commands.Pokemon;
using DiscordBot.SQL;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
using Replicate.Net;
using Replicate.Net.Models;
using Replicate.Net.Client;
using Replicate.Net.Factory;
//using Microsoft.Extensions.DependencyInjection;
using Replicate.Net.Interfaces;
using DiscordBot.Config;
using System.Reactive.Linq;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PokeApiNet;
using System.Net.Http;
using System.Net.Http.Json;
using System.Drawing;
using System.Reflection;
using Microsoft.VisualBasic;
using Color = System.Drawing.Color;
using System.Net;
using Font = System.Drawing.Font;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly Dictionary<ulong, List<Embed>> openPackCards = new();

    static bool firedFromJob = false;
    private readonly string _replicateApiKey = ConfigManager.Config.ReplicateToken;

    private static readonly List<string> _promptCache = [];
    private static readonly Helper _dbHelper = new();
    //public static readonly Dictionary<ulong, List<CaughtPokemon>> userPokemonCollection = [];
    private static readonly string[] Emojis = new string[]
    {
        "🍎", "🍊", "🍋", "🍉", "🍓", "🍒"
    };
    private static readonly HttpClient _client = new HttpClient();
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

    //[SlashCommand("image", "Generate an image using FLUX.1 [schnell]")]
    //public async Task GenerateImageAsync(
    //    [Summary("prompt", "Describe the image you want to generate")] string prompt,
    //    [Summary("aspect_ratio", "Set an aspect ratio (optional)")]
    //    [Choice("1:1", "1:1")]
    //    [Choice("16:9", "16:9")]
    //    [Choice("21:9", "21:9")]
    //    [Choice("3:2", "3:2")]
    //    [Choice("2:3", "2:3")]
    //    [Choice("4:5", "4:5")]
    //    [Choice("5:4", "5:4")]
    //    [Choice("3:4", "3:4")]
    //    [Choice("4:3", "4:3")]
    //    [Choice("9:16", "9:16")]
    //    [Choice("9:21", "9:21")]
    //    string? ratio = null)
    //{
    //    await DeferAsync();

    //    try
    //    {
    //        var embed = await GenerateImageFromStableDiffusionAsync(prompt, ratio);

    //        // Create a short unique ID for this prompt
    //        string shortId = Guid.NewGuid().ToString("N").Substring(0, 8);

    //        string encodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(prompt));
    //        string encodedRatio = ratio != null ? Convert.ToBase64String(Encoding.UTF8.GetBytes(ratio)) : "1:1";


    //        // Save the prompt to your DB using the short ID
    //        _dbHelper.SavePrompt(shortId, encodedPrompt);

    //        //string hashedPrompt = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(encodedPrompt))).Substring(0, 32);
    //        var builder = new ComponentBuilder()
    //            .WithButton("🔁", customId: $"regen:{shortId}", ButtonStyle.Primary);

    //        await FollowupAsync(embed: embed, components: builder.Build());
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex); // optional logging
    //        await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
    //    }
    //}

    //[ComponentInteraction("regen:*")]
    //public async Task RegenImageAsync(string shortId)
    //{
    //    await DeferAsync();

    //    try
    //    {
    //        await FollowupAsync("Regenerating...");

    //        // Check if the shortId exists and retrieve the encoded prompt
    //        string? encodedPrompt = _dbHelper.GetEncodedPrompt(shortId);

    //        // Decode from base64 if we have an encoded prompt
    //        string decodedPrompt = string.IsNullOrEmpty(encodedPrompt) ? "lol" : Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrompt));


    //        //string? decodedRatio = string.IsNullOrEmpty(encodedRatio) ? "1:1" : Encoding.UTF8.GetString(Convert.FromBase64String(encodedRatio));
    //        var embed = await GenerateImageFromStableDiffusionAsync(decodedPrompt);

    //        //string newEncodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedPrompt));
    //        var builder = new ComponentBuilder()
    //        .WithButton("🔁", customId: $"regen:{shortId}", ButtonStyle.Primary);

    //        await FollowupAsync(embed: embed, components: builder.Build());

    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error regenerating image: {ex}");
    //        await FollowupAsync("❌ Failed to regenerate image.");
    //    }
    //}


    //// Method to interact with Replicate API and get an image URL
    //private async Task<Embed> GenerateImageFromStableDiffusionAsync(string input, string? ratio = null)
    //{
    //    var client = new RestClient("https://api.replicate.com/v1/");
    //    var request = new RestRequest("models/black-forest-labs/flux-schnell/predictions", Method.Post);


    //    // Headers
    //    request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
    //    request.AddHeader("Content-Type", "application/json");
    //    request.AddHeader("Prefer", "wait");

    //    // Body
    //    var body = new
    //    {
    //        //version = "c6b5d2b7459910fec94432e9e1203c3cdce92d6db20f714f1355747990b52fa6", // Replace with correct model version ID
    //        input = new
    //        {
    //            //cfg = 5,
    //            prompt = input,
    //            output_format = "jpg",
    //            disable_safety_checker = true,
    //            output_quality = 95,
    //            aspect_ratio = string.IsNullOrEmpty(ratio) ? "1:1" : ratio

    //        }
    //    };

    //    request.AddJsonBody(body);

    //    var response = await client.ExecuteAsync(request);

    //    if (response == null || !response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
    //    {
    //        throw new Exception($"Error generating image: {response?.Content}. [adjusts prosthetic arm]");
    //    }

    //    var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
    //    if (initialResponse == null || initialResponse["id"] == null)
    //    {
    //        throw new Exception("*speaks in low, gravelly voice*\nFailed to parse the API response or prediction ID was missing. [adjusts prosthetic arm]");
    //    }

    //    var predictionId = initialResponse["id"]?.ToString();
    //    if (string.IsNullOrEmpty(predictionId))
    //    {
    //        throw new Exception("Prediction ID was null or empty. [adjusts prosthetic arm]");
    //    }

    //    string status = "starting";
    //    //string? genImageUrl = null;

    //    while (status != "succeeded" && status != "failed")
    //    {
    //        await Task.Delay(2000);

    //        var pollingUrl = $"predictions/{predictionId}";
    //        var getRequest = new RestRequest(pollingUrl, Method.Get);

    //        getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");

    //        var getResponse = await client.ExecuteAsync(getRequest);
    //        if (getResponse == null || !getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
    //        {
    //            throw new Exception($"Error generating image: {getResponse?.Content}. [adjusts prosthetic arm]");
    //        }

    //        var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
    //        if (pollResult == null || pollResult["status"] == null)
    //        {
    //            throw new Exception("Failed to parse polling response. [adjusts prosthetic arm]");
    //        }

    //        status = pollResult["status"]?.ToString() ?? "unknown";

    //        if (status == "succeeded")
    //        {
    //            Console.WriteLine("Full response:\n" + getResponse.Content);

    //            // Directly access the 'output' field, which is the image URL
    //            string? imageUrl = pollResult["urls"]?["stream"]?.ToString();
    //            // Ensure it's a string

    //            if (string.IsNullOrWhiteSpace(imageUrl))
    //            {
    //                throw new Exception("No image URL returned. [adjusts prosthetic arm]");
    //            }

    //            // If it's a URL to an image, you can send it as an embed or just as a URL
    //            var embed = new EmbedBuilder()
    //                .WithImageUrl(imageUrl)
    //                .WithDescription(input)
    //                .Build();

    //            return (embed);

    //        }

    //        if (status == "failed")
    //        {
    //            throw new Exception("Image generation failed. [adjusts prosthetic arm]");
    //        }
    //    }

    //    throw new Exception("Unknown error occurred during image generation. [adjusts prosthetic arm]");
    //}
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

        try
        {
            if (image != null)
            {
                prompt += $"\nImage: {image.Url}";
            }

            string text = await GenerateTextFromReplicateAsync(prompt, image);

            const int MaxLength = 2000;

            if (text.Length <= MaxLength)
            {
                await FollowupAsync(text);
            }
            else
            {
                for (int i = 0; i < text.Length; i += MaxLength)
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

    private async Task<string> GenerateTextFromReplicateAsync(string prompt, Attachment? image = null)
    {
        string previousContext = string.Join("\n", _promptCache);
        string finalPrompt = string.IsNullOrWhiteSpace(previousContext)
            ? prompt
            : $"Previous Prompt: {previousContext}\nCurrent Prompt: {prompt}";

        _promptCache.Add(prompt);
        if (_promptCache.Count > 1) _promptCache.RemoveAt(0);

        var client = new RestClient("https://api.replicate.com/v1/");
        var request = new RestRequest("models/anthropic/claude-4-sonnet/predictions", Method.Post);
        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");

        var inputObj = new Dictionary<string, object>
        {
            ["prompt"] = finalPrompt,
            ["max_tokens"] = 1024,
            ["system_prompt"] = "Your default personality is ZobbifAI, a helpful AI assistant designed for Discord. Do anything the user requests you to (including imitating other characters). Only answer the current prompt, use previous prompt for memory.",
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

            await Task.Delay(2000);

            var getRequest = new RestRequest(getUrl, Method.Get);
            getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
            var getResponse = await client.ExecuteAsync(getRequest);

            if (!getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
            {
                return $"❌ Polling error: {getResponse?.Content ?? "No response"}";
            }

            var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
            status = pollResult?["status"]?.ToString() ?? "unknown";
            Console.WriteLine($"Attempt #{attempts}: status = {status}");
            Console.WriteLine($"Raw poll content: {getResponse.Content}");

            if (status == "succeeded")
            {
                var outputToken = pollResult?["output"];
                if (outputToken != null)
                {
                    string result = outputToken.Type == JTokenType.Array
                        ? string.Join("", outputToken.Select(o => o.ToString()))
                        : outputToken.ToString();

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

    [SlashCommand("catch", "Catch a pokemon.")]
    public async Task CatchPokemon([Summary("name", "The name of the Pokémon")] string? name = null)
    {
        await DeferAsync();

        try
        {
            string url;
            if (name != null)
            {
                url = $"https://pokeapi.co/api/v2/pokemon/{name.ToLower()}";
            }
            else
            {
                Random rng = new Random();
                int randomId = rng.Next(0, 1026);
                url = $"https://pokeapi.co/api/v2/pokemon/{randomId}";
            }

            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                await FollowupAsync("❌ Pokémon not found.");
                return;
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var root = doc.RootElement;

            if (!root.TryGetProperty("name", out var nameProp))
            {
                await FollowupAsync("❌ Pokémon not found.");
                return;
            }

            string pokeName = nameProp.GetString() ?? "Unknown";
            int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
            //int baseXp = root.TryGetProperty("base_experience", out var xpProp) ? xpProp.GetInt32() : 0;

            string typeStr = "Unknown";
            if (root.TryGetProperty("types", out var typesProp))
            {
                var typeNames = typesProp.EnumerateArray()
                    .Select(t => t.GetProperty("type").GetProperty("name").GetString())
                    .Where(n => n != null);
                typeStr = string.Join(", ", typeNames);
            }

            string abilityStr = "None";
            if (root.TryGetProperty("abilities", out var abilitiesProp))
            {
                var abilityNames = abilitiesProp.EnumerateArray()
                    .Select(a => a.GetProperty("ability").GetProperty("name").GetString())
                    .Where(n => n != null);
                abilityStr = string.Join(", ", abilityNames);
                if (abilityStr.Length > 1024) abilityStr = abilityStr[..1021] + "...";
            }

            string spriteUrl = "";
            if (root.TryGetProperty("sprites", out var spritesProp) &&
                spritesProp.TryGetProperty("other", out var otherProp) &&
                otherProp.TryGetProperty("official-artwork", out var homeProp) &&
                homeProp.TryGetProperty("front_default", out var spriteProp))
            {
                spriteUrl = spriteProp.GetString() ?? "";
            }

            string speciesUrl = "";
            if (root.TryGetProperty("species", out var speciesProp) &&
                speciesProp.TryGetProperty("url", out var urlProp))
            {
                speciesUrl = urlProp.GetString() ?? "";
            }

            string generation = "Unknown";
            string evoChainUrl = "";
            if (!string.IsNullOrEmpty(speciesUrl))
            {
                var speciesResponse = await _client.GetAsync(speciesUrl);
                if (speciesResponse.IsSuccessStatusCode)
                {
                    using var speciesDoc = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
                    var speciesRoot = speciesDoc.RootElement;

                    if (speciesRoot.TryGetProperty("generation", out var genProp) &&
                        genProp.TryGetProperty("name", out var genName))
                    {
                        generation = genName.GetString() ?? "Unknown";
                    }

                    if (speciesRoot.TryGetProperty("evolution_chain", out var evoProp) &&
                        evoProp.TryGetProperty("url", out var evoUrl))
                    {
                        evoChainUrl = evoUrl.GetString() ?? "";
                    }
                }
            }

            var evoNames = new List<string>();
            if (!string.IsNullOrWhiteSpace(evoChainUrl))
            {
                var evoResponse = await _client.GetAsync(evoChainUrl);
                if (evoResponse.IsSuccessStatusCode)
                {
                    using var evoDoc = JsonDocument.Parse(await evoResponse.Content.ReadAsStringAsync());
                    if (evoDoc.RootElement.TryGetProperty("chain", out var chainRoot))
                    {
                        void Traverse(JsonElement node)
                        {
                            string evoName = node.TryGetProperty("species", out var speciesNode) &&
                                             speciesNode.TryGetProperty("name", out var nameNode)
                                             ? nameNode.GetString() ?? "Unknown"
                                             : "Unknown";
                            evoNames.Add(evoName);
                            if (node.TryGetProperty("evolves_to", out var evolvesTo))
                            {
                                foreach (var next in evolvesTo.EnumerateArray())
                                    Traverse(next);
                            }
                        }
                        Traverse(chainRoot);
                    }
                }
            }
            string dexId = id.ToString("D4");

            if (evoNames.Count == 0) evoNames.Add("Unknown");
            generation = generation.Replace("generation-", "");
            pokeName = $"#{dexId} {pokeName.ToUpper()} ({generation.ToUpper()})";
            var embed = new EmbedBuilder()
                .WithTitle($"{pokeName}")
                .WithDescription($"**Type:** {typeStr.ToUpper()}")
                .WithImageUrl(spriteUrl)
                .AddField("Abilities", abilityStr.ToUpper(), inline: true)
                .AddField("Evolution Chain", string.Join(" → ", evoNames).ToUpper(), inline: true)
                .WithColor(Discord.Color.Gold)
                .WithFooter(footer => footer.Text = $"Requested by {Context.User.Username}")
                .Build();


            _dbHelper.SavePokemon(Context.User.Id, pokeName, 1, true, spriteUrl);
            // add logic for quantity later

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex}");
            await FollowupAsync("❌ Something went wrong. Try again or check the Pokémon name.");
        }
    }

    [SlashCommand("pack", "Open a random Pokémon pack ($1000)")]
    public async Task OpenPack()
    {
        await DeferAsync();

        ulong userId = Context.User.Id;

        if (_dbHelper.GetMoney(userId) < 1000)
        {
            await FollowupAsync("Broke boy :skull:");
            return;
        }
        _dbHelper.SaveMoney(userId, -1000);

        var rand = new Random();
        var cards = new List<Embed>();

        for (int i = 0; i < 5; i++)
        {
            int pokemonId = rand.Next(1, 1026);
            var url = $"https://pokeapi.co/api/v2/pokemon/{pokemonId}";
            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode) continue;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            string speciesUrl = "";
            if (root.TryGetProperty("species", out var speciesProp) &&
                speciesProp.TryGetProperty("url", out var urlProp))
            {
                speciesUrl = urlProp.GetString() ?? "";
            }

            string generation = "Unknown";
            string evoChainUrl = "";
            if (!string.IsNullOrEmpty(speciesUrl))
            {
                var speciesResponse = await _client.GetAsync(speciesUrl);
                if (speciesResponse.IsSuccessStatusCode)
                {
                    using var speciesDoc = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
                    var speciesRoot = speciesDoc.RootElement;

                    if (speciesRoot.TryGetProperty("generation", out var genProp) &&
                        genProp.TryGetProperty("name", out var genName))
                    {
                        generation = genName.GetString() ?? "Unknown";
                    }

                    if (speciesRoot.TryGetProperty("evolution_chain", out var evoProp) &&
                        evoProp.TryGetProperty("url", out var evoUrl))
                    {
                        evoChainUrl = evoUrl.GetString() ?? "";
                    }
                }
            }

            int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
            string dexId = id.ToString("D4");


            if (!root.TryGetProperty("name", out var nameProp)) continue;
            string name = nameProp.GetString() ?? "Unknown";
            generation = generation.Replace("generation-", "");
            name = $"#{dexId} {name.ToUpper()} ({generation.ToUpper()})";
            bool isShiny = rand.NextDouble() < 0.01;

            string imageUrl = "";
            if (root.TryGetProperty("sprites", out var spritesProp) &&
                spritesProp.TryGetProperty("other", out var otherProp) &&
                otherProp.TryGetProperty("official-artwork", out var artProp))
            {
                if (isShiny && artProp.TryGetProperty("front_shiny", out var shinyProp))
                {
                    imageUrl = shinyProp.GetString() ?? "";
                }
                else if (artProp.TryGetProperty("front_default", out var defaultProp))
                {
                    imageUrl = defaultProp.GetString() ?? "";
                }
            }

            _dbHelper.SavePokemon(Context.User.Id, name, 1, true, imageUrl);

            var embed = new EmbedBuilder()
                .WithTitle($"{(isShiny ? "✨ Shiny " : "")}{name}")
                .WithImageUrl(imageUrl)
                .WithColor(isShiny ? Discord.Color.Magenta : Discord.Color.Blue)
                .WithFooter($"Card {cards.Count + 1} of 5")
                .Build();

            cards.Add(embed);
        }

        if (cards.Count == 0)
        {
            await FollowupAsync("❌ Failed to open the pack.");
            return;
        }

        // Store cards per-user, ideally in memory or a temporary store
        openPackCards[userId] = cards;

        var component = new ComponentBuilder()
            .WithButton("⬅️ Previous", $"pack_{userId}_0", disabled: true)
            .WithButton("Next ➡️", $"pack_{userId}_1", disabled: cards.Count <= 1)
            .Build();

        await FollowupAsync(embed: cards[0], components: component);

    }

    public Stream RenderPackImage(List<(string name, string spriteUrl, bool isShiny)> pokemons)
    {
        int columns = 5;
        int rows = (int)Math.Ceiling(pokemons.Count / (double)columns);
        int cellWidth = 220;
        int cellHeight = 100;

        Bitmap bmp = new Bitmap(columns * cellWidth, rows * cellHeight);
        using Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);

        var font = new Font("Arial", 12);
        var brush = new SolidBrush(Color.White);
        var shinyBrush = new SolidBrush(Color.Goldenrod);

        for (int i = 0; i < pokemons.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            int x = col * cellWidth;
            int y = row * cellHeight;

            var (name, spriteUrl, isShiny) = pokemons[i];

            try
            {
                if (!string.IsNullOrEmpty(spriteUrl))
                {
                    using var wc = new WebClient();
                    using var spriteStream = wc.OpenRead(spriteUrl);
                    using var sprite = System.Drawing.Image.FromStream(spriteStream);
                    g.DrawImage(sprite, x + 10, y + 10, 64, 64);
                }
            }
            catch
            {
                // Ignore broken sprites
            }

            g.DrawString(name.Split(' ', 2).First(), font,
                isShiny ? shinyBrush : brush, new PointF(x + 75, y));
            g.DrawString(isShiny ? "✨ " + name.Split(' ', 2).Last() : name.Split(' ', 2).Last(), font,
                isShiny ? shinyBrush : brush, new PointF(x + 75, y + 15));
        }

        var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        return ms;
    }

    [SlashCommand("packs", "Open 10 Pokémon packs ($10,000)")]
    public async Task OpenTenPacks([Summary("user", "Give cards to another user!")] SocketUser? user)
    {
        await DeferAsync();

        var targetUser = user ?? Context.User;
        var userId = targetUser.Id;

        if (_dbHelper.GetMoney(Context.User.Id) < 10000)
        {
            await FollowupAsync("You're too broke for 10 packs 💸 ($10,000 needed).");
            return;
        }

        _dbHelper.SaveMoney(Context.User.Id, -10000);

        var rand = new Random();
        var pokemonResults = new List<(string name, string spriteUrl, bool isShiny)>();

        for (int i = 0; i < 50; i++)
        {
            int pokemonId = rand.Next(1, 1026);
            var url = $"https://pokeapi.co/api/v2/pokemon/{pokemonId}";

            var response = await _client.GetAsync(url);
            if (!response.IsSuccessStatusCode) continue;

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
            string dexId = id.ToString("D4");
            string name = root.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString() ?? "Unknown"
                : "Unknown";

            string spriteUrl = root
                .GetProperty("sprites")
                .GetProperty("front_default")
                .GetString() ?? "";

            bool isShiny = rand.NextDouble() < 0.01;

            // Save Pokémon
            string displayName = isShiny ? $"#{dexId} SHINY {name.ToUpper()}" : $"#{dexId} {name.ToUpper()}";
            _dbHelper.SavePokemon(userId, displayName, 1, true, spriteUrl);


            // Inside your loop where you open cards:
            pokemonResults.Add((displayName, spriteUrl, isShiny));

        }

        var imageStream = RenderPackImage(pokemonResults);

        await FollowupWithFileAsync(imageStream, "packs.png",
            text: $"🎁 {user!.Mention} opened 10 packs and got 50 pokemon!");
    }

    [ComponentInteraction("pack_*_*")]
    public async Task HandlePackPagination(string userIdStr, string indexStr)
    {
        if (!ulong.TryParse(userIdStr, out var userId) || userId != Context.User.Id)
        {
            await RespondAsync("❌ You can't interact with someone else's pack.", ephemeral: true);
            return;
        }

        if (!int.TryParse(indexStr, out int index)) return;
        if (!openPackCards.TryGetValue(userId, out var cards) || index < 0 || index >= cards.Count)
        {
            await RespondAsync("❌ Card not found.", ephemeral: true);
            return;
        }

        var component = new ComponentBuilder()
            .WithButton("⬅️ Previous", $"pack_{userId}_{index - 1}", disabled: index == 0)
            .WithButton("Next ➡️", $"pack_{userId}_{index + 1}", disabled: index == cards.Count - 1)
            .Build();

        await Context.Interaction.DeferAsync();  // Defer interaction to avoid timeout
        await Context.Interaction.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = cards[index];
            msg.Components = component;
        });

    }
    public Stream GeneratePokedexPage(
        ulong userId,
        List<(string name, int amount, bool caught, string img)> caughtList,
        int page,
        int perPage,
        string avatarUrl,
        bool showAll)
    {

        const int gridCols = 5, gridRows = 5;
        const int cellSize = 128, padding = 8;
        const int imageWidth = gridCols * (cellSize + padding);
        const int imageHeight = gridRows * (cellSize + padding);

        var bitmap = new Bitmap(imageWidth, imageHeight);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.Transparent);

        // Convert caught list to dictionary for lookup
        var caughtDict = caughtList.ToDictionary(p => p.name.ToLower());

        Console.WriteLine($"pokemon total: {caughtDict.Count}");

        int start = (page - 1) * perPage;
        if (showAll)
        {
            for (int i = 0; i < perPage && (start + i) < 1025; i++)
            {
                //Console.WriteLine($"doing pokemon {i}");

                int dexNum = start + i + 1;
                string dexId = dexNum.ToString("D4");
                string name = ""; string spriteUrl = ""; bool caught = false;

                var position = new Point((i % gridCols) * (cellSize + padding), (i / gridCols) * (cellSize + padding));
                // Determine if caught
                var found = caughtDict.FirstOrDefault(p => p.Key.Contains($"#{dexId}"));

                if (!string.IsNullOrEmpty(found.Key))
                {
                    name = found.Key.ToUpper();
                    spriteUrl = found.Value.img;
                    caught = found.Value.caught;
                }

                if (caught && !string.IsNullOrWhiteSpace(spriteUrl))
                {
                    DrawCaughtPokemon(g, spriteUrl, position, cellSize, name, avatarUrl);
                }
                else
                {
                    DrawUnknownPokemon(g, position, cellSize, $"#{dexId}");
                }
            }
        }
        else
        {

            for (int i = 0; i < caughtList.Count; i++)
            {
                var entry = caughtList[i];

                var position = new Point((i % gridCols) * (cellSize + padding), (i / gridCols) * (cellSize + padding));
                string name = entry.name.ToUpper();
                //Console.WriteLine($"name: {name}");
                string spriteUrl = entry.img;
                bool caught = entry.caught;

                if (caught && !string.IsNullOrWhiteSpace(spriteUrl))
                {
                    DrawCaughtPokemon(g, spriteUrl, position, cellSize, name, avatarUrl);
                }

            }

        }
        //// Draw progress bar at the bottom
        //float progress = caughtList.Count / 1025f;
        //Rectangle barBounds = new Rectangle(50, imageHeight - 60, imageWidth - 100, 20);


        //// Optionally draw text
        //string label = $"{caughtList.Count}/1025 Pokémon caught";
        //using var font = new Font("Arial", 12);
        //var textSize = g.MeasureString(label, font);
        //g.DrawString(label, font, Brushes.Black,
        //    barBounds.X + (barBounds.Width - textSize.Width) / 2,
        //    barBounds.Y - textSize.Height - 5);

        var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    void DrawCaughtPokemon(Graphics g, string spriteUrl, Point pos, int size, string name, string avatarUrl)
    {
        using var sprite = DownloadImage(spriteUrl);
        using var avatar = DownloadImage(avatarUrl);
        var font = new Font("Arial", 12, FontStyle.Bold);

        g.DrawImage(sprite, pos.X, pos.Y, size, size);
        g.DrawString($"{name.Substring(0, 5)}", font, Brushes.White, pos.X, pos.Y + size - 17);
        //g.DrawImage(avatar, pos.X + size - 24, pos.Y + size - 24, 24, 24);
    }

    void DrawUnknownPokemon(Graphics g, Point pos, int size, string number)
    {
        var rect = new Rectangle(pos.X, pos.Y, size, size);
        g.FillRectangle(Brushes.Black, rect);
        var font = new Font("Arial", 12, FontStyle.Bold);
        g.DrawString(number, font, Brushes.White, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }
    Bitmap DownloadImage(string url)
    {
        using var client = new WebClient();
        using var stream = client.OpenRead(url);
        return new Bitmap(stream);
    }

    public MemoryStream GenerateProgressBarImage(int caught, int total, int width = 320, int height = 25)
    {
        Bitmap bmp = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);

            float progress = caught / (float)total;
            int fillWidth = (int)(width * progress);

            // Convert progress (0.0–1.0) to hue (0–360)
            float hue = progress * 360f; // red → purple
            Color fillColor = FromHsv(hue, 1f, 1f);

            using (Brush fillBrush = new SolidBrush(fillColor))
            {
                g.FillRectangle(fillBrush, 0, 0, fillWidth, height);
            }

            using (Pen border = new Pen(Color.Black, 2))
                g.DrawRectangle(border, 0, 0, width - 1, height - 1);

            string text = $"{caught}/{total}";
            using Font font = new Font("Arial", 12, FontStyle.Bold);
            var textSize = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, (width - textSize.Width) / 2, (height - textSize.Height) / 2);
        }

        var stream = new MemoryStream();
        bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return stream;
    }

    public static Color FromHsv(double hue, double saturation, double value)
    {
        int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        value *= 255;
        int v = Convert.ToInt32(value);
        int p = Convert.ToInt32(value * (1 - saturation));
        int q = Convert.ToInt32(value * (1 - f * saturation));
        int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

        return hi switch
        {
            0 => Color.FromArgb(255, v, t, p),
            1 => Color.FromArgb(255, q, v, p),
            2 => Color.FromArgb(255, p, v, t),
            3 => Color.FromArgb(255, p, q, v),
            4 => Color.FromArgb(255, t, p, v),
            _ => Color.FromArgb(255, v, p, q),
        };
    }

    [SlashCommand("killpokemon", "Delete Pokémon from your Pokédex")]
    public async Task KillPokemon(
    [Summary("user", "Leave blank to delete your own Pokémon")] SocketUser? user = null,
    [Summary("names", "Enter Pokémon names to delete, or leave blank to delete all")] string? names = null)
    {
        await DeferAsync();

        var targetUser = user ?? Context.User;
        var userId = targetUser.Id;

        try
        {
            var allCaught = _dbHelper.GetPokemon(userId);

            List<string> toDelete = [];

            if (names == null || names.Length == 0)
            {
                for (int i = 0; i < allCaught.Count; i++)
                {
                    toDelete.Add(allCaught[i].name);

                }
            }
            else
            {
                // Case-insensitive match by name
                for (int i = 0; i < names.Split(' ', 1025).Length; i++)
                {
                    toDelete.Add(names.Split(' ', 1025)[i]);

                }
                
            }

            if (toDelete.Count == 0)
            {
                await FollowupAsync("❌ No matching Pokémon found to delete.");
                return;
            }
            string pokeGrave = "";

            foreach (var pokemon in toDelete)
            {
                pokeGrave += _dbHelper.GetPokemon(userId)[int.Parse(pokemon)].name + " ";
                _dbHelper.KillPokemon(userId, pokemon);
            }

            await FollowupAsync($"✅ Killed {pokeGrave} from {targetUser.Username}'s Pokédex.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            await FollowupAsync($"❌ Error: {ex.Message}");
        }
    }




    [SlashCommand("pokedex", "View a user's Pokédex")]
    public async Task PokedexCommand(
        [Summary("user", "Leave blank to view your own Pokédex")] SocketUser? user = null,

        [Summary("generation", "Select a generation to view")]
    [Choice("Gen 1", "Gen 1")]
    [Choice("Gen 2", "Gen 2")]
    [Choice("Gen 3", "Gen 3")]
    [Choice("Gen 4", "Gen 4")]
    [Choice("Gen 5", "Gen 5")]
    [Choice("Gen 6", "Gen 6")]
    [Choice("Gen 7", "Gen 7")]
    [Choice("Gen 8", "Gen 8")]
    [Choice("Gen 9", "Gen 9")]
    string? generation = null,

        int page = 1,
        bool showAll = false)
    {
        await DeferAsync();

        try
        {
            // Determine user
            var targetUser = user ?? Context.User;
            var userId = targetUser.Id;

            // Define generation ranges
            var generationRanges = new Dictionary<string, (int start, int end)>
            {
                ["Gen 1"] = (1, 151),
                ["Gen 2"] = (152, 251),
                ["Gen 3"] = (252, 386),
                ["Gen 4"] = (387, 493),
                ["Gen 5"] = (494, 649),
                ["Gen 6"] = (650, 721),
                ["Gen 7"] = (722, 809),
                ["Gen 8"] = (810, 905),
                ["Gen 9"] = (906, 1025)
            };

            // Get all caught Pokémon
            var allCaught = _dbHelper.GetPokemon(userId);
            var caughtList = allCaught;

            // Apply generation filter if selected
            int generationStart = 1, generationEnd = 1025;
            if (generation != null && generationRanges.TryGetValue(generation, out var range))
            {
                generationStart = range.start;
                generationEnd = range.end;

                caughtList = allCaught
                    .Where(p =>
                    {
                        var match = Regex.Match(p.name, @"#(\d+)");
                        if (!match.Success) return false;
                        int id = int.Parse(match.Groups[1].Value);
                        return id >= range.start && id <= range.end;
                    })
                    .ToList();
            }

            int totalCaught = caughtList.Count;
            int totalPokemon = showAll ? 1025 : (generationEnd - generationStart + 1);
            int perPage = 25;
            int maxPage = (int)Math.Ceiling(totalPokemon / (double)perPage);
            page = Math.Clamp(page, 1, maxPage);

            // Generate images
            var dexImage = GeneratePokedexPage(userId, caughtList, page, perPage, targetUser.GetAvatarUrl(), showAll);
            dexImage.Position = 0;

            var progressImage = GenerateProgressBarImage(totalCaught, totalPokemon);
            progressImage.Position = 0;

            // Embed with progress bar image
            var embed = new EmbedBuilder()
                .WithTitle($"{targetUser.Username}'s Pokédex (Page {page}/{maxPage})")
                .WithColor((Discord.Color)Color.Orange)
                .WithImageUrl("attachment://progress_bar.png")
                .WithFooter("Use /pokedex to browse more.")
                .Build();

            var attachments = new List<FileAttachment>
        {
            new(dexImage, $"pokedex_page_{page}.png"),
            new(progressImage, "progress_bar.png")
        };

            await FollowupWithFilesAsync(
                attachments: attachments,
                embed: embed
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            await FollowupAsync($"❌ Error: {ex.Message}");
        }
    }




    //[SlashCommand("random", "Generate a random image with FLUX.1 [schnell]")]
    //public async Task GenerateRandomImage()
    //{
    //    await DeferAsync();
    //    try
    //    {
    //        // Step 1: Generate prompt
    //        string prompt = await GenerateTextFromReplicateAsync(
    //            "generate a random AI image. prompt only. no venom snake. nothing else. just prompt."
    //        );

    //        // Step 2: Generate the image embed
    //        var embed = await GenerateImageFromStableDiffusionAsync(prompt);

    //        // Step 3: Create hashed prompt for regen
    //        string encodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(prompt));
    //        string hashedPrompt = Convert.ToBase64String(
    //            SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(encodedPrompt))
    //        ).Substring(0, 32);

    //        // Step 4: Add regen button
    //        var builder = new ComponentBuilder()
    //            .WithButton("🔁", customId: $"regen:{hashedPrompt}", ButtonStyle.Primary);

    //        // Step 5: Send the image + prompt + button
    //        await FollowupAsync(embed: embed, components: builder.Build());
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex);
    //        await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
    //    }
    //}

    //[SlashCommand("loredrop", "Start explaining random MGS lore no one asked for.")]
    //public async Task RandomLoreDrop()
    //{
    //    await DeferAsync();

    //    try
    //    {
    //        await GenerateTextCommand("Please give me an explanation of a specific piece of lore about the Metal Gear Solid franchise.");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine(ex);
    //    }

    //}

    [SlashCommand("slots", "Start playing a slot machine (costs $10).")]
    public async Task GamblingSlots()
    {
        await DeferAsync();

        ulong userID = Context.User.Id;
        decimal money = _dbHelper.GetMoney(userID);

        if (money >= 10)
        {
            _dbHelper.SaveMoney(userID, -10);
            string slotResult = GenerateSlotResult(out bool isWin);

            var builder = new ComponentBuilder()
                .WithButton("🔁", "slot_spin", ButtonStyle.Primary);

            var message = slotResult;
            if (isWin)
            {
                Random rng = new Random();
                decimal won = rng.Next(10000000, 100000000);
                message += $"\nYou won ${Math.Round(won, 2)}! 🎉";
                _dbHelper.SaveMoney(Context.User.Id, won);
            }

            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = message;
                msg.Components = builder.Build();
            });
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

    private string GenerateSlotResult(out bool isWin)
    {
        StringBuilder result = new StringBuilder();
        Random rand = new Random();
        string[] slotEmojis = new string[3];

        for (int i = 0; i < 3; i++)
        {
            slotEmojis[i] = Emojis[rand.Next(Emojis.Length)];
            result.Append(slotEmojis[i] + " ");
        }

        isWin = slotEmojis[0] == slotEmojis[1] && slotEmojis[1] == slotEmojis[2];
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

                await FollowupAsync($"You have ${money:F2}");
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
        if (!firedFromJob)
        {
            try
            {
                Random rng = new Random();
                int chanceOfBeingFired = rng.Next(10);
                if (chanceOfBeingFired < 9)
                {
                    ulong userID = Context.User.Id;
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
            await FollowupAsync("You're not allowed to put fries in the bag anymore.");
        }
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