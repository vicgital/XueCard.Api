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
    Console.Write("Enter chinese character or type 'stop': ");
    var input = Console.ReadLine();
    if (input is not null)
    {
        if (input.Equals("stop"))
        {
            break;
        }
        var id = Guid.NewGuid();
        var translateTask = _translatorComponent.TranslateText(input, translateToLanguage, translateFromLanguage);
        var transliterateTask = _translatorComponent.TransliterateText(input, translateFromLanguage, transliterateScriptCode);
        var speechAudioTask = _textToSpeechComponent.GetAudioSpeechFromText(input, audioLanguage, audioVoiceName, audioProsodyRate);
        Task.WaitAll(translateTask, transliterateTask, speechAudioTask);

        var newFlashCard = new FlashCardModel
        {
            FlashCardId = id,
            Chinese = input,
            English = await translateTask,
            Pinyin = await transliterateTask,
        };

        var speechAudio = await speechAudioTask;
        newFlashCard.AudioName = await SaveAudioFile(folder, ankiFolder, id, speechAudio) ?? string.Empty;        
        flashCards.Add(newFlashCard);

        Console.WriteLine($"--------RESULT--------");
        Console.WriteLine($"Chinese: {newFlashCard.Chinese}");
        Console.WriteLine($"Pinyin: {newFlashCard.Pinyin}");
        Console.WriteLine($"English: {newFlashCard.English}");
        Console.WriteLine($"Audio Name: {newFlashCard.AudioName}");
        Console.WriteLine($"----------------------");
    }
    else
        stop = true;
}




#endregion

#region Generate Files
if (enableImageCreation)
{
    Console.WriteLine("Waiting to generate images...");
    await GenerateImages(flashCards);
}

// Save Flashcards to CSV
Console.WriteLine("Generating FashCards...");
var csvStream = FlashCardExporter.GenerateCsvStream(flashCards);
var csvFilePath = await SaveCsvFile(folder, csvStream);
Console.WriteLine($"FashCards Path: {csvFilePath}");

#endregion

#region Helper Methods

async Task GenerateImages(List<FlashCardModel> flashCards)
{
    var totalFlashCards = flashCards.Count;
    var count = 0;
    Console.WriteLine($"Total Flashcards: {totalFlashCards}");
    foreach (FlashCardModel card in flashCards)
    {
        Console.WriteLine($"Creating Image {count}/{totalFlashCards}...");
        var imageStream = enableImageCreation ? await _imageGeneratorComponent.GenerateImageFromChineseTextAsync(card.English) : null;
        var resizedImage = ImageResizer.ResizeImage(imageStream, 256, 256);
        card.ImageName = await SaveImageFile(folder, ankiFolder, card.FlashCardId, resizedImage) ?? string.Empty;
    }
}

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

async Task<string?> SaveAudioFile(string folder, string ankiFolder, Guid id, Stream? audioStream)
{
    string? audioName = null;
    // Save Audio
    if (audioStream is not null)
    {
        try
        {
            audioStream.Position = 0; // rewind the stream just in case    
            audioName = $"{id}.mp3";
            var audioFilePath = $"{folder}\\{audioName}";
            using var audioFileStream = File.Create(audioFilePath);
            await audioStream.CopyToAsync(audioFileStream);

            // Save file to anki media collection
            audioStream.Position = 0; // rewind the stream just in case    
            var ankiAudioFilePath = $"{ankiFolder}\\{audioName}";
            using var audioAnkiFileStream = File.Create(ankiAudioFilePath);
            await audioStream.CopyToAsync(audioAnkiFileStream);

        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unable to save audio file");
        }
    }

    return audioName;
}

async Task<string?> SaveImageFile(string folder, string ankiCollectionMediaFolder, Guid guid, Stream? imageStream)
{
    string? imageName = null;

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

    return imageName;
}

#endregion



