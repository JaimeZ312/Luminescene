using UnityEngine;

public class Racquet : MonoBehaviour
{
    public Transform Target;
    public Rigidbody RigidBody;

    public Vector3 RotationOffset;
    public float Offset = 0;

    public void FixedUpdate()
    {
        RigidBody.MoveRotation(Target.rotation * Quaternion.Euler(RotationOffset));
        RigidBody.MovePosition(Target.position + (Target.forward * Offset));
    }
}