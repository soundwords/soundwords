using FluentMigrator;

namespace SoundWords.Migrations
{
    [Migration(201709070000), FluentMigrator.Tags("Domain")]
    public class AddPhotoToSpeakerMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("DbSpeaker")
                 .AddColumn("PhotoPath").AsString(int.MaxValue).Nullable();
        }
    }
}
