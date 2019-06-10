using System;
using System.Collections.Generic;

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using DaggerfallWorkshop.Game.Serialization;



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
                    calendarFormat = "MM/dd/yyyy ";
                    break;
                case 2:
                    calendarFormat = "dd/MM/yyyy ";
                    break;
                case 3:
                    calendarFormat = "yyyy MMMM ";
                    break;
                case 4:
                    calendarFormat = "MMMM yyyy ";
                    break;
                default:
                    calendarFormat = "";
                    break;

            }     
            use24HourClock = settings.GetValue<bool>("Formatting", "24hrClock");
            timeFormat = use24HourClock ? "HH:mm" : "h:mm tt";
            displayTime = calendarFormat + timeFormat;
            enableLocation = settings.GetValue<bool>("Formatting", "ShowLocationName");

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

            //Save game when starting new game
            StartGameBehaviour.OnNewGame += Autosave;
        }

        private void CheckPlayerCharacter(SaveData_v1 saveData)
        {
            if(currPlayerName != saveData.playerData.playerEntity.name)
            {
                currPlayerName = saveData.playerData.playerEntity.name;
            }
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
                    string locationName = GameManager.Instance.PlayerGPS.CurrentLocation.Name == " " ? GameManager.Instance.PlayerGPS.CurrentRegion.Name : GameManager.Instance.PlayerGPS.CurrentLocation.Name;
                    saveName += " " + locationName;
                }

                saveName += " " + System.DateTime.Now.ToString(displayTime);

                lastSaveTime = Time.realtimeSinceStartup;

                GameManager.Instance.SaveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, saveName);

                if (autosaveLimit > 0)
                {
                    ShouldDeleteSaves();
                }
            }
        }

        private void ShouldDeleteSaves()
        {
            int[] currentKeys = GameManager.Instance.SaveLoadManager.GetCharacterSaveKeys(GameManager.Instance.PlayerEntity.Name);

            if(currentKeys.Length >= autosaveLimit)
            {
                List<SaveInfo_v1> saveList = new List<SaveInfo_v1>();
                foreach (var key in currentKeys)
                {
                    SaveInfo_v1 saveFolder = GameManager.Instance.SaveLoadManager.GetSaveInfo(key);
                    if (saveFolder.saveName.Contains("Autosave"))
                    {
                        saveList.Add(saveFolder);
                    }
                }

                saveList.Sort(delegate (SaveInfo_v1 sv1, SaveInfo_v1 sv2) {
                    return sv1.dateAndTime.realTime.CompareTo(sv2.dateAndTime.realTime);
                });

                while (saveList.Count >= autosaveLimit)
                {
                    SaveInfo_v1 saveToDelete = saveList[0];
                    int saveIdx = GameManager.Instance.SaveLoadManager.FindSaveFolderByNames(GameManager.Instance.PlayerEntity.Name, saveToDelete.saveName);
                    GameManager.Instance.SaveLoadManager.DeleteSaveFolder(saveIdx);
                    saveList.RemoveAt(0);
                }
            }
           
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
