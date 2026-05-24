using FluentMigrator;

namespace SoundWords.Migrations;

[Migration(202605231000), FluentMigrator.Tags("Users")]
public class CreateIdentityTablesMigration : ForwardOnlyMigration
{
    private const int IdLength = 255;
    private const int IndexedStringLength = 256;
    private const int TokenColumnLength = 128;

    public override void Up()
    {
        Create.Table("AspNetRoles")
              .WithColumn("Id").AsString(IdLength).PrimaryKey()
              .WithColumn("Name").AsString(IndexedStringLength).Nullable()
              .WithColumn("NormalizedName").AsString(IndexedStringLength).Nullable()
              .WithColumn("ConcurrencyStamp").AsString(int.MaxValue).Nullable();

        Create.Index("RoleNameIndex").OnTable("AspNetRoles")
              .OnColumn("NormalizedName").Ascending().WithOptions().Unique();

        Create.Table("AspNetUsers")
              .WithColumn("Id").AsString(IdLength).PrimaryKey()
              .WithColumn("UserName").AsString(IndexedStringLength).Nullable()
              .WithColumn("NormalizedUserName").AsString(IndexedStringLength).Nullable()
              .WithColumn("Email").AsString(IndexedStringLength).Nullable()
              .WithColumn("NormalizedEmail").AsString(IndexedStringLength).Nullable()
              .WithColumn("EmailConfirmed").AsBoolean().NotNullable()
              .WithColumn("PasswordHash").AsString(int.MaxValue).Nullable()
              .WithColumn("SecurityStamp").AsString(int.MaxValue).Nullable()
              .WithColumn("ConcurrencyStamp").AsString(int.MaxValue).Nullable()
              .WithColumn("PhoneNumber").AsString(int.MaxValue).Nullable()
              .WithColumn("PhoneNumberConfirmed").AsBoolean().NotNullable()
              .WithColumn("TwoFactorEnabled").AsBoolean().NotNullable()
              .WithColumn("LockoutEnd").AsDateTime().Nullable()
              .WithColumn("LockoutEnabled").AsBoolean().NotNullable()
              .WithColumn("AccessFailedCount").AsInt32().NotNullable()
              .WithColumn("FirstName").AsString(int.MaxValue).Nullable()
              .WithColumn("LastName").AsString(int.MaxValue).Nullable()
              .WithColumn("DisplayName").AsString(int.MaxValue).Nullable();

        Create.Index("UserNameIndex").OnTable("AspNetUsers")
              .OnColumn("NormalizedUserName").Ascending().WithOptions().Unique();

        Create.Index("EmailIndex").OnTable("AspNetUsers")
              .OnColumn("NormalizedEmail").Ascending();

        Create.Table("AspNetRoleClaims")
              .WithColumn("Id").AsInt32().PrimaryKey().Identity()
              .WithColumn("RoleId").AsString(IdLength).NotNullable()
                                   .ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId",
                                               "AspNetRoles", "Id").OnDelete(System.Data.Rule.Cascade)
              .WithColumn("ClaimType").AsString(int.MaxValue).Nullable()
              .WithColumn("ClaimValue").AsString(int.MaxValue).Nullable();

        Create.Index("IX_AspNetRoleClaims_RoleId").OnTable("AspNetRoleClaims")
              .OnColumn("RoleId").Ascending();

        Create.Table("AspNetUserClaims")
              .WithColumn("Id").AsInt32().PrimaryKey().Identity()
              .WithColumn("UserId").AsString(IdLength).NotNullable()
                                   .ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId",
                                               "AspNetUsers", "Id").OnDelete(System.Data.Rule.Cascade)
              .WithColumn("ClaimType").AsString(int.MaxValue).Nullable()
              .WithColumn("ClaimValue").AsString(int.MaxValue).Nullable();

        Create.Index("IX_AspNetUserClaims_UserId").OnTable("AspNetUserClaims")
              .OnColumn("UserId").Ascending();

        Create.Table("AspNetUserLogins")
              .WithColumn("LoginProvider").AsString(IdLength).NotNullable().PrimaryKey("PK_AspNetUserLogins")
              .WithColumn("ProviderKey").AsString(IdLength).NotNullable().PrimaryKey("PK_AspNetUserLogins")
              .WithColumn("ProviderDisplayName").AsString(int.MaxValue).Nullable()
              .WithColumn("UserId").AsString(IdLength).NotNullable()
                                   .ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId",
                                               "AspNetUsers", "Id").OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_AspNetUserLogins_UserId").OnTable("AspNetUserLogins")
              .OnColumn("UserId").Ascending();

        Create.Table("AspNetUserRoles")
              .WithColumn("UserId").AsString(IdLength).NotNullable().PrimaryKey("PK_AspNetUserRoles")
                                   .ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId",
                                               "AspNetUsers", "Id").OnDelete(System.Data.Rule.Cascade)
              .WithColumn("RoleId").AsString(IdLength).NotNullable().PrimaryKey("PK_AspNetUserRoles")
                                   .ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId",
                                               "AspNetRoles", "Id").OnDelete(System.Data.Rule.Cascade);

        Create.Index("IX_AspNetUserRoles_RoleId").OnTable("AspNetUserRoles")
              .OnColumn("RoleId").Ascending();

        Create.Table("AspNetUserTokens")
              .WithColumn("UserId").AsString(IdLength).NotNullable().PrimaryKey("PK_AspNetUserTokens")
                                   .ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId",
                                               "AspNetUsers", "Id").OnDelete(System.Data.Rule.Cascade)
              .WithColumn("LoginProvider").AsString(TokenColumnLength).NotNullable().PrimaryKey("PK_AspNetUserTokens")
              .WithColumn("Name").AsString(TokenColumnLength).NotNullable().PrimaryKey("PK_AspNetUserTokens")
              .WithColumn("Value").AsString(int.MaxValue).Nullable();
    }
}
