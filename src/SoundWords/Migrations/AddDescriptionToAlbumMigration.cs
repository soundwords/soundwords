using FluentMigrator;

namespace SoundWords.Migrations
{
    [Migration(201709110713)]
    public class AddDescriptionToAlbumMigration : ForwardOnlyMigration
    {
        public override void Up()
        {
            Alter.Table("DbAlbum")
                 .AddColumn("Description").AsString(int.MaxValue).Nullable();
        }
    }
}
