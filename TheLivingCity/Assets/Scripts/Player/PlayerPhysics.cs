using UnityEngine;

//柴田
public class PlayerPhysics : MonoBehaviour
{
    private Rigidbody _rigidBody;

    //清水 速くても地面をすり抜けないように、当たり判定を連続で調べる（Discrete だと1回の計算で数m進んで地面を飛びこえていた）
    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        if (_rigidBody) _rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

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
