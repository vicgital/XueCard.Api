using System.Text;
using Azure.AI.Translation.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Images;
using XueCard.Api.Business.AzureClients;
using XueCard.Api.Business.Components.Definition;
using XueCard.Api.Business.Components.Implementation;
using XueCard.Api.Business.Extensions;
using XueCard.Api.Business.Helpers;
using XueCard.Api.Business.Infrastructure;
using XueCard.Api.Business.Infrastructure.Definition;
using XueCard.Api.Business.Infrastructure.Implementation;
using XueCard.Api.Models;

var config = AppConfigurationFactory.GetAppConfigurationFromConnectionString();
var services = ConfigureApp(config);
var provider = services.BuildServiceProvider();

var translatorComponent = provider.GetRequiredService<ITranslatorComponent>();
var textToSpeechComponent = provider.GetRequiredService<ITextToSpeechComponent>();
var imageGeneratorComponent = provider.GetRequiredService<IDalleImageGeneratorComponent>();
Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;

bool stop = false;
Console.WriteLine("Creating new file directory..");
var folder = CreateNewFolder();
Console.WriteLine($"Folder Created: {folder}");
List<Task> tasks = [];

List<FlashCardModel> flashCards = [];

while (!stop)
{
    Console.WriteLine("Enter chinese character or type 'stop': ");
    var input = Console.ReadLine();
    if (input is not null)
    {
        if (input.Equals("stop"))
        {
            break;
        }
        var id = Guid.NewGuid();
        var translateResult = await translatorComponent.TranslateText(input, "en", "zh-Hans");
        var transliterateResult = await translatorComponent.TransliterateText(input, "zh-Hans", "Hans");
        flashCards.Add(new FlashCardModel
        {
            FlashCardId = id,
            Chinese = input,
            English = translateResult,
            Pinyin = transliterateResult
        });

        tasks.Add(Task.Run(async () =>
        {
            var flashCardId = id;
            var speechAudioStream = await textToSpeechComponent.GetAudioSpeechFromText(input, "zh-CN-XiaoxiaoNeural");
            var imageStream = await imageGeneratorComponent.GenerateImageFromChineseTextAsync(translateResult);
            var resizedImage = ImageResizer.ResizeImage(imageStream, 256, 256);
            (string audoFilePath, string imageFilePath) = await SaveFiles(folder, id, speechAudioStream, resizedImage);
            var flashCard = flashCards.FirstOrDefault(e => e.FlashCardId == flashCardId);
            if (flashCard is not null)
            {
                flashCard.AudioFilePath = audoFilePath;
                flashCard.ImageFilePath = imageFilePath;
            }
        }));



        Console.WriteLine($"--------RESULT--------");
        Console.WriteLine($"Chinese: {input}");
        Console.WriteLine($"Pinyin: {transliterateResult}");
        Console.WriteLine($"English: {translateResult}");
        Console.WriteLine($"----------------------");
    }
    else
        stop = true;
}

Task.WaitAll(tasks);




string CreateNewFolder()
{
    var baseDirectory = $"D:\\TEMP\\";
    var newDirectory = $"{baseDirectory}\\{DateTime.Now.ToFileTimeUtc()}";
    System.IO.Directory.CreateDirectory(newDirectory);
    return newDirectory;
}

async Task<(string, string)> SaveFiles(string folder, Guid guid, Stream audioStream, Stream imageStream)
{
    audioStream.Position = 0; // rewind the stream just in case    
    imageStream.Position = 0; // rewind the stream just in case    
    var filesFolder = $"{folder}\\{guid}";
    System.IO.Directory.CreateDirectory(filesFolder);

    // Save Audio
    var audioFilePath = $"{filesFolder}\\{guid}.mp3";
    using var audioFileStream = File.Create(audioFilePath);
    await audioStream.CopyToAsync(audioFileStream);

    // Save Image
    var imageFilePath = $"{filesFolder}\\{guid}.png";
    using var imageFileStream = File.Create(imageFilePath);
    await imageStream.CopyToAsync(imageFileStream);

    return (audioFilePath, imageFilePath);
}

static ServiceCollection ConfigureApp(IConfiguration config)
{
    ServiceCollection services = new();
    services.AddSingleton<IConfiguration>(config);
    services.AddSingleton<TextTranslationClient>(AzureClientFactory.GetTextTranslationClient(config));
    services.AddSingleton<SpeechConfig>(AzureClientFactory.GetSpeechSynthesizerConfig(config));
    services.AddSingleton<ImageClient>(AzureClientFactory.GetOpenAIImageClient(config));
    services.AddSingleton<IAppConfiguration, AppConfiguration>();
    services.AddSingleton<ITextToSpeechComponent, TextToSpeechComponent>();
    services.AddSingleton<ITranslatorComponent, TranslatorComponent>();
    services.AddSingleton<IDalleImageGeneratorComponent, DalleImageGeneratorComponent>();

    services.AddSerilogLogging(config);

    return services;

}