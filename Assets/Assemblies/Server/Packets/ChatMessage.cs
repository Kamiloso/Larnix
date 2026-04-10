using System;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Core.Utils;
using LogType = Larnix.Core.Echo.LogType;
using Larnix.Core.Serialization;

namespace Larnix.Server.Packets;

public sealed class ChatMessage : Payload_Legacy
{
    private static int SIZE => sizeof(LogType) + Binary<FixedString64>.Size + Binary<FixedString512>.Size;

    public LogType LogType => Binary<LogType>.Deserialize(Bytes, 0);
    public FixedString64 Sender => Binary<FixedString64>.Deserialize(Bytes, 1);
    public FixedString512 Message => Binary<FixedString512>.Deserialize(Bytes, 67);
    public ChatCode MsgCode => (ChatCode)Code;

    public enum ChatCode : byte
    {
        Default = 0,
        ClearChat = 1,
        PlayerToServer = 2,
        Incomplete = 3, // client caches incomplete messages and merges them until a complete one is found
    }

    public ChatMessage(LogType logType, FixedString64 sender, in FixedString512 message, ChatCode msgCode = ChatCode.Default)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Binary<LogType>.Serialize(logType),
            Binary<FixedString64>.Serialize(sender),
            Binary<FixedString512>.Serialize(message)
            ), (byte)msgCode);
    }

    public ChatMessage(in FixedString512 message, ChatCode msgCode) :
        this(LogType.Raw, default, message, msgCode) { }

    public bool TryAppendPrefix(string raw, out string msgText)
    {
        if (MsgCode == ChatCode.ClearChat || MsgCode == ChatCode.PlayerToServer)
        {
            msgText = default;
            return false;
        }

        if (LogType == LogType.Raw)
        {
            msgText = raw;
            return true;
        }

        string sender = Sender;

        msgText = sender.All(char.IsWhiteSpace) ?
            $"{raw}" :
            $"{sender} {raw}";

        return true;
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Enum.IsDefined(typeof(ChatCode), MsgCode);
    }
}
