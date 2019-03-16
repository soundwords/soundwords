using System;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace SoundWords.Models
{
    public class DbEntity : IHasId<long>
    {
        [AutoIncrement]
        public long Id { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public DateTime ModifiedOn { get; set; }

        public bool Deleted { get; set; }
    }
}
