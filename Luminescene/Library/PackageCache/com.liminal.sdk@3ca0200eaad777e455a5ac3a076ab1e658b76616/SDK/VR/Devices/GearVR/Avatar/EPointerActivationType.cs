/// <summary>
/// A selection of pointer activation type providing default pointer activation style and individual control.
/// </summary>
public enum EPointerActivationType
{
    /// <summary>
    /// Leaves the pointer activation to the developer.
    /// </summary>
    None, 
    /// <summary>
    /// The pointer activation is based on the last used controller.
    /// </summary>
    ActiveController, 
    /// <summary>
    /// Both controllers will always have a pointer.
    /// </summary>
    Both,
}
