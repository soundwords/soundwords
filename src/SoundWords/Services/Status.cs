using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoundWords.Services
{
    class Status
    {
        public string Id { get; private set; }
        public string Text { get; private set; }

        public Status(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }

}
