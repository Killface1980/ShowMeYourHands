using System;
using System.Globalization;
using UnityEngine;

namespace PawnAnimator;

internal class SaveableVector3
{
    private SaveableVector3(float x, float y, float z, float? angle)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.angle = angle ?? 0f;
    }

    public SaveableVector3(Vector3 vector3, float? number)
    {
        this.x = vector3.x;
        this.y = vector3.y;
        this.z = vector3.z;
        this.angle = number ?? 0f;
    }


    private float x { get; }

    private float y { get; }

    private float z { get; }
    private float angle { get; }

    public override string ToString()
    {
        return string.Format("({0:F3}, {1:F3}, {2:F3}, {3:F3})", new object[]
        {
            this.x,
            this.y,
            this.z,
            this.angle
        });
    }

    public static SaveableVector3 FromString(string Str)
    {
        Str = Str.TrimStart('(');
        Str = Str.TrimEnd(')');
        string[] array = Str.Split(',');
        CultureInfo invariantCulture = CultureInfo.InvariantCulture;
        float x = Convert.ToSingle(array[0], invariantCulture);
        float y = Convert.ToSingle(array[1], invariantCulture);
        float z = Convert.ToSingle(array[2], invariantCulture);
        if (array.Length > 2)
        {
            float angle = Convert.ToSingle(array[3], invariantCulture);
            return new SaveableVector3(x, y, z, angle);
        }
        else
        {
            return new SaveableVector3(x, y, z, 0f);
        }
    }

    public Vector3 ToVector3()
    {
        return new Vector3(this.x, this.y, this.z);
    }
    public float ToAngleFloat()
    {
        return this.angle;
    }
}