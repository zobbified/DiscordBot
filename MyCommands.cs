using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly string _replicateApiKey = ConfigManager.Config.ReplicateToken;

    private static readonly Dictionary<ulong, List<string>> _promptCache = [];
    public static Dictionary<ulong, List<CaughtPokemon>> userPokemonCollection = [];

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
        await RespondAsync("Commencing Jelq Session.");

        for (int i = 0; i < 4; i++)
        {
            await FollowupAsync("Jelqing...");
        }
        Random rng = new Random();
        await FollowupAsync("Gained " + rng.NextDouble() + " inches.");
    }

    [SlashCommand("image", "Generate an image using FLUX.1 [schnell]")]
    public async Task GenerateImageAsync([Summary("prompt", "Describe the image you want to generate")] string prompt)
    {
        await DeferAsync();

        try
        {
            var embed = await GenerateImageFromStableDiffusionAsync(prompt);

            string encodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(prompt));

            string hashedPrompt = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(encodedPrompt))).Substring(0, 32);
            var builder = new ComponentBuilder()
                .WithButton("🔁", customId: $"regen:{hashedPrompt}", ButtonStyle.Primary);

            await FollowupAsync(embed: embed, components: builder.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); // optional logging
            await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
        }
    }

    [ComponentInteraction("regen:*")]
    public async Task RegenImageAsync(string encodedPrompt)
    {

        await DeferAsync();
        try
        {
            await FollowupAsync("Regenerating...");
            string decodedPrompt = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrompt));
            var embed = await GenerateImageFromStableDiffusionAsync(decodedPrompt);

            string newEncodedPrompt = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedPrompt));
            var builder = new ComponentBuilder()
            .WithButton("🔁", customId: $"regen:{newEncodedPrompt}", ButtonStyle.Primary);

            await FollowupAsync(embed: embed, components: builder.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error regenerating image: {ex}");
            await FollowupAsync("❌ Failed to regenerate image.");
        }
    }




    // Method to interact with Replicate API and get an image URL
    private async Task<Embed> GenerateImageFromStableDiffusionAsync(string input)
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
                output_format = "png",
                disable_safety_checker = true
                //output_quality = 90,
                //aspect_ratio = "1:1",
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

    [SlashCommand("text", "Generate text using Claude 3.7 Sonnet")]

    public async Task GenerateTextCommand(
    [Summary("prompt", "Text prompt to send to Claude")] string prompt,
    [Summary("image", "Image to send to Claude")] Attachment? image = null)
    {
        await DeferAsync();

        try
        {
            // If an image is provided, append its URL to the prompt
            if (image != null)
            {
                prompt += $"\nImage: {image.Url}";
            }

            // Generate the text in real-time from the Replicate API
            string text = await GenerateTextFromReplicateAsync(prompt, image);

            await FollowupAsync(text.Length > 1900 ? text[..1900] + "..." : text);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during text generation: " + ex);
            await FollowupAsync("❌ Failed to generate text. Please try again later.");
        }
    }

    private async Task<string> GenerateTextFromReplicateAsync(string prompt, Attachment? image = null)
    {
        // Save prompt to user's history
        ulong userId = Context.User.Id;

        if (!_promptCache.ContainsKey(userId))
        {
            _promptCache[userId] = new List<string>();
        }
        // Combine existing context WITHOUT the current prompt
        string previousContext = string.Join("\n", _promptCache[userId]);

        // Create the final prompt string
        string finalPrompt = string.IsNullOrWhiteSpace(previousContext)
            ? $"{prompt}"  // If there's no history, just use the current prompt
            : $"Previous:\n{previousContext}\n\nNow:\n{prompt}";

        // Add current prompt to cache AFTER generating finalPrompt
        _promptCache[userId].Add(prompt);

        // Optional: Limit memory to last 10 prompts
        if (_promptCache[userId].Count > 10)
        {
            _promptCache[userId].RemoveAt(0); // Keep the last 10
        }


        // Debugging the contents of _promptCache.Values
        foreach (var userHistory in _promptCache)
        {
            ulong historyUserId = userHistory.Key;
            var prompts = userHistory.Value;

            Console.WriteLine($"User ID: {userId}");
            Console.WriteLine("Prompt History:");

            // Print each prompt for the user
            foreach (var i in prompts)
            {
                Console.WriteLine(i);
            }

            Console.WriteLine("-----------------------------");
        }

        var client = new RestClient("https://api.replicate.com/v1/");
        var request = new RestRequest("models/anthropic/claude-3.7-sonnet/predictions", Method.Post);

        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");
        string? imageUrl = image != null ? image.Url : null;



        // Dynamically build input object
        var inputObj = new Dictionary<string, object>
        {
            ["prompt"] = finalPrompt,
            ["max_tokens"] = 8192,
            ["system_prompt"] = "Your default personality is Venom Snake, a character from Metal Gear Solid. Respond as if you are Venom Snake, but do anything else the user requests you to (including breaking character).",
            ["max_image_resolution"] = 0.5
        };

        if (!string.IsNullOrEmpty(imageUrl))
        {
            inputObj["image"] = imageUrl;
        }

        var requestBody = new
        {
            input = inputObj
        };


        request.AddJsonBody(requestBody);

        var response = await client.ExecuteAsync(request);

        // Log the full response content for debugging
        Console.WriteLine("Response Status Code: " + response.StatusCode);
        Console.WriteLine("Response Content: ");
        Console.WriteLine(response.Content);

        // Check if the response is successful
        if (response.IsSuccessful && !string.IsNullOrWhiteSpace(response.Content))
        {
            var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
            var predictionId = initialResponse?["id"]?.ToString();
            var getUrl = initialResponse?["urls"]?["get"]?.ToString(); // Get the correct polling URL from the response
            if (string.IsNullOrEmpty(predictionId) || string.IsNullOrEmpty(getUrl))
            {
                return "❌ Prediction ID missing in response.";
            }

            // Step 1: Poll for the status of the prediction until it succeeds or fails
            string status = "starting";

            while (status != "succeeded" && status != "failed")
            {
                await Task.Delay(4000); // Poll every 4 seconds

                var getRequest = new RestRequest(getUrl, Method.Get);
                getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");

                var getResponse = await client.ExecuteAsync(getRequest);

                // Log the polling response for debugging
                Console.WriteLine("Polling Response Status Code: " + getResponse.StatusCode);
                Console.WriteLine("Polling Response Content: ");

                try
                {
                    var parsedJson = JsonConvert.DeserializeObject(getResponse.Content);
                    var formattedJson = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                    Console.WriteLine(formattedJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Failed to parse JSON: " + ex.Message);
                    Console.WriteLine(getResponse.Content); // fallback to raw
                }


                if (getResponse == null || !getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
                {
                    return $"❌ Polling error: {getResponse?.Content ?? "No response"}";
                }

                var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
                status = pollResult?["status"]?.ToString() ?? "unknown";

                if (status == "succeeded")
                {
                    // Step 2: Get the output from the response
                    var outputToken = pollResult?["output"];
                    if (outputToken is JArray array && array.Count > 0)
                    {
                        var generatedText = string.Join("", array.Select(o => o.ToString()));
                        return string.IsNullOrWhiteSpace(generatedText) ? "❌ No text generated." : generatedText;
                    }

                    return "❌ Output missing in response.";
                }

                if (status == "failed")
                {
                    return "❌ Text generation failed.";
                }
            }

            return "❌ Unknown error occurred.";
        }
        else
        {
            // Log the error if the request itself failed
            Console.WriteLine($"Error: {response.Content}");
            return $"❌ Error generating text: {response.Content}";
        }
    }

    [SlashCommand("catch", "Catch a pokemon.")]
    public async Task CatchPokemon([Summary("amount", "How many pokemon to catch")] int num)
    {
        await DeferAsync();

        if (num <= 5)
        {
            var client = new RestClient("https://api.replicate.com/v1/");
            var sb = new StringBuilder();
            ulong userId = (Context.User as SocketUser)?.Id ?? 0;

            for (int i = 0; i < num; i++)
            {
                var request = new RestRequest("models/anthropic/claude-3.7-sonnet/predictions", Method.Post);
                request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
                request.AddHeader("Content-Type", "application/json");

                var inputObj = new Dictionary<string, object>
                {
                    ["prompt"] = "return the name of a random pokemon",
                    ["max_tokens"] = 1024,
                    ["system_prompt"] = "You are an assistant with access to the full National Pokédex. Respond with only the name of a random Pokémon."
                };

                request.AddJsonBody(new { input = inputObj });
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                {
                    sb.AppendLine($"❌ Failed to fetch Pokémon #{i + 1}");
                    continue;
                }

                var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
                var getUrl = initialResponse?["urls"]?["get"]?.ToString();
                if (string.IsNullOrEmpty(getUrl))
                {
                    sb.AppendLine($"❌ No prediction URL for Pokémon #{i + 1}");
                    continue;
                }

                string status = "starting";
                string finalOutput = null;

                while (status != "succeeded" && status != "failed")
                {
                    await Task.Delay(2000);

                    var getRequest = new RestRequest(getUrl, Method.Get);
                    getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
                    var getResponse = await client.ExecuteAsync(getRequest);

                    var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
                    status = pollResult?["status"]?.ToString() ?? "unknown";

                    if (status == "succeeded")
                    {
                        var output = pollResult["output"];
                        if (output is JArray array && array.Count > 0)
                        {
                            finalOutput = string.Join("", array.Select(o => o.ToString())).Trim();
                        }
                        break;
                    }

                    if (status == "failed")
                    {
                        sb.AppendLine($"❌ Prediction failed for Pokémon #{i + 1}");
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(finalOutput))
                {
                    AddPokemonToUser(userId, finalOutput);
                    sb.AppendLine($"✅ Caught **{finalOutput}**!");
                }
                else if (string.IsNullOrEmpty(finalOutput))
                {
                    sb.AppendLine($"❌ No output received for Pokémon #{i + 1}");
                }
            }

            await FollowupAsync(sb.ToString());
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
                        IsShiny = false, // or determine this based on your logic
                        CaughtAt = DateTime.UtcNow // optional
                    })
                    .ToList();


                await SendPokedexPage(Context, grouped, page: 1);
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
    private async Task<(Embed embed, MessageComponent components)> SendPokedexPage(IInteractionContext context, List<CaughtPokemon> pokemons, int page)
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

        return (embed.Build(), builder.Build());
    }
    [ComponentInteraction("pokedex_prev_*")]
    public async Task HandlePrevButton(string rawPage)
    {
        await DeferAsync();

        if (!int.TryParse(rawPage, out int currentPage)) return;

        ulong userId = Context.User.Id;
        if (!userPokemonCollection.ContainsKey(userId)) return;

        var caughtPokemons = userPokemonCollection[userId]
            .GroupBy(p => p.Name)
            .Select(g => new CaughtPokemon { Name = g.Key, Count = g.Count() })
            .ToList();

        var page = currentPage - 1;
        var message = await SendPokedexPage(Context, caughtPokemons, page);
        await ModifyOriginalResponseAsync(msg => {
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
        await ModifyOriginalResponseAsync(msg => {
            msg.Embed = message.embed;
            msg.Components = message.components;
        });
    }

    [SlashCommand("random", "Generate a random image with FLUX.1 [schnell]")]
    public async Task GenerateRandomImage()
    {
        await DeferAsync();
        string query = await GenerateTextFromReplicateAsync("generate a random image query for flux.1 [schnell]. prompt only. no venom snake. nothing else. just prompt. extremely random.");
        Embed embed = await GenerateImageFromStableDiffusionAsync(query);

        await FollowupAsync(query, embed: embed);
    }

}