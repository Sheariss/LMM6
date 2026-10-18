using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.StickyNotes
{
    [Serializable]
    public sealed class StickyNoteSeed
    {
        [Tooltip("Optional stable note ID. Leave blank to generate one.")]
        public string id;

        [Tooltip("Blank applies to every user. Otherwise use an exact UserId.")]
        public string userId;

        [TextArea(3, 12)]
        public string text;

        public StickyNoteColor color;

        [Tooltip("Optional authored time/date label.")]
        public string stamp;

        public List<StickyNoteSpan> spans = new();
    }

    [CreateAssetMenu(
        fileName = "StickyNotesLibrary",
        menuName = "ATLAS/SOS/Sticky Notes/Library")]
    public sealed class StickyNotesLibrary : ScriptableObject
    {
        [SerializeField]
        private List<StickyNoteSeed> notes = new();

        public IReadOnlyList<StickyNoteSeed> Notes => notes;
    }
}