using SoundWords.Data;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SoundWords.Tests.Data;

public class StringListConverterTests
{
    [Test]
    public async Task Deserialize_NullOrEmpty_ReturnsNull()
    {
        await Assert.That(SoundWordsDb.DeserializeStringList(null)).IsNull();
        await Assert.That(SoundWordsDb.DeserializeStringList("")).IsNull();
    }

    [Test]
    public async Task Deserialize_LegacyUnquotedPaths()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[/var/audio/public/foo.jpg,/var/audio/public/bar.pdf]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "/var/audio/public/foo.jpg", "/var/audio/public/bar.pdf" });
    }

    [Test]
    public async Task Deserialize_SingleItem()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[/just/one.flac]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "/just/one.flac" });
    }

    [Test]
    public async Task Deserialize_EmptyList()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Serialize_ProducesJsv_RoundTrips()
    {
        List<string> input = new() { "/var/audio/public/foo.jpg", "/var/audio/public/bar.pdf" };
        string? serialized = SoundWordsDb.SerializeStringList(input);

        await Assert.That(serialized).IsEqualTo("[/var/audio/public/foo.jpg,/var/audio/public/bar.pdf]");

        List<string>? roundTripped = SoundWordsDb.DeserializeStringList(serialized);
        await Assert.That(roundTripped).IsNotNull();
        await Assert.That(roundTripped!).IsEquivalentTo(input);
    }

    [Test]
    public async Task Serialize_NullInput_ReturnsNull()
    {
        await Assert.That(SoundWordsDb.SerializeStringList(null)).IsNull();
    }
}
