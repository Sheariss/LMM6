using Atlas.Core.Persistence;
using Atlas.Presentation.GameUI.SaveSlotMenu;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.BootScreen
{
    public class BootScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------
        // private SOSManager sosManager;

        // -------------------- HELPERS --------------------
        private BootScreenBinder binder;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            // Call GameStateManager.LoadGame()

            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[BootScreenController] UIDocument was not found in parent hierarchy.");
                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder = new BootScreenBinder(uiDoc.rootVisualElement);

            if (!binder.IsValid)
            {
                Debug.LogError("[BootScreenController] UI bindings are invalid.");
                return;
            }

            //StartBootScreenFlow();
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            // sosManager = SOSManager.Instance;

            /*if (sosManager == null)
            {
                Debug.LogError("[SaveSlotMenuController] SaveManager instance was not found.");
                return false;
            } */


            return true;
        }

        /* StartBootScreenFLow{
         *
         * Check Progression Manager & Save Manager
         * Check unlocked accounts
         * Check last active users
         *
         * Call actual ui loading processes like
         * LockScreen Refresh, DesktopRefresh
         *
         * + Call SetProgress to make the actual fill bar and description move aacording to the current process
         */

        // SetProgress()

        // SetDescription()


    }
}
