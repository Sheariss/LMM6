using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.DateAndTime
{
    public sealed class DateTimePanelViewBuilder
    {
        public DateTimePanelView Build(VisualTreeAsset template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            TemplateContainer host = template.CloneTree();

            host.name = "DateTimePanelHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 12;
            host.style.right = 12;
            host.style.top = 12;
            host.style.bottom = 12;
            host.style.display = DisplayStyle.None;

            var binder = new DateTimePanelBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "The date/time panel is missing required elements.");
            }

            binder.Root.pickingMode = PickingMode.Position;

            ConfigureIcon(
                host,
                "CollapseCalendarIcon",
                "Icons/Arrows 1/Expand-Arrow/keyboard_arrow_down_32dp");

            ConfigureIcon(
                host,
                "PreviousMonthIcon",
                "Icons/Arrows 1/Collapse-Arrow/" +
                "keyboard_control_key_32dp_FFFFFF_FILL0_wght400_GRAD0_opsz40");

            ConfigureIcon(
                host,
                "NextMonthIcon",
                "Icons/Arrows 1/Expand-Arrow/keyboard_arrow_down_32dp");

            ConfigureIcon(
                host,
                "DecreaseFocusIcon",
                "Icons/Math/Minus/remove_32dp");

            ConfigureIcon(
                host,
                "IncreaseFocusIcon",
                "Icons/Math/Add/add_2_32dp");

            ConfigureIcon(
                host,
                "StartFocusIcon",
                "Icons/Media/Play/play_arrow_32dp");

            host.Query<Label>().ForEach(label =>
            {
                label.enableRichText = false;
                label.pickingMode = PickingMode.Ignore;
            });

            binder.DaysGrid.Clear();

            var days = new Button[42];

            for (int week = 0; week < 6; week++)
            {
                var row = new VisualElement();
                row.AddToClassList("calendar-week");

                for (int day = 0; day < 7; day++)
                {
                    int index = week * 7 + day;

                    var slot = new VisualElement();
                    slot.AddToClassList("calendar-day-slot");

                    var button = new Button
                    {
                        name = $"CalendarDay{index}",
                        enableRichText = false
                    };

                    button.AddToClassList("calendar-day");

                    days[index] = button;

                    slot.Add(button);
                    row.Add(slot);
                }

                binder.DaysGrid.Add(row);
            }

            return new DateTimePanelView(host, binder, days);
        }

        private static void ConfigureIcon(
            VisualElement root,
            string name,
            string resourcePath)
        {
            Image image = root.Q<Image>(name);

            if (image == null)
            {
                throw new InvalidOperationException(
                    $"Missing date/time icon element '{name}'.");
            }

            Texture2D texture = Resources.Load<Texture2D>(
                resourcePath);

            if (texture == null)
            {
                throw new InvalidOperationException(
                    $"Missing date/time icon asset '{resourcePath}'.");
            }

            image.image = texture;
            image.scaleMode = ScaleMode.ScaleToFit;
            image.tintColor = Color.white;
            image.pickingMode = PickingMode.Ignore;
        }
    }
}