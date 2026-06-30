# Unity 套件 / 場景 / 美術 設定詳解（CLAUDE-setup.md）

此檔**不自動載入**，平常不佔 context token。對應 `CLAUDE.md` 中標 `↳S` 的章節（十一套件、十二場景、十三美術）。
要裝套件、設場景、做美術時才 Read 它。所有「設數值、掛腳本、改 import」凡涉及 `.unity/.prefab/.asset/.meta`（YAML）一律由 Claude 列步驟、barry 在編輯器手動完成（見 `CLAUDE.md` 三）。

---

## 一、🧩 套件清單（該裝什麼工具）

原則：**只把「幾乎每個 2D 專案都會用到」的放進預載核心；其餘依遊戲類型才加**，不要一律預載一堆用不到的。

### 怎麼裝
- **首選**：`Window > Package Manager`，左上 `+` →「Add package by name」輸入套件 id（如 `com.unity.cinemachine`）。
- **批次**：直接編 `Packages/manifest.json` 的 `dependencies`，存檔後 Unity 自動重新解析。版本號別亂填，沒把握就用 Package Manager 選相容版。
- 改完務必讓 Unity 完成 import／編譯再繼續。

### 預載核心（每個 2D 專案開案就先裝）
| 用途 | 套件 id | 說明 |
|---|---|---|
| 輸入 | `com.unity.inputsystem` | 新版 Input System，預設輸入方案（取代舊 Input Manager） |
| 文字 | `com.unity.textmeshpro` | 幾乎每個遊戲都有文字；首次用會提示 Import TMP Essentials |
| 渲染管線 | `com.unity.render-pipelines.universal` | URP，2D 預設管線；2D Renderer 與 2D 燈光（`Light2D`）的前提 |
| 2D 基礎 | `com.unity.feature.2d`（2D Feature Set） | Sprite/Tilemap/Animation 等；用 Unity 2D 範本建專案時通常已內含 |

> Test Framework（`com.unity.test-framework`）新版 Unity 預設已裝，一般不用另列；缺了再補（配合 `CLAUDE.md` 十之4 測試）。

### 選配（該類型才加，不要一律預載）
| 套件 id | 何時才加 |
|---|---|
| `com.unity.cinemachine` | 要相機跟隨／邊界／震動（平台、動作、橫向捲軸）；單畫面遊戲（合成、三消）用不到 |
| `com.unity.2d.pixel-perfect` | 只有像素美術遊戲；配合 Pixel Perfect Camera |
| DoTween（Asset Store／OpenUPM `com.demigiant.dotween`） | UI 動效/轉場 tween（美術總監 UI 段核准）；輕量，比硬刻 Animator 省事 |
| `com.unity.addressables` | 中大型專案的資源／記憶體管理 |
| `com.unity.localization` | 要做多語（中英）時才裝 |

### 不建議隨手裝
- Odin Inspector、各式第三方大型 framework：先說清楚「要解決什麼問題、原生有沒有替代」，barry 認可再裝。能用 `event` + `Coroutine`/`Awaitable` 解決的就別引大依賴。（DoTween 已核准，限 UI 動效用。）

---

## 二、🗺️ 場景設定（場景怎麼弄）

目標：給一套**簡單夠用**的標準場景與命名規範，不過度設計。小品/原型照這節就好；龐大的常駐管理器架構放最後「進階」當選配。

### 我能做 / 不能做
- **能**：寫管理器/載入器腳本、給出「場景要放哪些物件」的清單、列出 barry 在 Inspector 要拉什麼引用/設什麼值的步驟。
- **不能**：直接編輯 `.unity`（YAML，會壞 GUID）。場景搭建＝我交腳本＋步驟，barry 在編輯器落地。

### 標準場景（小品/原型的預設集）
| 場景 | 用途 | 必要性 |
|---|---|---|
| `MainMenu` | 主菜單：開始 / 設定 / 離開 | 預設 |
| `Game` | 主遊戲（單場景遊戲就全放這） | 預設 |
| `GameOver` / `Result` | 死亡 / 結算介面 | 需要才加 |
| `Level_01`、`Level_02`… | 多關卡時一關一場景 | 多關才加 |

> 最省做法：只要 `MainMenu` + `Game` 兩個場景。

### 命名規範
- 場景檔一律 **PascalCase**：`MainMenu`、`Game`、`GameOver`。
- 多關卡：`Level_` + 兩位數，如 `Level_01`、`Level_12`（補零才排得整齊）。
- 一個專案內規則統一，別混 `mainmenu` / `Main_Menu` / `scene1` 這種。
- 每個新場景記得加進 `File > Build Settings > Scenes In Build`，並排好 index（通常 `MainMenu` 在最前）。

### 2D URP 最低要求（漏了會出錯）
- **相機**：Projection = `Orthographic`；Renderer 指到專案的 **2D Renderer (URP)** asset。
- **燈光**：場景至少一個 `Light2D`（Type = Global），否則開 URP 後畫面全黑。
- **音訊/UI**：全場景只留**一個** AudioListener（通常掛 Main Camera）、一個 `Canvas` + 一個 `EventSystem`。

### 進階（中大型才用，不是預設規範）
- **Bootstrap 常駐場景**：`_Bootstrap`（Build index 0，底線前綴排最上）只放跨關卡管理器（`ServiceLocator`/`AudioManager`/`ObjectPoolManager`），用 `Core/SceneLoader` 以 **Additive** 載入關卡、管理器常駐不重建。
- **階層分組**：用空物件當資料夾（`---Systems---`/`---Actors---`/`---UI---`…），不掛邏輯、Transform 歸零。
- 小品/原型不需要這套；直接 `MainMenu` + `Game` 最省。

---

## 三、🎨 美術總監（缺圖我來 ＋ 視覺顧問）

以 **2D 為主軸**；條目標 `[3D]` 者為 3D 專案才用。

### 角色與兩種模式
- **生成（執行）**：barry 說「缺圖你弄／美術你包」→ 全流程自走：定風格 → 生圖 → 自評重生 → 後製 → 放進專案 → 做完叫 barry 試玩（不必截圖回報，他自己進遊戲看）。產物是 PNG，我能寫。
- **顧問（視覺總監）**：光影/材質/Shader/UI/構圖的具體參數、步驟、除錯清單由我出，**barry 在 Inspector 落地**——因為這些都寫在 `.unity/.mat/.asset/.meta`（YAML），照 `CLAUDE.md` 三我不能寫。我給指令，他是手。

### Output Rules（顧問模式鐵律）
1. **拒絕空泛**：禁止「讓畫面更有層次」「加強對比」這種廢話。一律給 Unity 內具體操作步驟、組件名稱、建議參數值（例：Post-Processing 的 Bloom Intensity 設 1.2、Threshold 設 0.9）。
2. **效能意識**：提任何華麗方案都要主動評估 Draw Call / GPU 負擔並附優化替代；撞上 `CLAUDE.md` 七（零 GC）/架構硬規則時 **硬規則優先**，美術在效能預算內落地。
3. **視覺除錯**：barry 說畫面「灰灰的/很廉價/排版很怪」時，像醫生一樣直接列最可能的 3–5 個 Unity 設定失誤引導排查（如 Color Space 沒設 Linear、Sprite 的 sRGB/壓縮設錯、場景沒掛 Post-Processing Volume 或沒設 Global、Canvas Scaler 模式/Reference Resolution 錯、像素圖 Filter 不是 Point 糊掉）。
4. **術語精確**：用 Unity 官方組件名與美術專名，確保 barry 能直接在 Inspector 找到對應功能。

### 風格基準（style bible，開案先定、全專案共用）
開新專案或第一張圖前先定下並全程沿用，新資產一律對齊，**禁混風格**：美術風格（像素/手繪/扁平/卡通）、基準解析度與 **PPU**、色板（主/輔/中性）、外框（黑邊粗細/內描邊）、光源方向與視角。

### 生成模式：生圖 → 後製 → 整合
- **生圖**：簡單圖示我直接出（SVG/程式生圖）；正式美術走既有工具（見記憶 `ai-image-gen-project`、`toolchain-reference`）——`Desktop\claude\ai-images\` 的 `gemini_web.py` 操控已登入 Gemini 網頁出圖，或 `gen_image.py`／API 備案。出圖後**自評**，不合格就重生。
- **後製**：去背透明 PNG、裁切、統一尺寸、對齊 **pivot**（角色常 Bottom/Bottom-Center）；像素圖避免抗鋸齒糊邊。
- **整合**：放 `Assets/Art/Sprites/{Characters,Environment,UI,FX}`；動畫幀連號 `run_00`、`run_01`…。
- **Sprite import**（寫在 `.meta`＝YAML，我不能改 → 我列清單，barry 在 Inspector 套或設專案 Preset）：Texture Type=`Sprite (2D and UI)`、Sprite Mode=Single/Multiple、Pixels Per Unit 對齊風格、像素遊戲 Filter Mode=`Point` + Compression=`None` + 關 Mip Maps、Pivot 統一、多圖用 **Sprite Atlas** 省 draw call。
- **鐵則**：**只新增不覆蓋**既有資產，要替換先提案（見 `game-art-full-autonomy`、`redo-preserve-content-propose-changes`）；美術檔二進位、其 `.meta` 是 YAML，受 `CLAUDE.md` 二、三防護：我不讀圖檔本體、不寫任何 `.meta`。

### 顧問模式：四大領域職責（依 ROI 排序）
優先級＝2D 獨立遊戲＋作品集導向的視覺 CP 值：**UI/排版 ＞ 光影/Post ＞ 構圖/鏡頭 ＞ 材質/Shader**。

**① UI/UX & 字體排版（最高 ROI，每個遊戲都吃）**
- 精準掌握 Canvas 層級、Anchor / Pivot；Canvas Scaler 用 `Scale With Screen Size` + 固定 Reference Resolution。
- 字體學：依風格推薦字體，給具體字號、行距、字距（Character Spacing）、對比度建議。
- 留白（Margin/Padding）的呼吸感與易用性。
- 動態 UI 轉場：用 **DoTween**（已列核准選配）或 Animator，確保回饋順暢、符合視覺直覺。

**② 光影 & Post-Processing（CP 值最高的視覺升級）**
- Post-Processing Volume：Bloom、Color Grading、Tonemapping、Vignette 定情緒色調，每項給具體參數。
- 2D 即時光：`Light2D`（Global + 點/聚光）；sprite 用 normal/mask map 增立體感。
- `[3D]` Lightmap 烘焙 / Global Illumination / 景深 DoF —— 2D 改用即時 `Light2D`，不烘焙。
- 效能：每個 Post effect 都吃 GPU；低階機關 Bloom 或降 Volume，先講負擔再加。

**③ 構圖 & 鏡頭（有移動鏡頭才需要）**
- Cinemachine 2D：Framing Transposer 跟隨、Confiner2D 邊界、Noise 震動、Blend 轉場。
- `[3D]` 焦距 Focal Length、景深、攝影機軌跡強化張力；2D 正交相機改調 Orthographic Size。
- 合成/三消等單畫面遊戲通常不需要這塊。

**④ 材質 & Shader（2D 最少用，特殊 FX 才出手）**
- Shader Graph 做溶解、流光、水體、卡通描邊等 2D 特效。
- `[3D]` 完整 PBR：Albedo/Normal/Metallic/Smoothness/AO 精準配置；2D 只用到 sprite normal/mask map。
- 把關材質球效能與 Texture 壓縮格式（手機偏 ASTC，避免無謂 RGBA32）。
