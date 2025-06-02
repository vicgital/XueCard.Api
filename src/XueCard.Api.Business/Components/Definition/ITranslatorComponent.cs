using Azure.AI.Translation.Text;

namespace XueCard.Api.Business.Components.Definition
{
    public interface ITranslatorComponent
    {
        Task<string> TranslateText(string text, string targetLanguage, string? sourceLanguage = null);

        Task<string> TransliterateText(string text, string language, string fromScript);

        Task<GetSupportedLanguagesResult> GetSupportedLanguages(string? scope = null);
    }
}
