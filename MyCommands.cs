using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord.Audio.Streams;

[CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)] // Allow these commands in DMs

[IntegrationType(ApplicationIntegrationType.UserInstall)]
public class MyCommands() : InteractionModuleBase<SocketInteractionContext>
{
    private readonly string _replicateApiKey = "";

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
    // The Slash Command to generate an image from a prompt
    [SlashCommand("generate", "Generate an image based on your prompt!")]
    public async Task GenerateImageAsync([Summary("prompt", "Describe the image you want to generate")] string prompt)
    {
        await DeferAsync();

        try
        {
            var genImageUrl = await GenerateImageFromStableDiffusionAsync(prompt);
            await FollowupAsync(genImageUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); // optional logging
            await FollowupAsync("❌ Failed to generate image. Please try again later.");
        }
    }

    // Method to interact with Replicate API and get an image URL
    private async Task<string> GenerateImageFromStableDiffusionAsync(string input)
    {
        var client = new RestClient("https://api.replicate.com/v1/predictions");
        var request = new RestRequest
        {
            Method = Method.Post
        };

        // Headers
        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");

        // Body
        var body = new
        {
            version = "6ed1ce77cdc8db65550e76d5ab82556d0cb31ac8ab3c4947b168a0bda7b962e4", // Replace with correct model version ID
            input = new
            {
                prompt = input,
                width = 512,
                height = 512
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

            var getRequest = new RestRequest($"{predictionId}", Method.Get);
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
                string imageUrl = pollResult.GetValue("output").ToString();  // Ensure it's a string

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

    [SlashCommand("text", "Generate text using Replicate AI")]
    public async Task<string> GenerateTextAsync(string prompt)
    {
        var client = new RestClient("https://api.replicate.com/v1/predictions");
        var request = new RestRequest
        {
            Method = Method.Post
        };

        // Set up the headers
        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Prefer", "wait");

        // Set up the request body
        var requestBody = new
        {
            input = new
            {
                prompt = prompt,
                max_tokens = 8192,
                system_prompt = "",
                max_image_resolution = 0.5
            }
        };

        request.AddJsonBody(requestBody);

        // Send the request
        var response = await client.ExecuteAsync(request);

        // Handle response
        if (response.IsSuccessful)
        {
            // Deserialize response content
            dynamic result = JsonConvert.DeserializeObject(response.Content);
            return result?.output[0]?.ToString() ?? "No output returned.";
        }
        else
        {
            Console.WriteLine($"Error: {response.Content}");
            return $"Error generating text: {response.Content}";
        }
    }

    private async Task<string> GenerateTextFromReplicateAsync(string prompt)
    {
        var client = new RestClient("https://api.replicate.com/v1/models/anthropic/claude-3.7-sonnet/predictions");
        var request = new RestRequest(Convert.ToString(Method.Post));

        // Add headers
        request.AddHeader("Authorization", $"Bearer {_replicateApiKey}");
        request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Prefer", "wait");

        // Request body
        var requestBody = new
        {
            input = new
            {
                prompt = prompt,
                max_tokens = 8192,
                system_prompt = "",
                max_image_resolution = 0.5
            }
        };

        // Add JSON body to request
        request.AddJsonBody(requestBody);

        // Execute the request
        var response = await client.ExecuteAsync(request);

        // Log the response content to see what is being returned
        Console.WriteLine("Response Content: " + response.Content);

        if (response.IsSuccessful)
        {
            // Assuming the response contains the generated text in the "output" field
            dynamic responseBody = JsonConvert.DeserializeObject(response.Content);
            string generatedText = responseBody?.output?[0]?.ToString(); // Adjust the path if needed
            return generatedText ?? "No text generated.";
        }
        else
        {
            // Log the error content from the API response
            Console.WriteLine("Error: " + response.Content);
            return $"There was an error generating the text: {response.Content}";
        }

    }



}
