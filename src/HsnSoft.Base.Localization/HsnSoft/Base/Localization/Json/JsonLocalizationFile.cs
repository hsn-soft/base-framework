using System.Collections.Generic;

namespace HsnSoft.Base.Localization.Json;

public class JsonLocalizationFile
{
    /// <summary>
    /// Culture name; eg : en , en-us, zh-CN
    /// </summary>
    public string Culture { get; set; }

    public Dictionary<string, string> Texts { get; set; }

    public JsonLocalizationFile()
    {
        Texts = new Dictionary<string, string>();
    }
}