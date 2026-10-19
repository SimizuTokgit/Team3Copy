using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class MaterImageManager : MonoBehaviour
{
    [SerializeField] private List<MaterMask> materMasks = new List<MaterMask>(4);

    private MaterTextManager materTextManager;

    private Player player;

    private int MAX_SPEED_LIMIT;

    private int OneMaterMaxSpeed;

    void Start()
    {
        materTextManager = GetComponentInChildren<MaterTextManager>();
        player = FindFirstObjectByType<Player>();

        MAX_SPEED_LIMIT = player.PlayerStatus.GetMaxSpeedLimit();
        OneMaterMaxSpeed = MAX_SPEED_LIMIT / 4;
    }

    void Update()
    {
        var playerSpeed = player.Physics.GetVelocity().magnitude;

        if (playerSpeed != 0)
        {
            materTextManager.TextUpdate((int)playerSpeed, OneMaterMaxSpeed);

            float normalized = Mathf.InverseLerp(0, OneMaterMaxSpeed, playerSpeed % OneMaterMaxSpeed);


            materMasks[(int)playerSpeed / OneMaterMaxSpeed].SetFillAmount(normalized);

            if ((int)playerSpeed / OneMaterMaxSpeed >= 1) materMasks[(int)playerSpeed / OneMaterMaxSpeed - 1].IsOver();
        }
        
    }
}
