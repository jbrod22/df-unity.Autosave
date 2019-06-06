using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

/**
 * TODO
 * remove prefix
 * autosave limit
 * infinite autosave
 * onenterexit
 * onrest
 * Use C# DateTime formatting
 * Tamrielic Calendar
 * Hour Minute formatting
 * 24 hour clock
 * Show Location Name
 */


namespace AutosaveManager
{
    public class AutosaveManager : MonoBehaviour
    {
        // Mod
        private static Mod mod;

        // General
        private int saveInterval = 0;
        private bool saveInCombat = false;
        private int autosaveLimit = 0;
        private bool enableAutosaveTimer = false;
        private bool infiniteAutosaves = false;
        private bool onEnterExitSave = false;
        private bool onRestSave = false;

        // Formatting
        private string calendarFormat = "";
        private string timeFormat = "";
        private bool use24HourClock = false;
        private bool enableLocation = false;

        // Timer
        private float lastSaveTime = 0;

        // FD save name
        private string saveName = "";

        private void Start()
        {
            ModSettings settings = mod.GetSettings();

            // General
            saveInterval = settings.GetValue<int>("Autosave", "AutosaveInterval");
            enableAutosaveTimer = saveInterval == 0 ? true : false;
            saveInCombat = settings.GetValue<bool>("Autosave", "SaveInCombat");
            autosaveLimit = settings.GetValue<int>("Autosave", "AutosaveLimit");
            infiniteAutosaves = settings.GetValue<bool>("Autosave", "InfiniteAutosaves");
            onEnterExitSave = settings.GetValue<bool>("Autosave", "OnEnterExitSave");
            onRestSave = settings.GetValue<bool>("Autosave", "OnRestSave");

            // Formatting
            calendarFormat = settings.GetValue<string>("Formatting", "Calendar");
            timeFormat = settings.GetValue<string>("Formatting", "Time");
            use24HourClock = settings.GetValue<bool>("Formatting", "24hrClock");
            enableLocation = settings.GetValue<bool>("Formatting", "ShowLocationName");

            saveName = "Autosave -";

            if (enableAutosaveTimer)
            {
                Debug.Log("enableAutosaveTimer");

                // Check the watch every 45 seconds
                InvokeRepeating("CheckWatch", 45, 45);
            }
            if (onRestSave)
            {
                Debug.Log("onRestSave");

                // Subscribe to sleep event
                DaggerfallRestWindow.OnSleepTick += Autosave;
            }
            if (onEnterExitSave)
            {
                Debug.Log("onEnterExitSave");

                // Subscribe to transition events
                PlayerEnterExit.OnTransitionExterior += FireTransitionSave;
                PlayerEnterExit.OnTransitionInterior += FireTransitionSave;
                PlayerEnterExit.OnTransitionDungeonExterior += FireTransitionSave;
                PlayerEnterExit.OnTransitionDungeonInterior += FireTransitionSave;
            }
        }

        private void Autosave()
        {
            Debug.Log("Autosave");
            var enemiesNearby = GameManager.Instance.AreEnemiesNearby();
            if( (enemiesNearby && saveInCombat) || !enemiesNearby)
            {
                saveName += System.DateTime.Now.ToString(timeFormat + calendarFormat);
                if (enableLocation)
                {
                    saveName += " " + GameManager.Instance.PlayerGPS.CurrentLocation.Name;
                }
                lastSaveTime = Time.realtimeSinceStartup;
                GameManager.Instance.SaveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, saveName);
            }
        }

        private void CheckWatch()
        {
            Debug.Log("Check Watch");
            float timeNow = Time.realtimeSinceStartup;
            if (!InputManager.Instance.IsPaused && timeNow - lastSaveTime > saveInterval * 60)
            {
                Autosave();
            }
        }

        private void FireTransitionSave(PlayerEnterExit.TransitionEventArgs args)
        {
            Debug.Log("FireTransitionSave");
            Autosave();
        }

        [Invoke(StateManager.StateTypes.Game, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            GameObject gObject = new GameObject("autosavemanager");
            AutosaveManager autosaveManager = gObject.AddComponent<AutosaveManager>();
            ModManager.Instance.GetMod(initParams.ModTitle).IsReady = true;
        }
    }
}
