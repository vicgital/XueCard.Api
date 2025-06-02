namespace XueCard.Api.Business.Components.Definition
{
    public interface IDalleImageGeneratorComponent
    {
        Task<Stream> GenerateImageFromChineseTextAsync(string prompt);
    }
}
