# sts2OpenCC-zht

`sts2OpenCC-zht` 是 Slay the Spire 2 的繁體中文轉換模組。模組會在遊戲切換語言時掃描遊戲本體與已載入模組的 `zhs` localization 表格，使用內嵌 OpenCC 字典轉為繁體中文，再套用自訂字典覆寫，最後寫回目前語言資料。

模組不需要其他模組作為執行依賴。

## 架構

```text
MainFile.cs
    模組入口，讀取設定並套用 Harmony patches。

Core/
    LangConfig.cs
      來源與目標語言設定。
    OpenCcEngine.cs
      內嵌 OpenCC 字典的純 C# 轉換器。
    OpenCcBridge.cs
      轉換入口，負責 OpenCC 轉換與 CustomDict.txt 覆寫。
    TargetRegistry.cs
      掃描遊戲本體與已載入模組，判斷是否存在 zhs localization。
    LocConverter.cs
      讀取 localization 表格、轉換文字、合併回 LocManager。
    CacheStore.cs
      保存版本紀錄與已轉換表格快取。
    ModSettings.cs
      保存模組設定。
    JsonUtil.cs
      共用 JSON 輸出設定。

Patches/
    LocManagerSetLanguagePatch.cs
      在 LocManager.SetLanguage 後自動轉換與套用。
    MainMenuButtonPatch.cs
      在主選單加入繁體化入口。
    SettingsScreenPatch.cs
      在設定畫面加入繁體化入口。
    TraditionalizeSubmenuPatch.cs
      註冊繁體化 submenu 類型。

Ui/
    TraditionalizeSubmenu.cs
      目前使用的繁體化管理頁面。
    NativeUi.cs
      遊戲風格的按鈕、切換項與 row 元件。
    NativeScrollContainer.cs
      符合遊戲 submenu 的捲動容器。
    FoldableSection.cs
      可折疊的分類區塊。
    ConfirmDialog.cs
      重新套用前的確認對話框。

Dict/
    STCharacters.txt
    STPhrases.txt
    TWVariants.txt
    TWVariantsPhrases.txt
      內嵌 OpenCC s2tw 轉換字典。
```

## 轉換流程

1. `LocManager.SetLanguage` 完成後，模組重新掃描可用目標。
2. 若目標有 `localization/zhs` 表格，模組讀取原文並產生繁體內容。
3. 轉換順序為 OpenCC 字典，接著套用 `CustomDict.txt`。
4. 轉換結果合併回目前語言表格，並寫入 `Cache/` 供之後重用。
5. 若遊戲本體或模組版本變更，會重新產生對應快取。

## 管理頁面

主選單與設定畫面都會提供「繁體化」入口，開啟同一個 submenu。

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
簡體詞=繁體詞
```

空行與 `#` 開頭的行會被忽略。自訂字典會在 OpenCC 轉換後套用，因此可以用來覆寫 OpenCC 的預設結果。

## 檔案位置

建置後模組會輸出到：

```text
mods/sts2OpenCC-zht/
  sts2OpenCC-zht.dll
  sts2OpenCC-zht.json
  Assets/
    configbutton.png
  CustomDict.txt
  Settings.json
  Cache/
    versions.json
    __base_game__/{table}.json
    {modId}/{table}.json
```

`CustomDict.txt`、`Settings.json` 與 `Cache/` 會在執行時依需要建立。

## 建置

```powershell
dotnet build -c Release
```

此專案會複製 `sts2OpenCC-zht.dll`、`sts2OpenCC-zht.json` 與 UI 素材到 Slay the Spire 2 的 `mods/sts2OpenCC-zht/` 目錄。

## 授權與字典來源

內嵌 OpenCC 字典來源與授權資訊請見 `THIRD_PARTY_NOTICES.md`。

實作參考：BaseLib-StS2-3.3.5 與 Sts2ModTranslator。
