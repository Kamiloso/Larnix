using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Core.Utils
{
    public static class Validation
    {
        public static string WrongNicknameInfo => "Nickname should be 3-16 characters and only use: letters, digits, _ or -.";
        public static string WrongPasswordInfo => "Password should be 7-32 characters and not use white spaces or end with NULL (0x00).";
        public static string WrongWorldNameInfo => "World name should be 1-32 characters, be already trimmed and only use: letters, digits, space, _ or -.";

        private static IEnumerable<string> _denyWorldNames = new HashSet<string>
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
            "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
        };

        public static bool IsGoodNickname(string nickname) =>
            nickname != null &&
            !nickname.EndsWith('\0') &&
            nickname.Length is >= 3 and <= 16 &&
            nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

        public static bool IsGoodPassword(string password) =>
            password != null &&
            !password.EndsWith('\0') &&
            password.Length is >= 7 and <= 32 &&
            !password.Any(char.IsWhiteSpace);

        public static bool IsValidWorldName(string worldName) =>
            worldName != null &&
            !worldName.Contains('\0') &&
            worldName.Length is >= 1 and <= 32 &&
            worldName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ') &&
            worldName == worldName.Trim() &&
            !_denyWorldNames.Contains(worldName.ToUpperInvariant());

        public static bool IsGoodText<T>(string message) where T : IStringStruct, new() =>
            message != null &&
            !message.EndsWith('\0') &&
            message.Length <= new T().BinarySize / 2;
    }
}
