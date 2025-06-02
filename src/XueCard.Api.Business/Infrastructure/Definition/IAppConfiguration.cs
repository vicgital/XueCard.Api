namespace XueCard.Api.Business.Infrastructure.Definition
{
    public interface IAppConfiguration
    {
        string GetValue(string key);
        string GetValue(string key, string defaultValue);
    }
}
