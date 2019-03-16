using System;
using ServiceStack.DataAnnotations;

namespace SoundWords.Models
{
    class DbRecordingSpeaker
    {
        [References(typeof(Recording))]
        public long RecordingId { get; set; }
        [References(typeof(Speaker))]
        public long SpeakerId { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }
    }
}
