using System.Security.Cryptography;

namespace IncuSmart.Core.Utils
{
    public static class OtpUtil
    {
        private const string AlphanumericUpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string Generate6DigitOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        public static string Generate8CharOtp()
        {
            var chars = new char[8];
            for (int i = 0; i < 8; i++)
                chars[i] = AlphanumericUpperChars[RandomNumberGenerator.GetInt32(AlphanumericUpperChars.Length)];
            return new string(chars);
        }
    }
}
