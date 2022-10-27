using HarmonyLib;
using RimWorld;

namespace PawnAnimator.FSWalking.HairCut
{
    [HarmonyPatch(typeof(Pawn_StyleTracker), nameof(Pawn_StyleTracker.Notify_StyleItemChanged))]
    internal class Notify_StyleItemChanged_Postfix
    {
        public static void Postfix(Pawn_StyleTracker __instance)
        {
            ResolveApparelGraphics_Postfix.Postfix(__instance.pawn.Drawer.renderer.graphics);
        }
    }
}
