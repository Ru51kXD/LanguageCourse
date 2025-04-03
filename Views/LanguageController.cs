public class LanguageInfoViewModel
{
    public required string Name { get; set; }
    public required string NativeName { get; set; }
    public required string SpeakersCount { get; set; }
    public required string Region { get; set; }
    public required string FlagEmoji { get; set; }
    public required string BannerImage { get; set; }
    public required string Description { get; set; }
    public required List<LanguageFact> Facts { get; set; }
}

public class LanguageFact
{
    public required string Number { get; set; }
    public required string Title { get; set; }
    public required string Text { get; set; }
} 