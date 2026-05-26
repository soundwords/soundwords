using SoundWords.Models;

namespace SoundWords.Media;

public interface ISignedMediaUrls
{
    /// <summary>
    /// Returns an absolute URL the client can fetch directly from the media host (MinIO).
    /// Non-restricted recordings come from the anonymous-read bucket as a stable path;
    /// restricted recordings come from the private bucket as a pre-signed URL with the
    /// expiry configured in <see cref="ISoundWordsConfiguration.SignedUrlLifetime"/>.
    /// </summary>
    string ForRecording(Recording recording);
}
