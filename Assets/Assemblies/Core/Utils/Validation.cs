using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Core.Utils
{
    public static class Validation
    {
        public static string WrongNicknameInfo => "Nickname should be 3-16 characters and only use: letters, digits, _ or -.";
        public static string WrongPasswordInfo => "Password should be 7-32 characters and not end with NULL (0x00).";

        public static bool IsGoodNickname(string nickname) =>
            nickname != null &&
            !nickname.EndsWith('\0') &&
            nickname.Length is >= 3 and <= 16 &&
            nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

        public static bool IsGoodPassword(string password) =>
            password != null &&
            !password.EndsWith('\0') &&
            password.Length is >= 7 and <= 32;

        public static bool IsGoodText<T>(string message) where T : IStringStruct, new() =>
            message != null &&
            !message.EndsWith('\0') &&
            message.Length <= new T().BinarySize / 2;
    }
}
