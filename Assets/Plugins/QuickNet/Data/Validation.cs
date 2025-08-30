using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickNet.Data
{
    public static class Validation
    {
        public static bool IsGoodNickname(string nickname) =>
            nickname != null &&
            !nickname.EndsWith('\0') &&
            nickname.Length is >= 3 and <= 16 &&
            nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

        public static bool IsGoodPassword(string password) =>
            password != null &&
            !password.EndsWith('\0') &&
            password.Length is >= 7 and <= 32;

        public static bool IsGoodMessage(string message) =>
            message != null &&
            !message.EndsWith('\0') &&
            message.Length <= 256;

        public static bool IsGoodUserText(string message) =>
            message != null &&
            !message.EndsWith('\0') &&
            message.Length <= 128;
    }
}
