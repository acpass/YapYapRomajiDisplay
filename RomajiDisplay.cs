using BepInEx;
using HarmonyLib;
using System;
using YAPYAP;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

// Format: ("Unique ID", "Display Name", "Version")
[BepInPlugin("acpass.romajidisplay", "RomajiDisplay", "1.1.1")]
public class RomajiDisplay : BaseUnityPlugin
{
    // Awake is called when the mod is first loaded by the game.
    public void Awake()
    {
        // LogInfo sends a message to the BepInEx console
        Harmony.CreateAndPatchAll(typeof(RomajiDisplay).Assembly);
        Logger.LogInfo("RomajiDisplay is enabled.");
    }
}

// ----------------------------------------------------------------
// 1. 配置管理器：负责读取文件和缓存
// ----------------------------------------------------------------
public static class RomajiConfigManager
{
    // 缓存字典
    private static Dictionary<string, string> _romajiCache = new Dictionary<string, string>();
    
    // 配置文件路径 (放在 BepInEx/config 下)
    private static string ConfigPath => Path.Combine(Paths.ConfigPath, "romaji_mapping.txt");
    // 静态构造函数，游戏启动/类被首次调用时自动加载
    static RomajiConfigManager()
    {
        LoadConfig();
    }
    public static void LoadConfig()
    {
        _romajiCache.Clear();
        // 如果文件不存在，尝试从插件目录复制，如果也没有则创建默认文件
        if (!File.Exists(ConfigPath))
        {
            bool copied = false;
            try
            {
                // 获取当前 DLL 所在目录
                string pluginDir = Path.GetDirectoryName(typeof(RomajiDisplay).Assembly.Location);
                string sourcePath = Path.Combine(pluginDir, "romaji_mapping.txt");
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, ConfigPath);
                    Debug.Log($"[RomajiDisplay] 已从插件目录复制配置文件到: {ConfigPath}");
                    copied = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RomajiDisplay] 复制配置文件出错: {ex.Message}");
            }

            if (!copied)
            {
                CreateDefaultConfig();
                return;
            }
        }
        try
        {
            string[] lines = File.ReadAllLines(ConfigPath);
            foreach (string line in lines)
            {
                // 跳过空行和注释
                if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#")) continue;
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    
                    if (!_romajiCache.ContainsKey(key))
                    {
                        _romajiCache.Add(key, value);
                    }
                }
            }
            Debug.Log($"[RomajiDisplay] 配置文件已加载，共读取 {_romajiCache.Count} 条数据。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RomajiDisplay] 读取配置文件失败: {e.Message}");
        }
    }
    // 提供给外部获取罗马音的方法
    public static string GetValue(string key)
    {
        if (_romajiCache.TryGetValue(key, out string val))
        {
            return val;
        }
        return null;
    }

    public static string FormatRomaji(string romaji)
    {
        return $"<size=65%><color=#FFD700>{romaji}</color></size>";
    }

    private static void CreateDefaultConfig()
    {
        try
        {
            using (StreamWriter sw = File.CreateText(ConfigPath))
            {
                sw.WriteLine("# 罗马音映射配置文件");
                sw.WriteLine("# 按 F9 可以在游戏中重新加载此文件");
                sw.WriteLine("# 格式: VoiceCommandKey = 显示文本");
            }
            Debug.Log($"[RomajiDisplay] 已生成默认配置文件: {ConfigPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RomajiDisplay] 无法创建默认配置文件: {e.Message}");
        }
    }
}
// ----------------------------------------------------------------
// 2. 输入监听 Patch：监听 F9 重新加载
// ----------------------------------------------------------------
// 我们Hook VoiceSpell.Update，因为根据你的日志，这个类肯定在运行
// 实际上任何每帧运行的 Update 都可以
[HarmonyPatch(typeof(YAPYAP.VoiceSpell), "Update")]
public class ReloadInputPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        // 监听 F9 键
        if (Input.GetKeyDown(KeyCode.F9))
        {
            RomajiConfigManager.LoadConfig();
            // 在屏幕上或者控制台给个提示
            Debug.Log("[RomajiDisplay] 用户按下了 F9，正在重新加载配置...");
        }
    }
}
// ----------------------------------------------------------------
// 3. UI 修改 Patch (核心逻辑)
// ----------------------------------------------------------------
[HarmonyPatch(typeof(UISelectionWheelOptionElement), nameof(UISelectionWheelOptionElement.Initialize), 
    new Type[] { typeof(IChargeable), typeof(string), typeof(Sprite), typeof(Color), typeof(Color), typeof(Material) })]
public class WheelElementPatch
{
    private static Regex _cooldownRegex = new Regex(@"(\s<alpha=.*)$");
    [HarmonyPrefix]
    public static void Prefix(object chargeable, ref string title)
    {
        if (chargeable == null || string.IsNullOrEmpty(title)) return;
        if (title.Contains("<color=#FFD700>")) return;
        // 获取 Key
        Spell spell = chargeable as Spell;
        string key = spell?.VoiceCommandKey;
        if (string.IsNullOrEmpty(key)) return;
        string romaji = RomajiConfigManager.GetValue(key);
        
        // 如果配置文件里没有这个 Key，我们可以选择暂时不显示，或者显示 Key 方便调试
        // 如果想调试，取消下面这行的注释：
        // if (string.IsNullOrEmpty(romaji)) romaji = $"[{key}??]";
        if (string.IsNullOrEmpty(romaji)) return;
        string romajiText = RomajiConfigManager.FormatRomaji(romaji);
        if (_cooldownRegex.IsMatch(title))
        {
            title = _cooldownRegex.Replace(title, " " + romajiText + "$1");
        }
        else
        {
            title += "\n" + romajiText;
        }
    }
 }

// ----------------------------------------------------------------
// 4. 新增接口 Patch
// ----------------------------------------------------------------

[HarmonyPatch(typeof(YAPYAP.ReviveChant), "GetLocalisedVoiceCommandKey", new Type[] { typeof(string) })]
public class ReviveChantPatch
{
    [HarmonyPostfix]
    public static void Postfix(string __0, ref string __result)
    {
        // __0 是 key 参数
        string romaji = RomajiConfigManager.GetValue(__0);
        if (!string.IsNullOrEmpty(romaji))
        {
            __result += " " + RomajiConfigManager.FormatRomaji(romaji);
        }
    }
}

[HarmonyPatch(typeof(YAPYAP.ReviveSpell), "GetLocalisedVoiceCommandKey", new Type[] { typeof(string) })]
public class ReviveSpellPatch
{
    [HarmonyPostfix]
    public static void Postfix(string __0, ref string __result)
    {
        string romaji = RomajiConfigManager.GetValue(__0);
        if (!string.IsNullOrEmpty(romaji))
        {
            __result += " " + RomajiConfigManager.FormatRomaji(romaji);
        }
    }
}

[HarmonyPatch(typeof(YAPYAP.TradeSpell), "TryGetTradeWord")]
public class TradeSpellPatch
{
    [HarmonyPostfix]
    public static void Postfix(YAPYAP.TradeSpell __instance, ref bool __result, object[] __args)
    {
        // 如果成功获取到 TradeWord
        if (__result)
        {
            // __args[0] 应该是 out string
            if (__args.Length > 0 && __args[0] is string original)
            {
                string romaji = RomajiConfigManager.GetValue("SPELL_ARC_TRADE_2");
                if (!string.IsNullOrEmpty(romaji))
                {
                    // 修改 out 参数
                    __args[0] = original + " " + RomajiConfigManager.FormatRomaji(romaji);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(YAPYAP.UpdraftSpell), "TryGetJumpWord")]
public class UpdraftSpellPatch
{
    [HarmonyPostfix]
    public static void Postfix(YAPYAP.UpdraftSpell __instance, ref bool __result, object[] __args)
    {
        if (__result)
        {
            if (__args.Length > 0 && __args[0] is string original)
            {
                string romaji = RomajiConfigManager.GetValue("SPELL_BAS_UPDOG");
                if (!string.IsNullOrEmpty(romaji))
                {
                    __args[0] = original + " " + RomajiConfigManager.FormatRomaji(romaji);
                }
            }
        }
    }
}

// Ensure we don't use typeof(XiLanHuaPuzzle) directly if it causes missing reference errors related to Mirror.
// Using string-based patch definition.
[HarmonyPatch("YAPYAP.XiLanHuaPuzzle, Assembly-CSharp", "UpdateLocalizedWords")]
public class XiLanHuaPuzzlePatch
{
    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
        // 1. 获取私有的 symbolWords 数组
        var trv = Traverse.Create(__instance);
        var symbolWords = trv.Field("symbolWords").GetValue() as Array;
        
        // 2. 获取 worldSpaceXiLanHuaSigns 数组
        var signs = trv.Field("worldSpaceXiLanHuaSigns").GetValue() as GameObject[];
        
        if (symbolWords == null) return;

        for (int i = 0; i < symbolWords.Length; i++)
        {
            object swObj = symbolWords.GetValue(i);
            if (swObj == null) continue;

            var swTrv = Traverse.Create(swObj);
            string key = swTrv.Field("commandKey").GetValue<string>();
            string word = swTrv.Field("word").GetValue<string>();

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(word)) continue;

            string romaji = RomajiConfigManager.GetValue(key);
            if (!string.IsNullOrEmpty(romaji))
            {
                string romajiText = RomajiConfigManager.FormatRomaji(romaji);
                // 防止重复追加
                if (!word.Contains(romajiText))
                {
                    string newWord = word + " " + romajiText;
                    
                    // A. 更新 SymbolWord 数据 (内存中)
                    swTrv.Field("word").SetValue(newWord);

                    // B. 更新已经在场景中生成的 Sign (UI)
                    if (signs != null && i < signs.Length && signs[i] != null)
                    {
                        var signObj = signs[i];
                        if (signObj == null) continue;

                        // 获取 WorldSpaceXiLanHuaSign 组件
                        // 使用 string 防止类型引用问题
                        var signComp = signObj.GetComponent("WorldSpaceXiLanHuaSign");
                        if (signComp != null)
                        {
                            // 访问 wordText 字段 (TMP_Text)
                            var signCompTrv = Traverse.Create(signComp);
                            var tmproObj = signCompTrv.Field("wordText").GetValue();
                            
                            // 尝试设置 text 属性 (使用 Reflection 防止 Missing TMPro 引用)
                            if (tmproObj != null)
                            {
                                Traverse.Create(tmproObj).Property("text").SetValue(newWord);
                            }
                        }
                    }
                }
            }
        }
    }
}

// 注意: UserCode_CmdTryWords__String 是 Server 端逻辑 (Command)，负责判定输赢和广播 RPC。
// 修改它会导致发送给所有客户端的字符串发生变化。
// 作为一个显示 Mod，我们只应该修改本地显示 (UpdateLocalizedWords) 或者接收端的显示逻辑。
// UpdateLocalizedWords 已经处理了场景中固定显示的词。