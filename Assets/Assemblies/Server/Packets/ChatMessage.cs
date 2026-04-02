using System;
using Larnix.Model.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;
using System.Linq;
using LogType = Larnix.Core.Echo.LogType;
using Larnix.Core.Utils;

namespace Larnix.Server.Packets;

public sealed class ChatMessage : Payload
{
    private const int SIZE = sizeof(LogType) + String64.BYTE_SIZE + String512.BYTE_SIZE;

    public LogType LogType => Primitives.FromBytes<LogType>(Bytes, 0);
    public String64 Sender => Primitives.FromBytes<String64>(Bytes, 1);
    public String512 Message => Primitives.FromBytes<String512>(Bytes, 65);
    public ChatCode MsgCode => (ChatCode)Code;

    public enum ChatCode : byte
    {
        Default = 0,
        ClearChat = 1,
        PlayerToServer = 2,
        Incomplete = 3, // client caches incomplete messages and merges them until a complete one is found
    }

    public ChatMessage(LogType logType, String64 sender, in String512 message, ChatCode msgCode = ChatCode.Default)
    {
        InitializePayload(ArrayUtils.MegaConcat(
            Primitives.GetBytes(logType),
            Primitives.GetBytes(sender),
            Primitives.GetBytes(message)
            ), (byte)msgCode);
    }

    public ChatMessage(in String512 message, ChatCode msgCode) :
        this(LogType.Raw, default, message, msgCode) { }

    public bool TryGetMsgText(out string msgText) => TryAppendPrefix(Message, out msgText);
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
