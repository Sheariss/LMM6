using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.ColorPicker
{
    public sealed class ColorPickerController : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private readonly ColorPickerEngine engine = new();
        private readonly ColorPickerViewBuilder builder = new();
        private readonly List<Action> unsubscribe = new();

        private ColorPickerBinder binder;
        private ColorPickerView view;
        private VisualElement returnFocus;
        private int capturedPointer = -1;
        private bool isOpen;
        private bool palettesInitialized;

        public Color Current => engine.Current;

        public event Action<Color> ColorChanged;
        public event Action Closed;
        public event Action PalettesChanged;

        public void Open(
            Color initial,
            bool allowAlpha = true,
            string title = "Color Picker",
            VisualElement opener = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogError(
                    "[ColorPickerController] Activate the GameObject before opening.",
                    this);
                return;
            }

            if (!enabled)
                enabled = true;

            if (!Initialize())
                return;

            ReleasePointer();
            returnFocus = opener;
            engine.Open(initial, allowAlpha);
            isOpen = true;

            view.Show(title);
            Refresh();
            binder.SvArea.Focus();
        }

        public void Close()
        {
            if (!isOpen)
                return;

            isOpen = false;
            ReleasePointer();
            view?.Hide();

            VisualElement target = returnFocus;
            returnFocus = null;

            if (target?.panel != null)
                target.Focus();

            Closed?.Invoke();
        }

        public void SetPalettes(
            IEnumerable<Color> presets,
            IEnumerable<Color> customColors)
        {
            engine.SetPalettes(presets, customColors);
            palettesInitialized = true;

            if (view != null)
                Refresh();
        }

        public Color[] GetPresets()
        {
            return Copy(engine.Presets);
        }

        public Color[] GetCustomColors()
        {
            return Copy(engine.CustomColors);
        }

        private bool Initialize()
        {
            if (view != null)
                return true;

            if (document == null)
                document = GetComponentInParent<UIDocument>();

            if (document == null)
            {
                Debug.LogError("[ColorPickerController] UIDocument is missing.", this);
                return false;
            }

            binder = new ColorPickerBinder(document.rootVisualElement);

            if (!binder.IsValid)
            {
                binder = null;
                return false;
            }

            if (!palettesInitialized)
            {
                engine.SetPalettes(new Color[]
                {
                    new Color32(235, 104, 108, 255),
                    new Color32(242, 161, 102, 255),
                    new Color32(243, 213, 107, 255),
                    new Color32(67, 140, 112, 255),
                    new Color32(91, 180, 235, 255)
                }, Array.Empty<Color>());

                palettesInitialized = true;
            }

            view = new ColorPickerView(binder);
            view.Hide();

            RegisterCallbacks();
            return true;
        }

        private void RegisterCallbacks()
        {
            BindClick(binder.CloseButton, Close);
            BindClick(binder.AddPresetButton, () => AddColor(true));
            BindClick(binder.AddCustomButton, () => AddColor(false));

            BindClick(binder.EditPresetsButton, () =>
            {
                engine.ToggleEditing(true);
                Refresh();
            });

            BindClick(binder.EditCustomButton, () =>
            {
                engine.ToggleEditing(false);
                Refresh();
            });

            BindChange(binder.HueSlider, evt =>
                ChangeColor(() => engine.SetHue(evt.newValue)));

            BindChange(binder.AlphaSlider, evt =>
                ChangeColor(() => engine.SetAlpha(evt.newValue)));

            BindChange(binder.AlphaInput, evt =>
                ChangeColor(() => { engine.TrySetAlpha(evt.newValue); }));

            BindChange(binder.FormatDropdown, evt =>
            {
                if (Enum.TryParse(evt.newValue, out ColorPickerFormat format))
                {
                    engine.SetFormat(format);
                    Refresh();
                }
            });

            foreach (var pair in binder.Fields)
            {
                ColorPickerFormat format = pair.Key;

                foreach (TextField field in pair.Value)
                {
                    BindChange(field, evt =>
                    {
                        if (engine.Format == format)
                            SubmitChannels(format);
                    });
                }
            }

            view.SwatchActivated += OnSwatchActivated;
            unsubscribe.Add(() => view.SwatchActivated -= OnSwatchActivated);

            binder.SvArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
            binder.SvArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            binder.SvArea.RegisterCallback<PointerUpEvent>(OnPointerUp);
            binder.SvArea.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            binder.SvArea.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            binder.SvArea.RegisterCallback<KeyDownEvent>(OnSvKeyDown);
            binder.Root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
        }

        private void SubmitChannels(ColorPickerFormat format)
        {
            TextField[] fields = binder.Fields[format];
            string[] values = new string[fields.Length];

            for (int i = 0; i < fields.Length; i++)
                values[i] = fields[i].value;

            ChangeColor(() => { engine.TrySetChannels(values); });
        }

        private void ChangeColor(Action mutation)
        {
            Color previous = engine.Current;
            mutation();
            Refresh();

            if (!previous.Equals(engine.Current))
                ColorChanged?.Invoke(engine.Current);
        }

        private void AddColor(bool preset)
        {
            bool changed = engine.AddCurrent(preset);
            Refresh();

            if (changed)
                PalettesChanged?.Invoke();
        }

        private void OnSwatchActivated(bool preset, int index)
        {
            Color previous = engine.Current;
            bool paletteChanged = engine.ActivateSwatch(preset, index);
            Refresh();

            if (paletteChanged)
                PalettesChanged?.Invoke();

            if (!previous.Equals(engine.Current))
                ColorChanged?.Invoke(engine.Current);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || capturedPointer != -1)
                return;

            capturedPointer = evt.pointerId;
            binder.SvArea.CapturePointer(capturedPointer);
            binder.SvArea.Focus();

            UpdatePointer(evt.position);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != capturedPointer)
                return;

            UpdatePointer(evt.position);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != capturedPointer)
                return;

            UpdatePointer(evt.position);
            ReleasePointer();
            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == capturedPointer)
                ReleasePointer();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == capturedPointer)
                capturedPointer = -1;
        }

        private void UpdatePointer(Vector3 panelPosition)
        {
            Rect rect = binder.SvArea.contentRect;

            if (rect.width <= 0f || rect.height <= 0f)
                return;

            Vector2 local = binder.SvArea.WorldToLocal(
                new Vector2(panelPosition.x, panelPosition.y));

            float saturation = (local.x - rect.xMin) / rect.width;
            float value = 1f - (local.y - rect.yMin) / rect.height;

            ChangeColor(() => engine.SetSaturationValue(saturation, value));
        }

        private void ReleasePointer()
        {
            int pointer = capturedPointer;
            capturedPointer = -1;

            if (pointer >= 0 && binder?.SvArea != null &&
                binder.SvArea.HasPointerCapture(pointer))
            {
                binder.SvArea.ReleasePointer(pointer);
            }
        }

        private void OnSvKeyDown(KeyDownEvent evt)
        {
            float step = evt.shiftKey ? 0.1f : 0.01f;
            float saturation = engine.Saturation;
            float value = engine.Value;

            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow:
                    saturation -= step;
                    break;
                case KeyCode.RightArrow:
                    saturation += step;
                    break;
                case KeyCode.DownArrow:
                    value -= step;
                    break;
                case KeyCode.UpArrow:
                    value += step;
                    break;
                default:
                    return;
            }

            ChangeColor(() => engine.SetSaturationValue(saturation, value));
            evt.StopPropagation();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            Close();
            evt.StopPropagation();
        }

        private void Refresh()
        {
            view.Render(builder.Build(engine));
        }

        private void BindClick(Button button, Action callback)
        {
            button.clicked += callback;
            unsubscribe.Add(() => button.clicked -= callback);
        }

        private void BindChange<T>(
            BaseField<T> field,
            EventCallback<ChangeEvent<T>> callback)
        {
            field.RegisterValueChangedCallback(callback);
            unsubscribe.Add(() => field.UnregisterValueChangedCallback(callback));
        }

        private static Color[] Copy(IReadOnlyList<Color> source)
        {
            Color[] result = new Color[source.Count];

            for (int i = 0; i < result.Length; i++)
                result[i] = source[i];

            return result;
        }

        private void OnDisable()
        {
            Close();

            if (view == null)
                return;

            foreach (Action cleanup in unsubscribe)
                cleanup();

            unsubscribe.Clear();

            binder.SvArea.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            binder.SvArea.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            binder.SvArea.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            binder.SvArea.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            binder.SvArea.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            binder.SvArea.UnregisterCallback<KeyDownEvent>(OnSvKeyDown);
            binder.Root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown);

            view.Dispose();
            view = null;
            binder = null;
        }
    }
}