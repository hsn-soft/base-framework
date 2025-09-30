using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HsnSoft.Base.Json.Newtonsoft.Mask;

public static class JsonStringMasking
{
    public static string MaskFields(this string json, string[] blacklist, string mask, Dictionary<string, Func<string, string>> blacklistPartial = null)
    {
        if (string.IsNullOrWhiteSpace(json) == true)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (blacklist == null)
        {
            throw new ArgumentNullException(nameof(blacklist));
        }

        if (blacklist.Any() == false)
        {
            return json;
        }

        var deserilizedObject = JsonConvert.DeserializeObject(json);

        if (deserilizedObject is JArray)
        {
            foreach (var item in (JArray)deserilizedObject)
            {
                MaskFieldsFromJToken(item, blacklist, mask);
            }

            return deserilizedObject.ToString();
        }

        var jsonObject = (JObject)deserilizedObject;

        if (blacklistPartial != null)
        {
            MaskFieldsFromJToken(jsonObject, blacklist, mask, blacklistPartial);
        }
        else
        {
            MaskFieldsFromJToken(jsonObject, blacklist, mask);
        }

        return jsonObject.ToString();
    }

    private static void MaskFieldsFromJToken(JToken token, string[] blacklist, string mask)
    {
        JContainer container = token as JContainer;
        if (container == null)
        {
            return; // abort recursive
        }

        List<JToken> removeList = new List<JToken>();
        foreach (JToken jtoken in container.Children())
        {
            if (jtoken is JProperty prop)
            {
                var matching = blacklist.Any(item =>
                {
                    return IsMatch(prop.Path, item);
                });

                if (matching)
                {
                    removeList.Add(jtoken);
                }
            }

            // call recursive
            MaskFieldsFromJToken(jtoken, blacklist, mask);
        }

        // replace
        foreach (JToken el in removeList)
        {
            var prop = (JProperty)el;
            prop.Value = mask;
        }
    }

    private static void MaskFieldsFromJToken(JToken token, string[] blacklist, string mask, Dictionary<string, Func<string, string>> blacklistPartial)
    {
        JContainer container = token as JContainer;
        if (container == null)
        {
            return; // abort recursive
        }

        List<JToken> removeList = new List<JToken>();
        foreach (JToken jtoken in container.Children())
        {
            if (jtoken is JProperty prop)
            {
                var matching = blacklist.Any(item =>
                {
                    return IsMatch(prop.Path, item);
                });

                if (matching)
                {
                    removeList.Add(jtoken);
                }
            }

            // call recursive
            MaskFieldsFromJToken(jtoken, blacklist, mask, blacklistPartial);
        }

        foreach (JToken el in removeList)
        {
            var prop = (JProperty)el;

            if (blacklistPartial.TryGetValue(blacklistPartial.GetKey(prop.Path), out var maskFunc))
            {
                var value = prop.Value.ToString();
                try
                {
                    var valueMasked = maskFunc != null ? maskFunc(value) : mask;
                    prop.Value = valueMasked != value ? valueMasked : mask;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"An error occurred while executing the function in the dictionary value. {ex.Message}");
                }
            }
            else
            {
                prop.Value = mask;
            }
        }
    }

    private static string GetKey(this Dictionary<string, Func<string, string>> blacklistPartial, string key)
    {
        var result = blacklistPartial.Keys.FirstOrDefault(dictionaryKey =>
        {
            return IsMatch(key, dictionaryKey);
        });

        return result ?? key;
    }

    private static bool IsMatch(string key, string value)
    {
        return Regex.IsMatch(key, WildCardToRegular(value), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string WildCardToRegular(string value)
    {
        return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }
}