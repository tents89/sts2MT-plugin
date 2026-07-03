# Sts2ModTranslator - OpenCC 說明文件
> [!Note]
> `Sts2ModTranslatorOpenCC` 是專為 `Sts2ModTranslator` 開發的附加外掛模組（僅對其相依）。

本模組於 [Sts2ModTranslator](https://steamcommunity.com/sharedfiles/filedetails/?id=3752522987) 面板的「Install as mod」按鈕旁新增了一顆 **「簡轉繁」** 按鈕。其功能為一鍵將目前選定模組的 `zhs` 覆寫檔內容由簡體中文轉換為繁體中文，直接存檔並套用，同時支援疊加使用者自訂字典（已經內建部分詞彙）。

> **架構說明**
>
> 由於目前遊戲僅內建 `zhs`（簡體中文）語系，尚未獨立支援 `zht`（繁體中文），因此本外掛的運作邏輯為：**直接將簡體中文內容轉換為繁體字，並覆寫回原 `zhs` 覆寫檔案中**，而非另建 `zht` 語系。

---

## 核心設計：不依賴外部 NuGet 套件

在早期版本中，本模組採用 `OpenccNetLib` 套件進行簡轉繁，但於遊戲載入時會觸發 `System.IO.FileNotFoundException` 錯誤。

### 錯誤原因

1. **單一 DLL 機制**：遊戲的模組載入機制僅識別「單一主 DLL」，無法自動搜尋並讀取同目錄下的相依性 DLL。
2. **錯誤隔離缺乏**：單一模組載入失敗會導致整體遊戲無法啟動，無法透過常規的 `AssemblyResolve` 註冊機制進行補救。
3. **組態檔案干擾**：建置時產生的 `*.deps.json` 等檔案會被遊戲的模組掃描器視為 manifest 解析，進而引發錯誤記錄。

### 解決方案

本模組將 OpenCC 官方字典原始文字檔（`Dict/*.txt`，基於 Apache-2.0 授權）**直接內嵌至模組的 DLL 中**，並於 `Core/OpenCcEngine.cs` 實作純 C# 的「最長匹配分段轉換引擎」，完全移除了外部 DLL 相依性。

**輸出目錄結構：**

```text
mods/Sts2ModTranslatorOpenCC/
├── Sts2ModTranslatorOpenCC.dll   (主程式)
├── Sts2ModTranslatorOpenCC.json  (唯一的 Manifest)
└── CustomDict.txt                (首次執行後自動產生)

```

專案組態（`csproj`）已明確關閉 `GenerateDependencyFile` 與 `GenerateRuntimeConfigurationFiles`，建置目標（Target）設定為僅複製主 DLL 與 JSON Manifest，確保目錄乾淨。

---

## 轉換機制與品質

本模組重現了 OpenCC 官方 `s2tw.json`（標準簡轉繁，僅轉換字形，不做詞彙在地化）的轉換鏈：

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

* **演算法**：由左至右掃描，於各位置之字典群組中搜尋最長匹配（Longest Match）。若查無對應則遞補至下一字典；皆無匹配則保留原字輸出。
* **品質**：轉換結果與官方 OpenCC 基礎函式庫高度一致。少數特殊用字或詞彙差異可透過 `CustomDict.txt` 進行校正。

---

## 執行流程

於模組語言列表頁面按下「簡轉繁」按鈕後，系統將依序執行以下作業：

1. **讀取原文**：讀取該模組內建的簡體中文原文（即 `Reference(zhs)`，非第三方翻譯包或現存的覆寫檔）。
2. **文本轉換**：批次進行簡轉繁運算（結合 OpenCC s2tw 規範與 `CustomDict.txt` 自訂字典）。
3. **覆寫套用**：**不論 `Translation(zhs)` 當前是否有內容，均直接覆蓋**，寫入 `zhs` 覆寫檔並即時套用至遊戲。

> **注意事項**
> * 若模組本身未內建 `zhs` 原文字源，則無法執行轉換（狀態列將提示原因）。
> * 本操作具**不可逆性（會直接覆蓋現存檔案）**，執行前會跳出確認提示。建議先透過面板的「Open Folder」功能備份 `overrides` 資料夾。
> 
> 

---

## 自訂字典 (`CustomDict.txt`)

首次執行模組後，系統將於 DLL 同級目錄下自動生成 `CustomDict.txt`。

* **相容格式**：
```text
# 每行開頭為「#」代表註解
軟件=軟體

```


* **運作邏輯**：此設定是在 OpenCC 標準轉換**完成之後**才執行的全域替換（純字串取代）。適用於修正特定角色名稱、卡牌名稱，或將標準字形轉換為在地化詞彙（例如：將「軟件」修正為「軟體」）。
* **套用方式**：修改並儲存檔案後，重啟遊戲即可生效。

---

## 建置與部署

1. **環境確認**：確保原模組已正確放置於 `<STS2>/mods/Sts2ModTranslator/`（須包含 `Sts2ModTranslator.dll`）。
2. **相依性**：本專案編譯期間無須從外部還原任何執行期 NuGet 套件（`Krafs.Publicizer` 僅供編譯期使用，不會被封裝輸出）。
3. **編譯指令**：
```bash
dotnet build -c Release

```


建置腳本會自動將 `Sts2ModTranslatorOpenCC.dll` 與 `Sts2ModTranslatorOpenCC.json` 複製至 `<STS2>/mods/Sts2ModTranslatorOpenCC/`。
4. **啟用**：啟動遊戲後，請於模組管理器中同時勾選並啟用這兩個模組才會生效。

## Credit

| 名稱 | 用途 |
| ---------- | ---------- |
| [Sts2ModTranslator](https://github.com/ing-gom/sts2-mod-translator) | 依賴 |
| [OpenCC](https://github.com/BYVoid/OpenCC) | 利用其字典 |
