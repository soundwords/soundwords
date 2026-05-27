using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.DependencyInjection;
using SoundWords.Data;
using SoundWords.Models;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace SoundWords.Tests.Data;

public class DbAlbumPersistenceTests
{
    [ClassDataSource<SoundWordsApp>(Shared = SharedType.PerTestSession)]
    public required SoundWordsApp App { get; init; }

    [Test]
    public async Task Insert_StoresAttachmentPathsAsJsv_AndRoundTrips()
    {
        Func<SoundWordsDb> dbFactory = ResolveDbFactory();

        DbAlbum album = new()
                        {
                            Uid = Guid.NewGuid().ToString("N"),
                            Name = "Round-trip album",
                            AttachmentPaths = new List<string> { "/a.jpg", "/b.pdf" }
                        };

        long albumId;
        using (SoundWordsDb db = dbFactory())
        {
            albumId = await db.InsertWithTimestampsAsync(album);
        }

        using (SoundWordsDb db = dbFactory())
        {
            string? raw = db.Query<string?>(
                                "SELECT AttachmentPaths FROM DbAlbum WHERE Id = @id",
                                new DataParameter("id", albumId))
                            .Single();
            await Assert.That(raw).IsEqualTo("[/a.jpg,/b.pdf]");

            DbAlbum? loaded = db.Albums.SingleOrDefault(a => a.Id == albumId);
            await Assert.That(loaded).IsNotNull();
            await Assert.That(loaded!.AttachmentPaths).IsNotNull();
            await Assert.That(loaded!.AttachmentPaths!)
                        .IsEquivalentTo(new[] { "/a.jpg", "/b.pdf" });
        }
    }

    [Test]
    public async Task Insert_NullAttachmentPaths_StoredAsNull()
    {
        Func<SoundWordsDb> dbFactory = ResolveDbFactory();

        DbAlbum album = new()
                        {
                            Uid = Guid.NewGuid().ToString("N"),
                            Name = "Null attachments",
                            AttachmentPaths = null
                        };

        long albumId;
        using (SoundWordsDb db = dbFactory())
        {
            albumId = await db.InsertWithTimestampsAsync(album);
        }

        using (SoundWordsDb db = dbFactory())
        {
            string? raw = db.Query<string?>(
                                "SELECT AttachmentPaths FROM DbAlbum WHERE Id = @id",
                                new DataParameter("id", albumId))
                            .Single();
            await Assert.That(raw).IsNull();

            DbAlbum? loaded = db.Albums.SingleOrDefault(a => a.Id == albumId);
            await Assert.That(loaded).IsNotNull();
            await Assert.That(loaded!.AttachmentPaths).IsNull();
        }
    }

    private Func<SoundWordsDb> ResolveDbFactory()
    {
        return App.Factory.Services.GetRequiredService<Func<SoundWordsDb>>();
    }
}
