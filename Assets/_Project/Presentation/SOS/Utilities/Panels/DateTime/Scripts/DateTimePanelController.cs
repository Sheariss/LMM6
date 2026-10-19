using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Panels;
using Atlas.SOS.Calendar;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.DateAndTime
{
    public sealed class DateTimePanelController : PanelRuntime
    {
        [Header("Authored Data")]
        [SerializeField] private CalendarLibrary calendarLibrary;

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        private readonly List<Action> unsubscribe = new();

        private DateTimePanelEngine engine;
        private DateTimePanelView view;

        private bool initialized;

        public override VisualElement Surface =>
            view?.Binder.Root;

        public override void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "DateTimePanelController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            try
            {
                engine = new DateTimePanelEngine(
                    calendarLibrary);

                var builder = new DateTimePanelViewBuilder();
                view = builder.Build(panelTemplate);

                parent.Add(view.Host);

                RegisterEvents();

                initialized = true;

                Render();
                view.Hide();
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public override void Show()
        {
            if (!initialized)
                return;

            engine.Tick(Time.realtimeSinceStartupAsDouble);

            Render();
            view.Show();
        }

        public override void Hide()
        {
            // Keep browsing state and the focus session.
            view?.Hide();
        }

        public override void Shutdown()
        {
            initialized = false;

            Hide();

            foreach (Action remove in unsubscribe)
                remove();

            unsubscribe.Clear();

            view?.Detach();

            view = null;
            engine = null;
        }

        // Call from a future shared scenario-time service.
        // This does not change CalendarLibrary or the Calendar app.
        public void SetScenarioTime(DateTime scenarioLocalTime)
        {
            if (!initialized)
            {
                throw new InvalidOperationException(
                    "Initialize the date/time panel before setting its time.");
            }

            engine.SetScenarioTime(scenarioLocalTime);
            Render();
        }

        private void RegisterEvents()
        {
            BindClick(view.Binder.CollapseButton, () =>
            {
                engine.ToggleCollapsed();
                Render();
            });

            BindClick(view.Binder.PreviousMonthButton, () =>
            {
                engine.MoveMonth(-1);
                Render();
            });

            BindClick(view.Binder.NextMonthButton, () =>
            {
                engine.MoveMonth(1);
                Render();
            });

            BindClick(view.Binder.DecreaseFocusButton, () =>
            {
                engine.ChangeFocusDuration(-1);
                view.RenderFocus(engine);
            });

            BindClick(view.Binder.IncreaseFocusButton, () =>
            {
                engine.ChangeFocusDuration(1);
                view.RenderFocus(engine);
            });

            BindClick(view.Binder.StartFocusButton, () =>
            {
                engine.ToggleFocus(
                    Time.realtimeSinceStartupAsDouble);

                view.RenderFocus(engine);
            });

            for (int index = 0; index < 42; index++)
            {
                int slot = index;

                BindClick(view.GetDayButton(slot), () =>
                {
                    engine.SelectDate(engine.GetDay(slot));

                    Render();

                    // Selecting an adjacent-month date moves the grid.
                    // Focus the selected date's new position.
                    int selectedIndex = (int)(
                        engine.SelectedDate - engine.GridStart).TotalDays;

                    view.GetDayButton(selectedIndex).Focus();
                });
            }
        }

        private void BindClick(Button button, Action handler)
        {
            button.clicked += handler;

            unsubscribe.Add(
                () => button.clicked -= handler);
        }

        private void Render()
        {
            if (initialized)
                view.Render(engine);
        }

        private void Update()
        {
            if (!initialized || !engine.IsFocusing)
                return;

            if (engine.Tick(Time.realtimeSinceStartupAsDouble))
                view.RenderFocus(engine);
        }

        private void OnDisable()
        {
            Shutdown();
        }
    }
}