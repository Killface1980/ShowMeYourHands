using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnAnimator.FSWalking.Animator
{
    [HarmonyPatch(typeof(Prefs), nameof(Prefs.Save))]
    class DrawAt_Patch
    {
        static void Postfix()
        {
            MainButtonDef button = DefDatabase<MainButtonDef>.GetNamedSilentFail("WalkAnimator");
            //   MainButtonDef button2 = DefDatabase<MainButtonDef>.GetNamedSilentFail("PoseAnimator");
            if (button != null)
            {
                button.buttonVisible = Prefs.DevMode;
            }
        }


    }
}
