using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    public static VideoManager instance;

    public GameObject video;          // 包住 VideoPlayer 的外層物件
    public VideoPlayer videoPlayer;

    private const float PLAY_TIME =2;   // 播放 3 秒
    private Coroutine stopRoutine;

    private void Awake()
    {
        instance = this;
       
    }

    private void Start()
    {
      
        PlayAndStopVideo();
    }

    public void PlayAndStopVideo()
    {
        StartCoroutine(PrepareAndPlay());
    }

    private IEnumerator PrepareAndPlay()
    {
        // 停掉上一輪的計時
        if (stopRoutine != null)
            StopCoroutine(stopRoutine);

        video.SetActive(true);

        // 如果還沒載好 → Prepare 並等待
        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);
        }

        // 從頭播放
        videoPlayer.time = 0;
        videoPlayer.Play();

        // 開始倒數 3 秒
        stopRoutine = StartCoroutine(StopAfterDelay(PLAY_TIME));
    }

    private IEnumerator StopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        videoPlayer.Stop();
        video.SetActive(false);
    }
}
