#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Core.Vectors;
using Larnix.Model;
using Larnix.Model.Packets;
using Larnix.Server.Network;
using ChatCode = Larnix.Model.Packets.ChatMessage.ChatCode;

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
            Col32 color = ICmdExecutor.ResultToCol32(result);

            FixedString512[] answerParts = FixedStringUtils.Cut<FixedString512>(answer, s => new(s));
            for (int i = 0; i < answerParts.Length; i++)
            {
                bool isLast = i == answerParts.Length - 1;
                ChatCode msgCode = isLast ?
                    result == CmdResult.Clear ? ChatCode.ClearChat : ChatCode.Default :
                    ChatCode.Incomplete;

                Server.Send(nickname, new ChatMessage(
                    color: color,
                    sender: new FixedString64("<Server>"),
                    message: answerParts[i],
                    msgCode: msgCode
                ));
            }
        }
    }

    private void BroadcastMsg(string nickname, string message)
    {
        ChatMessage payload = new(
            color: Col32.White,
            sender: new FixedString64($"[{nickname}]"),
            message: new FixedString512(message),
            msgCode: ChatCode.Default
        );

        string fullMsg = payload.Message; // no fragmentation here
        if (payload.TryAppendPrefix(fullMsg, out string msgText))
        {
            Echo.Log(msgText);
        }

        Server.Broadcast(payload);
    }
}
