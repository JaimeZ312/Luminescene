using UnityEngine;

/// <summary>
/// A base class for drawing a window view
/// </summary>
public abstract class BaseWindowDrawer
{
    public Vector2 Size;

    public abstract void Draw(BuildWindowConfig config);

    public virtual void OnEnabled()
    {

    }
}