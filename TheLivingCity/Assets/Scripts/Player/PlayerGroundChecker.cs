using UnityEngine;

//柴田
//地面の判定用クラス
public class PlayerGroundChecker : MonoBehaviour
{
    public enum LandingRank
    {
        Perfect,
        Nice,
        Bad
    }

    private float _rayDistance = 1.1f;
    
    private float _forwardNormalRayDistance = 20f;
    private float _downNormalRayDistance = 2f;

    public bool IsGrounded()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y +1f, transform.position.z);

        //Debug.DrawRay(origin, Vector3.down * _rayDistance, Color.yellow);

        return Physics.Raycast(
            origin,
            Vector3.down,
            _rayDistance,
            LayerMask.GetMask("Ground")
        );
    }

    //少し前の法線ベクトルを取得
    public Vector3 GetForwardNormalVector()
    {
        float forwardWeight = 3f;

        Vector3 origin = new Vector3(transform.position.x + transform.forward.x * forwardWeight, transform.position.y + 10f, transform.position.z + transform.forward.z * forwardWeight);

        Vector3 dir = Vector3.down.normalized;   

        Ray ray = new Ray(origin,dir);

        //Debug.DrawRay(ray.origin,ray.direction * _forwardNormalRayDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, _forwardNormalRayDistance , LayerMask.GetMask("Ground")))
        {
            Vector3 normal = hit.normal;

             return normal;
        }

        return Vector3.zero;
    }

    //真下の法線ベクトルを取得
    public Vector3 GetDownNormalVector()
    {
        Vector3 origin = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);

        Vector3 dir = Vector3.down;   

        Ray ray = new Ray(origin, dir);

        //Debug.DrawRay(ray.origin, ray.direction * _downNormalRayDistance, Color.blue);

        if (Physics.Raycast(ray, out RaycastHit hit, _downNormalRayDistance, LayerMask.GetMask("Ground")))
        {
            Vector3 normal = hit.normal;

            return normal;
        }

        return Vector3.zero;
    }

    //いま下り坂かどうか
    public bool IsDownhill()
    {
        Vector3 normal = GetDownNormalVector();

        Vector3 downhillDir =
            Vector3.ProjectOnPlane(Vector3.down, normal).normalized;

        float dot =
            Vector3.Dot(transform.forward, downhillDir);

        return dot >= 0.4f;

    }


    public bool JumpAble()
    {
        return false;
    }

    private Vector3 smoothNormal;
    //地面に沿って走る
    public void MoveAlongGround(Vector3 normal,float smoothSpeed)
    {
        Vector3 rawNormal = normal;

        if( rawNormal != Vector3.zero )
        {
            smoothNormal = Vector3.Slerp(smoothNormal, rawNormal, 100 * Time.deltaTime);

            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, smoothNormal).normalized;

            Quaternion target = Quaternion.LookRotation(forward, smoothNormal);

            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothSpeed * Time.deltaTime);
        }
    }

    public LandingRank OnGroundBoost()
    {
        Vector3 normal = GetDownNormalVector();

        Vector3 downhillDir = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;

        float dot = Vector3.Dot(transform.forward, downhillDir);

        if (dot > 0.7f) return LandingRank.Perfect;

        else if (dot > 0.45) return LandingRank.Nice;

        else return LandingRank.Bad;
    }
}
