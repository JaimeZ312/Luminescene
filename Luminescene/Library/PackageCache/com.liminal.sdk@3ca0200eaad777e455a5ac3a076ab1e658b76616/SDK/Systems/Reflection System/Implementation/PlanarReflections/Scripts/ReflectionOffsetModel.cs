using UnityEngine;

public class ReflectionOffsetModel
{
    public float RX = 1.104f;
    public float RZ = -0.116f;
    public float LOffset = 1;

    [Header("Leave as is")]
    public float RY = 1;
    public float RW = 0;

    public bool UseL = false;
    public float LX;
    public float LZ;

    public ReflectionOffsetModel(float rX, float rZ, float lOffset)
    {
        RX = rX;
        RZ = rZ;
        LOffset = lOffset;

        UseL = false;
    }

    public ReflectionOffsetModel(float rX, float rZ, float lX, float lZ)
    {
        RX = rX;
        RZ = rZ;

        LX = lX;
        LZ = lZ;

        UseL = true;
    }
}