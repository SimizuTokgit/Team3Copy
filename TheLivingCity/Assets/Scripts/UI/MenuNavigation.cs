using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//清水
//ボタンを キーボード（WASD・矢印）/ 左スティック / 十字キー で選べるようにする
//・ボタンどうしの移動先（Navigation）を並び順で決める（端まで行ったら反対側へ回る）
//・選択が外れたら（何もない所をクリックしたなど）、最後に選んでいたボタンを選び直す
//ゲームオーバーの Yes / No、ポーズ画面の Back / Quit で使う
public class MenuNavigation : MonoBehaviour
{
    public enum Direction
    {
        Horizontal, //左右に並んでいる（A / D、←→、スティック左右で動く）
        Vertical,   //上下に並んでいる（W / S、↑↓、スティック上下で動く）
    }

    [SerializeField] private Direction direction = Direction.Horizontal;

    //もう一方の向きでも動かすか。Yes / No は上下でも動いてよいが、
    //ポーズ画面は左右が音量バーと重なるので false にする
    [SerializeField] private bool useOtherAxis = true;

    [SerializeField] private Selectable firstSelected;

    private readonly List<Selectable> items = new List<Selectable>();
    private Selectable last;
    private bool selectPending = false;

    /// <summary>
    /// root の下のボタンをまとめて操作できるようにする（root が非アクティブでもよい。出たときに効く）
    /// list を渡さなければ root の下の Selectable を全部使う
    /// </summary>
    public static MenuNavigation Attach(GameObject root, Direction direction, bool useOtherAxis, Selectable first, IList<Selectable> list = null)
    {
        if (!root) return null;

        MenuNavigation nav = root.GetComponent<MenuNavigation>();
        if (!nav) nav = root.AddComponent<MenuNavigation>();

        nav.direction = direction;
        nav.useOtherAxis = useOtherAxis;
        if (first) nav.firstSelected = first;

        nav.items.Clear();
        if (list != null)
        {
            foreach (Selectable s in list) if (s) nav.items.Add(s);
        }

        if (root.activeInHierarchy) nav.Open();
        return nav;
    }

    private void OnEnable()
    {
        Open();
    }

    private void Open()
    {
        if (items.Count == 0) items.AddRange(GetComponentsInChildren<Selectable>(true));
        items.RemoveAll(s => !s);
        if (items.Count == 0) return;

        Link();

        //最初に選ぶボタン。指定がなければ EventSystem の First Selected（この中にあれば）、それもなければ先頭
        Selectable first = firstSelected;
        EventSystem es = EventSystem.current;
        if (!first && es && es.firstSelectedGameObject)
        {
            Selectable s = es.firstSelectedGameObject.GetComponent<Selectable>();
            if (s && items.Contains(s)) first = s;
        }
        if (!first) first = items[0];

        //選ぶのは次の Update で（出た瞬間は子のボタンの準備（Awake）がまだのことがあるため）
        last = first;
        selectPending = true;
    }

    /// <summary>
    /// 並び順（左から右 / 上から下）に、前後のボタンを移動先にする
    /// </summary>
    private void Link()
    {
        if (direction == Direction.Horizontal)
            items.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        else
            items.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        int n = items.Count;
        for (int i = 0; i < n; i++)
        {
            Selectable prev = items[(i - 1 + n) % n];
            Selectable next = items[(i + 1) % n];
            if (n == 1) prev = next = null;

            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
            if (direction == Direction.Horizontal)
            {
                nav.selectOnLeft = prev;
                nav.selectOnRight = next;
                if (useOtherAxis) { nav.selectOnUp = prev; nav.selectOnDown = next; }
            }
            else
            {
                nav.selectOnUp = prev;
                nav.selectOnDown = next;
                if (useOtherAxis) { nav.selectOnLeft = prev; nav.selectOnRight = next; }
            }
            items[i].navigation = nav;
        }
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (!es) return;

        //開いた直後は、決めておいたボタンを選ぶ
        if (selectPending)
        {
            selectPending = false;
            if (last && last.isActiveAndEnabled && last.interactable) Select(last);
            return;
        }

        GameObject current = es.currentSelectedGameObject;

        //今どれかを選んでいれば覚えておく
        if (current)
        {
            Selectable s = current.GetComponent<Selectable>();
            if (s && items.Contains(s)) last = s;

            //別の場所（音量バーなど）を選んでいるときは、そのままにする
            if (current.activeInHierarchy) return;
        }

        //何も選ばれていない → 最後のボタンを選び直す（パッドやキーボードで動かせなくならないように）
        if (last && last.isActiveAndEnabled && last.interactable) Select(last);
    }

    private void OnDisable()
    {
        //閉じたあとに、見えないボタンが選ばれたまま残らないようにする
        EventSystem es = EventSystem.current;
        if (!es || !es.currentSelectedGameObject) return;

        Selectable s = es.currentSelectedGameObject.GetComponent<Selectable>();
        if (s && items.Contains(s)) es.SetSelectedGameObject(null);
    }

    private static void Select(Selectable s)
    {
        EventSystem es = EventSystem.current;
        if (!s || !es) return;
        if (es.currentSelectedGameObject == s.gameObject) return; //もう選ばれている（ON画像は ButtonSpriteChanger が出す）

        es.SetSelectedGameObject(s.gameObject);
    }
}
