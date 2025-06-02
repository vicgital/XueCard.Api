using Microsoft.CognitiveServices.Speech;
using XueCard.Api.Business.Components.Definition;

namespace XueCard.Api.Business.Components.Implementation
{
    public class TextToSpeechComponent(SpeechConfig speechConfig) : ITextToSpeechComponent
    {
        private readonly SpeechConfig _speechConfig = speechConfig;


        public async Task<Stream> GetAudioSpeechFromText(string text, string voiceName)
        {

            _speechConfig.SpeechSynthesisVoiceName = voiceName;
            _speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio48Khz192KBitRateMonoMp3);

            using var synthesizer = new SpeechSynthesizer(_speechConfig);

            string ssml = @$"
                            <speak version='1.0' xml:lang='zh-CN'>
                              <voice name='{voiceName}'>
                                <prosody rate='0%' pitch='0%'>
                                  {text}
                                </prosody>
                              </voice>
                            </speak>";

            var speechSynthesisResult = await synthesizer.SpeakSsmlAsync(ssml);

            if (speechSynthesisResult.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                MemoryStream stream = new(speechSynthesisResult.AudioData)
                {
                    Position = 0
                };
                return stream;
            }
            else
                throw new Exception("Unable to Get Audio from Text");


        }
    }
}
