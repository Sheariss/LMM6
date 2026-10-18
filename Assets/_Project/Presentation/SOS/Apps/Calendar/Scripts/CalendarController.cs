using System;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.Calendar;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calendar
{
    public sealed class CalendarController : AppContentController
    {
        [Header("Authored Data")]
        [SerializeField] private CalendarLibrary calendarLibrary;

        private CalendarBinder binder;
        private CalendarEngine engine;
        private CalendarView view;

        protected override void OnInitialize()
        {
            if (calendarLibrary == null)
            {
                throw new InvalidOperationException(
                    "[Calendar] CalendarLibrary is not assigned.");
            }

            binder = new CalendarBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "[Calendar] UI binding failed. Check the missing elements in the Console.");
            }


            engine = new CalendarEngine(calendarLibrary);

            view = new CalendarView(
                binder,
                new CalendarViewBuilder());

            RegisterCallbacks();
            Refresh();
        }

        protected override void OnShutdown()
        {
            // AppContentController removes static callbacks first.
            // The view removes handlers on generated controls.
            view?.Clear();

            view = null;
            engine = null;
            binder = null;
        }

        // Explicit integration point for a future scenario-time service.
        // This method does not read the operating system clock.
        public void SetScenarioTime(DateTime scenarioLocalTime)
        {
            if (engine == null)
            {
                throw new InvalidOperationException(
                    "Initialize Calendar before setting its scenario time.");
            }

            engine.SetScenarioTime(scenarioLocalTime);
            Refresh();
        }

        private void RegisterCallbacks()
        {
            BindClick(binder.ToggleSidebar, () =>
            {
                engine.ToggleSidebar();
                Refresh();
            });

            BindClick(binder.Today, () =>
            {
                engine.GoToday();
                Refresh();
            });

            BindClick(binder.Previous, () => Move(-1));
            BindClick(binder.Next, () => Move(1));

            for (int index = 0; index < binder.ViewButtons.Length; index++)
            {
                CalendarViewMode mode = (CalendarViewMode)index;

                BindClick(binder.ViewButtons[index], () =>
                {
                    engine.SetMode(mode);
                    Refresh();
                });
            }

            BindCallback<ChangeEvent<string>>(
                binder.Search,
                change =>
                {
                    engine.SetSearch(change.newValue);
                    Refresh();
                });

            BindCategory(binder.Personal, CalendarCategory.Personal);
            BindCategory(binder.Work, CalendarCategory.Work);
            BindCategory(binder.Reminders, CalendarCategory.Reminders);

            BindClick(binder.CloseEditor, CloseDetails);
            BindClick(binder.CancelEditor, CloseDetails);

            BindCallback<KeyDownEvent>(
                binder.Root,
                OnKeyDown,
                TrickleDown.TrickleDown);
        }

        private void BindCategory(
            Toggle toggle,
            CalendarCategory category)
        {
            BindCallback<ChangeEvent<bool>>(
                toggle,
                change =>
                {
                    engine.SetCategoryEnabled(
                        category, change.newValue);

                    Refresh();
                });
        }

        private void Move(int direction)
        {
            engine.Move(direction);
            Refresh();
        }

        private void SelectDay(DateTime date)
        {
            engine.SelectDay(date);
            Refresh();

            binder.ScheduleScroll.scrollOffset = Vector2.zero;
        }

        private void Refresh()
        {
            view.Render(engine, SelectDay, view.ShowDetails);
        }

        private void CloseDetails()
        {
            view.CloseDetails();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!view.DetailsOpen)
                return;

            if (evt.keyCode == KeyCode.Escape)
            {
                view.CloseDetails();
            }
            else if (evt.keyCode == KeyCode.Tab)
            {
                view.CycleDetailButtons();
            }
            else
            {
                return;
            }

            evt.StopPropagation();
        }
    }
}