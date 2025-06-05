using System.ClientModel;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Translation.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using OpenAI.Images;
using XueCard.Api.Business.Infrastructure.Constants;

namespace XueCard.Api.Business.AzureClients
{
    public static class AzureClientFactory
    {
        public static TextTranslationClient GetTextTranslationClient(IConfiguration config)
        {
            var key = config[AppConfigurationKeyNames.XUECARD_AI_SERVICES_KEY] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_AI_SERVICES_KEY} not found");
            var region = config[AppConfigurationKeyNames.XUECARD_AI_SERVICES_REGION] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_AI_SERVICES_REGION} not found"); ;
            AzureKeyCredential credential = new(key);
            TextTranslationClient client = new(credential, region);
            return client;
        }

        public static SpeechConfig GetSpeechSynthesizerConfig(IConfiguration config)
        {
            string aiSvcKey = config[AppConfigurationKeyNames.XUECARD_AI_SERVICES_KEY] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_AI_SERVICES_KEY} not found");
            string aiSvcRegion = config[AppConfigurationKeyNames.XUECARD_AI_SERVICES_REGION] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_AI_SERVICES_REGION} not found"); ;

            // Configure speech service
            return SpeechConfig.FromSubscription(aiSvcKey, aiSvcRegion);
        }

        public static ImageClient GetOpenAIImageClient(IConfiguration config)
        {
            string openAiEndpoint = config[AppConfigurationKeyNames.XUECARD_OPENAI_ENDPOINT] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_OPENAI_ENDPOINT} not found");
            string openAiKey = config[AppConfigurationKeyNames.XUECARD_OPENAI_KEY] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_OPENAI_KEY} not found");
            string openAiRegion = config[AppConfigurationKeyNames.XUECARD_OPENAI_REGION] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_OPENAI_REGION} not found");
            string openAiModel = config[AppConfigurationKeyNames.XUECARD_OPENAI_DALLE_MODEL] ?? throw new ArgumentNullException($"{AppConfigurationKeyNames.XUECARD_OPENAI_DALLE_MODEL} not found");


            AzureOpenAIClient azureClient = new(new Uri(openAiEndpoint), new ApiKeyCredential(openAiKey));
            ImageClient client = azureClient.GetImageClient(openAiModel);
            return client;
        }

    }
}
