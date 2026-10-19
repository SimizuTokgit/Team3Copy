using NUnit.Framework.Internal.Builders;
using UnityEngine;

public class PressMachine : MonoBehaviour
{
    [SerializeField]
    private float MoveSpeed;

    //上で待機する時間
    [SerializeField]
    private float StayTime;
    public float stayTime;

    //下で待機する時間
    [SerializeField]
    private float DownStayTime;
    public float downstayTime;

    public enum MoveState
    {
        Up,
        Down,
        Down_Stay,
        Stay
    };
    public MoveState state;

    //現在のY座標
    private float PosY;

    //初期のY座標
    private float StartPosY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        state = MoveState.Stay;

        stayTime = 0.0f;

        PosY = transform.position.y;
        StartPosY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == MoveState.Up)
        {
            UpMove();

            if (transform.position.y >= StartPosY)
            {
                stayTime = 0.0f;
                state = MoveState.Stay;
            }
        }
        else if (state == MoveState.Down)
        {
            DownMove();

            if (transform.position.y <= 0.0f)
            {
                downstayTime = 0.0f;
                state = MoveState.Down_Stay;
            }
        }
        else if (state == MoveState.Down_Stay)
        {
            downstayTime += Time.deltaTime;

            if (downstayTime > DownStayTime)
            {
                state = MoveState.Up;
            }
        }
        else if (state == MoveState.Stay)
        {
            stayTime += Time.deltaTime;

            if (stayTime > StayTime)
            {
                state = MoveState.Down;
            }
        }
    }

    public void UpMove()
    {
        PosY += MoveSpeed * Time.deltaTime;

        transform.position = new Vector3(transform.position.x, PosY, transform.position.z);
    }

    public void DownMove()
    {
        PosY -= MoveSpeed * Time.deltaTime;

        transform.position = new Vector3(transform.position.x, PosY, transform.position.z);
    }
}
