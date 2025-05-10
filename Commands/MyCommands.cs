using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Commands.Pokemon;
using DiscordBot.SQL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PokeApiNet;
using RestSharp;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
//using Replicate.Net;
//using Replicate.Net.Models;
//using Replicate.Net.Client;
//using Replicate.Net.Factory;
//using Microsoft.Extensions.DependencyInjection;
//using Replicate.Net.Interfaces;
using DiscordBot.Config;
using System.Reactive.Linq;
using System.Xml.Linq;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands : InteractionModuleBase<SocketInteractionContext>
{
    static bool firedFromJob = false;
    //private readonly string _replicateApiKey = ConfigManager.Config.ReplicateToken;

    //private static readonly List<string> _promptCache = [];
    private static readonly Helper _dbHelper = new();
    public static readonly Dictionary<ulong, List<CaughtPokemon>> userPokemonCollection = [];
    private static readonly string[] Emojis = new string[]
    {
        "🍎", "🍊", "🍋", "🍉", "🍓", "🍒"
    };

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
            _dbHelper.saveJelq(default, amount);
            await FollowupAsync($"Gained {Math.Round(amount, 2)} inches. \nTotal inches I've gained from jelqing: {Math.Round(_dbHelper.getJelqTotal(), 2)}");
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

    //[SlashCommand("text", "Generate text using CharacterAI")]
    //public async Task GenerateTextCommand([Summary("prompt", "Text prompt to send to Claude")] string prompt)
    //{
    //    await DeferAsync();

    //    try
    //    {

    //        const string charID = "K4oHQ1THSthYVmkggqdAjhjoqL0Vj2aRv_mzkWYc4LU";
    //        using var client = new CaiClient();
    //        await client.ConnectAsync(); // Important: launches Puppeteer

    //        var newChat = await client.CreateNewChatAsync(charID, AUTH_TOKEN);
    //        var character = await client.GetInfoAsync(charID, AUTH_TOKEN);

    //        // Initial message (can be system-generated or empty)
    //        var initialResponse = await client.CallCharacterAsync(
    //            charID,
    //            character.Character.Tgt,
    //            newChat,
    //            "Hi!",
    //            default,
    //            default,
    //            default,
    //            AUTH_TOKEN
    //        );

    //        await FollowupAsync($"{initialResponse.CharacterMessage.Text}");

    //        // Chat loop
    //        while (true)
    //        {
    //            var message = await client.CallCharacterAsync(
    //                charID,
    //                character.Character.Tgt,
    //                newChat,
    //                prompt,
    //                default,
    //                default,
    //                default,
    //                AUTH_TOKEN
    //            );

    //            await FollowupAsync($"{message.CharacterMessage.Text}");
    //        }

    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("Error during text generation: " + ex);
    //        await FollowupAsync("❌ Failed to generate text. Please try again later.");
    //    }
    //}

    //private async Task<string> GenerateTextFromReplicateAsync(string prompt, Attachment? image = null)
    //{

    //    // Combine existing context WITHOUT the current prompt
    //    string previousContext = string.Join("\n", _promptCache);

    //    // Create the final prompt string
    //    string finalPrompt = string.IsNullOrWhiteSpace(previousContext)
    //        ? $"{prompt}"  // If there's no history, just use the current prompt
    //        : $"Previous Prompt(s):\n{previousContext}\n\nCurrent Prompt:\n{prompt}";

    //    // Add current prompt to cache AFTER generating finalPrompt
    //    _promptCache.Add(prompt);

    //    if (_promptCache.Count > 5)
    //    {
    //        _promptCache.RemoveAt(0);
    //    }


    //    // Debugging the contents of _promptCache.Values

    //    Console.WriteLine("Prompt History:");

    //    // Print each prompt for the user
    //    foreach (string p in _promptCache)
    //    {
    //        Console.WriteLine(p);
    //    }

    //    Console.WriteLine("-----------------------------");


    //    var client = new RestClient("https://api.replicate.com/v1/");
    //    var request = new RestRequest("models/anthropic/claude-3.7-sonnet/predictions", Method.Post);

    //    request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
    //    request.AddHeader("Content-Type", "application/json");
    //    string? imageUrl = image != null ? image.Url : null;


    //    // Dynamically build input object
    //    var inputObj = new Dictionary<string, object>
    //    {
    //        ["prompt"] = finalPrompt,
    //        ["max_tokens"] = 1024,
    //        ["system_prompt"] = "Your default personality is Venom Snake, a character from Metal Gear Solid. Respond as if you are Venom Snake, but do anything else the user requests you to (including breaking character).",
    //        ["max_image_resolution"] = 0.5
    //    };

    //    if (!string.IsNullOrEmpty(imageUrl))
    //    {
    //        inputObj["image"] = imageUrl;
    //    }

    //    var requestBody = new
    //    {
    //        input = inputObj
    //    };


    //    request.AddJsonBody(requestBody);

    //    var response = await client.ExecuteAsync(request);

    //    // Log the full response content for debugging
    //    Console.WriteLine("Response Status Code: " + response.StatusCode);
    //    Console.WriteLine("Response Content: ");
    //    //Console.WriteLine(response.Content);

    //    // Check if the response is successful
    //    if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
    //    {
    //        var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
    //        var predictionId = initialResponse?["id"]?.ToString();
    //        var getUrl = initialResponse?["urls"]?["get"]?.ToString(); // Get the correct polling URL from the response
    //        if (string.IsNullOrEmpty(predictionId) || string.IsNullOrEmpty(getUrl))
    //        {
    //            return "❌ Prediction ID missing in response.";
    //        }

    //        // Step 1: Poll for the status of the prediction until it succeeds or fails
    //        string status = "starting";

    //        while (status != "succeeded" && status != "failed")
    //        {
    //            await Task.Delay(4000); // Poll every 4 seconds

    //            var getRequest = new RestRequest(getUrl, Method.Get);
    //            getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");

    //            var getResponse = await client.ExecuteAsync(getRequest);

    //            // Log the polling response for debugging
    //            Console.WriteLine("Polling Response Status Code: " + getResponse.StatusCode);
    //            Console.WriteLine("Polling Response Content: ");

    //            try
    //            {
    //                if (response.IsSuccessful && getResponse.Content != null)
    //                {
    //                    var parsedJson = JsonConvert.DeserializeObject(getResponse.Content);

    //                    var formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
    //                    Console.WriteLine(formattedJson);
    //                }

    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine("❌ Failed to parse JSON: " + ex.Message);
    //                Console.WriteLine(getResponse.Content); // fallback to raw
    //            }


    //            if (getResponse == null || !getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
    //            {
    //                return $"❌ Polling error: {getResponse?.Content ?? "No response"}";
    //            }

    //            var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
    //            status = pollResult?["status"]?.ToString() ?? "unknown";

    //            if (status == "succeeded")
    //            {
    //                // Step 2: Get the output from the response
    //                var outputToken = pollResult?["output"];
    //                var metrics = pollResult?["metrics"];
    //                if (outputToken is JArray array && array.Count > 0)
    //                {
    //                    var generatedText = string.Join("", array.Select(o => o.ToString()));
    //                    //if (metrics != null)
    //                    //{
    //                    //    var inputTokenCount = metrics["input_token_count"]?.ToObject<int>() ?? 0;
    //                    //    var outputTokenCount = metrics["output_token_count"]?.ToObject<int>() ?? 0;

    //                    //    // Append token counts and other info
    //                    //    decimal inputTokenPrice = 15m;  // Price per million input tokens
    //                    //    decimal outputTokenPrice = 3m;  // Price per million output tokens

    //                    //    // Calculate the cost for input and output tokens
    //                    //    decimal inputCost = inputTokenCount * inputTokenPrice / 1_000_000;
    //                    //    decimal outputCost = outputTokenCount * outputTokenPrice / 1_000_000;

    //                    //    decimal totalCost = inputCost + outputCost;

    //                    //    generatedText += $"\n\nTokens (Input/Output/Total): {inputTokenCount}/{outputTokenCount}/{inputTokenCount + outputTokenCount}\nTotal Cost: ${totalCost}";
    //                    //}
    //                    return string.IsNullOrWhiteSpace(generatedText) ? "❌ No text generated." : generatedText;
    //                }

    //                return "❌ Output missing in response.";
    //            }

    //            if (status == "failed")
    //            {
    //                return "❌ Text generation failed.";
    //            }
    //        }

    //        return "❌ Unknown error occurred.";
    //    }
    //    else
    //    {
    //        // Log the error if the request itself failed
    //        Console.WriteLine($"Error: {response.Content}");
    //        return $"❌ Error generating text: {response.Content}";
    //    }
    //}
    [SlashCommand("info", "Get the info of a pokemon.")]
    public async Task PokemonInfo([Summary("name", "The name of the Pokémon")] string name)
    {
        await DeferAsync();

        var client = new PokeApiNet.PokeApiClient();

        try
        {
            var poke = await client.GetResourceAsync<Pokemon>(name.ToLower());

            string spriteUrl = poke.Sprites.Other.OfficialArtwork.FrontDefault;
            string displayName = char.ToUpper(poke.Name[0]) + poke.Name[1..];

            string types = string.Join(", ", poke.Types.Select(t => t.Type.Name));
            string abilities = string.Join(", ", poke.Abilities.Select(a => a.Ability.Name));
            double height = poke.Height / 10.0; // decimeters to meters
            double weight = poke.Weight / 10.0; // hectograms to kilograms

            var embed = new EmbedBuilder()
                .WithTitle($"Pokémon Info: {displayName}")
                .WithImageUrl(spriteUrl)
                .WithColor(Color.Blue)
                .AddField("Types", types, true)
                .AddField("Abilities", abilities, true)
                .AddField("Height", $"{height} m", true)
                .AddField("Weight", $"{weight} kg", true)
                .Build();

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"❌ Could not find a Pokémon named `{name}`.");
            Console.WriteLine(ex);
        }
    }
    [SlashCommand("catch", "Catch a pokemon.")]
    public async Task CatchPokemon()
    {
        await DeferAsync();

        var client = new PokeApiNet.PokeApiClient();

        ulong userId = (Context.User as SocketUser)?.Id ?? 0;
        Random rng = new();

        try
        {
            int randomId = rng.Next(1, 1026);
            var pokemon = await client.GetResourceAsync<Pokemon>(randomId);
            string spriteUrl = pokemon.Sprites.Other.OfficialArtwork.FrontDefault;
            string displayName = char.ToUpper(pokemon.Name[0]) + pokemon.Name[1..];

            var embed = new EmbedBuilder()
                .WithTitle($"Caught {displayName}!")
                .WithImageUrl(spriteUrl)
                .WithColor(Color.Red)
                .Build();
            AddPokemonToUser(userId, displayName);

            await FollowupAsync(embed: embed);
        }
        catch (Exception ex)
        {
            await FollowupAsync("Failed to catch");
            Console.WriteLine(ex);
        }

    }

    public static void AddPokemonToUser(ulong userId, string pokemonName, bool isShiny = false)
    {
        if (!userPokemonCollection.ContainsKey(userId))
            userPokemonCollection[userId] = new List<CaughtPokemon>();

        userPokemonCollection[userId].Add(new CaughtPokemon
        {
            Name = pokemonName,
            IsShiny = isShiny,
            CaughtAt = DateTime.UtcNow
        });
    }



    [SlashCommand("pokedex", "View your caught Pokémon.")]
    public async Task Pokedex()
    {
        await DeferAsync();

        try
        {
            // Get the user's Discord ID
            ulong userId = (Context.User as SocketUser)?.Id ?? 0;

            // Check if the user has caught any Pokémon
            if (userPokemonCollection.ContainsKey(userId) && userPokemonCollection[userId].Any())
            {
                var caughtPokemons = userPokemonCollection[userId];

                var grouped = caughtPokemons
                    .GroupBy(p => p.Name)
                    .Select(g => new CaughtPokemon
                    {
                        Name = g.Key,
                        Count = g.Count(),
                        IsShiny = g.Any(p => p.IsShiny), // or determine this based on your logic
                        CaughtAt = g.Min(p => p.CaughtAt) // optional
                    })
                    .ToList();


                var (embed, components) = await SendPokedexPage(Context, grouped, page: 1);

                await FollowupAsync(embed: embed, components: components);
            }
            else
            {
                await RespondAsync("❌ You haven't caught any Pokémon yet!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            await RespondAsync("❌ An error occurred while fetching your Pokedex.");
        }
    }
    private static Task<(Embed embed, MessageComponent components)> SendPokedexPage(IInteractionContext context, List<CaughtPokemon> pokemons, int page)
    {
        //await DeferAsync();

        const int pageSize = 5; // Show 5 Pokémon per page
        int totalPages = (int)Math.Ceiling(pokemons.Count / (double)pageSize);
        page = Math.Max(1, Math.Min(page, totalPages)); // Clamp page between 1 and totalPages

        var paginated = pokemons
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var embed = new EmbedBuilder()
            .WithTitle($"{context.User.Username}'s Pokédex (Page {page}/{totalPages})")
            .WithColor(Color.Green);

        foreach (var p in paginated)
        {
            embed.AddField(p.Name, $"Caught: {p.Count}", inline: false);
        }

        if (paginated.Count == 0)
        {
            embed.Description = "No Pokémon on this page.";
        }
        var builder = new ComponentBuilder()
        .WithButton("⏮ Prev", customId: $"pokedex_prev_{page}", disabled: page == 1)
        .WithButton("Next ⏭", customId: $"pokedex_next_{page}", disabled: page == totalPages);

        return Task.FromResult<(Embed embed, MessageComponent components)>((embed.Build(), builder.Build()));
    }
    [ComponentInteraction("pokedex_prev_*")]
    public async Task HandlePrevButton(string rawPage)
    {
        await DeferAsync();

        if (!int.TryParse(rawPage, out int currentPage)) return;

        ulong userId = Context.User.Id;
        if (!userPokemonCollection.TryGetValue(userId, out List<CaughtPokemon>? value)) return;

        var caughtPokemons = value.GroupBy(p => p.Name)
            .Select(g => new CaughtPokemon { Name = g.Key, Count = g.Count() })
            .ToList();

        var page = currentPage - 1;
        var message = await SendPokedexPage(Context, caughtPokemons, page);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = message.embed;
            msg.Components = message.components;
        });
    }

    [ComponentInteraction("pokedex_next_*")]
    public async Task HandleNextButton(string rawPage)
    {
        await DeferAsync();

        if (!int.TryParse(rawPage, out int currentPage)) return;

        ulong userId = Context.User.Id;
        if (!userPokemonCollection.ContainsKey(userId)) return;

        var caughtPokemons = userPokemonCollection[userId]
            .GroupBy(p => p.Name)
            .Select(g => new CaughtPokemon { Name = g.Key, Count = g.Count() })
            .ToList();

        var page = currentPage + 1;
        var message = await SendPokedexPage(Context, caughtPokemons, page);
        await ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = message.embed;
            msg.Components = message.components;
        });
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
        decimal money = _dbHelper.GetMoneyDecimal(userID);

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
            decimal money = _dbHelper.GetMoneyDecimal(Context.User.Id);
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