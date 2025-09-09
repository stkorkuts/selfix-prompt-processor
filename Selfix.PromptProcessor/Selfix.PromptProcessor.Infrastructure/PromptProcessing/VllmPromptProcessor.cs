using System.Net.Http.Json;
using LanguageExt;
using Microsoft.Extensions.Options;
using Selfix.PromptProcessor.Application.Abstractions;
using Selfix.PromptProcessor.Shared.Settings;
using System.Text.Json;
using Selfix.PromptProcessor.Infrastructure.PromptProcessing.Schema;
using Serilog;

namespace Selfix.PromptProcessor.Infrastructure.PromptProcessing;

public sealed class VllmPromptProcessor : IPromptProcessor
{
    private readonly HttpClient _httpClient;
    private readonly VllmSettings _settings;

    public VllmPromptProcessor(IOptions<VllmSettings> options)
    {
        _settings = options.Value;
        _httpClient = new HttpClient { BaseAddress = new Uri($"http://{_settings.Host}:{_settings.Port}") };
    }

    public OptionT<IO, string> Process(string prompt, long seed, CancellationToken cancellationToken) =>
        IO.liftAsync(async () =>
        {
            Log.Information("Beginning prompt processing with seed {Seed}", seed);
            Log.Information("Prompt text: {PromptText}", prompt);

            try
            {
                Log.Debug("Fetching available models from vLLM server at {Endpoint}", "/v1/models");
                using var modelsResponse = await _httpClient.GetAsync("/v1/models", cancellationToken);
                modelsResponse.EnsureSuccessStatusCode();

                var modelsResult = await modelsResponse.Content.ReadFromJsonAsync<ModelsResponse>(cancellationToken);
                var modelId = modelsResult?.Data.FirstOrDefault()?.Id;

                if (modelId is null)
                {
                    Log.Warning("No models found in vLLM server response");
                    return Option<string>.None;
                }

                Log.Information("Using model: {ModelId}", modelId);

                var request = new VllmChatCompletionRequest
                {
                    Model = modelId, // Using the actual model ID instead of "default"
                    Messages =
                    [
                        new Message 
                        { 
                            Role = "system", 
                            Content = """
                                      You are translating Russian users prompts into English prompts for Stable Diffusion image generation.
                                      Users create avatars using their images, which analyzed by computer vision algorithms.
                                      This computer vision algorithm provide a textual description of users photos.
                                      You should extract information about their gender, age, appearance and other important stuff from this textual avatar description.
                                      Also they provide a gender-neutral prompt in Russian and you should translate it into English, 
                                      apply information from avatar description and add more details to make Stable Diffusion English prompt more detailed.
                                      
                                      Example of user prompt: `
                                      Профессиональная студийная фотография успешного человека в деловом стиле
                                      `
                                      
                                      Example of avatar description: `
                                      GNAVTRTKN a young man standing in front of a white wall, wearing a green t-shirt. He has a serious expression on his face, and his hands are clasped in his lap. 
                                      His hair is short and dark, and he has a stubble on his chin. He is wearing a pair of dark jeans and white sneakers.
                                      GNAVTRTKN a man standing in front of a mirror in a room. He is wearing a green t-shirt and there are clothes hanging from a hanger in the background. 
                                      On the left side of the image, there is a table and a door.
                                      `
                                      
                                      Example of generated prompt: `
                                      A successful businessman, confident yet approachable, stands against a clean white backdrop bathed in soft, diffused light. 
                                      His dark gray suit fits impeccably, paired with a crisp white shirt and a geometric patterned tie. 
                                      A subtle hint of shadow gracefully outlines his figure, enhancing the silhouette while maintaining natural contours. 
                                      The expression on his face is neutral but warm, revealing just enough to suggest a gentle smile.
                                      `

                                      IMPORTANT INSTRUCTIONS:
                                      1. Generate ONLY ONE English prompt
                                      2. Length of the prompt should be from 100 to 150 words
                                      3. Create DETAILED prompts with visual descriptions
                                      4. Include artistic style, lighting, composition, and color when relevant
                                      5. Do NOT include explanations, meta-commentary, or instructions
                                      6. Do NOT include phrases like "Prompt:", "English prompt:", etc.
                                      7. Do NOT mention the translation process in your output
                                      
                                      Only extract from user's avatar description following information if it is present:
                                      1. Gender
                                      2. Age
                                      3. Hair color
                                      4. Nationality
                                      
                                      Do not extract following information from user's avatar description:
                                      1. Environment
                                      2. Clothes
                                      3. Accessories
                                      4. Background
                                      5. Pose
                                      6. Emotions
                                      7. Facial expression
                                      8. Makeup
                                      9. Lighting
                                      
                                      Additional prompt instructions as long as it does not contradict with user's original request:
                                      1. Image should be in realism style without any anime or cartoonish elements
                                      2. There should only be one person in the image
                                      3. Often people getting good results using following phrases: "Smooth skin", "Right proportions", "Superstar", "Realistic", "Hyper-realistic", "4k", "High quality"
                                      4. Make sure that person's face is visible even if request does not mean it explicitly
                                      
                                      Important: Your output must start and end with the actual ENGLISH prompt text only. 
                                      Important: final prompt should be written in English.
                                      Important: if user already written prompt in English and it has more than 20 words in it just send me this input prompt without modifications.
                                      """
                        },
                        new Message { Role = "user", Content = prompt }
                    ],
                    MaxTokens = _settings.MaxTokens,
                    Temperature = _settings.Temperature,
                    Seed = seed,
                    Stop = ["\n\n", "---", "Note:", "**Note", "Prompt:"]
                };

                Log.Debug("Sending completion request with MaxTokens={MaxTokens}, Temperature={Temperature}",
                    _settings.MaxTokens, _settings.Temperature);

                using var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    Log.Error("vLLM API request failed with status {StatusCode}: {ErrorContent}",
                        response.StatusCode, errorContent);
                    return Option<string>.None;
                }

                Log.Debug("Received successful response from vLLM server");
                var result = await response.Content.ReadFromJsonAsync<VllmChatCompletionResponse>(cancellationToken);

                var completionText = result?.Choices.FirstOrDefault()?.Message.Content.Trim();
                if (completionText is null)
                {
                    Log.Warning("No completion text in vLLM response");
                    return Option<string>.None;
                }

                Log.Information("Successfully processed prompt, generated {TextLength} characters",
                    completionText.Length);
                Log.Information("Generated text: {GeneratedText}", completionText);

                return Prelude.Optional(completionText).Map(text => $"{_settings.LoraKeyWord} {text}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing prompt: {ErrorMessage}", ex.Message);
                throw;
            }
        });
}