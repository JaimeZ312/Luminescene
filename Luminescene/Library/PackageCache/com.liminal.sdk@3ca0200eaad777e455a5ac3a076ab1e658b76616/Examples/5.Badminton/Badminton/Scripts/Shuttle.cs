using UnityEngine;

public class Shuttle : MonoBehaviour
{
    public Rigidbody Rigidbody;
    public Transform Cork;
    public float RotationForce = 1.5F;

    private void Awake()
    {
        //Rigidbody.centerOfMass = Cork.position;
    }

    private void FixedUpdate()
    {
        var dir = Rigidbody.velocity.normalized;
        Debug.DrawRay(Cork.position, dir * 10);

        var targetPosition = transform.position + dir;
        var targetRotation = Quaternion.LookRotation(dir);

        Rigidbody.rotation = Quaternion.Lerp(Rigidbody.rotation, targetRotation, RotationForce * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision c)
    {

    }
}