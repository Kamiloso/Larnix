using UnityEngine;
using TMPro;
using System.Text;
using Larnix.Core;
using Larnix.Packets;
using Larnix.GameCore.Utils;
using System.Collections.Generic;
using Larnix.Scoping;
using ChatCode = Larnix.Packets.ChatMessage.ChatCode;

namespace Larnix.Client.Chat
{
    public class ChatManager : MonoBehaviour
    {
        [SerializeField] GameObject InputPanel;
        [SerializeField] TMP_InputField InputField;
        
        private Client Client => GlobRef.Get<Client>();
        private ChatOrigin ChatOrigin => GlobRef.Get<ChatOrigin>();

        public bool IsChatOpen => Scopes.Matches(ScopeID.Chat);

        private Queue<string> _incompleteMsgs = new();

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
            StringBuilder sb = new();
            while (_incompleteMsgs.TryDequeue(out string part))
            {
                sb.Append(part);
            }
            return sb.ToString();
        }

        public void AddMessage(ChatMessage message)
        {
            if (message.MsgCode == ChatCode.Incomplete)
            {
                _incompleteMsgs.Enqueue(message.Message);
            }
            else
            {
                if (message.MsgCode == ChatCode.ClearChat)
                {
                    ChatOrigin.Clear();
                }

                if (message.TryGetMsgText(UncacheText(), out string msgText))
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
