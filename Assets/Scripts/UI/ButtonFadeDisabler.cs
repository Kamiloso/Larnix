using System;
using UnityEngine.UI;

namespace Larnix.UI
{
    public class ButtonFadeDisabler : IDisposable
    {
        private readonly Button button;
        private readonly float fadeDuration;

        public ButtonFadeDisabler(Button button)
        {
            this.button = button;

            var cb = button.colors;
            fadeDuration = cb.fadeDuration;
            cb.fadeDuration = 0f;
            button.colors = cb;
        }

        public void Dispose()
        {
            var cb = button.colors;
            cb.fadeDuration = fadeDuration;
            button.colors = cb;
        }
    }
}
