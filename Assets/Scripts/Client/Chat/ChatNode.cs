using System;
using TMPro;
using UnityEngine;
using Larnix.Core;
using LogType = Larnix.Core.Debug.LogType;

namespace Larnix.Client.Chat
{
    public class ChatNode : MonoBehaviour
    {
        private const int MSG_LIMIT = 128;
        private const float GAP_SIZE = 5f;
        private const float DISPLAY_TIME = 6f; // seconds
        private const float FADE_TIME = 0.75f; // seconds

        [SerializeField] CanvasGroup CanvasGroup;
        [SerializeField] RectTransform Panel;
        [SerializeField] TMP_Text TextField;
        [SerializeField] float DisplayMoment;

        private ChatOrigin ChatOrigin => GlobRef.Get<ChatOrigin>();

        private float BaseHeight { get; set; }
        private ChatNode Next { get; set; }

        private bool _initialized = false;

        private static Color GetColorByLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Log or LogType.Raw => Color.white,
                LogType.Info => Color.cyan,
                LogType.Warning => Color.yellow,
                LogType.Error => Color.red,
                LogType.Success => Color.green,
                _ => Color.white
            };
        }

        private void Awake()
        {
            BaseHeight = Panel.rect.height;
        }
        
        public void Initialize(string message, LogType logType, ChatNode next)
        {
            if (_initialized)
                throw new InvalidOperationException($"{nameof(ChatNode)} is already initialized.");

            transform.localPosition = Vector3.zero;
            TextField.text = message;
            TextField.color = GetColorByLogType(logType);
            Next = next;
            DisplayMoment = 0f;

            Propagate();

            _initialized = true;
        }

        private void LateUpdate()
        {
            float fadeStart = DISPLAY_TIME;
            float fadeEnd = DISPLAY_TIME + FADE_TIME;
            float clampedTime = Mathf.Clamp(DisplayMoment, fadeStart, fadeEnd);

            CanvasGroup.alpha = ChatOrigin.ForceVisible ?
                1f : 1f - (clampedTime - fadeStart) / FADE_TIME;

            DisplayMoment += Time.deltaTime;
        }

        private void Propagate(int depth = 0, float posY = 0f)
        {
            if (depth > MSG_LIMIT)
            {
                CutOff();
                return;
            }

            UpdateTransform(posY);

            if (Next != null)
            {
                Next.Propagate(
                    depth: depth + 1,
                    posY: posY + Panel.rect.height + GAP_SIZE
                    );
            }
        }

        private void UpdateTransform(float posY)
        {
            TextField.ForceMeshUpdate();

            int lineCount = TextField.textInfo.lineCount;
            float lineHeight = TextField.textInfo.lineInfo[0].lineHeight;

            Panel.sizeDelta = new Vector2(Panel.rect.width, BaseHeight + (lineCount - 1) * lineHeight);
            Panel.localPosition = new Vector3(0f, posY, 0f);
        }

        public void CutOff()
        {
            Destroy(gameObject);

            if (Next != null)
            {
                Next.CutOff();
                Next = null;
            }
        }
    }
}
