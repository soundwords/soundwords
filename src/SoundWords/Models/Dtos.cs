using SoundWords.Social;

namespace SoundWords.Models;

public class IndexResponse
{
    public List<AlbumWithSpeakers> LatestAlbums { get; set; } = new();
    public List<SpeakerInfo> Speakers { get; set; } = new();
}

public class AboutResponse
{
    public List<SpeakerInfo> Speakers { get; set; } = new();
}

public class RecordingResponse
{
    public List<Recording> Recordings { get; set; } = new();
}

public class SpeakerDetailsRequest : IHaveEscapedFragment
{
    public string Name { get; set; } = string.Empty;
    public string? Album { get; set; }
    public string? EscapedFragment { get; set; }
}

public class SpeakerDetailsResponse
{
    public string? Uid { get; set; }
    public string? Speaker { get; set; }
    public List<AlbumInfo> Albums { get; set; } = new();
    public List<SpeakerInfo> Speakers { get; set; } = new();
    public AlbumInfo? SelectedAlbum { get; set; }
    public string? Description { get; set; }
    public bool HasPhoto { get; set; }
}

public class IsRestrictedResponse
{
    public bool IsRestricted { get; set; }
}

public class SpeakerInfo
{
    public string? Uid { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public bool Selected { get; set; }
    public bool HasPhoto { get; set; }
    public string? Description { get; set; }
}

public class AlbumInfo
{
    public string? Uid { get; set; }
    public string? AlbumSpeakers { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool HasAlbumArt { get; set; }
    public List<RecordingInfo> Recordings { get; set; } = new();
    public List<AttachmentInfo> Attachments { get; set; } = new();
}

public class RecordingInfo
{
    public string? Uid { get; set; }
    public string? Title { get; set; }
    public int Track { get; set; }
    public int? Year { get; set; }
    public string? Comment { get; set; }
    public List<SpeakerInfo> Speakers { get; set; } = new();
}

public class AttachmentInfo
{
    public int Index { get; set; }
    public string? Name { get; set; }
}
