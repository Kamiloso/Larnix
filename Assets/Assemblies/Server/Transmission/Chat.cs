#nullable enable
using Larnix.Core;
using Larnix.Model;
using Larnix.Model.Utils;
using Larnix.Server.Commands;
using Larnix.Server.Packets;
using Larnix.Socket.Backend;
using LogType = Larnix.Core.Echo.LogType;
using ChatCode = Larnix.Server.Packets.ChatMessage.ChatCode;

namespace Larnix.Server.Transmission;

internal class Chat
{
    private QuickServer QuickServer => GlobRef.Get<QuickServer>();
    private CmdManager CmdManager => GlobRef.Get<CmdManager>();

    public void OnArrive(string nickname, string message)
    {
        if (message.StartsWith("/"))
        {
            string command = message[1..];
            ExecuteCommand(nickname, command);
        }
        else
        {
            BroadcastMsg(nickname, message);
        }
    }

    private void ExecuteCommand(string nickname, string command)
    {
        var (result, answer) = CmdManager.ExecuteCommand(command, nickname);

        if (result != CmdResult.Ignore)
        {
            LogType logType = ICmdExecutor.ConvertToLogType(result);

            String512[] answerParts = IStringStruct.Cut<String512>(answer, s => new(s));
            for (int i = 0; i < answerParts.Length; i++)
            {
                bool isLast = i == answerParts.Length - 1;
                ChatCode msgCode = isLast ?
                    (result == CmdResult.Clear ? ChatCode.ClearChat : ChatCode.Default) :
                    ChatCode.Incomplete;

                QuickServer.Send(nickname, new ChatMessage(
                    logType: logType,
                    sender: (String64)"<Server>",
                    message: answerParts[i],
                    msgCode: msgCode
                ));
            }
        }
    }

    private void BroadcastMsg(string nickname, string message)
    {
        ChatMessage packet = new ChatMessage(
                logType: LogType.Log,
                sender: (String64)$"[{nickname}]",
                message: (String512)message
            );

        if (packet.TryGetMsgText(out string msgText))
        {
            Echo.Log(msgText); // log to console
        }

        QuickServer.Broadcast(packet);
    }
}
