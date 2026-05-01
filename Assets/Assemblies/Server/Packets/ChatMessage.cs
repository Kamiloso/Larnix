#nullable enable
using System;
using System.Linq;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;
using Larnix.Core.Vectors;

namespace Larnix.Server.Packets;

[CmdId(0x02)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct ChatMessage : ISanitizable<ChatMessage>
{
    public Col32 Color { get; }
    public ChatCode MsgCode { get; }
    public FixedString64 Sender { get; }
    public FixedString512 Message { get; }

    private readonly byte _padding = 0xFF; // avoids end-compression

    public enum ChatCode : byte
    {
        Default = 0,
        ClearChat = 1,
        PlayerToServer = 2,
        Incomplete = 3, // client caches and merges split messages
    }

    public ChatMessage(Col32 color, ChatCode msgCode, in FixedString64 sender, in FixedString512 message)
    {
        Color = color;
        MsgCode = Sanitizer_Legacy.SanitizeEnum(msgCode);
        Sender = sender;
        Message = message;
    }

    public ChatMessage(in FixedString512 message, ChatCode msgCode)
    {
        Color = Col32.White;
        Sender = new FixedString64();
        Message = message;
        MsgCode = Enum.IsDefined(typeof(ChatCode), msgCode) ? msgCode : ChatCode.Default;
    }

    public bool TryAppendPrefix(string raw, out string msgText)
    {
        if (MsgCode == ChatCode.ClearChat || MsgCode == ChatCode.PlayerToServer)
        {
            msgText = null!;
            return false;
        }

        string sender = Sender;

        msgText = sender.All(char.IsWhiteSpace) ?
            $"{raw}" :
            $"{sender} {raw}";

        return true;
    }

    public ChatMessage Sanitize()
    {
        return new ChatMessage(Color, MsgCode, Sender, Message);
    }
}
