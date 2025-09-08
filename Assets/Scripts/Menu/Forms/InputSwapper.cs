using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Larnix.Forms
{
    public class InputSwapper : MonoBehaviour
    {
        [SerializeField] List<TMP_InputField> InputFields;
        public int State = 0;

        private void Start()
        {
            UpdateView();
        }

        public void SetState(int state)
        {
            if (state >= 0 && state < InputFields.Count)
            {
                State = state;
                UpdateView();
            }
            else throw new System.IndexOutOfRangeException();
        }

        public void IncrementState()
        {
            State++;
            if (State == InputFields.Count)
                State = 0;

            UpdateView();
        }

        private void UpdateView()
        {
            for (int i = 0; i < InputFields.Count; i++)
            {
                TMP_InputField inputField = InputFields[i];
                if (inputField != null)
                    inputField.gameObject.SetActive(i == State);
            }
        }
    }
}
