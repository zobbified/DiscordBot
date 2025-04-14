using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly string _replicateApiKey = ConfigManager.Config.ReplicateToken;

    //[SlashCommand("hello", "Say hello back!")]
    //public async Task HelloCommand()
    //{
    //    await RespondAsync($"👋 Hello, {Context.User.Username}!");
    //}

    //[SlashCommand("ping", "Test the bot latency.")]
    //public async Task PingCommand()
    //{
    //    await RespondAsync("🏓 Pong!");
    //}

    [SlashCommand("jelq", "Start jelqing.")]
    public async Task JelqCommand()
    {
        for (int i = 0; i < 10; i++)
        {
            await RespondAsync("Jelqing...");
        }
        Random rng = new Random();
        await RespondAsync("Gained " + rng.NextDouble() + " inches.");
    }

    [SlashCommand("image", "Generate an image using Stable Diffusion 3.5 Large")]
    public async Task GenerateImageAsync([Summary("prompt", "Describe the image you want to generate")] string prompt)
    {
        await DeferAsync();

        try
        {
            var genImageUrl = await GenerateImageFromStableDiffusionAsync(prompt);
            var builder = new ComponentBuilder()
           .WithButton("🔁", customId: $"regen:{prompt}", ButtonStyle.Primary);
            await FollowupAsync(genImageUrl, components: builder.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); // optional logging
            await FollowupAsync("❌ Failed to generate image. Please try again later.\n[adjusts prosthetic arm]");
        }
    }

    // Method to interact with Replicate API and get an image URL
    private async Task<string> GenerateImageFromStableDiffusionAsync(string input)
    {
        var client = new RestClient("https://api.replicate.com/v1/");
        var request = new RestRequest("models/black-forest-labs/flux-1.1-pro/predictions", Method.Post);


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
                cfg = 5,
                prompt = input,
                output_format = "png",
                output_quality = 100,
                aspect_ratio = "16:9",
            }
        };

        request.AddJsonBody(body);

        var response = await client.ExecuteAsync(request);

        if (response == null || !response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
        {
            return $"There was an error generating the image: {response?.Content}";
        }

        var initialResponse = JsonConvert.DeserializeObject<JObject>(response.Content);
        if (initialResponse == null || initialResponse["id"] == null)
        {
            return "Failed to parse the API response or prediction ID was missing.";
        }

        var predictionId = initialResponse["id"]?.ToString();
        if (string.IsNullOrEmpty(predictionId))
        {
            return "Prediction ID was null or empty.";
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
                return $"Error polling image generation: {getResponse?.Content}";
            }

            var pollResult = JsonConvert.DeserializeObject<JObject>(getResponse.Content);
            if (pollResult == null || pollResult["status"] == null)
            {
                return "Failed to parse polling response.";
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
                    return "No image URL returned.";
                }

                return imageUrl; // Returning the image URL
            }

            if (status == "failed")
            {
                return "Image generation failed.";
            }
        }

        return "Unknown error occurred.";

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
        var client = new RestClient("https://api.replicate.com/v1/");
        var request = new RestRequest("models/anthropic/claude-3.7-sonnet/predictions", Method.Post);

        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");
        string? imageUrl = image != null ? image.Url : null;


        // Prepare the request body
        // Dynamically build input object
        var inputObj = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["max_tokens"] = 8192,
            ["system_prompt"] = "You are now Venom Snake, a character from Metal Gear Solid. Respond as if you are Venom Snake.",
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
                await Task.Delay(2000); // Poll every 2 seconds

                var getRequest = new RestRequest(getUrl, Method.Get);
                getRequest.AddHeader("Authorization", $"Bearer {_replicateApiKey}");

                var getResponse = await client.ExecuteAsync(getRequest);

                // Log the polling response for debugging
                Console.WriteLine("Polling Response Status Code: " + getResponse.StatusCode);
                Console.WriteLine("Polling Response Content: ");
                Console.WriteLine(getResponse.Content);

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

}