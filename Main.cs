using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StateLogger;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Final
{
    [BepInPlugin("com.yourname.myplugin", "Plugin", "1.0.0")]
    public class Main : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        
        public static Main Instance { get; private set; }

        private static readonly HashSet<string> PausingStateNames = new HashSet<string>
        {
            "PauseState",
            "PowerupMenuState",
            "DevilDealState",
            "GunEvoMenuState",
            "ChestState",
            "OptionsState",
            "SynergyUIState",
        };

        public bool placementMode = false;

        public List<GameObject> InjectedUIComponents = new List<GameObject>();

        public PlacementManager placementManager = new PlacementManager();
        public ActionManager actionManager = new ActionManager();
        public ImageRipper imageRipper = new ImageRipper();
        public ScreenLogger screenLogger = new ScreenLogger();
        public UIControl uiAssetLoader = new UIControl();

        public GameObject _enemyUIInstance = null;
        public GameObject canvasObj = null;

        public string pendingEnemyName = "";

        private bool firstFightStateDetected = false;

        void Awake()
        {
            Instance = this;

            Log = base.Logger;

            var harmony = new Harmony("com.yourname.myplugin");
            harmony.PatchAll();
            placementManager = new PlacementManager();
            Patches.CatalogEnemyPrefabs();

            uiAssetLoader.Start();
        }

        void Update()
        {
            var keyboard = Keyboard.current;

            if (SceneManager.GetActiveScene().name == "Battle" && !firstFightStateDetected)
            {
                FirstFightState();
                firstFightStateDetected = true;
            }
            if (SceneManager.GetActiveScene().name != "Battle")
            {
                firstFightStateDetected = false;
            }

            if (SceneManager.GetActiveScene().name == "Battle")
            {
                if (keyboard.tabKey.wasPressedThisFrame && !actionManager.ExecutingActions)
                {
                    GameObject potentialUI = GameObject.Find("MainUIInjection");

                    if (potentialUI == null)
                    {
                        GameObject Clone = uiAssetLoader.MakeMainUI(canvasObj.transform);
                        Clone.transform.SetAsFirstSibling();

                        if (Clone != null)
                        {
                            Clone.name = "MainUIInjection";
                        }
                    }
                    else
                    {
                        placementManager.CancelPlacement();
                        Destroy(potentialUI);
                        uiAssetLoader.UnsetDisplayBox();
                        actionManager.StartExecuting();
                    }
                }
            }
           
            placementManager.Update();
            TimeControl();
            actionManager.SyncDisplay();
        }

        void FixedUpdate()
        {
            actionManager.Tick();
        }

        private void FirstFightState()
        {
            canvasObj = GameObject.Find("Canvas");

            imageRipper.FightSceneRip();
            Patches.GatherResources();

            GameObject ActionDisplay = uiAssetLoader.CreateActionDisplayUI(canvasObj.transform);
            GameObject DisplayBox = uiAssetLoader.CreateDisplayBox(canvasObj.transform);
            ActionDisplay.transform.SetAsFirstSibling();
            DisplayBox.transform.SetAsFirstSibling();
            
        }
        private void TimeControl()
        {
            string currentStateName = GameStateMonitor.CurrentState?.GetType().Name;
            bool inPausingState = currentStateName != null && PausingStateNames.Contains(currentStateName);
            if (currentStateName == "TransitionToRetryState" && firstFightStateDetected == true)
            {
                firstFightStateDetected = false;
            }
            else
            {
                if (inPausingState || GameObject.Find("MainUIInjection") != null)
                {
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = 1f;
                }
            }

        }
    }
}

