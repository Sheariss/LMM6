using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Apps
{
    public abstract class AppContentController : MonoBehaviour
    {
        private readonly Stack<Action> cleanupActions = new();

        protected VisualElement InstanceRoot { get; private set; }

        public bool IsInitialized { get; private set; }

        public void Initialize(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            if (InstanceRoot != null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} already has a UI instance.");
            }

            InstanceRoot = instanceRoot;

            try
            {
                OnInitialize();
                IsInitialized = true;
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        protected abstract void OnInitialize();

        // Apps can release additional resources here.
        protected virtual void OnShutdown()
        {
        }

        protected void BindClick(Button button, Action handler)
        {
            if (InstanceRoot == null)
                throw new InvalidOperationException("Initialize first.");

            if (button == null)
                throw new ArgumentNullException(nameof(button));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            button.clicked += handler;

            // Retain the exact delegate, including anonymous lambdas.
            cleanupActions.Push(() => button.clicked -= handler);
        }

        protected void BindCallback<TEvent>(
            VisualElement element,
            EventCallback<TEvent> handler,
            TrickleDown trickleDown = TrickleDown.NoTrickleDown)
            where TEvent : EventBase<TEvent>, new()
        {
            if (InstanceRoot == null)
                throw new InvalidOperationException("Initialize first.");

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            element.RegisterCallback<TEvent>(handler, trickleDown);

            cleanupActions.Push(() =>
                element.UnregisterCallback<TEvent>(
                    handler, trickleDown));
        }

        public void Shutdown()
        {
            if (InstanceRoot == null)
                return;

            IsInitialized = false;

            // Unsubscribe before releasing the binder and engine.
            while (cleanupActions.Count > 0)
            {
                Action cleanup = cleanupActions.Pop();

                try
                {
                    cleanup();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            try
            {
                OnShutdown();
            }
            finally
            {
                InstanceRoot = null;
            }
        }

        protected virtual void OnDestroy()
        {
            Shutdown();
        }
    }
}