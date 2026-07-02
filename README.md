# Sts2ModTranslatorOpenCC

STS2 Mod Translator 的附加外掛（不含它的 dll，只依賴它）。
在它的面板裡「Install as mod」按鈕旁邊加一顆 **簡轉繁** 按鈕：一鍵用 OpenCC 把目前選定模組的
`zhs` 覆寫檔內容從簡體轉成繁體，直接存檔並套用，還可以疊加你自己的自訂字典。

> 遊戲目前只有 `zhs` 這個中文語系，沒有獨立的 `zht`，所以這個外掛是**直接把 zhs 覆寫檔的內容
> 換成繁體字後存回同一個檔案**，而不是另外開一個 zht 語系。以後如果遊戲加了 zht，這裡要跟著調整。

## 為什麼能做到「不包含他的 dll」

`Sts2ModTranslatorOpenCC.csproj` 只用 `<Reference HintPath=".../Sts2ModTranslator.dll"><Private>false</Private></Reference>`
去「引用」原模組已經建置好的 dll 做編譯期型別解析，`Private=false` 代表建置完不會把它複製進本模組
的輸出資料夾。執行期則直接吃遊戲已經載入的那一份——因為 manifest 裡宣告了
`"dependencies": [{"id": "Sts2ModTranslator", ...}]`，遊戲的 mod loader 會保證先載入原模組。

同時用 `Krafs.Publicizer` 把 `TranslatorPanel` 裡的 `private static` 成員
（`_content`、`_mod`、`SetStatus`、`Confirm`）在編譯期當 public 用，所以能直接把按鈕插進他
既有的面板，完全不用修改、不用複製他的原始碼——跟原模組自己拿 Publicizer 處理 `sts2.dll` 是同一招。

## 建置

1. 先確定 `Sts2ModTranslator` 這個原模組已經建置/安裝在 `<STS2>/mods/Sts2ModTranslator/`（要有
   `Sts2ModTranslator.dll`）。
2. 需要能連上 nuget.org 還原 `OpenccNetLib` 套件（純 C# 的簡繁轉換函式庫，內建 OpenCC 詞庫，
   MIT 授權，詞庫本身是 OpenCC 專案的 Apache-2.0 授權資料）。
3. 跟原模組一樣：

   ```
   dotnet build -c Release
   ```

   會自動把整個輸出資料夾（dll + OpenccNetLib 需要的相依檔案）＋ manifest 複製到
   `<STS2>/mods/Sts2ModTranslatorOpenCC/`。

4. 啟動遊戲，兩個模組都要打勾啟用。進 Mod Translator 面板 → 點一個模組 → 語言列表頁最下面，
   「Install as mod」旁邊會多一顆「簡轉繁」。

> 這份程式碼是照 OpenccNetLib 官方文件的用法（`new Opencc("s2twp").Convert(text)`）寫的，
> 但因為我這邊沒有 STS2 遊戲檔案、也連不到 nuget.org，沒辦法實際 `dotnet build` 驗證。
> 如果套件實際 API 跟文件有出入，只要改 `Core/OpenCcBridge.cs` 這一個檔案就好，
> 其他檔案（Harmony patch、業務邏輯）都不用動。

## 自訂字典

第一次執行後，模組資料夾裡會自動產生 `CustomDict.txt`（跟 dll 放在一起），格式：

```
# 開頭是註解
服务器=伺服器
```

每行 `來源文字=想要輸出的繁體文字`。這份字典的優先權永遠高於 OpenCC 內建詞庫，適合拿來修正
角色名、卡牌名、特定用語等 OpenCC 轉不準的地方。改完存檔，重啟遊戲或重新按一次「簡轉繁」
（目前是程式啟動時載入一次；要熱重載可以在 `OpenCcBridge.ReloadCustomDict()` 上面掛個按鈕）。

## 想換轉換標準？

`Core/OpenCcBridge.cs` 最上面的 `Config` 常數：

- `s2t`   — 簡體 → OpenCC 標準繁體
- `s2tw`  — 簡體 → 台灣正體（只轉字形）
- `s2twp` — 簡體 → 台灣正體 + 常用詞彙（預設）
- `s2hk`  — 簡體 → 香港繁體
