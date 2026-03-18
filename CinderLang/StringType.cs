using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang
{
    public enum StringType
    {
        word,
        character,
    }

    public static class StringTypeExtensions
    {
        public static bool IsString(char type)
        {
            return type switch
            {
                '"' => true,
                '\'' => true,
                _ => false
            };
        }

        public static StringType GetStringType(char type)
        {
            return type switch
            {
                '"' => StringType.word,
                '\'' => StringType.character,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Not expected char value: {type}"),
            };
        }

        public static char GetString(this StringType type)
        {
            return type switch
            {
                StringType.word => '"',
                StringType.character => '\'',
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Not expected StringType value: {type}"),
            };
        }
    }
}
