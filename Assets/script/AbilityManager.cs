using System.Collections.Generic;
using UnityEngine;

// roguelite 能力系統：持有「當前 run」抽到的能力，並提供效果查詢給其他系統。
// 自舉單例：第一次被存取時自動建立 GameObject，不需在場景手動掛載。
public enum AbilityId
{
    Heavy,        // 重擊：合成傷害 +25%（可疊）
    ChainMaster,  // 連鎖大師：連鎖每段額外 +20% 傷害
    Bomber,       // 炸彈手：每投放第 5 顆變炸彈，落地清小範圍
    MelonBurst,   // 寵物大爆發：合成出高級寵物時清周圍一圈並追加傷害
    Ceiling,      // 加高天花板：爆頂線上移
    Calm,         // 沉著：爆頂判定 +3s
    LuckyStart,   // 好運起手：投放偏高級果
    Sweep         // 大掃除：立即清掉場上所有 Lv1 果
}

public class AbilityManager : MonoBehaviour
{
    private static AbilityManager _instance;
    public static AbilityManager instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AbilityManager");
                _instance = go.AddComponent<AbilityManager>();
            }
            return _instance;
        }
    }

    // ===== 當前 run 的能力狀態（疊加） =====
    public int heavyStacks = 0;
    public int chainStacks = 0;
    public bool bomber = false;
    public bool melonBurst = false;
    public int ceilingStacks = 0;
    public int calmStacks = 0;
    public bool luckyStart = false;

    // ===== 效果查詢（給各系統呼叫） =====
    // 合成傷害總倍率
    public float DamageMultiplier => 1f + 0.25f * heavyStacks;
    // 連鎖每段加成（乘在傷害上：1 + perStep * comboStep）
    public float ChainBonusPerStep => 0.20f * chainStacks;
    // 爆頂判定秒數（基礎 5s + 沉著）
    public float OverflowSeconds => 5f + 3f * calmStacks;
    // 天花板上移總量（世界座標 y，給 EndTrigger 用）
    public float CeilingRaise => 0.6f * ceilingStacks;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    // 新 run 重置（Restart 重載場景時 static 會自然清掉；這裡保險用）
    public void ResetRun()
    {
        heavyStacks = 0;
        chainStacks = 0;
        bomber = false;
        melonBurst = false;
        ceilingStacks = 0;
        calmStacks = 0;
        luckyStart = false;
    }

    // ===== 抽牌 =====
    // 從能力池抽 3 張（不重複）。盤面擁擠時保證至少 1 張「清場/生存」類（橡皮筋防螺旋）。
    // 「加高天花板」(Ceiling) 已從抽牌池移除，不再提供給玩家抽。
    // enum / Apply / EndTrigger 的上移機制保留不動，避免牽動其他系統，只是永遠不會被抽到。
    private static readonly AbilityId[] AllAbilities =
    {
        AbilityId.Heavy, AbilityId.ChainMaster, AbilityId.Bomber, AbilityId.MelonBurst,
        AbilityId.Calm, AbilityId.LuckyStart, AbilityId.Sweep
    };
    private static readonly AbilityId[] SurvivalAbilities =
    {
        AbilityId.Bomber, AbilityId.MelonBurst, AbilityId.Sweep
    };

    // 盤面被視為「擁擠」的水果數門檻（初版，待試玩手調）
    public int crowdedFruitCount = 18;

    public List<AbilityId> OfferThree()
    {
        List<AbilityId> pool = new List<AbilityId>(AllAbilities);
        // Fisher-Yates
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        List<AbilityId> offer = pool.GetRange(0, 3);

        // 橡皮筋：盤面擁擠且三張都沒有生存類 → 強塞一張生存類
        int fruitCount = FindObjectsOfType<Watermelon>().Length;
        if (fruitCount >= crowdedFruitCount && !offer.Exists(a => System.Array.IndexOf(SurvivalAbilities, a) >= 0))
        {
            AbilityId survivor = SurvivalAbilities[Random.Range(0, SurvivalAbilities.Length)];
            offer[Random.Range(0, offer.Count)] = survivor;
        }

        return offer;
    }

    public void Apply(AbilityId id)
    {
        switch (id)
        {
            case AbilityId.Heavy: heavyStacks++; break;
            case AbilityId.ChainMaster: chainStacks++; break;
            case AbilityId.Bomber: bomber = true; break;
            case AbilityId.MelonBurst: melonBurst = true; break;
            case AbilityId.Ceiling: ceilingStacks++; ApplyCeiling(); break;
            case AbilityId.Calm: calmStacks++; break;
            case AbilityId.LuckyStart: luckyStart = true; break;
            case AbilityId.Sweep: DoSweep(); break;
        }
    }

    // 立即把爆頂線整體上移（EndTrigger 物件 y 提高）
    private void ApplyCeiling()
    {
        if (EndTrigger.instance != null)
        {
            // 以基準 y + 累計上移量重設（避免重複疊加時偏差）
            EndTrigger.instance.SetCeilingRaise(CeilingRaise);
        }
    }

    // 大掃除：清掉場上所有 Lv1 果
    public void DoSweep()
    {
        Watermelon[] all = FindObjectsOfType<Watermelon>();
        foreach (var w in all)
        {
            if (w != null && w.level == 1)
                Destroy(w.gameObject);
        }
    }

    // ===== UI 文案 =====
    public static string NameOf(AbilityId id)
    {
        switch (id)
        {
            case AbilityId.Heavy: return "重擊";
            case AbilityId.ChainMaster: return "連鎖大師";
            case AbilityId.Bomber: return "炸彈手";
            case AbilityId.MelonBurst: return "玩具大爆發";
            case AbilityId.Ceiling: return "加高天花板";
            case AbilityId.Calm: return "沉著";
            case AbilityId.LuckyStart: return "好運起手";
            case AbilityId.Sweep: return "大掃除";
            default: return id.ToString();
        }
    }

    public static string DescOf(AbilityId id)
    {
        switch (id)
        {
            case AbilityId.Heavy: return "合成傷害 +25%（可疊加）";
            case AbilityId.ChainMaster: return "連鎖合成每多一段，傷害再 +20%";
            case AbilityId.Bomber: return "每投放第 5 顆變炸彈，落地清掉周圍的玩具";
            case AbilityId.MelonBurst: return "合成出高級玩具（第 6 級以上）時，炸開周圍一圈並重擊頭目";
            case AbilityId.Ceiling: return "把爆頂線往上抬，給你更多空間";
            case AbilityId.Calm: return "爆頂倒數 +3 秒，更多救場時間";
            case AbilityId.LuckyStart: return "投放更容易出現高級玩具";
            case AbilityId.Sweep: return "立刻清掉場上所有最小的玩具";
            default: return "";
        }
    }
}
