using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CMDController : MonoBehaviour
    {
        [Header("Commands")]
        [SerializeField] private CMDCommandLibrary commandLibrary;

        private UIDocument uiDocument;
        private CMDBinder binder;
        private CMDView view;
        private CMDViewBuilder viewBuilder;
        private CMDEngine engine;

        private void Start()
        {
            uiDocument = GetComponent<UIDocument>();

            binder = new CMDBinder(
                uiDocument.rootVisualElement);

            engine = new CMDEngine(
                commandLibrary);

            viewBuilder = new CMDViewBuilder();

            view = new CMDView(
                binder);

            RegisterCallbacks();

            InitializeTerminal();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        // -------------------- INITIALIZATION --------------------
        private void InitializeTerminal()
        {
            Render();

            view.FocusInput();
        }

        // -------------------- CALLBACKS --------------------
        private void RegisterCallbacks()
        {
            if (binder.CommandInput == null)
                return;

            binder.CommandInput.RegisterCallback<KeyDownEvent>(
                OnInputKeyDown);
        }

        private void UnregisterCallbacks()
        {
            if (binder?.CommandInput == null)
                return;

            binder.CommandInput.UnregisterCallback<KeyDownEvent>(
                OnInputKeyDown);
        }

        // -------------------- INPUT --------------------
        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            SubmitCommand();

            evt.StopPropagation();
        }

        private void SubmitCommand()
        {
            string input =
                binder.CommandInput.value;

            if (string.IsNullOrWhiteSpace(input))
            {
                view.ClearInput();
                view.FocusInput();
                return;
            }

            engine.Execute(input);

            Render();

            view.ClearInput();
            view.FocusInput();
        }

        // -------------------- RENDER --------------------
        private void Render()
        {
            CMDViewState state =
                viewBuilder.Build(engine);

            view.Render(state);
        }
    }
}