namespace XueCard.Api.Business.Components.Definition
{
    public interface ITextToSpeechComponent
    {

        Task<Stream?> GetAudioSpeechFromText(string text, string languageCode, string voiceName, string audioProsodyRate);

    }
}
