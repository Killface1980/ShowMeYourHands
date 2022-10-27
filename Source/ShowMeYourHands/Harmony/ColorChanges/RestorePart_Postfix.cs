using HarmonyLib;
using Verse;

namespace PawnAnimator.Harmony.ColorChanges
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RestorePart))]
    internal class RestorePart_Postfix
    {
        public static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            Pawn pawn = ___pawn;
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