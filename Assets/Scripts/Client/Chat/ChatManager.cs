using UnityEngine;
using TMPro;
using System.Text;
using Larnix.Core;
using Larnix.Server.Packets;
using Larnix.Model.Utils;
using System.Collections.Generic;
using Larnix.Scoping;
using ChatCode = Larnix.Server.Packets.ChatMessage.ChatCode;
using Larnix.Core.Binary;

namespace Larnix.Client.Chat
{
    public class ChatManager : MonoBehaviour
    {
        [SerializeField] GameObject InputPanel;
        [SerializeField] TMP_InputField InputField;
        
        private Client Client => GlobRef.Get<Client>();
        private ChatOrigin ChatOrigin => GlobRef.Get<ChatOrigin>();

        public bool IsChatOpen => Scopes.Matches(ScopeID.Chat);

        private readonly Queue<String512> _incompleteMsgs = new();

        private void Awake()
        {
            GlobRef.Set(this);
            InputField.onFocusSelectAll = false;
        }

        private void CheckEnable()
        {
            bool t = MyInput.GetKeyDown(KeyCode.T, ScopeID.All);
            bool slash = MyInput.GetKeyDown(KeyCode.Slash, ScopeID.All);

            if (Scopes.EnterScopeWhen(ScopeID.Chat, () => t || slash))
            {
                InputField.text = slash ? "/" : "";
            }
        }

        private void CheckDisable()
        {
            bool enter = MyInput.GetKeyDown(KeyCode.Return, ScopeID.All);
            bool esc = MyInput.GetKeyDown(KeyCode.Escape, ScopeID.All);

            if (Scopes.LeaveScopeWhen(ScopeID.Chat, () => enter || esc))
            {
                if (enter) ApplyMessage(InputField.text);
                InputField.text = "";
            }
        }

        private void Update()
        {
            CheckEnable();

            if (IsChatOpen)
            {
                if (!InputField.isFocused)
                {
                    InputField.ActivateInputField();
                    InputField.MoveTextEnd(false);
                }

                CheckDisable();
            }
        }

        private void LateUpdate()
        {
            InputPanel.SetActive(IsChatOpen);
        }

        private string UncacheText()
        {
            List<byte> byteList = new();
            while (_incompleteMsgs.TryDequeue(out String512 part))
            {
                byte[] bytes = Primitives.GetBytes(part);
                byteList.AddRange(bytes);
            }
            return Encoding.Unicode.GetString(byteList.ToArray()).TrimEnd('\0');
        }

        public void AddMessage(ChatMessage message)
        {
            _incompleteMsgs.Enqueue(message.Message);

            if (message.MsgCode != ChatCode.Incomplete)
            {
                if (message.MsgCode == ChatCode.ClearChat)
                {
                    ChatOrigin.Clear();
                }

                string fullMsg = UncacheText();

                if (message.TryAppendPrefix(fullMsg, out string msgText))
                {
                    ChatOrigin.AddMessage(msgText, message.LogType);
                }
            }
        }

        public void ApplyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return; // Don't trim! This condition is ok.

            var msg = new ChatMessage((String512)message, ChatCode.PlayerToServer);
            Client.Send(msg);
        }
    }
}
