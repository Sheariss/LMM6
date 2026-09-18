using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Utils
{
    public abstract class UIBinder
    {
        public bool IsValid { get; protected set; } = true;

        protected T Bind<T>(
            VisualElement root,
            string elementName)
            where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element == null)
            {
                Debug.LogError(
                    $"[{GetType().Name}] Could not find " +
                    $"{typeof(T).Name} named '{elementName}'."
                );

                IsValid = false;
            }

            return element;
        }
    }
}