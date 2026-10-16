using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    [Serializable]
    public sealed class AssistantTopic
    {
        public string buttonLabel;

        [TextArea(1, 3)]
        public string question;

        public string[] keywords;

        [TextArea(3, 8)]
        public string response;
    }

    public sealed class AssistantEngine
    {
        private readonly List<AssistantTopic> topics = new();
        private readonly string fallbackResponse;

        public IReadOnlyList<AssistantTopic> Topics => topics;

        public AssistantEngine(
            IEnumerable<AssistantTopic> authoredTopics,
            string fallbackResponse)
        {
            this.fallbackResponse = fallbackResponse;

            if (authoredTopics == null)
                return;

            foreach (AssistantTopic topic in authoredTopics)
            {
                if (topic != null &&
                    !string.IsNullOrWhiteSpace(topic.question) &&
                    !string.IsNullOrWhiteSpace(topic.response))
                {
                    topics.Add(topic);
                }
            }
        }

        public string Respond(
            string message,
            AssistantTopic selectedTopic = null)
        {
            // Preset questions identify their topic directly.
            if (selectedTopic != null && topics.Contains(selectedTopic))
                return selectedTopic.response;

            string text = message?.Trim() ?? string.Empty;

            // Prefer an exact authored question before keyword matching.
            foreach (AssistantTopic topic in topics)
            {
                if (string.Equals(
                    text,
                    topic.question.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return topic.response;
                }
            }

            foreach (AssistantTopic topic in topics)
            {
                if (topic.keywords == null)
                    continue;

                foreach (string keyword in topic.keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword))
                        continue;

                    // Match whole words or phrases.
                    string pattern =
                        @"(?<!\w)" +
                        Regex.Escape(keyword.Trim()) +
                        @"(?!\w)";

                    if (Regex.IsMatch(
                        text,
                        pattern,
                        RegexOptions.IgnoreCase |
                        RegexOptions.CultureInvariant))
                    {
                        return topic.response;
                    }
                }
            }

            return fallbackResponse;
        }
    }
}