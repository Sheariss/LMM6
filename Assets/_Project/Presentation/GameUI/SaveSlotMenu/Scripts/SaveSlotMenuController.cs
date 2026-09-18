using System;
using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Core.Persistence;

namespace Atlas.Presentation.GameUI.SaveSlotMenu
{
    public enum SaveSlotMenuMode
    {
        NewGame,
        Continue
    }
    public sealed class SaveSlotMenuController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------
        private SaveManager saveManager;

        // -------------------- HELPERS --------------------
        private SaveSlotMenuBinder binder;
        private SaveSlotMenuView view;
        private SaveSlotMenuBuilder builder;

        // -------------------- STATE --------------------
        private SaveSlotMenuMode currentMode;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[SaveSlotMenuController] UIDocument was not found in parent hierarchy.");
                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder = new SaveSlotMenuBinder(uiDoc.rootVisualElement);

            if (!binder.IsValid)
            {
                Debug.LogError("[SaveSlotMenuController] UI bindings are invalid.");
                return;
            }

            view = new SaveSlotMenuView(binder);
            builder = new SaveSlotMenuBuilder(saveManager);

            BindActions();
            view.Hide();
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            saveManager = SaveManager.Instance;

            if (saveManager == null)
            {
                Debug.LogError("[SaveSlotMenuController] SaveManager instance was not found.");
                return false;
            }

            return true;
        }

        // -------------------- ACTION BINDING --------------------
        private void BindActions()
        {
            binder.BindActions(OnSlotPressed, OnBackPressed);
        }

        // -------------------- SHOW --------------------
        public void ShowForNewGame()
        {
            Debug.Log("[SaveSlotMenuController] ShowForNewGame started.");
            currentMode = SaveSlotMenuMode.NewGame;
            RefreshView();
            view.Show();
        }

        public void ShowForContinue()
        {
            currentMode = SaveSlotMenuMode.Continue;
            RefreshView();
            view.Show();
        }

        // -------------------- HIDE --------------------
        public void Hide()
        {
            view?.Hide();
        }

        // -------------------- VIEW --------------------
        public void RefreshView()
        {
            if (builder == null || view == null)
            {
                return;
            }

            SaveSlotMenuView.ViewState state = builder.Build(currentMode);
            view.Apply(state);
        }

        // -------------------- ACTIONS --------------------
        private void OnSlotPressed(int slotNumber)
        {
            switch (currentMode)
            {
                case SaveSlotMenuMode.NewGame:
                    HandleNewGameSlot(slotNumber);
                    break;

                case SaveSlotMenuMode.Continue:
                    HandleContinueSlot(slotNumber);
                    break;
            }
        }

        private void OnBackPressed()
        {
            Hide();
        }

        // -------------------- NEW GAME --------------------
        private void HandleNewGameSlot(int slotNumber)
        {
            bool hasSave =saveManager.HasSave(slotNumber);

            if (hasSave)
            {
                RequestOverwriteConfirmation(slotNumber);
                return;
            }
            StartNewGame(slotNumber);
        }

        private void StartNewGame(int slotNumber)
        {
            saveManager.SelectSaveSlot(slotNumber);

            // TODO: saveManager.StartNewSave();

            Debug.Log($"[SaveSlotMenuController] New game initialized in Slot {slotNumber}.");

            Hide();

            // TODO: Tell GameStateManager to enter the beginning of the investigation flow.
        }


        // -------------------- CONTINUE --------------------
        private void HandleContinueSlot(int slotNumber)
        {
            bool hasSave = saveManager.HasSave(slotNumber);

            if (!hasSave)
            {
                RequestNewGameConfirmation(slotNumber);
                return;
            }
            ContinueGame(slotNumber);
        }

        private void ContinueGame(int slotNumber)
        {
            saveManager.SelectSaveSlot(slotNumber);

            saveManager.LoadGame();

            Debug.Log($"[SaveSlotMenuController] Loaded Slot {slotNumber}.");

            Hide();

            // TODO:
            // Resume stored scene / gameplay context.
        }


        // -------------------- CONFIRMATIONS --------------------

        private void RequestOverwriteConfirmation(int slotNumber)
        {
            Debug.Log(
                $"[SaveSlotMenuController] Slot {slotNumber} already contains save data. " +
                "Overwrite confirmation is required."
            );

            // TODO:
            //
            // confirmationController.ShowOverwrite(
            //     slotNumber,
            //     () => StartNewGame(slotNumber)
            // );
        }

        private void RequestNewGameConfirmation(int slotNumber)
        {
            Debug.Log(
                $"[SaveSlotMenuController] Slot {slotNumber} is empty. " +
                "New game confirmation is required."
            );

            // TODO:
            //
            // confirmationController.ShowEmptySlot(
            //     slotNumber,
            //     () => StartNewGame(slotNumber)
            // );
        }
    }
}