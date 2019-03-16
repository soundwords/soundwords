using FluentMigrator;

namespace SoundWords.Migrations
{
    [Migration(0)]
    public class CreateInitialDbMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Create.Table("DbAlbum")
                  .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                  .WithColumn("CreatedOn").AsDateTime().NotNullable()
                  .WithColumn("ModifiedOn").AsDateTime().NotNullable()
                  .WithColumn("Deleted").AsBoolean().NotNullable()
                  .WithColumn("Uid").AsString(40).NotNullable().Unique()
                  .WithColumn("Name").AsString(255).Nullable().Indexed()
                  .WithColumn("ProductNo").AsString(50).Nullable()
                  .WithColumn("MasterNo").AsString(50).Nullable()
                  .WithColumn("StorageNo").AsString(50).Nullable()
                  .WithColumn("Occasion").AsString(255).Nullable()
                  .WithColumn("Place").AsString(255).Nullable()
                  .WithColumn("Comment").AsString(int.MaxValue).Nullable()
                  .WithColumn("Path").AsString(int.MaxValue).Nullable()
                  .WithColumn("AlbumArtPath").AsString(int.MaxValue).Nullable()
                  .WithColumn("AttachmentPaths").AsString(int.MaxValue).Nullable()
                  .WithColumn("Restricted").AsBoolean().NotNullable();

            Create.Table("DbSpeaker")
                  .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                  .WithColumn("CreatedOn").AsDateTime().NotNullable()
                  .WithColumn("ModifiedOn").AsDateTime().NotNullable()
                  .WithColumn("Deleted").AsBoolean().NotNullable()
                  .WithColumn("Uid").AsString(40).NotNullable().Unique()
                  .WithColumn("FirstName").AsString(255).Nullable()
                  .WithColumn("LastName").AsString(255).Nullable()
                  .WithColumn("BirthDay").AsInt16().Nullable()
                  .WithColumn("BirthMonth").AsInt16().Nullable()
                  .WithColumn("BirthYear").AsInt16().Nullable()
                  .WithColumn("Nationality").AsString(50).Nullable()
                  .WithColumn("Description").AsString(int.MaxValue).Nullable();

            Create.Index().OnTable("DbSpeaker")
                  .OnColumn("FirstName").Ascending()
                  .OnColumn("LastName").Ascending();

            Create.Table("DbRecording")
                  .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                  .WithColumn("CreatedOn").AsDateTime().NotNullable()
                  .WithColumn("ModifiedOn").AsDateTime().NotNullable()
                  .WithColumn("Deleted").AsBoolean().NotNullable()
                  .WithColumn("Uid").AsString(40).NotNullable().Unique()
                  .WithColumn("AlbumId").AsInt64().NotNullable().ForeignKey("DbAlbum", "Id")
                  .WithColumn("Title").AsString(255).Nullable()
                  .WithColumn("Track").AsInt16().NotNullable()
                  .WithColumn("Comment").AsString(int.MaxValue).Nullable()
                  .WithColumn("Day").AsInt16().Nullable()
                  .WithColumn("Month").AsInt16().Nullable()
                  .WithColumn("Year").AsInt16().Nullable()
                  .WithColumn("Path").AsString(int.MaxValue).Nullable()
                  .WithColumn("Restricted").AsBoolean().NotNullable().Indexed();

            Create.Table("DbRecordingSpeaker")
                  .WithColumn("RecordingId").AsInt64().NotNullable().ForeignKey("DbRecording", "Id")
                  .WithColumn("SpeakerId").AsInt64().NotNullable().ForeignKey("DbSpeaker", "Id")
                  .WithColumn("CreatedOn").AsDateTime().NotNullable()
                  .WithColumn("ModifiedOn").AsDateTime().NotNullable();

            Create.Index().OnTable("DbRecordingSpeaker")
                  .OnColumn("RecordingId").Ascending()
                  .OnColumn("SpeakerId").Ascending();

            Create.Table("DbScripture")
                  .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                  .WithColumn("CreatedOn").AsDateTime().NotNullable()
                  .WithColumn("ModifiedOn").AsDateTime().NotNullable()
                  .WithColumn("RecordingId").AsInt64().NotNullable().ForeignKey("DbRecording", "Id")
                  .WithColumn("Book").AsInt16().NotNullable()
                  .WithColumn("FromChapter").AsInt16().Nullable()
                  .WithColumn("FromVerse").AsInt16().Nullable()
                  .WithColumn("ToChapter").AsInt16().Nullable()
                  .WithColumn("ToVerse").AsInt16().Nullable();
        }
    }
}
