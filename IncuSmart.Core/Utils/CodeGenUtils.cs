using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Utils
{
    public static class CodeGenUtils
    {
        private static readonly Random _random = Random.Shared;

        /// <summary>Sinh mã thuần số.</summary>
        /// <example>GenerateNumeric(6) → "482910"</example>
        public static string GenerateNumeric(int length = 6)
        {
            return string.Concat(Enumerable.Range(0, length)
                                           .Select(_ => _random.Next(0, 10).ToString()));
        }

        /// <summary>Sinh mã chữ hoa + số ngẫu nhiên.</summary>
        /// <example>GenerateAlphanumeric(6) → "A3F9KL"</example>
        public static string GenerateAlphanumeric(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return string.Concat(Enumerable.Range(0, length)
                                           .Select(_ => chars[_random.Next(chars.Length)]));
        }

        /// <summary>
        /// Sinh mã theo format cố định.
        /// '#' = số ngẫu nhiên, '@' = chữ hoa ngẫu nhiên, ký tự khác giữ nguyên.
        /// </summary>
        /// <example>GenerateByFormat("@@-####") → "KF-8392"</example>
        public static string GenerateByFormat(string format)
        {
            const string digits = "0123456789";
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            return string.Concat(format.Select(c => c switch
            {
                '#' => digits[_random.Next(digits.Length)].ToString(),
                '@' => letters[_random.Next(letters.Length)].ToString(),
                _ => c.ToString()
            }));
        }
    }

}
