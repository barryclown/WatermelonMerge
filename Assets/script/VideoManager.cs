using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public static VideoManager instance;

    public GameObject video;          // 包住 VideoPlayer 的外層物件
    public VideoPlayer videoPlayer;

    public bool IsPlaying { get { return video != null && video.activeSelf; } }

    private void Awake()
    {
        instance = this;
        DisableLegacyVideo();
    }

    private void Start()
    {
        // 履歷展示版移除舊 Warning 影片過場；這個管理器只保留給場景引用不斷線。
    }

    public void PlayAndStopVideo()
    {
        DisableLegacyVideo();
    }

    private void DisableLegacyVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
        }

        if (video != null)
            video.SetActive(false);
    }
}
