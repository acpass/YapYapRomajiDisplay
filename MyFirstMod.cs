using BepInEx;
using HarmonyLib;
using System;
using YAPYAP;
using TMPro;

using System.Reflection;

using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections.Generic;
// 假设命名空间是 YAPYAP，根据你提供的代码推断


// This attribute tells BepInEx this is a mod. 
// Format: ("Unique ID", "Display Name", "Version")
[BepInPlugin("myfirstmod.yapyap.console", "MyYAPYAPMod", "1.0.0")]
public class MyFirstMod : BaseUnityPlugin
{
    // Awake is called when the mod is first loaded by the game.
    public void Awake()
    {
        // LogInfo sends a message to the BepInEx console
        Harmony.CreateAndPatchAll(typeof(MyFirstMod).Assembly);
        Logger.LogInfo("MyYAPYAPMod is enabled.");
    }
}

// 假设包含 Initialize 的类是 UISelectionWheelElement
[HarmonyPatch(typeof(UISelectionWheelOptionElement), nameof(UISelectionWheelOptionElement.Initialize),
    new Type[] { typeof(IChargeable), typeof(string), typeof(Sprite), typeof(Color), typeof(Color), typeof(Material) })]
public class WheelElementPatch
{
    // 缓存反射信息，提升性能
    private static FieldInfo _voiceCommandKeyField;
    private static bool _reflectionFailed = false;

    // 针对 TextMeshPro 的富文本标签进行匹配
    // 匹配规则：空格 + <alpha= + 任意字符 + 直到字符串结尾
    private static Regex _cooldownRegex = new Regex(@"(\s<alpha=.*)$");
    // 使用 Prefix，在赋值给 this.titleText.text 之前，修改传入的 'title' 参数
    [HarmonyPrefix]
    public static void Prefix(object chargeable, ref string title)
    {
        // 1. 基础检查
        if (chargeable == null || string.IsNullOrEmpty(title)) return;
        // 2. 避免重复添加 (防止每一帧文字越来越长)
        // 检查 title 里是否已经包含了我们自定义的罗马音颜色代码
        if (title.Contains("<color=#FFD700>")) return;
        // 3. 获取 Key (反射获取子类的 VoiceCommandKey)
        string key = ((Spell)(chargeable)).VoiceCommandKey;
        Debug.Log($"[MyYAPYAPMod] Retrieved VoiceCommandKey: {key}");
        if (string.IsNullOrEmpty(key)) return;
        // 4. 获取罗马音
        string romaji = GetRomaji(key);
        if (string.IsNullOrEmpty(romaji)) return;
        // 5. 格式化并插入
        string romajiText = $"<size=65%><color=#FFD700>{romaji}</color></size>";
        // 使用正则检测末尾是否有冷却时间格式 (例如 " <alpha=#50>(3.5s)<alpha=#FF>")
        if (_cooldownRegex.IsMatch(title))
        {
            // 情况 A：有冷却时间
            // 目标：把罗马音插在冷却时间 **前面**
            // 变换前： "Fireball <alpha=#50>(3.5s)..."
            // 变换后： "Fireball [Faia] <alpha=#50>(3.5s)..."
            
            // $1 代表正则匹配到的那坨冷却时间字符串，我们在它前面加上 空格+罗马音
            title = _cooldownRegex.Replace(title, " " + romajiText + "$1");
        }
        else
        {
            // 情况 B：没有冷却时间 (或者是无限次数的技能)
            // 目标：换行显示罗马音，更美观
            // 变换后： "Fireball\n[Faia]"
            title += "\n" + romajiText;
        }
    }
    // --- 以下是辅助方法 ---
    // 反射获取 VoiceCommandKey
    private static string GetKeyFromObject(object obj)
    {
        if (_reflectionFailed) return null;
        try
        {
            if (_voiceCommandKeyField == null)
            {
                // 尝试获取 VoiceCommandKey 字段 (无论公私有)
                // 假设它定义在 Spell 类里
                var type = obj.GetType();
                _voiceCommandKeyField = type.GetField("VoiceCommandKey", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                // 如果是属性(Property)不是字段，请把上面的 GetField 改成 GetProperty，下面用 GetValue(obj, null)
            }
            if (_voiceCommandKeyField != null)
            {
                return _voiceCommandKeyField.GetValue(obj) as string;
            }
        }
        catch { _reflectionFailed = true; }
        return null;
    }
    // 你的查表逻辑
    private static string GetRomaji(string key)
    {
        // 示例数据
        return "[Faia-boru]";
    }
}
