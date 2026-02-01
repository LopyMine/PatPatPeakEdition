using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PatPat.Patches
{
    [HarmonyPatch(typeof(Character))]
    internal class PatPatCharacterPatch
    {

        [HarmonyPatch("CanRegenStamina")]
        [HarmonyPrefix]
        private static bool NoRegenAfterPat(ref bool __result)
        {
            if (Time.time - PatPatPeakMod.lastPatTime < PatPatConfig.PatCooldown.Value)
            {
                __result = false;
                return false;
            }

            return true;
        }

    }
}