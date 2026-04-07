#nullable enable
using Larnix.Core;
using Larnix.Model.Utils;
using Larnix.Server.Packets;
using LogType = Larnix.Core.Echo.LogType;
using ChatCode = Larnix.Server.Packets.ChatMessage.ChatCode;
using Larnix.Model;

namespace Larnix.Server.Commands;

internal interface IChat
{
    void OnArrive(string nickname, string message);
}

internal class Chat : IChat
{
    private IServer Server => GlobRef.Get<IServer>();
    private ICmdManager CmdManager => GlobRef.Get<ICmdManager>();

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
                    result == CmdResult.Clear ? ChatCode.ClearChat : ChatCode.Default :
                    ChatCode.Incomplete;

                Server.Send(nickname, new ChatMessage(
                    logType: logType,
                    sender: new String64("<Server>"),
                    message: answerParts[i],
                    msgCode: msgCode
                ));
            }
        }
    }

    private void BroadcastMsg(string nickname, string message)
    {
        var packet = new ChatMessage(
                logType: LogType.Log,
                sender: new String64($"[{nickname}]"),
                message: new String512(message)
            );

        string fullMsg = packet.Message; // no fragmentation here
        if (packet.TryAppendPrefix(fullMsg, out string msgText))
        {
            Echo.Log(msgText);
        }

        Server.Broadcast(packet);
    }
}
