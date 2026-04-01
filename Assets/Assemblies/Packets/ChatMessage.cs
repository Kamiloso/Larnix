using System;
using Larnix.GameCore.Utils;
using Larnix.Core.Binary;
using Larnix.Socket.Packets;
using System.Linq;
using Larnix.Core.Misc;
using LogType = Larnix.Core.Echo.LogType;

namespace Larnix.Packets;

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

    public bool TryGetMsgText(out string msgText) => TryGetMsgText(string.Empty, out msgText);
    public bool TryGetMsgText(string uncached, out string msgText)
    {
        if (MsgCode == ChatCode.ClearChat || MsgCode == ChatCode.PlayerToServer)
        {
            msgText = default;
            return false;
        }

        string fullMsg = string.IsNullOrEmpty(uncached) ?
            Message : uncached + Message;

        if (LogType == LogType.Raw)
        {
            msgText = fullMsg;
            return true;
        }

        string sender = Sender;

        msgText = sender.All(char.IsWhiteSpace) ?
            $"{fullMsg}" :
            $"{sender} {fullMsg}";

        return true;
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE &&
            Enum.IsDefined(typeof(ChatCode), MsgCode);
    }
}
