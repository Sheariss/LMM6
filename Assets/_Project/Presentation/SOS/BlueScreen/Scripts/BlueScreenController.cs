using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.BlueScreen
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BlueScreenController : MonoBehaviour
    {
        [Header("Default crash")]
        [SerializeField, TextArea(3, 5)]
        private string message = BlueScreenEngine.DefaultMessage;

        [SerializeField]
        private string stopCode = BlueScreenEngine.DefaultStopCode;

        [SerializeField, TextArea(2, 4)]
        private string qrPayload = "https://www.windows.com/stopcode";

        [Header("Timing")]
        [SerializeField, Min(0.1f)]
        private float durationSeconds = 8f;

        [SerializeField, Min(0f)]
        private float completionHoldSeconds = 0.75f;

        [Header("Events")]
        [SerializeField]
        private UnityEvent onRestartRequested = new UnityEvent();

        public event Action RestartRequested;

        public bool IsRunning => engine.IsRunning;

        private readonly BlueScreenEngine engine = new BlueScreenEngine();
        private readonly BlueScreenViewBuilder viewBuilder = new BlueScreenViewBuilder();
        private readonly QRCodeEngine qrEngine = new QRCodeEngine();

        private UIDocument uiDocument;
        private BlueScreenView view;
        private Texture2D qrImage;
        private bool hasStarted;

        private void Start()
        {
            hasStarted = true;
            InitializeView();
        }

        private void OnEnable()
        {
            // UIDocument may recreate its visual tree after re-enabling.
            if (hasStarted)
                InitializeView();
        }

        private void OnDisable()
        {
            Cancel();
            view = null;
        }

        private bool InitializeView()
        {
            if (view != null)
                return true;

            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();

            var binder = new BlueScreenBinder(
                uiDocument.rootVisualElement);

            if (!binder.IsValid)
                return false;

            view = new BlueScreenView(binder);
            Refresh();
            return true;
        }

        [ContextMenu("Play Blue Screen")]
        public void Play()
        {
            Play(
                message,
                stopCode,
                qrPayload,
                durationSeconds,
                completionHoldSeconds);
        }

        public void Play(
            string errorMessage,
            string errorCode,
            string payload,
            float duration = 8f,
            float completionHold = 0.75f)
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            BlueScreenEngine.ValidateTiming(duration, completionHold);

            if (!InitializeView())
                return;

            Texture2D nextImage = null;

            if (!string.IsNullOrWhiteSpace(payload))
            {
                try
                {
                   nextImage = qrEngine.Generate(payload);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[BlueScreenController] QR generation failed. " +
                        $"Continuing without a QR image.\n{exception}",
                        this);
                }
            }

            // Detach the previous texture before destroying it.
            Cancel();

            qrImage = nextImage;

            engine.Begin(
                errorMessage,
                errorCode,
                payload,
                duration,
                completionHold);

            Refresh();
        }

        private void Update()
        {
            if (!engine.IsRunning)
                return;

            bool changed = engine.Advance(Time.unscaledDeltaTime);

            if (changed)
                Refresh();

            if (!engine.IsRestartReady)
                return;

            // Stop before callbacks so completion cannot fire twice.
            Cancel();

            onRestartRequested?.Invoke();
            RestartRequested?.Invoke();
        }

        public void Cancel()
        {
            engine.Stop();

            Texture2D previousImage = qrImage;
            qrImage = null;

            Refresh();

            if (previousImage != null)
                Destroy(previousImage);
        }

        private void Refresh()
        {
            if (view == null)
                return;

            view.Render(viewBuilder.Build(engine, qrImage));
        }
    }
}