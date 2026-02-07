using BepInEx;
using HarmonyLib;
using System;
using YAPYAP;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

// Format: ("Unique ID", "Display Name", "Version")
[BepInPlugin("acpass.romajidisplay", "RomajiDisplay", "1.0.0")]
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
        // 如果文件不存在，创建一个带说明的默认文件
        if (!File.Exists(ConfigPath))
        {
            CreateDefaultConfig();
            return;
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
        string romajiText = $"<size=65%><color=#FFD700>{romaji}</color></size>";
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