using Microsoft.Extensions.Logging;
using OpenAI.Images;
using XueCard.Api.Business.Components.Definition;

namespace XueCard.Api.Business.Components.Implementation
{
    public class DalleImageGeneratorComponent(ImageClient client, ILogger<DalleImageGeneratorComponent> logger) : IDalleImageGeneratorComponent
    {
        private readonly ImageClient _client = client;
        private readonly ILogger<DalleImageGeneratorComponent> _logger = logger;

        public async Task<Stream?> GenerateImageFromChineseTextAsync(string prompt)
        {
            try
            {
                var fullPrompt = $"Create an illustrative and stylized educational image for a study flashcard using this phrase: \"{prompt}\". " +
                         $"Do NOT include any text in the image at all." +
                         $"I don't wan't any letters, just illustrative images";
                var response = await _client.GenerateImageAsync(fullPrompt, new ImageGenerationOptions
                {
                    EndUserId = "xuecard-api",
                    Size = GeneratedImageSize.W1024xH1024,
                    Quality = GeneratedImageQuality.Standard,
                    Style = GeneratedImageStyle.Vivid,
                    ResponseFormat = GeneratedImageFormat.Bytes,
                });

                var streamResult = response.Value.ImageBytes.ToStream();
                return streamResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateImageFromChineseTextAsync()");
                return null;
            }


        }
    }
}
