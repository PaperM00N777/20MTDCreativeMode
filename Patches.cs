using flanne;
using flanne.Player;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Final
{
    public static class Patches
    {
        public static Dictionary<string, GameObject> enemyPrefabs = new Dictionary<string, GameObject>();

        public static GunEvolution[] GunEvolutions;
        public static GunData[] Guns;
        public static Powerup[] AllPowerups;

        public static void GatherResources()
        {
            GunEvolutions = Resources.FindObjectsOfTypeAll<GunEvolution>();
            Guns = Resources.FindObjectsOfTypeAll<GunData>();
            AllPowerups = Resources.FindObjectsOfTypeAll<Powerup>();
        }
        public static void CatalogEnemyPrefabs() // has to be separate from gather resources because running in the battle scene will gather current enemies
        {
            AIComponent[] allAI = Resources.FindObjectsOfTypeAll<AIComponent>();

            foreach (var ai in allAI)
            {
                if (!enemyPrefabs.ContainsKey(ai.gameObject.name))
                {
                    enemyPrefabs.Add(ai.gameObject.name, ai.gameObject);
                }
            }
        }
        public static float GetXPToNextLevel(PlayerXP PlayerXPInstance) // should probably combine this
        {
            var xpField = AccessTools.Field(typeof(PlayerXP), "xp");
            float xpValue = (float)xpField.GetValue(PlayerXPInstance);

            float num = (float)(PlayerXPInstance.level + 1);
            if (num < 20f) return (num * 10f - 5f) - xpValue + 0.1f;
            if (num < 40f) return (num * 13f - 6f) - xpValue + 0.1f;
            if (num < 60f) return (num * 16f - 8f) - xpValue + 0.1f;
            return ((num * num) - xpValue) + 0.1f;
        }

        public static void LVUP()
        {
            PlayerXP_Awake_Patch.Instance.GainXP(GetXPToNextLevel(PlayerXP_Awake_Patch.Instance));
        }

        [HarmonyPatch(typeof(PlayerXP), "Awake")]
        public static class PlayerXP_Awake_Patch
        {
            public static PlayerXP Instance { get; private set; }

            [HarmonyPostfix]
            static void Postfix(PlayerXP __instance)
            {
                Instance = __instance;
            }
        }
        [HarmonyPatch(typeof(PlayerBuffs))]
        public class PlayerBuffsPatch
        {

            public static PlayerBuffs Instance { get; private set; }

            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            static void StartPostfix(PlayerBuffs __instance)
            {
                Instance = __instance;


            }

            [HarmonyPatch("OnDestroy")]
            [HarmonyPrefix]
            static void OnDestroyPrefix(PlayerBuffs __instance)
            {
                if (Instance == __instance)
                {
                    Instance = null;

                }
            }
        }
        [HarmonyPatch(typeof(PlayerController))]
        public class PlayerControllerPatch
        {
            public static PlayerController Instance { get; private set; }
            [HarmonyPatch(typeof(PlayerController), "Awake")]
            [HarmonyPostfix]
            static void AwakePostfix(PlayerController __instance)
            {

                Instance = __instance;

            }
        }
        [HarmonyPatch(typeof(ReloadState))]
        public class ReloadState_Enter_Patch
        {
            [HarmonyPatch(nameof(ReloadState.Enter))]
            [HarmonyPrefix]
            static bool Prefix(ReloadState __instance)
            {
                if (Time.timeScale == 1f)
                {
                    return true; 
                }

                var ownerField = AccessTools.Field(typeof(PlayerState), "owner");
                object owner = ownerField.GetValue(__instance);

                var changeState = AccessTools.Method(owner.GetType(), "ChangeState")
                    .MakeGenericMethod(typeof(IdleState));
                changeState.Invoke(owner, null);

                return false; 
            }
        }
        [HarmonyPatch(typeof(Gun))]
        public class GunPatch
        {
            public static Gun Instance { get; private set; }

            [HarmonyPatch(nameof(Gun.LoadGun))]
            [HarmonyPrefix]
            static void LoadGunPrefix(Gun __instance)
            {
                Instance = __instance;
            }

            [HarmonyPatch(nameof(Gun.StartShooting))]
            [HarmonyPrefix]
            static bool StartShootingPrefix()
            {
                if (Time.timeScale == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}


