using UnityEngine;

namespace Malchin.Economy
{
    /// <summary>
    /// Put this on a GameObject in the Main scene.
    /// It loads the save file after HerdManager has initialized.
    /// Also saves automatically when the app pauses or quits.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Start()
        {
            SaveSystem.Load();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save();
        }

        void OnApplicationQuit()
        {
            SaveSystem.Save();
        }
    }
}
