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
        public static Mod mod;
        private string displayTime = "";

        private void Start()
        {
            ModSettings settings = mod.GetSettings();
            saveInterval = settings.GetValue<int>("Section", "AutosaveInterval");
            savePrefix = settings.GetValue<string>("Section", "AutosavePrefix");
            InvokeRepeating("Autosave", 1.0f, saveInterval);
        }

        private void Autosave()
        {
            displayTime = System.DateTime.Now.ToString("HH:mm:ss");
            GameManager.Instance.SaveLoadManager.Save(GameManager.Instance.PlayerEntity.Name, savePrefix + "-" + displayTime);
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
