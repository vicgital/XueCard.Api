using System.Text;
using XueCard.Api.Models;

namespace XueCard.Api.Business.Helpers
{
    public static class FlashCardExporter
    {
        public static Stream GenerateCsvStream(List<FlashCardModel> flashCards)
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8);

            // Write header
            //streamWriter.WriteLine("Chinese|Pinyin|English|Audio|Image");

            // Write rows
            foreach (var card in flashCards)
            {
                string line = string.Join("|",
                    card.Chinese,
                    card.Pinyin,
                    card.English,
                    (string.IsNullOrEmpty(card.AudioName) ? "" : $"[sound:{card.AudioName}]"),
                    (string.IsNullOrEmpty(card.ImageName) ? "" : $"<img src=\"{card.ImageName}\">")
                );

                streamWriter.WriteLine(line);
            }

            streamWriter.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }        
    }
}