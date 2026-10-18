using UnityEngine;
using TMPro;

//安井
//プレイヤに関する情報管理
public class PlayerStatusTextManager : MonoBehaviour
{
    private Player Player;

    [SerializeField] private TextMeshProUGUI SPEED_TEXT;

    [SerializeField] private TextMeshProUGUI ACCEL_TEXT;
    [SerializeField] private TextMeshProUGUI MAX_SPEED_TEXT;

    void Start()
    {
        Player = FindAnyObjectByType<Player>();
    }

    void Update()
    {
        UpdateSpeedText();
    }

    /// <summary>
    /// プレイヤーの速度表示
    /// </summary>
    private void UpdateSpeedText()
    {
        //float nowSpeed = GroundChecker.MoveController._rigidbody.linearVelocity.magnitude * 3.6f;
        //SPEED_TEXT.text = $"{nowSpeed:F1} km/h";

        //ACCEL_TEXT.text = GroundChecker.MoveController._currentAccel.ToString() ;
        //MAX_SPEED_TEXT.text = GroundChecker.MoveController._currentMaxSpeed.ToString();
    }
}
