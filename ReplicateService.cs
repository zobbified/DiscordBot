using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

public class ReplicateService
{
    private const string ReplicateApiKey = "";  // Replace with your Replicate API key
    private const string ModelVersionId = ""; // Replace with the correct model version

    public async Task<string> GenerateTextAsync(string prompt)
    {
        var client = new RestClient("https://api.replicate.com/v1/predictions");
        var request = new RestRequest
        {
            Method = Method.Post
        };

        // Set up the headers
        request.AddHeader("Authorization", $"Bearer {ReplicateApiKey}");
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
        if (!response.IsSuccessful)
        {
            Console.WriteLine($"Error: {response.Content}");
            return $"Error generating text: {response.Content}";
        }

        // Deserialize response content
        dynamic result = JsonConvert.DeserializeObject(response.Content);
        string predictionId = result?.id;

        if (string.IsNullOrEmpty(predictionId))
        {
            return "Failed to get a valid prediction ID.";
        }

        // Polling for the result
        string status = "starting";
        string generatedText = string.Empty;

        while (status != "succeeded" && status != "failed")
        {
            await Task.Delay(2000); // Wait a bit before polling again

            var getRequest = new RestRequest($"/v1/predictions/{predictionId}", Method.Get);
            getRequest.AddHeader("Authorization", $"Bearer {ReplicateApiKey}");

            var getResponse = await client.ExecuteAsync(getRequest);
            if (getResponse == null || !getResponse.IsSuccessful || string.IsNullOrWhiteSpace(getResponse.Content))
            {
                return $"Error polling text generation: {getResponse?.Content}";
            }

            dynamic pollResult = JsonConvert.DeserializeObject(getResponse.Content);
            if (pollResult == null || pollResult["status"] == null)
            {
                return "Failed to parse polling response.";
            }

            status = pollResult["status"]?.ToString() ?? "unknown";

            if (status == "succeeded")
            {
                Console.WriteLine("Full response:\n" + getResponse.Content);

                // Directly access the 'output' field, which is the generated text
                generatedText = pollResult.GetValue("output")?.ToString() ?? "No output returned.";

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    return "No generated text returned.";
                }

                return generatedText; // Returning the generated text
            }

            if (status == "failed")
            {
                return "Text generation failed.";
            }
        }

        return "Unknown error occurred during text generation.";
    }
}