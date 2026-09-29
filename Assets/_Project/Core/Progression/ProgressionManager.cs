using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Core.Progression
{
    public sealed class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }
        public event Action<string> MilestoneUnlocked;

        private readonly HashSet<string> unlockedMilestones = new();

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

        public bool IsUnlocked(string milestone)
        {
            return unlockedMilestones.Contains(milestone);
        }

        public void Unlock(string milestone)
        {
            if (string.IsNullOrWhiteSpace(milestone))
                return;

            if (!unlockedMilestones.Add(milestone))
                return;

            MilestoneUnlocked?.Invoke(milestone);
        }

        public void ResetProgression()
        {
            unlockedMilestones.Clear();
        }
    }
}