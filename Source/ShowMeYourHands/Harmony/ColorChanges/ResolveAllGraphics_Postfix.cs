using HarmonyLib;
using Verse;

namespace PawnAnimator.Harmony.ColorChanges
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveAllGraphics))]
    internal class ResolveAllGraphics_Postfix
    {
        public static void Postfix(PawnGraphicSet __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn == null)
            {
                return;
            }

            LongEventHandler.ExecuteWhenFinished(
                () =>
                {
                    pawn.GetCompAnim()?.pawnBodyGraphic?.Initialize();
                });

        }
    }
}