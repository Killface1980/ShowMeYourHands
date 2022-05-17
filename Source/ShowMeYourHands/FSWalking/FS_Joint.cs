using UnityEngine;

namespace FacialStuff.Animator;

public class FS_Joint
{
    public bool isMirrored = false;
    public string jointName;
    public Vector3 desiredPosition;
    public Vector3 currentPosition;
    public Vector2 allowedAngles;
    public Vector2 currentAngles;
    public float softness;
    public float jointLength;
    public Color color = Color.white;


}