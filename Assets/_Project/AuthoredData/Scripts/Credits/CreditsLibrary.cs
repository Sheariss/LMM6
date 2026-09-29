using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.Credits
{
    [CreateAssetMenu(
        fileName = "CreditsLibrary",
        menuName = "ATLAS/Credits/Credits Library"
    )]
    public sealed class CreditsLibrary : ScriptableObject
    {
        [Header("Hero")]
        [SerializeField] private string projectLabel = "A TEAM ATLAS PROJECT";
        [SerializeField] private string projectTitle = "INTERACTIVE";
        [SerializeField] private string projectSubtitle = "INVESTIGATION SIMULATOR";
        [SerializeField] private string institution = "Polytechnic University of Puerto Rico";
        [SerializeField] private string projectDescription = "Computer Science Capstone Project";

        [Header("Credits")]
        [SerializeField] private List<CreditsSection> sections = new();

        [Header("Ending")]
        [SerializeField] private string endingTitle = "THANK YOU FOR INVESTIGATING";
        [SerializeField] private string endingSubtitle = "THE TRUTH IS IN THE DETAILS.";
        [SerializeField] private string endingMeta = "TEAM ATLAS // 2026";

        public string ProjectLabel => projectLabel;
        public string ProjectTitle => projectTitle;
        public string ProjectSubtitle => projectSubtitle;
        public string Institution => institution;
        public string ProjectDescription => projectDescription;

        public IReadOnlyList<CreditsSection> Sections => sections;

        public string EndingTitle => endingTitle;
        public string EndingSubtitle => endingSubtitle;
        public string EndingMeta => endingMeta;
    }

    [Serializable]
    public sealed class CreditsSection
    {
        [SerializeField] private string title;
        [SerializeField] private List<CreditsEntry> entries = new();

        public string Title => title;
        public IReadOnlyList<CreditsEntry> Entries => entries;
    }

    [Serializable]
    public sealed class CreditsEntry
    {
        [SerializeField] private string name;
        [SerializeField] private string role;
        [SerializeField] private CreditsEntryStyle style = CreditsEntryStyle.Standard;

        public string Name => name;
        public string Role => role;
        public CreditsEntryStyle Style => style;
    }

    public enum CreditsEntryStyle
    {
        Standard,
        Featured,
        Joke,
        SpecialThanks
    }
}