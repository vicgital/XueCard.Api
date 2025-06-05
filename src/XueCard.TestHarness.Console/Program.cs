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


#region Setup
Console.InputEncoding = Encoding.Unicode;
Console.OutputEncoding = Encoding.Unicode;
var config = AppConfigurationFactory.GetAppConfigurationFromConnectionString();
var services = ConfigureApp(config);
var provider = services.BuildServiceProvider();
Console.WriteLine("Creating new file directory..");
var folder = CreateNewFolder();
Console.WriteLine($"Folder Created: {folder}");

var _translatorComponent = provider.GetRequiredService<ITranslatorComponent>();
var _textToSpeechComponent = provider.GetRequiredService<ITextToSpeechComponent>();
var _imageGeneratorComponent = provider.GetRequiredService<IDalleImageGeneratorComponent>();

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

#endregion

#region Variable Initialization
string ankiFolder = "C:\\Users\\vic_a\\AppData\\Roaming\\Anki2\\User 1\\collection.media";
string translateToLanguage = "en";
string translateFromLanguage = "zh-Hans";
string transliterateScriptCode = "Hans";
string audioVoiceName = "zh-CN-YunxiNeural";
string audioLanguage = "zh-CN";
string audioProsodyRate = "slow"; // slow, medium, fast
bool enableImageCreation = false;
#endregion




bool stop = false;
List<Task> tasks = [];
List<FlashCardModel> flashCards = [];

#region User Customization

// ---- Create Image ? -------
Console.Write("Create Image? (Y/N): ");
var userEnabledImage = Console.ReadLine();
enableImageCreation = !string.IsNullOrEmpty(userEnabledImage) && userEnabledImage.Equals("y", StringComparison.InvariantCultureIgnoreCase);

// ---- Choose Voice -------
Console.WriteLine("Chinese Voices");
Console.WriteLine("1. zh-CN-YunxiNeural");
Console.WriteLine("2. zh-CN-XiaoxiaoNeural");
Console.WriteLine("3. zh-CN-YunjianNeural");
Console.WriteLine("4. zh-CN-YunyangNeural");
Console.Write("Choose Chinese Voice (Default is zh-CN-YunxiNeural):");
switch (Console.ReadLine())
{
    case "1":
        audioVoiceName = "zh-CN-YunxiNeural";
        break;
    case "2":
        audioVoiceName = "zh-CN-XiaoxiaoNeural";
        break;
    case "3":
        audioVoiceName = "zh-CN-YunjianNeural";
        break;
    case "4":
        audioVoiceName = "zh-CN-YunyangNeural";
        break;
    default:
        break;
}
#endregion


#region Start Workflow

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
        var translateResult = await _translatorComponent.TranslateText(input, translateToLanguage, translateFromLanguage);
        var transliterateResult = await _translatorComponent.TransliterateText(input, translateFromLanguage, transliterateScriptCode);
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
            var speechAudioStream = await _textToSpeechComponent.GetAudioSpeechFromText(input, audioLanguage, audioVoiceName, audioProsodyRate);
            var imageStream = enableImageCreation ? await _imageGeneratorComponent.GenerateImageFromChineseTextAsync(translateResult) : null;
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


#endregion 

#region Generate Files

Console.WriteLine("Waiting to generate images and audios...");
Task.WaitAll(tasks);

// Save Flashcards to CSV
Console.WriteLine("Generating FashCards...");
var csvStream = FlashCardExporter.GenerateCsvStream(flashCards);
var csvFilePath = await SaveCsvFile(folder, csvStream);
Console.WriteLine($"FashCards Path: {csvFilePath}");

#endregion

#region Helper Methods

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

#endregion



