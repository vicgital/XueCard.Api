namespace XueCard.Api.Models
{
    public class FlashCardModel
    {
        public Guid FlashCardId { get; set; }
        public string Chinese { get; set; } = string.Empty;
        public string Pinyin { get; set;} = string.Empty;
        public string English { get; set; } = string.Empty;
        public string AudioName { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;

    }
}
