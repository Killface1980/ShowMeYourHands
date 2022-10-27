using UnityEngine;
using Verse;

namespace PawnAnimator;

public class WhandCompProps : CompProperties
{
    public Vector3 MainHand = Vector3.zero;
    public Vector3 SecHand = Vector3.zero;

    public float MainHandAngle = 0f;
    public float SecHandAngle = 0f;


    public Vector3 WeaponPositionOffset = Vector3.zero;
    public Vector3 AimedWeaponPositionOffset = Vector3.zero;


    public WhandCompProps()
    {
        this.compClass = typeof(WhandComp);
    }
}