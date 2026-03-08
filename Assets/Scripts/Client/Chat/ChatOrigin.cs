using Larnix.Core;
using UnityEngine;
using LogType = Larnix.Core.Echo.LogType;

namespace Larnix.Client.Chat
{
    public class ChatOrigin : MonoBehaviour
    {
        [SerializeField] GameObject ChatNodePrefab;

        private ChatManager ChatManager => GlobRef.Get<ChatManager>();
        
        public ChatNode Head { get; private set; }
        public bool ForceVisible => ChatManager.IsChatOpen;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        public void AddMessage(string message, LogType logType)
        {
            ChatNode node = Instantiate(ChatNodePrefab, transform).GetComponent<ChatNode>();
            node.Initialize(message, logType, Head);
            Head = node;
        }

        public void Clear()
        {
            if (Head != null)
            {
                Head.CutOff();
                Head = null;
            }
        }
    }
}
