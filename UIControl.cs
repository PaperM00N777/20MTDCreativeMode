using BepInEx;
using flanne;
using flanne.Player;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Final
{
    public class UIControl
    {


        public string mainBundlePath = Path.Combine(Paths.PluginPath, "CreativeMode/20mtdbundle");
        public AssetBundle mainBundle;

        private GameObject actionDisplayInstance = null;
        private Transform actionDisplayContent = null;
        private readonly List<GameObject> actionDisplayBoxes = new List<GameObject>();

        private GameObject displayBoxInstance = null;

        private String[] ButtonBoxNames = { "PowerUps", "Enemies", "Guns" };

        private const float ActionDisplayBoxDefaultWidth = 160f;
        private const float ActionDisplayBoxDefaultHeight = 30f;
        private const float ActionDisplayBoxSpacing = 4f;
        private const float ActionDisplayTextMaxFontSize = 10f;
        private const float ActionDisplayTextMinFontSize = 4f;
        private const float ActionDisplayTextHorizontalPadding = 4f;
        private const float ActionDisplayTextVerticalPadding = 2f;
        private const float ActionDisplayVisibleAreaScale = 0.95f;


        private Color PanelButtonColor;

        public void Start()
        {
            mainBundle = LoadBundleSafe(this.mainBundlePath);
            PanelButtonColor = ParseColor("#293448");
        }
        
        private AssetBundle LoadBundleSafe(string path)
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError("Failed to load asset bundle at " + path);
            }
            return bundle;
        }

        private GameObject LoadPrefab(AssetBundle bundle, string assetName)
        {
            if (bundle == null)
            {
                Main.Log.LogError("Asset bundle is null, cannot load '" + assetName + "'");
                return null;
            }

            GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
            if (prefab == null)
            {
                Main.Log.LogError("Could not load '" + assetName + "' from bundle!");
            }
            return prefab;
        }

        private Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private T FindComponent<T>(Transform root, string path) where T : Component
        {
            Transform t = root.Find(path);
            if (t == null)
            {
                Main.Log.LogError("Could not find '" + path + "' under '" + root.name + "'");
                return null;
            }

            T comp = t.GetComponent<T>();
            if (comp == null)
            {
                Main.Log.LogError("'" + path + "' has no " + typeof(T).Name + " component");
            }
            return comp;
        }

        private void StyleBackground(Transform uiRoot)
        {
            Image bg = FindComponent<Image>(uiRoot, "BG");
            if (bg == null) return;

            bg.sprite = Main.Instance.imageRipper.GenericBox.sprite;
            bg.type = Image.Type.Sliced;
        }

        private void StyleScrollbar(Transform uiRoot)
        {
            Image scrollBar = FindComponent<Image>(uiRoot, "Scrollbar");
            Image handle = FindComponent<Image>(uiRoot, "Scrollbar/Sliding Area/Handle");
            if (scrollBar == null || handle == null) return;

            scrollBar.sprite = Main.Instance.imageRipper.ScrollBar.sprite;
            scrollBar.color = Main.Instance.imageRipper.ScrollBar.color;
            handle.sprite = Main.Instance.imageRipper.ScrollHandle.sprite;
            handle.color = Main.Instance.imageRipper.ScrollHandle.color;
        }

        private Button StyleMenuButton(Transform uiRoot, string buttonName)
        {
            Button button = FindComponent<Button>(uiRoot, buttonName);
            if (button == null) return null;

            button.GetComponent<Image>().color = Main.Instance.imageRipper.ButtonBlue;

            TextMeshProUGUI text = FindComponent<TextMeshProUGUI>(button.transform, "Text");
            if (text != null)
            {
                text.font = Main.Instance.imageRipper.Lantern;
                text.color = Main.Instance.imageRipper.LanternRed;
            }
            return button;
        }

        private void ClearButtonPanels(Transform uiRoot)
        {
            foreach (Transform child in uiRoot)
            {
                if (ButtonBoxNames.Contains(child.gameObject.name))
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        // Shared builder for the powerup/enemy/gun/gun-evolution list buttons: box + label +
        // hover tint/tooltip + press feedback. Centralizing this avoids ~4 near-identical copies.
        private GameObject CreateTooltipButton(
            Transform parent,
            string objectName,
            string labelText,
            Color baseColor,
            UnityAction onClick,
            UnityAction onHoverShowTooltip)
        {
            GameObject btnObj = new GameObject(objectName, typeof(RectTransform), typeof(Button), typeof(Image));
            btnObj.transform.SetParent(parent, false);

            Image btnImage = btnObj.GetComponent<Image>();
            btnImage.color = baseColor;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            txt.text = labelText;
            txt.fontSize = 7;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = true;
            txt.overflowMode = TextOverflowModes.Truncate;
            txt.font = Main.Instance.imageRipper.Express;

            if (onClick != null)
            {
                btnObj.GetComponent<Button>().onClick.AddListener(onClick);
            }

            EventTrigger trigger = btnObj.AddComponent<EventTrigger>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
            {
                txt.color = Main.Instance.imageRipper.LanternRed;
                onHoverShowTooltip?.Invoke();
            });

            AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
            {
                txt.color = Color.white;
                UnsetDisplayBox();
            });

            AddTrigger(trigger, EventTriggerType.PointerDown, _ => btnImage.color = Color.black);
            AddTrigger(trigger, EventTriggerType.PointerUp, _ => btnImage.color = baseColor);

            return btnObj;
        }

        // ---------------------------------------------------------------
        // Main UI
        // ---------------------------------------------------------------

        public GameObject MakeMainUI(Transform parent)
        {
            GameObject uiPrefab = LoadPrefab(mainBundle, "MainUI");
            if (uiPrefab == null) return null;

            GameObject uiInstance = GameObject.Instantiate(uiPrefab, parent);

            Button lvUpButton = StyleMenuButton(uiInstance.transform, "LVUpButton");
            Button powerUpMenuLoaderButton = StyleMenuButton(uiInstance.transform, "PowerUpsButton");
            Button enemyMenuLoaderButton = StyleMenuButton(uiInstance.transform, "EnemiesButton");
            Button gunMenuLoaderButton = StyleMenuButton(uiInstance.transform, "GunsButton");

            if (lvUpButton == null || powerUpMenuLoaderButton == null ||
                enemyMenuLoaderButton == null || gunMenuLoaderButton == null)
            {
                Main.Log.LogError("MainUI is missing one or more menu buttons.");
                return uiInstance;
            }

            lvUpButton.onClick.AddListener(() =>
            {
                GameAction LVupAction = new GameAction("LVUP");
                LVupAction.action = () =>
                {
                    Patches.LVUP();
                };
                Main.Instance.actionManager.Actions.Add(LVupAction);
                SyncActionDisplay(Main.Instance.actionManager.Actions);
            });

            powerUpMenuLoaderButton.onClick.AddListener(() =>
            {
                ClearButtonPanels(uiInstance.transform);
                createPowerUpUI(uiInstance.transform, Patches.AllPowerups, Main.Instance.actionManager.Actions);
            });

            gunMenuLoaderButton.onClick.AddListener(() =>
            {
                ClearButtonPanels(uiInstance.transform);
                createGunUI(uiInstance.transform, Patches.Guns, Patches.GunEvolutions, Main.Instance.actionManager.Actions);
            });

            enemyMenuLoaderButton.onClick.AddListener(() =>
            {
                ClearButtonPanels(uiInstance.transform);
                Main.Instance._enemyUIInstance = createEnemyUI(uiInstance.transform, Patches.enemyPrefabs, (enemyName) =>
                {
                    Main.Instance.placementManager.StartPlacement(enemyName);

                    foreach (Transform child in uiInstance.transform) // this block could be moved into the func above
                    {
                        child.gameObject.SetActive(false);
                    }
                });
            });
            return uiInstance;
        }

        public GameObject CreateDisplayBox(Transform parent)
        {
            GameObject uiPrefab = LoadPrefab(mainBundle, "DisplayBox");
            if (uiPrefab == null) return null;

            GameObject uiInstance = GameObject.Instantiate(uiPrefab, parent);
            uiInstance.name = "DisplayBox";
            displayBoxInstance = uiInstance;

            StyleBackground(uiInstance.transform);
            return uiInstance;
        }

        // ---------------------------------------------------------------
        // Action display (queue of pending actions)
        // ---------------------------------------------------------------

        public GameObject CreateActionDisplayUI(Transform parent)
        {
            DestroyActionDisplayUI();

            GameObject uiPrefab = LoadPrefab(mainBundle, "ActionDisplay");
            if (uiPrefab == null) return null;

            actionDisplayInstance = GameObject.Instantiate(uiPrefab, parent);
            actionDisplayInstance.name = "ActionDisplayInjection";
            StyleBackground(actionDisplayInstance.transform);

            actionDisplayContent = actionDisplayInstance.transform.Find("Grid/Content");

            SyncActionDisplay(Main.Instance.actionManager.Actions);
            return actionDisplayInstance;
        }

        public void DestroyActionDisplayUI()
        {
            for (int i = actionDisplayBoxes.Count - 1; i >= 0; i--)
            {
                if (actionDisplayBoxes[i] != null)
                {
                    GameObject.Destroy(actionDisplayBoxes[i]);
                }
            }

            actionDisplayBoxes.Clear();
            actionDisplayContent = null;

            if (actionDisplayInstance != null)
            {
                GameObject.Destroy(actionDisplayInstance);
                actionDisplayInstance = null;
            }
        }

        public void SyncActionDisplay(List<GameAction> actions)
        {
            if (actionDisplayInstance == null || actionDisplayContent == null)
            {
                return;
            }

            List<string> actionNames = actions == null
                ? new List<string>()
                : actions.Select(a => a != null ? a.Name : "Unnamed Action").ToList();

            for (int i = actionDisplayBoxes.Count - 1; i >= 0; i--)
            {
                if (actionDisplayBoxes[i] == null)
                {
                    actionDisplayBoxes.RemoveAt(i);
                }
            }

            while (actionDisplayBoxes.Count > actionNames.Count)
            {
                int lastIndex = actionDisplayBoxes.Count - 1;

                if (actionDisplayBoxes[lastIndex] != null)
                {
                    GameObject.Destroy(actionDisplayBoxes[lastIndex]);
                }

                actionDisplayBoxes.RemoveAt(lastIndex);
            }

            for (int i = 0; i < actionNames.Count; i++)
            {
                if (i >= actionDisplayBoxes.Count)
                {
                    GameObject newBox = CreateActionDisplayBox(actionNames[i]);
                    newBox.GetComponent<Image>().color = Main.Instance.imageRipper.ButtonBlue;
                    newBox.GetComponentInChildren<TextMeshProUGUI>().font = Main.Instance.imageRipper.Express;
                    actionDisplayBoxes.Add(newBox);
                }
                else
                {
                    SetActionDisplayBoxText(actionDisplayBoxes[i], actionNames[i]);
                }
            }

            FitActionDisplayBoxesToContainer();
        }

        private GameObject CreateActionDisplayBox(string actionName)
        {
            GameObject boxObj = new GameObject(actionName + "_ActionDisplayBox", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement));
            boxObj.transform.SetParent(actionDisplayContent, false);

            RectTransform boxRt = boxObj.GetComponent<RectTransform>();
            boxRt.sizeDelta = new Vector2(ActionDisplayBoxDefaultWidth, ActionDisplayBoxDefaultHeight);

            boxObj.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(boxObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(ActionDisplayTextHorizontalPadding, ActionDisplayTextVerticalPadding);
            textRt.offsetMax = new Vector2(-ActionDisplayTextHorizontalPadding, -ActionDisplayTextVerticalPadding);

            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            ConfigureActionDisplayText(txt, new Vector2(ActionDisplayBoxDefaultWidth, ActionDisplayBoxDefaultHeight));

            SetActionDisplayBoxText(boxObj, actionName);

            return boxObj;
        }

        private void SetActionDisplayBoxText(GameObject boxObj, string actionName)
        {
            if (boxObj == null)
            {
                return;
            }

            boxObj.name = actionName + "_ActionDisplayBox";

            TextMeshProUGUI txt = boxObj.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.text = actionName;
                ConfigureActionDisplayText(txt, GetRectTransformSize(boxObj.GetComponent<RectTransform>()));
            }
        }

        private void FitActionDisplayBoxesToContainer()
        {
            if (actionDisplayContent == null || actionDisplayBoxes.Count == 0)
            {
                return;
            }

            RectTransform contentRt = actionDisplayContent as RectTransform;
            if (contentRt == null)
            {
                return;
            }

            PrepareActionDisplayContentRect(contentRt);

            Vector2 availableSize = GetBestAvailableActionDisplaySize(contentRt);

            GridLayoutGroup grid = actionDisplayContent.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                grid = actionDisplayContent.gameObject.AddComponent<GridLayoutGroup>();
            }

            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.spacing = new Vector2(0f, ActionDisplayBoxSpacing);

            int count = actionDisplayBoxes.Count;
            float innerWidth = Mathf.Max(1f, availableSize.x - grid.padding.left - grid.padding.right);
            float innerHeight = Mathf.Max(1f, availableSize.y - grid.padding.top - grid.padding.bottom);

            float cellWidth = innerWidth;

            float totalNormalHeight = (ActionDisplayBoxDefaultHeight * count) + (ActionDisplayBoxSpacing * Mathf.Max(0, count - 1));
            float cellHeight = ActionDisplayBoxDefaultHeight;

            if (totalNormalHeight > innerHeight)
            {
                cellHeight = (innerHeight - ActionDisplayBoxSpacing * Mathf.Max(0, count - 1)) / count;
            }

            cellHeight = Mathf.Max(1f, cellHeight);
            grid.cellSize = new Vector2(cellWidth, cellHeight);

            foreach (GameObject boxObj in actionDisplayBoxes)
            {
                if (boxObj == null)
                {
                    continue;
                }

                RectTransform boxRt = boxObj.GetComponent<RectTransform>();
                if (boxRt != null)
                {
                    boxRt.sizeDelta = grid.cellSize;
                }

                LayoutElement layoutElement = boxObj.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredWidth = grid.cellSize.x;
                    layoutElement.preferredHeight = grid.cellSize.y;
                    layoutElement.minWidth = 1f;
                    layoutElement.minHeight = 1f;
                }

                TextMeshProUGUI txt = boxObj.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    ConfigureActionDisplayText(txt, grid.cellSize);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        private void PrepareActionDisplayContentRect(RectTransform contentRt)
        {
            RectTransform parentRt = contentRt.parent as RectTransform;
            if (parentRt == null)
            {
                return;
            }

            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.pivot = new Vector2(0.5f, 1f);
        }

        private Vector2 GetBestAvailableActionDisplaySize(RectTransform contentRt)
        {
            RectTransform gridRt = contentRt.parent as RectTransform;
            Vector2 visibleGridSize = GetRectTransformSize(gridRt);

            if (visibleGridSize.x > 0f && visibleGridSize.y > 0f)
            {
                return visibleGridSize * ActionDisplayVisibleAreaScale;
            }

            Vector2 contentSize = GetRectTransformSize(contentRt);
            if (contentSize.x > 0f && contentSize.y > 0f)
            {
                return contentSize * ActionDisplayVisibleAreaScale;
            }

            return new Vector2(ActionDisplayBoxDefaultWidth, ActionDisplayBoxDefaultHeight);
        }

        private Vector2 GetRectTransformSize(RectTransform rt)
        {
            if (rt == null)
            {
                return Vector2.zero;
            }

            return new Vector2(Mathf.Abs(rt.rect.width), Mathf.Abs(rt.rect.height));
        }

        private void ConfigureActionDisplayText(TextMeshProUGUI txt, Vector2 boxSize)
        {
            if (txt == null)
            {
                return;
            }

            RectTransform textRt = txt.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(ActionDisplayTextHorizontalPadding, ActionDisplayTextVerticalPadding);
                textRt.offsetMax = new Vector2(-ActionDisplayTextHorizontalPadding, -ActionDisplayTextVerticalPadding);
            }

            float maxFontSizeForHeight = Mathf.Max(1f, boxSize.y - ActionDisplayTextVerticalPadding * 2f);
            float fontSizeMax = Mathf.Min(ActionDisplayTextMaxFontSize, maxFontSizeForHeight);

            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;
            txt.enableAutoSizing = true;
            txt.fontSizeMax = fontSizeMax;
            txt.fontSizeMin = Mathf.Min(ActionDisplayTextMinFontSize, fontSizeMax) + 10f;
            txt.fontSize = fontSizeMax;
            txt.overflowMode = TextOverflowModes.Ellipsis;
            txt.raycastTarget = false;
        }

        // ---------------------------------------------------------------
        // Powerup / Enemy / Gun panels
        // ---------------------------------------------------------------

        public GameObject createPowerUpUI(Transform parent, Powerup[] allpowerups, List<GameAction> actions)
        {
            GameObject uiPrefab = LoadPrefab(mainBundle, "PowerUps");
            if (uiPrefab == null) return null;

            GameObject uiInstance = GameObject.Instantiate(uiPrefab, parent);
            uiInstance.name = "PowerUps";
            StyleBackground(uiInstance.transform);

            Transform contentTransform = uiInstance.transform.Find("ScrollArea/Content");
            if (contentTransform == null)
            {
                Main.Log.LogError("Could not find ScrollArea/Content in PowerUps prefab!");
                return uiInstance;
            }

            if (actions == null)
            {
                Main.Log.LogError("actions list is null!");
                return uiInstance;
            }

            AddSearchBarFunctionality(contentTransform, "PowerUpsSearchBar");

            if (allpowerups != null)
            {
                var uniquePowerups = allpowerups.GroupBy(p => p.nameString).Select(g => g.First());

                foreach (var p in uniquePowerups)
                {
                    Powerup currentPowerup = p;

                    CreateTooltipButton(
                        contentTransform,
                        currentPowerup.nameString + "_Button",
                        currentPowerup.nameString,
                        PanelButtonColor,
                        onClick: () =>
                        {
                            GameAction powerUpAction = new GameAction("Add Power Up");
                            powerUpAction.action = () =>
                            {
                                currentPowerup.Apply(Patches.PlayerControllerPatch.Instance);
                            };
                            actions.Add(powerUpAction);
                            SyncActionDisplay(actions);
                        },
                        onHoverShowTooltip: () => SetDisplayBox(currentPowerup.description, currentPowerup.icon));
                }
            }

            StyleScrollbar(uiInstance.transform);
            return uiInstance;
        }

        public GameObject createEnemyUI(Transform parent, Dictionary<string, GameObject> enemyPrefabs, Action<string> onEnemySelected)
        {
            GameObject uiPrefab = LoadPrefab(mainBundle, "Enemies");
            if (uiPrefab == null) return null;

            GameObject uiInstance = GameObject.Instantiate(uiPrefab, parent);
            uiInstance.name = "Enemies";
            StyleBackground(uiInstance.transform);

            Transform contentTransform = uiInstance.transform.Find("ScrollArea/Content");
            if (contentTransform == null)
            {
                Main.Log.LogError("Could not find ScrollArea/Content in Enemies prefab!");
                return uiInstance;
            }

            if (enemyPrefabs != null)
            {
                foreach (var kvp in enemyPrefabs)
                {
                    string enemyName = kvp.Key;
                    GameObject enemyPrefab = kvp.Value;

                    CreateTooltipButton(
                        contentTransform,
                        enemyName + "_Button",
                        enemyName,
                        PanelButtonColor,
                        onClick: () =>
                        {
                            Main.Instance.screenLogger.LogToScreen("Press cntrl to close build mode", 1f, 1f);
                            onEnemySelected(enemyName);
                        },
                        onHoverShowTooltip: () => SetDisplayBox(enemyName, GetEnemyIcon(enemyPrefab)));
                }
            }

            StyleScrollbar(uiInstance.transform);
            AddSearchBarFunctionality(contentTransform, "EnemiesSearchBar");
            return uiInstance;
        }

        private Sprite GetEnemyIcon(GameObject enemyPrefab)
        {
            if (enemyPrefab == null) return null;

            SpriteRenderer sr = enemyPrefab.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        public GameObject createGunUI(Transform parent, GunData[] guns, GunEvolution[] gunEvolutions, List<GameAction> actions)
        {
            GameObject uiPrefab = LoadPrefab(mainBundle, "Guns");
            if (uiPrefab == null) return null;

            GameObject uiInstance = GameObject.Instantiate(uiPrefab, parent);
            uiInstance.name = "Guns";
            StyleBackground(uiInstance.transform);

            Transform contentTransform = uiInstance.transform.Find("ScrollArea/Content");
            if (contentTransform == null)
            {
                Main.Log.LogError("Could not find ScrollArea/Content in Guns prefab!");
                return uiInstance;
            }

            if (actions == null)
            {
                Main.Log.LogError("actions list is null!");
                return uiInstance;
            }

            AddSearchBarFunctionality(contentTransform, "GunsSearchBar");

            if (guns != null)
            {
                var uniqueGuns = guns.GroupBy(g => g.nameString).Select(g => g.First());
                foreach (var g in uniqueGuns)
                {
                    GunData currentGun = g;

                    CreateTooltipButton(
                        contentTransform,
                        currentGun.nameString + "_Button",
                        currentGun.nameString,
                        Main.Instance.imageRipper.ButtonBlue,
                        onClick: () =>
                        {
                            GameAction gunAction = new GameAction("Load Gun");
                            gunAction.action = () =>
                            {
                                Patches.GunPatch.Instance.LoadGun(currentGun);
                            };
                            actions.Add(gunAction);
                            SyncActionDisplay(actions);
                        },
                        onHoverShowTooltip: () => SetDisplayBox(currentGun.description, currentGun.icon));
                }
            }

            if (gunEvolutions != null)
            {
                var uniqueEvolutions = gunEvolutions.GroupBy(e => e.nameString).Select(e => e.First());
                foreach (var e in uniqueEvolutions)
                {
                    GunEvolution currentEvolution = e;
                    GunData evolutionGunData = Traverse.Create(currentEvolution).Field("gunData").GetValue<GunData>();

                    CreateTooltipButton(
                        contentTransform,
                        currentEvolution.nameString + "_Button",
                        currentEvolution.nameString,
                        Main.Instance.imageRipper.ButtonBlue,
                        onClick: () =>
                        {
                            GameAction evoAction = new GameAction("Load Gun");
                            evoAction.action = () =>
                            {
                                // Passing null here is intentional - LoadGun falls back to a default gun.
                                Patches.GunPatch.Instance.LoadGun(evolutionGunData);
                            };
                            actions.Add(evoAction);
                            SyncActionDisplay(actions);
                        },
                        onHoverShowTooltip: () =>
                        {
                            if (evolutionGunData != null)
                            {
                                SetDisplayBox("Gun evolution descriptions not avaliable (ask flanne)", GetEvolutionIcon(evolutionGunData));
                            }
                        });
                }
            }

            StyleScrollbar(uiInstance.transform);
            return uiInstance;
        }

        private Sprite GetEvolutionIcon(GunData evoGunData)
        {
            if (evoGunData == null || evoGunData.model == null) return null;

            Sprite gunSprite = null;

            foreach (SpriteRenderer sr in evoGunData.model.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name == "SwordSprite")
                {
                    return sr.sprite; // Sword takes priority.
                }

                if (sr.name == "GunSprite")
                {
                    gunSprite = sr.sprite; // Fallback if no sword found.
                }
            }

            return gunSprite;
        }

        private void AddSearchBarFunctionality(Transform contentTransform, String barName)
        {
            TMP_InputField searchBar = GameObject.Find(barName)?.GetComponentInChildren<TMP_InputField>(true);

            if (searchBar != null)
            {
                searchBar.onValueChanged.AddListener(search =>
                {
                    search = search.ToLower();

                    foreach (Transform child in contentTransform)
                    {
                        bool match = child.name.ToLower().Contains(search);
                        child.gameObject.SetActive(match);
                    }
                });
                searchBar.textComponent.font = Main.Instance.imageRipper.Express;
                searchBar.textComponent.color = Color.white;
            }
        }

        // ---------------------------------------------------------------
        // Display box (tooltip)
        // ---------------------------------------------------------------

        public void SetDisplayBox(string description, Sprite iconSprite)
        {
            if (displayBoxInstance == null)
            {
                Main.Log.LogError("DisplayBox is null.");
                return;
            }

            Image iconImage = FindComponent<Image>(displayBoxInstance.transform, "Icon");
            TextMeshProUGUI descriptionText = FindComponent<TextMeshProUGUI>(displayBoxInstance.transform, "Description");

            if (iconImage == null || descriptionText == null)
            {
                return;
            }

            iconImage.color = Color.white;
            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;

            descriptionText.text = description ?? string.Empty;
            descriptionText.font = Main.Instance.imageRipper.Express;
        }

        public void UnsetDisplayBox()
        {
            if (displayBoxInstance == null)
            {
                Main.Log.LogError("DisplayBox is null.");
                return;
            }

            Image iconImage = FindComponent<Image>(displayBoxInstance.transform, "Icon");
            TextMeshProUGUI descriptionText = FindComponent<TextMeshProUGUI>(displayBoxInstance.transform, "Description");

            if (iconImage == null || descriptionText == null)
            {
                return;
            }

            iconImage.sprite = null;
            iconImage.enabled = false;

            Color iconColor = iconImage.color;
            iconColor.a = 0f;
            iconImage.color = iconColor;

            descriptionText.text = string.Empty;
        }
    }
}