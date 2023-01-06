using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using UnityEngine.Assertions;

public class VideoBufferManager : MonoBehaviour
{
    #region Variables
    [SerializeField]
    private MasterVideoClass[] _videoRepo;
    public MasterVideoClass[] VideoRepo { get { return _videoRepo; } private set { _videoRepo = value; } }

    public string BundleName { get; private set; } = "asset_video";
    public string FolderTitle360 { get; private set; } = "360 Videos";
    public string FolderTitle2D { get; private set; } = "2D Videos";
    public string AssetPathHeader { get; private set; }
    
    public int RepoIndex { get; private set; }
    public int TempIndex { get; private set; }
    public int SceneIndex { get; set; } = 0;
    public int VideoIndex360 { get; set; } = 0;
    public int VideoIndex2D { get; set; } = 0;

    private bool Ready360 { get; set; }
    private bool Ready2D { get; set; }
    #endregion
    #region Primary Runtime
    private void Awake()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 90;

        AssetPathHeader = Application.installMode == ApplicationInstallMode.Editor ? $"D:\\Users\\Admin\\Desktop\\Clean 4H\\Assets\\{BundleName}\\" : $"{Application.persistentDataPath}/{BundleName}/";
    }

    private void Start()
    {
        foreach (MasterVideoClass upper in VideoRepo)
        {
            try
            {
                foreach (VideoClass360 lower360 in upper.videoList360)
                {
                    Assert.IsNotNull(lower360.VideoClip);
                    Assert.IsNotNull(lower360.ImageType);
                }
                foreach (VideoClass2D lower2D in upper.videoList2D)
                {
                    Assert.IsNotNull(lower2D.VideoClip);
                    Assert.IsNotNull(lower2D.OverlayVideoName);
                }
            }
            catch
            {
                Debug.LogError("Value evaluated to null: Ensure that all Video Player Buffer parameters are filled in");
            }
        }
        StartCoroutine(VideoPreparationRoutine());
    }
    #endregion
    #region Supporting Coroutines
    private IEnumerator VideoPreparationRoutine()
    {
        yield return new WaitForEndOfFrame();
        for (RepoIndex = 0; RepoIndex < VideoRepo.Length; RepoIndex++)
        {
            tempHandler($"Scene {RepoIndex}", gameObject.transform);
            createGameObjects(VideoRepo[RepoIndex].videoList360, FolderTitle360, (i) => { Ready360 = i; });
            createGameObjects(VideoRepo[RepoIndex].videoList2D, FolderTitle2D, (i) => { Ready2D = i; });
            yield return new WaitUntil(() => Ready360 && Ready2D);
        }
        StartCoroutine(PrimaryMenuController.loadScene("Menu"));
    }

    private IEnumerator configureVideos<RepoClass>(RepoClass[] classPath, string folderTitle, Action<bool> callback) where RepoClass : ClassProps
    {
        for (int localIndex = 0; localIndex < classPath.Length; localIndex++)
        {
            var tempObject = gameObject.transform.GetChild(RepoIndex).Find(folderTitle).GetChild(localIndex);
            var tempDir = classPath[localIndex];
            VideoPlayer tempPlayerPath = tempObject.GetComponent<VideoPlayer>();
            UnityWebRequest www;
            try
            {
                tempPlayerPath.url = $"{AssetPathHeader}Clips/{tempDir.VideoClip.name}.mp4";
                tempPlayerPath.targetTexture = new RenderTexture((int)tempDir.VideoClip.width, (int)tempDir.VideoClip.height, 32, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, -1);
            }
            catch
            {
                Debug.LogError("Issue getting at asset path");
            }
            if (tempDir.AudioClip != null)
            {
                tempPlayerPath.audioOutputMode = VideoAudioOutputMode.AudioSource;
                using (www = UnityWebRequestMultimedia.GetAudioClip($"{AssetPathHeader}Audio Clips/{tempDir.AudioClip.name}.wav", AudioType.WAV))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.ConnectionError)
                    {
                        Debug.LogError($"Issue retrieving audio clip: {www.error}");
                    }
                    else
                    {
                        tempObject.GetComponent<AudioSource>().clip = DownloadHandlerAudioClip.GetContent(www);
                    }
                }
            }
            else
            {
                tempPlayerPath.audioOutputMode = VideoAudioOutputMode.Direct;
            }
            tempObject.GetComponent<VideoPlayer>().Prepare();
            yield return new WaitUntil(() => tempObject.GetComponent<VideoPlayer>().isPrepared);
        }
        callback(true);
    }
    #endregion
    #region Supporting Methods
    private void tempHandler(string name, Transform transform)
    {
        GameObject temp = new GameObject();
        temp.transform.parent = transform;
        temp.name = name;
    }

    private void createGameObjects<RepoClass>(RepoClass[] classPath, string folderTitle, Action<bool> callback) where RepoClass : ClassProps
    {
        tempHandler(folderTitle, gameObject.transform.GetChild(RepoIndex));
        for (int localIndex = 0; localIndex < classPath.Length; localIndex++)
        {
            GameObject lowestTemp = new GameObject();
            lowestTemp.transform.parent = gameObject.transform.GetChild(RepoIndex).transform.Find(folderTitle);
            lowestTemp.name = classPath[localIndex].VideoClip.name;
            lowestTemp.AddComponent<VideoPlayer>();
            if (classPath[localIndex].AudioClip != null)
            {
                lowestTemp.AddComponent<AudioSource>();
            }
            componentInit(lowestTemp.GetComponent<VideoPlayer>());
        }
        StartCoroutine(configureVideos(classPath, folderTitle, (i) => { callback(i); }));
    }

    private void componentInit(VideoPlayer player)
    {
        player.playOnAwake = false;
        player.waitForFirstFrame = true;
        player.isLooping = false;
        player.skipOnDrop = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.source = VideoSource.Url;
        if (player.GetComponent<AudioSource>() != null)
        {
            var temp = player.gameObject.GetComponent<AudioSource>();
            temp.playOnAwake = false;
            temp.loop = false;
        }
    }
    #endregion
}
#region Serializable Classes and Interfaces
[Serializable]
public class VideoClass360 : ClassProps
{
    [Header("Required")]
    [SerializeField]
    private VideoClip _videoClip;
    public VideoClip VideoClip { get { return _videoClip; } private set { _videoClip = value; } }

    [SerializeField] private string _imageType;
    public string ImageType { get { return _imageType; } private set { _imageType = value; } }

    [Header("Optional")]
    [SerializeField]
    private AudioClip _audioClip;
    public AudioClip AudioClip { get { return _audioClip; } private set { _audioClip = value; } }

    [Tooltip("Panel which you want to follow the video if necessary for navigation")] [SerializeField]
    private GameObject _followingPanel;
    public GameObject FollowingPanel { get { return _followingPanel; } private set { _followingPanel = value; } }

    public long LastFrame { get; set; } = 0;
    public float Timestamp { get; set; } = 0;
}

[Serializable]
public class VideoClass2D : ClassProps
{
    [Header("Required")]
    [SerializeField]
    private VideoClip _videoClip;
    public VideoClip VideoClip { get { return _videoClip; } private set { _videoClip = value; } }

    [Tooltip("Name of 360 video of which this video is to overlay")] [SerializeField]
    private string _overlayVideoName;
    public string OverlayVideoName { get { return _overlayVideoName; } private set { _overlayVideoName = value; } }

    [Tooltip("Frame that this video appears on in the 360 video")] [SerializeField]
    private int _appearanceFrame;
    public int AppearanceFrame { get { return _appearanceFrame; } private set { _appearanceFrame = value; } }

    [Header("Optional")]
    [SerializeField]
    private AudioClip _audioClip;
    public AudioClip AudioClip { get { return _audioClip; } private set { _audioClip = value; } }

    public long LastFrame { get; set; } = 0;
    public float Timestamp { get; set; } = 0;
}

[Serializable]
public class MasterVideoClass
{
    public VideoClass360[] videoList360 { get; private set; }
    public VideoClass2D[] videoList2D { get; private set; }
}

interface ClassProps
{
    VideoClip VideoClip { get; }
    AudioClip AudioClip { get; }
    long LastFrame { get; }
    float Timestamp { get; }
}
#endregion
