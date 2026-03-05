using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Larnix.Core;
using System.Linq;

namespace Larnix.Client
{
    public class Chat : MonoBehaviour
    {
        [SerializeField] int maxLineMemory = 128;
        [SerializeField] int maxVisibleLines = 17;
        [SerializeField] int maxLineLength = 50;
        [SerializeField] int lineWrapLength = 30;

        [SerializeField] GameObject textPanel;
        [SerializeField] TextMeshProUGUI textField;

        [SerializeField] GameObject inputPanel;
        [SerializeField] TMP_InputField inputField;

        private LinkedList<string> _allMessages = new();
        private bool _isChatOpen = false;
        private int _scrollIndex = 0;
        private bool _dirty = false;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void Update()
        {
            if (!_isChatOpen)
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    _isChatOpen = true;
                    inputField.text = "";
                }
                else if (Input.GetKeyDown(KeyCode.Slash))
                {
                    _isChatOpen = true;
                    inputField.text = "/";
                }
            }

            if (_isChatOpen)
            {
                if (!inputField.isFocused)
                {
                    inputField.ActivateInputField();
                    inputField.Select();
                    inputField.caretPosition = inputField.text.Length;
                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    ApplyMessage(inputField.text);
                    inputField.text = "";
                    _isChatOpen = false;
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    inputField.text = "";
                    _isChatOpen = false;
                }
            }
        }

        private void LateUpdate()
        {
            inputPanel.SetActive(_isChatOpen);

            IEnumerable<string> visibleLines = _allMessages
                .SelectMany(l => l.Split('\n'))
                .Skip(_scrollIndex)
                .Take(maxVisibleLines);

            textPanel.SetActive(visibleLines.Count() > 0);

            RectTransform textPanelRect = (RectTransform)textPanel.transform;
            textPanelRect.sizeDelta = new Vector2(
                textPanelRect.sizeDelta.x,
                visibleLines.Count() * textField.fontSize * 1.2f
            );

            if (_dirty)
            {
                textField.text = string.Join("\n", visibleLines);
                _dirty = false;
            }
        }

        public void AddMessage(string message, string sender = null)
        {
            string formattedMessage;

            if (sender == null)
            {
                formattedMessage = message;
            }
            else if (sender.Contains("@"))
            {
                sender = sender.Remove(sender.IndexOf("@"), 1);
                formattedMessage = $"<{sender}> {message}";
            }
            else
            {
                formattedMessage = $"[{sender}] {message}";
            }

            _allMessages.AddLast(formattedMessage);
            if (_allMessages.Count > maxLineMemory)
            {
                _allMessages.RemoveFirst();
            }

            _dirty = true;
        }

        public void ApplyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            // TODO: implement chat
            AddMessage(message, "@You");
        }

        public void ClearChat()
        {
            _allMessages.Clear();
            _dirty = true;
        }
    }
}
