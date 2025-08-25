using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Larnix.Files;
using System.Text;
using System;

namespace Larnix.Menu.Settings
{
    public class Settings : MonoBehaviour
    {
        public static Settings Instance { get; private set; } = null;
        public string SettingsPath { get; private set; } = null;

        private Dictionary<string, string> options = null;
        private static Dictionary<string, string> defaultOptions = new()
        {
            { "P2P_Server", "relay.se3.page" }
        };

        private void Awake()
        {
            Instance = this;
            SettingsPath = Path.Combine(Application.persistentDataPath, "Settings");

            options = new(defaultOptions);
            string read = FileManager.Read(SettingsPath, "settings.txt");
            if (read != null)
            {
                string[] lines = read.Replace("\r", "").Split('\n');
                for (int i = 0; i + 1 < lines.Length; i += 2)
                {
                    string key = lines[i];
                    string value = lines[i + 1];

                    options[key] = value;
                }
            }
        }

        public void SetValue(string key, string value, bool save)
        {
            if (
                (key != null && (key.Contains('\r') || key.Contains('\n'))) ||
                (value != null && (value.Contains('\r') || value.Contains('\n'))))
            {
                throw new System.ArgumentException("Characters '\\r' and '\\n' are not supported.");
            }

            if (value != null) options[key] = value;
            else
            {
                if (defaultOptions.TryGetValue(key, out string defaultValue))
                {
                    options[key] = defaultValue;
                }
                else options.Remove(key);
            }

            if (save) Save();
        }

        public string GetValue(string key)
        {
            if (options.TryGetValue(key, out var value)) return value;
            return null;
        }

        public void ResetValue(string key, bool save)
        {
            SetValue(key, null, save);
        }

        public void ResetData(bool save)
        {
            options = new(defaultOptions);
            if (save) Save();
        }

        private void Save()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var vkp in options)
            {
                sb.Append(vkp.Key + "\n");
                sb.Append(vkp.Value + "\n");
            }

            string str = sb.ToString();
            FileManager.Write(SettingsPath, "settings.txt", str);
        }
    }
}
