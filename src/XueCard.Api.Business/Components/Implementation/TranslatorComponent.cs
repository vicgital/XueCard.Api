using System.Data;
using Azure.AI.Translation.Text;
using XueCard.Api.Business.Components.Definition;

namespace XueCard.Api.Business.Components.Implementation
{
    public class TranslatorComponent(TextTranslationClient client) : ITranslatorComponent
    {
        private readonly TextTranslationClient _client = client;

        public async Task<GetSupportedLanguagesResult> GetSupportedLanguages(string? scope = null)
        {
            var result = await _client.GetSupportedLanguagesAsync(scope: scope);
            return result.Value;
        }

        public async Task<string> TranslateText(string text, string targetLanguage, string? sourceLanguage = null)
        {
            var options = new TextTranslationTranslateOptions(targetLanguage, text)
            {
                SourceLanguage = sourceLanguage,
                TextType = TextType.Plain, 
            };

            var translationResult = await _client.TranslateAsync(options);

            var result = translationResult.Value;

            TranslatedTextItem? translatedText = result[0];
            if (translatedText is not null)
                return translatedText.Translations[0].Text;
            else
                throw new OperationCanceledException("Unable to translate Text");
        }



        public async Task<string> TransliterateText(string text, string language, string fromScript)
        {
            var options = new TextTranslationTransliterateOptions(language, fromScript, "Latn", text);

            var transliteratorResult = await _client.TransliterateAsync(options);
            var result = transliteratorResult.Value;

            var translatedText = result.FirstOrDefault();
            if (translatedText is not null)
                return translatedText.Text;
            else
                return "";
        }





    }
}
