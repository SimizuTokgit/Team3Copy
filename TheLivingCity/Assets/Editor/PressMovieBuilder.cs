using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

// 絵コンテ「死亡Movie②挟まれてつぶされる」(PG-011) のシーンを丸ごと作る
// Unityのメニュー Tools > Movie > 死亡Movie②(プレス) のシーンを作る から実行する
//
// カメラは正面固定のまま、サイズだけ変える構成。全10カット・6.00秒。
// モデルは仮のCubeで置いてあるので、あとから実物に差し替える。
public static class PressMovieBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/Game/PressMovie.unity";
    private const string ASSET_DIR = "Assets/Animation/PressMovie";
    private const string TIMELINE_PATH = ASSET_DIR + "/PressMovie.playable";
    private const string PRESS_ANIM_PATH = "Assets/Import/Yoshiguti/Press/Press.anim";
    private const string SE_PRESS = "Assets/Sounds/SE/PressMachine.mp3";
    private const string SE_CRUSH = "Assets/Sounds/SE/PressMachineCrushed.mp3";

    private const float MOVIE_LENGTH = 6.0f;

    // 絵コンテのカット表。start と duration は秒
    private struct Cut
    {
        public string name;
        public float start;
        public float duration;
        public string camera;

        public Cut(string name, float start, float duration, string camera)
        {
            this.name = name; this.start = start; this.duration = duration; this.camera = camera;
        }
    }

    private static readonly Cut[] CUTS =
    {
        new Cut("C1 全景",      0.00f, 0.60f, "CM_Wide"),
        new Cut("C2 板アオリ",   0.60f, 0.70f, "CM_PlateLow"),
        new Cut("C3 支える",     1.30f, 0.90f, "CM_Bust"),
        new Cut("C4 顔アップ",   2.20f, 0.40f, "CM_Face"),
        new Cut("C5 止め",       2.60f, 0.15f, "CM_Wide"),
        new Cut("C6 崩落",       2.75f, 0.20f, "CM_Wide"),
        new Cut("C7 着弾",       2.95f, 0.15f, "CM_Wide"),
        new Cut("C8 沈黙",       3.10f, 1.10f, "CM_Wide"),
        new Cut("C9 板が上がる", 4.20f, 0.80f, "CM_Wide"),
        new Cut("C10 GAME OVER",5.00f, 1.00f, "CM_Wide"),
    };

    [MenuItem("Tools/Movie/死亡Movie②(プレス) のシーンを作る")]
    public static void Build()
    {
        if (System.IO.File.Exists(SCENE_PATH))
        {
            bool ok = EditorUtility.DisplayDialog(
                "確認",
                SCENE_PATH + " がすでにあります。\n作り直すと今の中身は消えます。続けますか？",
                "作り直す", "やめる");
            if (!ok) return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(ASSET_DIR);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- 見た目 ----------
        var light = new GameObject("Directional Light", typeof(Light));
        light.GetComponent<Light>().type = LightType.Directional;
        light.GetComponent<Light>().intensity = 1.0f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var mainCam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
        mainCam.tag = "MainCamera";
        mainCam.GetComponent<Camera>().backgroundColor = new Color(0.05f, 0.05f, 0.06f);
        mainCam.transform.position = new Vector3(0f, 1.2f, -7f);

        // ---------- セット（あとで実物に差し替える） ----------
        var set = new GameObject("Set");

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "床_仮";
        floor.transform.SetParent(set.transform, false);
        floor.transform.localScale = new Vector3(3f, 1f, 3f);

        // Press.anim は "ContainerLig03" というパスを動かすので、その名前の子を用意しておく
        var press = new GameObject("PressMachine_仮", typeof(Animator));
        press.transform.SetParent(set.transform, false);

        var pressRig = new GameObject("ContainerLig03");
        pressRig.transform.SetParent(press.transform, false);

        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "上板_仮";
        plate.transform.SetParent(pressRig.transform, false);
        plate.transform.localPosition = new Vector3(0f, 3.0f, 0f);
        plate.transform.localScale = new Vector3(4.0f, 0.5f, 3.0f);

        MakeCube("柱L_仮", press.transform, new Vector3(-2.3f, 1.8f, 0f), new Vector3(0.4f, 4.0f, 0.4f));
        MakeCube("柱R_仮", press.transform, new Vector3(2.3f, 1.8f, 0f), new Vector3(0.4f, 4.0f, 0.4f));
        MakeCube("受け台_仮", press.transform, new Vector3(0f, -0.15f, 0f), new Vector3(4.4f, 0.3f, 3.2f));

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player_仮";
        player.transform.SetParent(set.transform, false);
        player.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        // ---------- エフェクト（あとでParticleに差し替える） ----------
        var fx = new GameObject("FX");
        var dust = MakeCube("土煙_仮", fx.transform, new Vector3(0f, 0.4f, 0f), new Vector3(7.0f, 0.8f, 3.5f));
        dust.SetActive(false);

        // ---------- カメラ（絵コンテ通り 正面固定・サイズだけ変える） ----------
        var cams = new GameObject("MovieCams");
        var camTable = new Dictionary<string, CinemachineCamera>
        {
            // C1/C5〜C10 全身の引き。やや下から見上げて機械を大きく見せる
            { "CM_Wide",     MakeCam(cams.transform, "CM_Wide",     new Vector3(0f, 1.00f, -7.0f), new Vector3(0f, 1.80f, 0f), 50f) },
            // C2 床すれすれのアオリ。板が覆いかぶさってくる画
            { "CM_PlateLow", MakeCam(cams.transform, "CM_PlateLow", new Vector3(0f, 0.30f, -3.2f), new Vector3(0f, 2.60f, 0f), 60f) },
            // C3 バストアップ
            { "CM_Bust",     MakeCam(cams.transform, "CM_Bust",     new Vector3(0f, 1.55f, -2.2f), new Vector3(0f, 1.55f, 0f), 40f) },
            // C4 顔アップ
            { "CM_Face",     MakeCam(cams.transform, "CM_Face",     new Vector3(0f, 1.62f, -0.9f), new Vector3(0f, 1.62f, 0f), 32f) },
        };

        // ---------- UI（フェード・フラッシュ・GAME OVER） ----------
        var canvasGO = new GameObject("MovieCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var flash = FullScreenImage("Flash", canvas.transform, new Color(1f, 1f, 1f, 0f));
        flash.gameObject.AddComponent<Animator>();

        var fade = FullScreenImage("Fade", canvas.transform, new Color(0f, 0f, 0f, 1f));
        fade.gameObject.AddComponent<Animator>();

        var gameOverGO = new GameObject("GameOverText", typeof(RectTransform));
        gameOverGO.transform.SetParent(canvas.transform, false);
        var gameOverRT = (RectTransform)gameOverGO.transform;
        gameOverRT.anchorMin = new Vector2(0.5f, 0.5f);
        gameOverRT.anchorMax = new Vector2(0.5f, 0.5f);
        gameOverRT.sizeDelta = new Vector2(1400f, 300f);
        gameOverRT.anchoredPosition = Vector2.zero;
        var tmp = gameOverGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "GAME OVER";
        tmp.fontSize = 140f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 18f;
        gameOverGO.SetActive(false);

        // ---------- Director ----------
        var directorGO = new GameObject("CutSceneDirector",
            typeof(PlayableDirector), typeof(CinemachineImpulseSource), typeof(AudioSource), typeof(PressMovieShake));
        var director = directorGO.GetComponent<PlayableDirector>();
        var audioSource = directorGO.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // ---------- Timeline ----------
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, TIMELINE_PATH);
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = MOVIE_LENGTH;
        timeline.editorSettings.frameRate = 30f;

        // カメラトラック（1クリップ＝1カット。絵コンテの番号をそのままクリップ名にしている）
        var camTrack = timeline.CreateTrack<CinemachineTrack>(null, "カメラ");
        foreach (var cut in CUTS)
        {
            var clip = camTrack.CreateClip<CinemachineShot>();
            clip.start = cut.start;
            clip.duration = cut.duration;
            clip.displayName = cut.name;

            var shot = (CinemachineShot)clip.asset;
            var id = new PropertyName(System.Guid.NewGuid().ToString());
            shot.VirtualCamera.exposedName = id;
            director.SetReferenceValue(id, camTable[cut.camera]);
        }
        director.SetGenericBinding(camTrack, mainCam.GetComponent<CinemachineBrain>());

        // プレス機のアニメ
        var pressAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(PRESS_ANIM_PATH);
        if (pressAnim)
        {
            var pressTrack = timeline.CreateTrack<AnimationTrack>(null, "プレス機");
            var clip = pressTrack.CreateClip(pressAnim);
            clip.start = 0.0;
            clip.displayName = "Press";
            director.SetGenericBinding(pressTrack, press.GetComponent<Animator>());
        }
        else
        {
            Debug.LogWarning("Press.anim が見つかりませんでした: " + PRESS_ANIM_PATH);
        }

        // フェード（C1の明け / C9の暗転）
        var fadeIn = MakeAlphaClip("Fade_In", 1f, 0f, 0.90f);
        var fadeOut = MakeAlphaClip("Fade_Out", 0f, 1f, 0.80f);
        var fadeTrack = timeline.CreateTrack<AnimationTrack>(null, "フェード");
        var fadeInClip = fadeTrack.CreateClip(fadeIn);
        fadeInClip.start = 0.0; fadeInClip.displayName = "明ける";
        var fadeOutClip = fadeTrack.CreateClip(fadeOut);
        fadeOutClip.start = 4.20; fadeOutClip.displayName = "暗転";
        director.SetGenericBinding(fadeTrack, fade.GetComponent<Animator>());

        // 白フラッシュ（C6の崩落 / C7の着弾）
        var flashClip = MakeAlphaClip("Flash", 1f, 0f, 0.14f);
        var flashTrack = timeline.CreateTrack<AnimationTrack>(null, "白フラッシュ");
        var f1 = flashTrack.CreateClip(flashClip);
        f1.start = 2.75; f1.duration = 0.10; f1.displayName = "崩落";
        var f2 = flashTrack.CreateClip(flashClip);
        f2.start = 2.95; f2.duration = 0.14; f2.displayName = "着弾";
        director.SetGenericBinding(flashTrack, flash.GetComponent<Animator>());

        // 土煙（C7で出してC8で晴らす）
        var dustTrack = timeline.CreateTrack<ActivationTrack>(null, "土煙");
        var dustClip = dustTrack.CreateDefaultClip();
        dustClip.start = 2.95; dustClip.duration = 1.65; dustClip.displayName = "土煙";
        director.SetGenericBinding(dustTrack, dust);

        // GAME OVER（C10）
        var textTrack = timeline.CreateTrack<ActivationTrack>(null, "GAME OVER");
        var textClip = textTrack.CreateDefaultClip();
        textClip.start = 5.00; textClip.duration = 1.00; textClip.displayName = "GAME OVER";
        director.SetGenericBinding(textTrack, gameOverGO);

        // SE
        var seTrack = timeline.CreateTrack<AudioTrack>(null, "SE");
        AddAudio(seTrack, SE_PRESS, 0.60, "プレス機の駆動音");
        AddAudio(seTrack, SE_CRUSH, 2.95, "着弾");
        director.SetGenericBinding(seTrack, audioSource);

        director.playableAsset = timeline;
        director.playOnAwake = true;
        director.extrapolationMode = DirectorWrapMode.Hold;

        EditorUtility.SetDirty(timeline);
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log("死亡Movie②のシーンを作りました: " + SCENE_PATH + "\nTimeline: " + TIMELINE_PATH);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(SCENE_PATH));
    }

    // ---------- 小物 ----------

    private static CinemachineCamera MakeCam(Transform parent, string name, Vector3 pos, Vector3 lookAt, float fov)
    {
        var go = new GameObject(name, typeof(CinemachineCamera));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation((lookAt - pos).normalized, Vector3.up);

        var cam = go.GetComponent<CinemachineCamera>();
        var lens = cam.Lens;
        lens.FieldOfView = fov;
        cam.Lens = lens;
        return cam;
    }

    private static GameObject MakeCube(string name, Transform parent, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        return go;
    }

    private static Image FullScreenImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Imageのアルファだけを動かすクリップを作って保存する
    private static AnimationClip MakeAlphaClip(string name, float from, float to, float length)
    {
        string path = ASSET_DIR + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }

        var curve = new AnimationCurve(new Keyframe(0f, from), new Keyframe(length, to));
        clip.ClearCurves();
        clip.SetCurve("", typeof(Image), "m_Color.a", curve);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void AddAudio(AudioTrack track, string assetPath, double start, string label)
    {
        var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (!audio)
        {
            Debug.LogWarning("SEが見つかりませんでした: " + assetPath);
            return;
        }

        var clip = track.CreateClip(audio);
        clip.start = start;
        clip.displayName = label;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(folder);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
