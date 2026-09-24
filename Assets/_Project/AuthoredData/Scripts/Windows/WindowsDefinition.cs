using UnityEngine;

namespace Atlas.Presentation.SOS.Windows
{
    [CreateAssetMenu(
        fileName = "WindowDefinition",
        menuName = "ATLAS/SOS/Window Definition")]
    public sealed class WindowDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string windowTitle;
        [SerializeField] private Texture2D windowIcon;

        [Header("Initial Size")]
        [SerializeField] private Vector2 initialSize = new(900f, 600f);

        [Header("Minimum Size")]
        [SerializeField] private Vector2 minimumSize = new(400f, 300f);

        [Header("Capabilities")]
        [SerializeField] private bool canMinimize = true;
        [SerializeField] private bool canMaximize = true;
        [SerializeField] private bool canResize = true;
        [SerializeField] private bool canClose = true;

        public string WindowTitle => windowTitle;
        public Texture2D WindowIcon => windowIcon;

        public Vector2 InitialSize => initialSize;
        public Vector2 MinimumSize => minimumSize;

        public bool CanMinimize => canMinimize;
        public bool CanMaximize => canMaximize;
        public bool CanResize => canResize;
        public bool CanClose => canClose;
    }
}