using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.Calendar
{
    public enum CalendarCategory
    {
        Personal,
        Work,
        Reminders
    }

    [Serializable]
    public sealed class CalendarEventDefinition
    {
        public string id;
        public string title;

        public CalendarCategory calendar;

        [Tooltip("Scenario-local time: yyyy-MM-dd HH:mm")]
        public string startLocal;

        [Tooltip(
            "Exclusive end: yyyy-MM-dd HH:mm. " +
            "An all-day event ending on October 17 covers October 16.")]
        public string endLocal;

        public bool allDay;
        public string location;

        [TextArea(3, 10)]
        public string description;
    }

    [CreateAssetMenu(
        fileName = "Calendar Library",
        menuName = "ATLAS/Calendar Library")]
    public sealed class CalendarLibrary : ScriptableObject
    {
        [Tooltip("Explicit scenario-local time: yyyy-MM-dd HH:mm")]
        [SerializeField]
        private string scenarioLocalNow = "2026-10-15 09:00";

        [Tooltip(
            "Display label only. Event times are already scenario-local; " +
            "the Calendar does not perform timezone conversion.")]
        [SerializeField]
        private string timezoneLabel = "Scenario time · GMT−04:00";

        [SerializeField]
        private List<CalendarEventDefinition> events = new();

        public string ScenarioLocalNow => scenarioLocalNow;
        public string TimezoneLabel => timezoneLabel;

        public IReadOnlyList<CalendarEventDefinition> Events => events;
    }
}