using UnityEngine;

//柴田
public class PlayerPhysics : MonoBehaviour
{
    private Rigidbody _rigidBody;

    public void InitVelocity(Vector3 velocity)
    {
        _rigidBody = GetComponent<Rigidbody>();
        _rigidBody.linearVelocity = velocity;
    }

    public Vector3 GetVelocity()
    {
        var velocity = _rigidBody.linearVelocity;
        
        return velocity;
    }

    public void SetVelocity(Vector3 velocity)
    {
        _rigidBody.linearVelocity = velocity;
    }
}
