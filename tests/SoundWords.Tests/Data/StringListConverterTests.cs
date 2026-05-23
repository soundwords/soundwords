using SoundWords.Data;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SoundWords.Tests.Data;

public class StringListConverterTests
{
    [Test]
    public async Task ReturnsNull_ForNullOrEmpty()
    {
        await Assert.That(SoundWordsDb.DeserializeStringList(null)).IsNull();
        await Assert.That(SoundWordsDb.DeserializeStringList("")).IsNull();
    }

    [Test]
    public async Task ParsesJson_NewFormat()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[\"/a/b.jpg\",\"/c/d.pdf\"]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "/a/b.jpg", "/c/d.pdf" });
    }

    [Test]
    public async Task ParsesJsv_LegacyUnquotedPaths()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[/var/audio/public/foo.jpg,/var/audio/public/bar.pdf]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "/var/audio/public/foo.jpg", "/var/audio/public/bar.pdf" });
    }

    [Test]
    public async Task ParsesJsv_SingleItem()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[/just/one.flac]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "/just/one.flac" });
    }

    [Test]
    public async Task ParsesJsv_QuotedItemWithDoubledQuote()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[\"weird\"\"path\",/plain/path.jpg]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!).IsEquivalentTo(new[] { "weird\"path", "/plain/path.jpg" });
    }

    [Test]
    public async Task ParsesJsv_EmptyList()
    {
        List<string>? actual = SoundWordsDb.DeserializeStringList("[]");
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!.Count).IsEqualTo(0);
    }
}
