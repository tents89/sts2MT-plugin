# Sts2ModTranslatorOpenCC

STS2 Mod Translator 的附加外掛（不含它的 dll，只依賴它）。
在它的面板裡「Install as mod」按鈕旁邊加一顆 **簡轉繁** 按鈕：一鍵把目前選定模組的
`zhs` 覆寫檔內容從簡體轉成繁體，直接存檔並套用，還可以疊加你自己的自訂字典。

> 遊戲目前只有 `zhs` 這個中文語系，沒有獨立的 `zht`，所以這個外掛是**直接把 zhs 覆寫檔的內容
> 換成繁體字後存回同一個檔案**，而不是另外開一個 zht 語系。以後如果遊戲加了 zht，這裡要跟著調整。

## 這個版本的關鍵改動：不再依賴外部 NuGet 套件

最早的版本用 `OpenccNetLib` 這個 NuGet 套件做簡轉繁，結果遊戲載入時噴：

```
[ERROR] System.IO.FileNotFoundException: Could not load file or assembly 'OpenccNetLib, ...'
```

且整個遊戲直接起不來。原因是：STS2 的 mod 載入機制看起來只認「單一 dll」，不會像一般
.NET 程式那樣自動去同一個資料夾找相依的 dll，所以只要 mod 的 dll 依賴了另一顆額外的 dll
（哪怕就放在同一層），載入當下就可能直接炸掉、而且看起來沒有妥善的錯誤隔離（一個 mod
爆掉會拖累整個遊戲起不來），沒辦法單純用「早一點註冊 AssemblyResolve」這種技巧補救。

同時，原本的建置腳本會把 NuGet 套件連帶產生的 `Sts2ModTranslatorOpenCC.deps.json` 等
建置產物也複製進 mods 資料夾，而遊戲的 mod 掃描器似乎會去讀資料夾裡「每一個」`.json`
當作 manifest 解析，讀到看不懂的 json 就在 log 裡報錯。

**這個版本的解法：把簡轉繁需要的字典資料直接內嵌進本模組的 dll 裡**（`Dict/*.txt`，
OpenCC 官方字典的原始文字檔，Apache-2.0 授權，見 `THIRD_PARTY_NOTICES.md`），
自己用純 C# 寫一個最長匹配的分段轉換引擎（`Core/OpenCcEngine.cs`），完全不吃任何
外部 dll。輸出資料夾現在只會有：

```
mods/Sts2ModTranslatorOpenCC/
├── Sts2ModTranslatorOpenCC.dll
├── Sts2ModTranslatorOpenCC.json   <- 唯一的 manifest
└── CustomDict.txt                  <- 第一次執行後自動產生
```

跟原本能正常運作的 `Sts2ModTranslator` 資料夾長得一模一樣（一顆 dll + 一份 json），
不會再有找不到相依 dll、或多餘 json 被誤判的問題。`csproj` 也明確關掉了
`GenerateDependencyFile` / `GenerateRuntimeConfigurationFiles`，並把複製建置產物的
target 改成只複製 `TargetPath`（主 dll）跟 manifest 這兩個檔案，不再整批複製輸出資料夾。

## 轉換品質 vs. 真正的 OpenCC 函式庫

`Core/OpenCcEngine.cs` 重現的是 OpenCC 官方 `s2tw.json` 設定檔描述的轉換鏈（標準簡轉繁，
只轉字形、不做詞彙在地化，例如「软件」只會變成「軟件」不會變成「軟體」）：

1. 簡體 → 繁體標準字：`STPhrases`（詞語，優先） → `STCharacters`（單字，備援）
2. 繁體標準字 → 台灣正體字形：`TWVariantsPhrases` → `TWVariants`

每一段都是「由左到右掃描、每個位置在字典群組裡找最長匹配、找不到就換下一個字典、
全部找不到就照原字輸出」，字典資料直接來自 OpenCC 官方 repo。這對絕大部分文字都會給出
跟真正的 OpenCC 函式庫一致或非常接近的結果，少數落差用 `CustomDict.txt` 修正即可。

## 建置

1. 先確定 `Sts2ModTranslator` 這個原模組已經建置/安裝在 `<STS2>/mods/Sts2ModTranslator/`（要有
   `Sts2ModTranslator.dll`）。
2. 這個專案現在**不需要**連外網還原任何執行期 NuGet 套件（`Krafs.Publicizer` 只在編譯期用，
   不會被複製進輸出）。
3. 跟原模組一樣：

   ```
   dotnet build -c Release
   ```

   會把 `Sts2ModTranslatorOpenCC.dll` + `Sts2ModTranslatorOpenCC.json` 複製到
   `<STS2>/mods/Sts2ModTranslatorOpenCC/`。用 Godot 編輯器內建的建置按鈕建置也一樣會跑這個
   複製步驟（`AfterTargets="PostBuildEvent"`），只是輸出資料夾路徑可能落在
   `.godot/mono/temp/bin/...` 而不是一般的 `bin/Release/net9.0/`，正常情況下不影響複製結果。

4. 啟動遊戲，兩個模組都要打勾啟用。進 Mod Translator 面板 → 點一個模組 → 語言列表頁最下面，
   「Install as mod」旁邊會多一顆「簡轉繁」。

## 按下「簡轉繁」會發生什麼事

在模組列表頁（模組 ▸ 語言列表那頁，Install as mod 旁邊）按下「簡轉繁」，一鍵做完：

1. 對這個模組的每一個檔案，讀取 **Reference(zhs)**——也就是模組自己內建的簡體中文原文
   （不是別人上傳的翻譯包，也不是你現有的 zhs 覆寫檔）。
2. 逐值跑過簡轉繁（OpenCC s2tw + 你的自訂字典）。
3. **不管 Translation(zhs) 原本有沒有內容，直接覆蓋**，存進 zhs 覆寫檔並立即套用到遊戲。

如果這個模組本身沒有內建 zhs 原文可以當 Reference，就沒有東西可以轉換，按了也不會有效果
（狀態列會顯示原因）。這是**會覆蓋、且無法復原**的動作，按下去前會跳確認框，建議先用
面板上的 Open Folder 備份一下 `overrides` 資料夾。

## 自訂字典

第一次執行後，模組資料夾裡會自動產生 `CustomDict.txt`（跟 dll 放在一起），格式：

```
# 開頭是註解
軟件=軟體
```

每行 `OpenCC 轉換後看到的文字=想換成的文字`。這是在 OpenCC 轉換**完成之後**才套用的
單純找字換字，適合拿來修正角色名、卡牌名，或是把 `s2tw` 沒轉的詞彙（像上面例子的
軟件→軟體）手動補上。改完存檔，重啟遊戲即可生效
（要熱重載可以在 `OpenCcBridge.ReloadCustomDict()` 上面掛個按鈕）。

## 想換轉換標準？

目前固定走 `s2tw`（簡體 → 台灣正體，只轉字形）。想換成 `s2twp`（多一段台灣慣用詞彙
在地化，例如软件→軟體、鼠标→滑鼠）的話：去
`https://raw.githubusercontent.com/BYVoid/OpenCC/master/data/dictionary/TWPhrases.txt`
抓這個字典檔放進 `Dict/` 資料夾、csproj 的 `EmbeddedResource` 清單裡加一條，再把
`Core/OpenCcEngine.cs` 加回 `_twPhrases` 欄位、載入、`Convert()` 的 pass2 呼叫（放在
`_twVariantsPhrases` 前面）即可；要支援香港繁體（`s2hk`）則需要另外抓
`HKVariants.txt` 之類的字典，比照現有模式加一段轉換鏈。
