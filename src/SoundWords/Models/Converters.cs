using System;
using ServiceStack;
using SoundWords.Services;

namespace SoundWords.Models
{
    internal static class Converters
    {
        public static Recording ToRecording(this DbRecording dbRecording)
        {
            return dbRecording.ConvertTo<Recording>();
        }

        public static Album ToAlbum(this DbAlbum dbAlbum)
        {
            return dbAlbum.ConvertTo<Album>();
        }

        public static Speaker ToSpeaker(this DbSpeaker dbSpeaker)
        {
            return dbSpeaker.ConvertTo<Speaker>();
        }

        public static SpeakerInfo ToSpeakerInfo(this DbSpeaker dbSpeaker, Func<DbSpeaker, bool> isSelected = null)
        {
            SpeakerInfo speakerInfo = dbSpeaker.ConvertTo<SpeakerInfo>();
            speakerInfo.FullName = $"{speakerInfo.FirstName} {speakerInfo.LastName}";
            speakerInfo.Selected = isSelected?.Invoke(dbSpeaker) ?? false;
 
            return speakerInfo;
        }

        public static SpeakerInfo ToSpeakerInfo(this Speaker speaker, Func<Speaker, bool> isSelected = null)
        {
            SpeakerInfo speakerInfo = speaker.ConvertTo<SpeakerInfo>();
            speakerInfo.Selected = isSelected?.Invoke(speaker) ?? false;
            speakerInfo.HasPhoto = speaker.PhotoPath != null;
            return speakerInfo;
        }

        public static Scripture ToScripture(this DbScripture dbScripture)
        {
            return dbScripture.ConvertTo<Scripture>();
        }
    }
}
