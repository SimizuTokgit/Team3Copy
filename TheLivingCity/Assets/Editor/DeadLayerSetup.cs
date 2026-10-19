using UnityEditor;
using UnityEngine;

//清水
//一撃死の判定に使う「Dead」レイヤーが無ければ、プロジェクトを開いたときに自動で足す
//（.unitypackage には ProjectSettings が入らないので、取り込んだ先でも動くように）
//PlayerMagnet（アイテムを吸う範囲）とは当たらないようにする。Obstacle と同じ扱い
[InitializeOnLoad]
public static class DeadLayerSetup
{
    private const string DeadLayer = "Dead";
    private const string MagnetLayer = "PlayerMagnet";

    static DeadLayerSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    private static void Setup()
    {
        if (LayerMask.NameToLayer(DeadLayer) < 0 && !AddLayer(DeadLayer)) return;

        int dead = LayerMask.NameToLayer(DeadLayer);
        int magnet = LayerMask.NameToLayer(MagnetLayer);
        if (dead >= 0 && magnet >= 0 && !Physics.GetIgnoreLayerCollision(dead, magnet))
        {
            Physics.IgnoreLayerCollision(dead, magnet, true);
        }
    }

    private static bool AddLayer(string name)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return false;

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        //8番より前は Unity の予約なので、空いている最初のユーザーレイヤーに入れる
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = name;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"レイヤー「{name}」を {i} 番に追加しました");
            return true;
        }

        Debug.LogWarning($"空いているレイヤーが無いので「{name}」を追加できませんでした");
        return false;
    }
}
