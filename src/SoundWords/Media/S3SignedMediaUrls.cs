using Amazon.S3;
using Amazon.S3.Model;
using SoundWords.Models;

namespace SoundWords.Media;

public sealed class S3SignedMediaUrls : ISignedMediaUrls
{
    private readonly IAmazonS3 _s3;
    private readonly ISoundWordsConfiguration _config;

    public S3SignedMediaUrls(IAmazonS3 s3, ISoundWordsConfiguration config)
    {
        _s3 = s3;
        _config = config;
    }

    public string ForRecording(Recording recording)
    {
        if (recording.Path == null)
        {
            throw new InvalidOperationException($"Recording {recording.Uid} has no path on disk.");
        }

        string baseFolder = recording.Restricted
                                ? _config.RestrictedRecordingsFolder
                                : _config.RecordingsFolder;
        string key = Path.GetRelativePath(baseFolder, recording.Path).Replace('\\', '/');

        if (!recording.Restricted)
        {
            // Anonymous-read bucket — stable, unsigned, safe to embed in the RSS feed.
            return $"{_config.MediaUrl.TrimEnd('/')}/{_config.S3PublicBucket}/{EscapePath(key)}";
        }

        // Restricted — short-lived pre-signed URL via AWS SDK v4 signing.
        return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
                                   {
                                       BucketName = _config.S3RestrictedBucket,
                                       Key = key,
                                       Verb = HttpVerb.GET,
                                       Expires = DateTime.UtcNow.Add(_config.SignedUrlLifetime)
                                   });
    }

    private static string EscapePath(string key) =>
        string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
}
