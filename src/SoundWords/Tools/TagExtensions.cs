using System;
using TagLib;
using TagLib.Id3v2;
using TagLib.Ogg;
using File = TagLib.File;
using Tag = TagLib.Id3v2.Tag;

namespace SoundWords.Tools
{
    public static class TagExtensions
    {
        public static string GetCustomField(this File tagFile, string key)
        {
            if (tagFile.GetTag(TagTypes.Id3v2) is Tag id3V2Tag)
            {
                var userTextInformationFrame = UserTextInformationFrame.Get(id3V2Tag, key, StringType.UTF8,
                                                                            false,
                                                                            true);

                return userTextInformationFrame != null ? string.Join(';', userTextInformationFrame.Text) : null;
            }

            if (tagFile.GetTag(TagTypes.Xiph) is XiphComment xiphComment)
                return xiphComment.GetFirstField(key.ToUpperInvariant());

            throw new ArgumentException(nameof(tagFile));
        }

        public static void SetCustomField(this File tagFile, string key, string value)
        {
            if (tagFile.GetTag(TagTypes.Id3v2) is Tag id3V2Tag)
            {
                var userTextInformationFrame = UserTextInformationFrame.Get(id3V2Tag, key, StringType.UTF8,
                                                                            true,
                                                                            true);
                if (string.IsNullOrEmpty(value))
                    id3V2Tag.RemoveFrame(userTextInformationFrame);
                else
                    userTextInformationFrame.Text = value.Split(';');
            }
            else if (tagFile.GetTag(TagTypes.Xiph) is XiphComment xiphComment)
            {
                xiphComment.SetField(key.ToUpperInvariant(), value);
            }
            else
            {
                throw new ArgumentException(nameof(tagFile));
            }
        }


    }
}
