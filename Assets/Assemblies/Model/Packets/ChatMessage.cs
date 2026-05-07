#nullable enable
using System;
using System.Linq;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;
using Larnix.Core.Vectors;

namespace Larnix.Model.Packets;

[CmdId(2)]
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
        Incomplete = 3,
    }

    public ChatMessage(Col32 color, ChatCode msgCode, in FixedString64 sender, in FixedString512 message)
    {
        Color = Sanitizer.Filter(color);
        MsgCode = Sanitizer.Filter(msgCode);
        Sender = Sanitizer.Filter(sender);
        Message = Sanitizer.Filter(message);
    }

    public static ChatMessage CreateRaw(ChatCode msgCode, in FixedString512 message)
    {
        return new ChatMessage(Col32.White, msgCode, new FixedString64(), message);
    }

    public bool TryAppendPrefix(string raw, out string msgText)
    {
        if (MsgCode == ChatCode.ClearChat ||
            MsgCode == ChatCode.PlayerToServer)
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
