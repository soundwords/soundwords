using System;
using System.Linq;
using SoundWords.Models;

namespace SoundWords.Tools
{
    public static class NameExtensions
    {
        public static NameInfo ToNameInfo(this string input)
        {
            var name = new NameInfo();
            var nameParts = input.Split(", ");
            if (nameParts.Length > 1)
            {
                name.LastName = nameParts[0].Trim();
                name.FirstName = string.Join(" ", nameParts.Skip(1)).Trim();
            }
            else
            {
                nameParts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                name.FirstName = string.Join(" ", nameParts.Take(nameParts.Length - 1));
                name.LastName = nameParts.Last().Trim();
            }

            return name;
        }
    }
}
