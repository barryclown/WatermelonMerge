using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject deathPanel;
    public GameObject askPanel;
    public GameObject searchPanel;

    [Header("Text")]
    public TextMeshProUGUI score;
    public TextMeshProUGUI eggText;
    public TextMeshProUGUI coinText;

    [Header("Canvas Groups")]
    public CanvasGroup uiCanvasGroup;
    public CanvasGroup deathPanelGroup;

    [Header("需要被隱藏/顯示的遊戲物件")]
    [Tooltip("例如：整個遊戲世界的 Root、敵人群組、場景物件等，之後不用每次 Find。")]
    [SerializeField] private List<GameObject> objectsToHide = new List<GameObject>();

    public static UIManager instance;

    // 實際在流程中有被關閉的物件（避免多次呼叫時重複塞）
    private List<GameObject> hiddenObjects = new List<GameObject>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void Start()
    {
        // 開場先跑搜尋動畫
        StartSearch();

        if (BossManager.instance != null)
        {
            UpdateEggUI(BossManager.instance.eggCount);
            UpdateCoinUI(BossManager.instance.coinCount);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Ask();
        }
    }

    // ====== UI 專用 API ======

    public void UpdateScoreUI(string totalDamage)
    {
        if (score != null)
            score.text = totalDamage;
    }

    public void UpdateEggUI(int value)
    {
        if (eggText != null)
            eggText.text = ":" + value;
    }

    public void UpdateCoinUI(int value)
    {
        if (coinText != null)
            coinText.text = ":" + value;
    }

    // ====== Ask / End 流程 ======

    public void Ask()
    {
        HideAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        askPanel.SetActive(true);

        CanvasGroup askPanelCanvasGroup = askPanel.GetComponent<CanvasGroup>();
        if (askPanelCanvasGroup != null)
        {
            askPanelCanvasGroup.interactable = true;
            askPanelCanvasGroup.blocksRaycasts = true;
        }
    }

    public void End()
    {
        StartCoroutine(EndCoroutine());
    }

    private IEnumerator EndCoroutine()
    {
        HideAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        Time.timeScale = 0;
        deathPanel.SetActive(true);

        CanvasGroup deathPanelCanvasGroup = deathPanel.GetComponent<CanvasGroup>();
        if (deathPanelCanvasGroup != null)
        {
            deathPanelCanvasGroup.interactable = true;
            deathPanelCanvasGroup.blocksRaycasts = true;
        }

        yield return null;
    }

    public void Yes()
    {
        StartCoroutine(YesCoroutine());
    }

    private IEnumerator YesCoroutine()
    {
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }

        foreach (GameObject obj in EndTrigger.instance.hasEnd)
        {
            Destroy(obj);
        }
        EndTrigger.instance.hasEnd.Clear();

        ShowAllNonUIObjects();
        askPanel.SetActive(false);
        Time.timeScale = 1;

        yield return null;
    }

    public void No()
    {
        StartCoroutine(NoCoroutine());
    }

    private IEnumerator NoCoroutine()
    {
        askPanel.SetActive(false);
        yield return StartCoroutine(EndCoroutine());
    }

    // ====== 顯示 / 隱藏場景物件（改過） ======

    public void ShowAllNonUIObjects()
    {
        foreach (GameObject obj in hiddenObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        hiddenObjects.Clear();
    }

    public void HideAllNonUIObjects()
    {
        hiddenObjects.Clear();

        // 只處理事先指定在 objectsToHide 裡面的物件
        foreach (GameObject obj in objectsToHide)
        {
            if (obj == null) continue;

            if (obj.activeSelf)
            {
                hiddenObjects.Add(obj);
                obj.SetActive(false);
            }
        }
    }

    // ====== 搜尋流程 ======

    public void StartSearch()
    {
        StartCoroutine(SearchCoroutine());
    }

    private IEnumerator SearchCoroutine()
    {
        HideAllNonUIObjects();
        BattleStageManager.instance.gameObject.SetActive(true);

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        if (searchPanel != null)
            searchPanel.SetActive(true);

        if (SearchBoss.instance != null)
            SearchBoss.instance.Anim();
        else
            Debug.LogError("UIManager: SearchBoss.instance is null!");

        Time.timeScale = 0;

        yield return new WaitForSecondsRealtime(5f);

        StopSearch();
    }

    public void StopSearch()
    {
        StartCoroutine(StopSearchCoroutine());
    }

    private IEnumerator StopSearchCoroutine()
    {
        if (searchPanel != null)
            searchPanel.SetActive(false);

        ShowAllNonUIObjects();

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.interactable = true;
            uiCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 1;

        yield return null;
    }
}
