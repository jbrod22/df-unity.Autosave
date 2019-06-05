using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

namespace AutosaveManager
{
    public class AutosaveManager : MonoBehaviour
    {
        private string savePrefix = "";
        private int saveInterval = 0;
        private bool saveInCombat = true;
        public static Mod mod;
        private string displayTime = "";

        private void Start()
        {
            ModSettings settings = mod.GetSettings();

            saveInterval = settings.GetValue<int>("Autosave", "AutosaveInterval");
            savePrefix = settings.GetValue<string>("Autosave", "AutosavePrefix");
            saveInCombat = settings.GetValue<bool>("Autosave", "SaveInCombat");

            InvokeRepeating("Autosave", saveInterval * 60, saveInterval * 60);
        }

        private void Autosave()
        {
            displayTime = System.DateTime.Now.ToString("HH:mm");
            var enemiesNearby = GameManager.Instance.AreEnemiesNearby();
            if( (enemiesNearby && saveInCombat) || !enemiesNearby)
            {
                GameManager.Instance.SaveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, savePrefix + "-" + displayTime);
            }
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
