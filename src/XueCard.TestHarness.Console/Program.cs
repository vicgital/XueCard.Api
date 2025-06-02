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
string ankiFolder = "C:\\Users\\vic_a\\AppData\\Roaming\\Anki2\\User 1\\collection.media";
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

            (string? audioName, string? imageName) = await SaveFiles(folder, ankiFolder, id, speechAudioStream, resizedImage);
            var flashCard = flashCards.FirstOrDefault(e => e.FlashCardId == flashCardId);
            if (flashCard is not null)
            {
                flashCard.AudioName = audioName ?? string.Empty;
                flashCard.ImageName = imageName ?? string.Empty;
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

Console.WriteLine("Waiting to generate images and audios...");
Task.WaitAll(tasks);

// Save Flashcards to CSV
Console.WriteLine("Generating FashCards...");
var csvStream = FlashCardExporter.GenerateCsvStream(flashCards);
var csvFilePath = await SaveCsvFile(folder, csvStream);
Console.WriteLine($"FashCards Path: {csvFilePath}");





async Task<string> SaveCsvFile(string folder, Stream csvStream)
{
    csvStream.Position = 0; // rewind the stream just in case    
    var csvFilePath = $"{folder}\\ankiCards.csv";
    using var csvFileStream = File.Create(csvFilePath);
    await csvStream.CopyToAsync(csvFileStream);
    return csvFilePath;
}

string CreateNewFolder()
{
    var baseDirectory = $"D:\\TEMP\\";
    var newDirectory = $"{baseDirectory}\\{DateTime.Now.ToFileTimeUtc()}";
    System.IO.Directory.CreateDirectory(newDirectory);
    return newDirectory;
}

async Task<(string?, string?)> SaveFiles(string folder, string ankiCollectionMediaFolder, Guid guid, Stream? audioStream, Stream? imageStream)
{

    string? audioName = null;
    string? imageName = null;

    // Save Audio
    if (audioStream is not null)
    {
        try
        {
            audioStream.Position = 0; // rewind the stream just in case    
            audioName = $"{guid}.mp3";
            var audioFilePath = $"{folder}\\{audioName}";
            using var audioFileStream = File.Create(audioFilePath);
            await audioStream.CopyToAsync(audioFileStream);

            // Save file to anki media collection
            audioStream.Position = 0; // rewind the stream just in case    
            var ankiAudioFilePath = $"{ankiCollectionMediaFolder}\\{audioName}";
            using var audioAnkiFileStream = File.Create(ankiAudioFilePath);
            await audioStream.CopyToAsync(audioAnkiFileStream);

        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unable to save audio file");
        }
    }

    // Save Image
    if (imageStream is not null)
    {
        try
        {
            imageStream.Position = 0; // rewind the stream just in case    
            imageName = $"{guid}.png";
            var imageFilePath = $"{folder}\\{imageName}";
            using var imageFileStream = File.Create(imageFilePath);
            await imageStream.CopyToAsync(imageFileStream);

            // Save file to anki media collection
            imageStream.Position = 0; // rewind the stream just in case    
            var ankiImageFilePath = $"{ankiCollectionMediaFolder}\\{imageName}";
            using var imageAnkiFileStream = File.Create(ankiImageFilePath);
            await imageStream.CopyToAsync(imageAnkiFileStream);

        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unable to save image file");
        }
    }

    return (audioName, imageName);
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