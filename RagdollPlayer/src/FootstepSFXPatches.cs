using HarmonyLib;

using Il2CppSLZ.Marrow.Audio;

namespace RagdollPlayer;

[HarmonyPatch(typeof(FootstepSFX))]
public static class FootstepSFXPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(FootstepSFX.PlayStep))]
    public static bool PlayStep(FootstepSFX __instance)
    {
        if (RagdollPlayerMod.DisableFootstep(__instance))
        {
            return false;
        }

        return true;
    }
}
