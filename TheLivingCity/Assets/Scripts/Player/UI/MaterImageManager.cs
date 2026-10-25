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

            //清水 メーターの目盛りを超えたら全部満タン（今までは80以上で配列の外を読んでエラーになっていた）
            int index = (int)playerSpeed / OneMaterMaxSpeed;
            if (index >= materMasks.Count)
            {
                foreach (MaterMask mask in materMasks) mask.IsOver();
                return;
            }

            float normalized = Mathf.InverseLerp(0, OneMaterMaxSpeed, playerSpeed % OneMaterMaxSpeed);

            materMasks[index].SetFillAmount(normalized);

            if (index >= 1) materMasks[index - 1].IsOver();
        }
        
    }
}
