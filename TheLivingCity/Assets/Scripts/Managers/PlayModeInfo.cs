//清水
//今どのモードで遊んでいるかを覚えておく
//リトライでどのシーンに戻るか、ハイスコアをどこに保存するかに使う
public static class PlayModeInfo
{
    public enum Mode
    {
        Story,
        Endless
    }

    //GameManager の Awake で上書きされる
    public static Mode Current = Mode.Story;

    public static bool IsEndless => Current == Mode.Endless;

    //リトライ時に読み込むシーン名
    public static string GameSceneName => IsEndless ? "Main" : "StoryGameScene";

    //モードごとにハイスコアを分けて保存する
    public static string HighScoreKey => IsEndless ? "HighScore_Endless" : "HighScore_Story";
}
