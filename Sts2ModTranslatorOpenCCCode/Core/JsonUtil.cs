using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sts2ModTranslatorOpenCC.Core;

/// <summary>共用的 JSON 輸出格式，跟 Sts2ModTranslator 自己存檔用的格式一致（縮排 + 不轉義中文）。</summary>
internal static class JsonUtil
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
