namespace SoundWords.Models;

internal static class Converters
{
    public static Recording ToRecording(this DbRecording dbRecording)
    {
        return new Recording
               {
                   Id = dbRecording.Id,
                   CreatedOn = dbRecording.CreatedOn,
                   ModifiedOn = dbRecording.ModifiedOn,
                   Uid = dbRecording.Uid,
                   AlbumId = dbRecording.AlbumId,
                   Title = dbRecording.Title,
                   Track = (short)dbRecording.Track,
                   Comment = dbRecording.Comment,
                   Day = dbRecording.Day,
                   Month = dbRecording.Month,
                   Year = dbRecording.Year,
                   Path = dbRecording.Path,
                   Restricted = dbRecording.Restricted
               };
    }

    public static Album ToAlbum(this DbAlbum dbAlbum)
    {
        return new Album
               {
                   Id = dbAlbum.Id,
                   CreatedOn = dbAlbum.CreatedOn,
                   ModifiedOn = dbAlbum.ModifiedOn,
                   Uid = dbAlbum.Uid,
                   Name = dbAlbum.Name,
                   ProductNo = dbAlbum.ProductNo,
                   MasterNo = dbAlbum.MasterNo,
                   StorageNo = dbAlbum.StorageNo,
                   Occasion = dbAlbum.Occasion,
                   Place = dbAlbum.Place,
                   Comment = dbAlbum.Comment,
                   Path = dbAlbum.Path,
                   AlbumArtPath = dbAlbum.AlbumArtPath,
                   AttachmentPaths = dbAlbum.AttachmentPaths,
                   Restricted = dbAlbum.Restricted
               };
    }

    public static Speaker ToSpeaker(this DbSpeaker dbSpeaker)
    {
        return new Speaker
               {
                   Id = dbSpeaker.Id,
                   CreatedOn = dbSpeaker.CreatedOn,
                   ModifiedOn = dbSpeaker.ModifiedOn,
                   Uid = dbSpeaker.Uid,
                   FirstName = dbSpeaker.FirstName,
                   LastName = dbSpeaker.LastName,
                   BirthDay = dbSpeaker.BirthDay,
                   BirthMonth = dbSpeaker.BirthMonth,
                   BirthYear = dbSpeaker.BirthYear,
                   Nationality = dbSpeaker.Nationality,
                   Description = dbSpeaker.Description,
                   PhotoPath = dbSpeaker.PhotoPath
               };
    }

    public static SpeakerInfo ToSpeakerInfo(this DbSpeaker dbSpeaker, Func<DbSpeaker, bool>? isSelected = null)
    {
        return new SpeakerInfo
               {
                   Uid = dbSpeaker.Uid,
                   FirstName = dbSpeaker.FirstName,
                   LastName = dbSpeaker.LastName,
                   FullName = $"{dbSpeaker.FirstName} {dbSpeaker.LastName}",
                   Selected = isSelected?.Invoke(dbSpeaker) ?? false,
                   HasPhoto = dbSpeaker.PhotoPath != null,
                   Description = dbSpeaker.Description
               };
    }

    public static SpeakerInfo ToSpeakerInfo(this Speaker speaker, Func<Speaker, bool>? isSelected = null)
    {
        return new SpeakerInfo
               {
                   Uid = speaker.Uid,
                   FirstName = speaker.FirstName,
                   LastName = speaker.LastName,
                   FullName = speaker.FullName,
                   Selected = isSelected?.Invoke(speaker) ?? false,
                   HasPhoto = speaker.PhotoPath != null,
                   Description = speaker.Description
               };
    }
}
