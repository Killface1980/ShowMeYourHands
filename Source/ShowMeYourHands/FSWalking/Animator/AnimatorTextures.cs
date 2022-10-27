using UnityEngine;
using Verse;

namespace PawnAnimator.FSWalking.Animator
{
    [StaticConstructorOnStartup]
    public static class AnimatorTextures
    {
        public static readonly Texture2D BackgroundAnimTex = ContentFinder<Texture2D>.Get("UI/walkbg-01");
        public static readonly Texture2D BackgroundTex = ContentFinder<Texture2D>.Get("UI/gradient");

    }
}
