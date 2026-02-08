using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using System.Reflection;

namespace Larnix.Menu.Settings
{
    public class SettingsInput : MonoBehaviour
    {
        public static SettingsInput Instance { get; private set; } = null;
        private HashSet<string> listenersAdded = new();

        [SerializeField] TMP_InputField P2P_Server;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ReloadValues();
        }

        private void ReloadValues()
        {
            InitializeType<TMP_InputField>(
                (ifield, key) =>
                {
                    ifield.text = Settings.Instance.GetValue(key);

                    if (!listenersAdded.Contains(key))
                    {
                        ifield.onEndEdit.AddListener((string text) =>
                        {
                            Settings.Instance.SetValue(key, text, true);
                        });
                        listenersAdded.Add(key);
                    }
                });
        }
        
        public void ResetByKey(string key)
        {
            Settings.Instance.ResetValue(key, true);
            ReloadValues();
        }

        private void InitializeType<T>(Action<T, string> Initialize) where T : class
        {
            foreach (var kvp in GetReferencesOfType<T>(this))
            {
                string key = kvp.Key;
                T ifield = kvp.Value;

                Initialize(ifield, key);
            }
        }

        private static Dictionary<string, T> GetReferencesOfType<T>(SettingsInput settingsInput) where T : class
        {
            Type type = settingsInput.GetType();
            Dictionary<string, T> dict = new();

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(settingsInput);
                if (value is T tValue)
                    dict[field.Name] = tValue;
            }

            return dict;
        }
    }
}
