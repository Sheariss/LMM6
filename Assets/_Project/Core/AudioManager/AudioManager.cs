using UnityEngine;

namespace Atlas.Core.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SFXVolume { get; private set; } = 1f;
        public float NotificationVolume { get; private set; } = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
        }

        public void SetSFXVolume(float value)
        {
            SFXVolume = Mathf.Clamp01(value);
        }

        public void SetNotificationVolume(float value)
        {
            NotificationVolume = Mathf.Clamp01(value);
        }
    }
}