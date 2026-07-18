using UnityEngine;
using UnityEngine.InputSystem;

namespace Final
{
    public class PlacementManager
    {
        public bool PlacementMode { get; private set; } = false;

        private GameObject enemyUIInstance;
        private string pendingEnemyName = "";

        public void Update()
        {
            if (!PlacementMode)
                return;

            var keyboard = Keyboard.current;

            if (keyboard.leftCtrlKey.wasPressedThisFrame)
            {
                CancelPlacement();
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                PlaceEnemy();
            }
        }

        public void StartPlacement(string enemyName)
        {
            pendingEnemyName = enemyName;
            PlacementMode = true;
        }

        private void PlaceEnemy()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPos.x,
                    mouseScreenPos.y,
                    Camera.main.nearClipPlane
                )
            );

            worldPos.z = 0f;

            string selectedEnemyName = pendingEnemyName;

            GameObject previewObj = new GameObject(selectedEnemyName + "_PlacementPreview");
            previewObj.transform.position = worldPos;


            if (Patches.enemyPrefabs.TryGetValue(selectedEnemyName, out GameObject prefab))
            {
                SpriteRenderer prefabSr = prefab.GetComponentInChildren<SpriteRenderer>();

                if (prefabSr != null)
                {
                    SpriteRenderer sr = previewObj.AddComponent<SpriteRenderer>();

                    sr.sprite = prefabSr.sprite;
                    sr.sortingLayerID = prefabSr.sortingLayerID;
                    sr.sortingOrder = prefabSr.sortingOrder;
                    sr.color = new Color(1f, 1f, 1f, 0.5f);

                    previewObj.transform.localScale = prefab.transform.localScale;
                }
            }


            GameAction spawnEnemyAction = new GameAction("SpawnEnemy");

            spawnEnemyAction.action = () =>
            {
                if (Patches.enemyPrefabs.TryGetValue(selectedEnemyName, out GameObject enemyPrefab))
                {
                    UnityEngine.Object.Instantiate(
                        enemyPrefab,
                        worldPos,
                        Quaternion.identity
                    );
                }

                if (previewObj != null)
                {
                    UnityEngine.Object.Destroy(previewObj);
                }
            };

            Main.Instance.actionManager.AddAction(spawnEnemyAction);
        }


        public void CancelPlacement()
        {
            PlacementMode = false;
            pendingEnemyName = "";

            if (enemyUIInstance != null)
            {
                UnityEngine.Object.Destroy(enemyUIInstance);
                enemyUIInstance = null;
            }


            GameObject injection = GameObject.Find("MainUIInjection");

            if (injection != null)
            {
                foreach (Transform child in injection.transform)
                {
                    if (child.name != "EnemiesUI")
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
