using UnityEngine;

public class PlayerMagnetManager : MonoBehaviour
{
    private readonly float collectDistance = 2f;

    private Player player;

    //チュートリアルアイテムを何個か取ったらクリア
    private int GetItem = 5;
    //取った個数
    private int GetItemCount = 0;

    //清水 アイテムを取った瞬間に上乗せする速さ（少しずつ元に戻る）
    [SerializeField] private float ItemSpeedKick = 8f;


    private void Start()
    {
        player = GetComponentInParent<Player>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<Item>(out Item item))
        {
            // プレイヤー方向
            Vector3 targetPos = transform.position;

            // 距離
            float distance = Vector3.Distance(item.transform.position, targetPos);

            /*
             * 近いほど速く吸い寄せる
             */
            float speedMultiplier = Mathf.Lerp(2f, 0.3f, distance / GetComponent<SphereCollider>().radius);

            // 移動
            item.transform.position = Vector3.MoveTowards(
                item.transform.position,
                targetPos,
                player.PlayerStatus.MAGNET_POWER * speedMultiplier * Time.deltaTime
            );

            // 十分近づいたら取得
            if (distance <= collectDistance)
            {
                item.UpGrade(player.PlayerStatus);
                Instantiate(item.ItemEffect,player.transform);

                //清水 何を取ったか分かるように文字を出して、少しだけ加速させる
                GameHUD.Get().Popup(item.GetPopupText(player.PlayerStatus), item.PopupColor);
                player.MoveController.AddSpeed(ItemSpeedKick);

                if (player.tutorialManager != null)
                {
                    if (player.tutorialManager.tutorialCount == 2)
                    {
                        GetItemCount++;

                        if (GetItem > GetItemCount)
                        {
                            player.tutorialManager.ItemGetTextNumberChange(GetItemCount);
                        }
                        else
                            player.tutorialManager.ClearTutorial();

                    }
                }
            }
        }
    }
}