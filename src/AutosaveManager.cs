using System;
using System.Collections.Generic;

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;

/**
 * TODO
 * autosave limit
 * infinite autosave
 * 1. GetCharacterNames
 * 2. GetCharacterSaveKeys
 * 3. GetSaveInfo
 * 4. 
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
        private bool onEnterExitSave = false;
        private bool onRestSave = false;
        private bool onFastTravel = false;

        // Formatting
        private string calendarFormat = "";
        private int calendarIndex = 0;
        private string timeFormat = "";
        private bool use24HourClock = false;
        private bool enableLocation = false;
        private string displayTime = "";

        // Maximum autosaves
        Queue<int> queueAutosaves = new Queue<int>();
        private string currPlayerName = "";

        // Timer
        private float lastSaveTime = 0;

        // FD save name
        private string saveName = "";

        private void Start()
        {
            ModSettings settings = mod.GetSettings();

            // General
            saveInterval = settings.GetValue<int>("Autosave", "AutosaveInterval");
            enableAutosaveTimer = saveInterval != 0 ? true : false;
            saveInCombat = settings.GetValue<bool>("Autosave", "SaveInCombat");
            onEnterExitSave = settings.GetValue<bool>("Autosave", "OnEnterExitSave");
            onRestSave = settings.GetValue<bool>("Autosave", "OnRestSave");
            autosaveLimit = settings.GetValue<int>("Autosave", "AutosaveLimit");
            onFastTravel = settings.GetValue<bool>("Autosave", "OnFastTravel");

            // Formatting
            calendarIndex = settings.GetValue<int>("Formatting", "Calendar");
            switch(calendarIndex)
            {
                case 0:
                    calendarFormat = "";
                    break;
                case 1:
                    calendarFormat = "MM/dd/yyyy";
                    break;
                case 2:
                    calendarFormat = "dd/MM/yyyy";
                    break;
                case 3:
                    calendarFormat = "yyyy MMMM";
                    break;
                case 4:
                    calendarFormat = "MMMM yyyy";
                    break;
                default:
                    calendarFormat = "";
                    break;

            }     
            use24HourClock = settings.GetValue<bool>("Formatting", "24hrClock");
            timeFormat = use24HourClock ? "HH:mm" : "h:mm tt";
            displayTime = calendarFormat + " " + timeFormat;
            enableLocation = settings.GetValue<bool>("Formatting", "ShowLocationName");

            // Autosave Limiting
            if (autosaveLimit != 0)
            {
                currPlayerName = GameManager.Instance.PlayerEntity.Name;
                SaveLoadManager.OnLoad += CheckPlayerCharacter;
                SaveLoadManager.OnSave += FireOnSaveEvent;
                InsertPlayerSaves();
            }

            if (enableAutosaveTimer)
            {
                // Check the watch every 45 seconds
                InvokeRepeating("CheckWatch", 45, 45);
            }

            if (onRestSave)
            {
                // Subscribe to sleep event
                DaggerfallRestWindow.OnSleepTick += Autosave;
            }

            if (onEnterExitSave)
            {
                // Subscribe to transition events, needs to be pre transition for now
                PlayerEnterExit.OnPreTransition += FireTransitionSave;
            }

            if (onFastTravel)
            {
                // Subscribe to fast travel event
                DaggerfallTravelPopUp.OnPostFastTravel += Autosave;
            }

        }

        private void CheckPlayerCharacter(SaveData_v1 saveData)
        {
            if(currPlayerName != saveData.playerData.playerEntity.name)
            {
                currPlayerName = saveData.playerData.playerEntity.name;
                InsertPlayerSaves();
            }
            return;
        }

        private void InsertPlayerSaves()
        {
            queueAutosaves.Clear();
            int[] currKeys = GameManager.Instance.SaveLoadManager.GetCharacterSaveKeys(currPlayerName);

            for (int i = 0; i < currKeys.Length / 2; i++)
            {
                int tmp = currKeys[i];
                currKeys[i] = currKeys[currKeys.Length - i - 1];
                currKeys[currKeys.Length - i - 1] = tmp;
            }

            // Need to check order by time
            foreach (var key in currKeys)
            {
                SaveInfo_v1 saveFolder = GameManager.Instance.SaveLoadManager.GetSaveInfo(key);
                if (saveFolder.saveName.Contains("Autosave"))
                {
                    Debug.Log("Key: " + key.ToString() + " Name: " + saveFolder.saveName + ". Date: " + saveFolder.dateAndTime.realTime.ToString());
                    // Assume autosave, in the final native version this will be done more securely/efficiently 
                    queueAutosaves.Enqueue(key);
                }
            }
            return;
        }

        private void FireOnSaveEvent(SaveData_v1 saveData)
        {
            InsertPlayerSaves();
            return;
        }

        private void Autosave()
        {
            saveName = "Autosave";
            var enemiesNearby = GameManager.Instance.AreEnemiesNearby();
            if( (enemiesNearby && saveInCombat) || !enemiesNearby && !GameManager.Instance.SaveLoadManager.LoadInProgress && !InputManager.Instance.IsPaused)
            {
                if (enableLocation)
                {
                    saveName += " " + GameManager.Instance.PlayerGPS.CurrentLocation.Name;
                }

                saveName += " " + System.DateTime.Now.ToString(displayTime);

                lastSaveTime = Time.realtimeSinceStartup;

                if (autosaveLimit > 0 && queueAutosaves.Count >= autosaveLimit)
                {
                    DeleteQueuedAutosave();
                }

                GameManager.Instance.SaveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, saveName);
            }
        }

        private void DeleteQueuedAutosave()
        {
            int idxToDelete = queueAutosaves.Dequeue();
            GameManager.Instance.SaveLoadManager.DeleteSaveFolder(idxToDelete);
        }

        private void CheckWatch()
        {
            float timeNow = Time.realtimeSinceStartup;
            float compare = timeNow - lastSaveTime;
            float toBeat = saveInterval * 60;
            if (!InputManager.Instance.IsPaused && timeNow - lastSaveTime > saveInterval * 60)
            {
                Autosave();
            }
        }

        private void FireTransitionSave(PlayerEnterExit.TransitionEventArgs args)
        {
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
