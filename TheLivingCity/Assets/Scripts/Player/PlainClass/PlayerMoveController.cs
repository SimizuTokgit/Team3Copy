using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

//柴田
//プレイヤーの速度を計算するためのクラス
public class PlayerMoveController 
{
    private readonly int NiceBoost = 5;         //Nice時のブースト定数
    private readonly int PerfectBoost = 10;

    private readonly float AngleLimit = 85.0f;

    private readonly float NormalGravity = -12f;
    private readonly float DiveGravity = -50f;

    private readonly float baseDecel = 0.70f; //70%失う

    private bool isDive = false;

    private float nowSpeed = 0;

    //チュートリアルに必要な変数
    private bool moveleft = false;
    private bool moveright = false;

    public void SetStartSpeed(PlayerStatus status)
    {
        nowSpeed = status.MAX_SPEED;
    }

    //地上の加速関数
    public void GroundAccel(PlayerStatus status)
    {
        if (nowSpeed < status.MAX_SPEED)
        {
            nowSpeed = Mathf.MoveTowards(nowSpeed, status.MAX_SPEED, status.ACCELERATOR_POWER * Time.deltaTime);
        }
    }

    //清水 一時的に速さを上乗せする（最高速度を超えた分は BoostSpeedCorrection で戻る）
    public void AddSpeed(float amount)
    {
        nowSpeed += amount;
    }

    //ブーストやバグの速度修正処理
    public void BoostSpeedCorrection(PlayerStatus status)
    {
        if (nowSpeed > status.MAX_SPEED)
        {
            nowSpeed = Mathf.MoveTowards(nowSpeed, status.MAX_SPEED, 10 * Time.deltaTime);
        }
    }

    //地上の前進関数
    public Vector3 GroundMoveForward(Vector3 forward)
    {
        Vector3 velocity = forward.normalized * nowSpeed;

        return velocity;
    }

    public void SetIsDive(bool Dive)
    {
        isDive = Dive;
    }

    //空中の前進関数
    public Vector3 AirMoveForward(Vector3 currentVelocity, Vector3 forward)
    {
        Vector3 velocity = currentVelocity;


        if (isDive)
        {
            velocity.y += DiveGravity * Time.deltaTime;
        }
        else velocity.y += NormalGravity * Time.deltaTime;


        Vector3 move = forward.normalized * nowSpeed;

        velocity.x = move.x;
        velocity.z = move.z;

        return velocity;
    }

    //入力に応じて回転する関数
    public void Turn(InputInfo input, Transform transform, PlayerStatus status, TutorialManager manager)
    {
        if (input.Move.x > 0.2f || input.Move.x < -0.2f)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(transform.localEulerAngles.x, input.Move.x * AngleLimit, transform.localEulerAngles.z), (status.TURNING_POWER / 8) * Time.deltaTime);

        //チュートリアルのクリア判定
        if (manager != null)
        {
            if (input.Move.x < -0.2f)
            {
                moveleft = true;
            }
            if (input.Move.x > 0.2f)
            {
                moveright = true;
            }

            if (moveleft & moveright & manager.tutorialCount == 0)
            {
                //チュートリアル1 クリア
                manager.ClearTutorial();
            }
        }
    }

    //減速処理
    public Vector3 Deceleration(Vector3 currentVelocity, PlayerStatus status)
    {
        float lossPercent = baseDecel / (1f + status.PERSEVERANCE_POWER * 0.1f);

        var Velocity = currentVelocity * (1f - lossPercent);

        nowSpeed *= 1f - lossPercent;

        return Velocity;
    }


    private readonly float jumpPower = 2f;
    private readonly float speedInfluence = 0.2f;
    public Vector3 Jump(Vector3 currentVelocity,Transform transform)
    {
        float forwardSpeed = Vector3.Dot(currentVelocity, transform.forward);

        float extraJump = Mathf.Max(0f, forwardSpeed) * speedInfluence;

        Vector3 jumpVelocity = Vector3.up * (jumpPower + extraJump);

        return new Vector3(currentVelocity.x, 0f, currentVelocity.z) + jumpVelocity;
    }

    public Vector3 LandingBoost(Vector3 currentVelocity, Transform transform, PlayerGroundChecker.LandingRank rank, PlayerStatus status, TutorialManager manager)
    {
        var velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        if (rank == PlayerGroundChecker.LandingRank.Bad)
        {
            return velocity;
        }
        else if (rank == PlayerGroundChecker.LandingRank.Nice)
        {
            nowSpeed += status.BOOST_POWER + NiceBoost ;
        }
        else if (rank == PlayerGroundChecker.LandingRank.Perfect)
        {
            nowSpeed += status.BOOST_POWER + PerfectBoost;

            if (manager != null)
            {
                if (manager.tutorialCount == 1)
                    manager.ClearTutorial();
            }
        }

        return velocity;
    }
}
