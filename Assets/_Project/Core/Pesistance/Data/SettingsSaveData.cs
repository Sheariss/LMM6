using System;
using System.Collections.Generic;

namespace Atlas.Core.Settings
{
    [Serializable]
    public sealed class SettingsSaveData
    {
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public SettingControlType ControlType;

            public bool ToggleValue;
            public float SliderValue;
            public string StringValue;

            public Entry Clone()
            {
                return (Entry)MemberwiseClone();
            }
        }

        public List<Entry> Values = new();

        public Entry Get(string id)
        {
            if (Values == null)
                return null;

            foreach (Entry entry in Values)
            {
                if (entry != null &&
                    string.Equals(entry.Id, id, StringComparison.Ordinal))
                {
                    return entry.Clone();
                }
            }

            return null;
        }

        public void Set(Entry value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Id))
                throw new ArgumentException("A setting value requires an ID.");

            Values ??= new List<Entry>();

            // Replace any existing entries for this ID.
            Values.RemoveAll(entry =>
                entry != null &&
                string.Equals(entry.Id, value.Id, StringComparison.Ordinal));

            Values.Add(value.Clone());
        }

        public SettingsSaveData Clone()
        {
            SettingsSaveData copy = new();

            if (Values == null)
                return copy;

            foreach (Entry entry in Values)
            {
                if (entry != null)
                    copy.Values.Add(entry.Clone());
            }

            return copy;
        }
    }
}