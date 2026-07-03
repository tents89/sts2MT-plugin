# 第三方授權聲明

本模組的 `Sts2ModTranslatorOpenCCCode/Dict/*.txt` 內嵌字典資料，直接取自：

- 專案：OpenCC (Open Chinese Convert) — https://github.com/BYVoid/OpenCC
- 檔案：`data/dictionary/STCharacters.txt`、`STPhrases.txt`、`TWPhrases.txt`、
  `TWVariants.txt`、`TWVariantsPhrases.txt`
- 授權：Apache License 2.0 — https://github.com/BYVoid/OpenCC/blob/master/LICENSE

這些字典資料未經修改（原樣取用），本模組的轉換演算法（最長匹配、分段轉換鏈）是另外自行
實作的純 C# 程式碼，不是 OpenCC 官方程式庫本身，也未鏈結 OpenCC 的任何原始碼或編譯產物。
