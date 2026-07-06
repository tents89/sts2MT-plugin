# Sts2OpenCC-Zht

**Sts2OpenCC-zht 是 Slay the Spire 2 的繁體中文轉換模組。模組會在遊戲切換語言時掃描遊戲本體與已載入模組的 `zhs` localization 表格，使用內嵌 OpenCC 字典轉為繁體中文，再套用自訂字典覆寫，最後寫回目前語言資料。**

![image](https://github.com/tents89/sts2MT-plugin/blob/main/png/1.png)

> [!Note]
>
> 若模組本身未使用遊戲本身的在地化系統，就算有 `zhs` 原文字源，也無法執行轉換，因此仍會為轉換的部分存在。

## 架構

> [!IMPORTANT]
> 
> v2.0 已重構模組，不再依賴其他模組。

```text
├── MainFile.cs                       # 模組入口，讀取設定並套用 Harmony patches。
├── Core/
│   ├── LangConfig.cs                 # 來源與目標語言設定。
│   ├── OpenCcEngine.cs               # 內嵌 OpenCC 字典的純 C# 轉換器。
│   ├── OpenCcBridge.cs               # 轉換入口，負責 OpenCC 轉換與 CustomDict.txt 覆寫。
│   ├── TargetRegistry.cs             # 掃描遊戲本體與已載入模組，判斷是否存在 zhs localization。
│   ├── LocConverter.cs               # 讀取 localization 表格、轉換文字、合併回 LocManager。
│   ├── CacheStore.cs                 # 保存版本紀錄與已轉換表格快取。
│   ├── ModSettings.cs                # 保存模組設定。
│   └── JsonUtil.cs                   # 共用 JSON 輸出設定。
├── Patches/
│   ├── LocManagerSetLanguagePatch.cs # 在 LocManager.SetLanguage 後自動轉換與套用。
│   ├── MainMenuButtonPatch.cs        # 在主選單加入繁體化入口。
│   ├── SettingsScreenPatch.cs        # 在設定畫面加入繁體化入口。
│   └── TraditionalizeSubmenuPatch.cs # 註冊繁體化 submenu 類型。
├── Ui/
│   ├── TraditionalizeSubmenu.cs     # 目前使用的繁體化管理頁面。
│   ├── NativeUi.cs                   # 遊戲風格的按鈕、切換項與 row 元件。
│   ├── NativeScrollContainer.cs      # 符合遊戲 submenu 的捲動容器。
│   ├── FoldableSection.cs            # 可折疊的分類區塊。
│   └── ConfirmDialog.cs              # 重新套用前的確認對話框。
└── Dict/
    ├── STCharacters.txt              # 內嵌 OpenCC s2tw 轉換字典。
    ├── STPhrases.txt                 # 內嵌 OpenCC s2tw 轉換字典。
    ├── TWVariants.txt                # 內嵌 OpenCC s2tw 轉換字典。
    └── TWVariantsPhrases.txt         # 內嵌 OpenCC s2tw 轉換字典。
```

## 轉換流程

1. `LocManager.SetLanguage` 完成後，模組重新掃描可用目標。
2. 若目標有 `localization/zhs` 表格，模組讀取原文並產生繁體內容。
3. 轉換順序為 OpenCC 字典，接著套用 `CustomDict.txt`。

```
[簡體文字] 
   │
   ▼
(Pass 1) 轉換為繁體標準字：優先比對 STPhrases (詞語) ──> 備援比對 STCharacters (單字)
   │
   ▼
(Pass 2) 轉換為台灣正體字形：優先比對 TWVariantsPhrases ──> 備援比對 TWVariants
   │
   ▼
(Pass 3) 套用自訂字典
   │
   ▼
[繁體文字輸出]

```

4. 轉換結果合併回目前語言表格，並寫入 `Cache/` 供之後重用。
5. 若遊戲本體或模組版本變更，會重新產生對應快取。

## 管理頁面

主選單與設定畫面都會提供「繁體化」入口，開啟子選單。

頁面內容包含：

- 是否在主選單顯示入口。
- 遊戲本體。
- 支援轉換的模組。
- 不支援轉換的模組與原因。
- 單一目標重新套用。
- 全部重新套用。
- 開啟快取資料夾。

管理頁面的按鈕與互動回饋使用遊戲原生 UI 節點與素材，包含 hover、focus、press/release 狀態。

## 自訂字典

自訂字典位於模組資料夾的 `CustomDict.txt`。格式為每行一組：

```text
視頻=影片
主菜單=主選單
```

空行與 `#` 開頭的行會被忽略。自訂字典會在 OpenCC 轉換後套用，因此可以用來覆寫 OpenCC 的預設結果。

## 檔案位置

建置後模組會輸出到：

```text
mods/Sts2OpenCC-zht/
├── Sts2OpenCC-zht.dll
├── Sts2OpenCC-zht.json
├── CustomDict.txt
├── Settings.json
├── Assets/
│   └── configbutton.png
└── Cache/
    ├── versions.json
    ├── __base_game__/
    │   └── {table}.json
    └── {modId}/
        └── {table}.json
```

`CustomDict.txt`、`Settings.json` 與 `Cache/` 會在執行時自動建立。

## 建置

```powershell
dotnet build -c Release
```

編譯後會自動複製 `Sts2OpenCC-zht.dll`、`Sts2OpenCC-zht.json` 與 UI 素材到 Slay the Spire 2 的 `mods/Sts2OpenCC-zht/` 目錄。

## Credit

| 名稱 | 用途 |
| ---------- | ---------- |
| Claude與GPT | 天才程式設計師 |
| [Sts2ModTranslator](https://github.com/ing-gom/sts2-mod-translator) | 已脫離依賴但參考其作法 |
| [OpenCC](https://github.com/BYVoid/OpenCC) | 字典來源與授權資訊請見 `THIRD_PARTY_NOTICES.md`。 |
| [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)| 參考作法 |
