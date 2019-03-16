namespace SoundWords
{
    public interface IMetaDataCollection
    {
        string Title { get; set; }
        string Description { get; set; }
        string Url { get; set; }
        string this[string key] { get; set; }
    }
}