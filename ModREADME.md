# YapYap Romaji Display

此 Mod 可以在游戏中显示咒语和物品的罗马音提示，帮助不熟悉假名的玩家快速记忆和输入。

## 功能介绍
- **法术轮盘 (Spell Wheel)**: 在选择法术时，标题旁会显示对应的罗马音。
- **复活/交易 (Revive/Trade)**: 在相关界面显示罗马音。
- **热重载**: 修改配置文件后，在游戏中按 **F9** 即可立即生效，无需重启游戏。

## 安装说明
1. 确保已安装 **BepInEx** 框架。
2. 将压缩包内的以下文件放入游戏根目录的 `BepInEx/plugins/RomajiDisplay` 文件夹中：
   - `RomajiDisplay.dll`
   - `romaji_mapping.txt`

## 配置文件
配置文件位于 `BepInEx/config/romaji_mapping.txt`。
- 首次运行时，Mod 会自动将 `plugins` 目录下的默认配置文件复制到 `config` 目录（如果 config 目录下不存在该文件）。
- 如果两个地方都没有，Mod 会生成一个空白的默认模板。

### 自定义修改
你可以直接编辑 `BepInEx/config/romaji_mapping.txt` 文件来自定义显示的罗马音。

**格式说明**：
- 使用 `Key = Value` 的格式。
- `Key`: 游戏内部的语音指令键名（如 `SPELL_ARC_FIRE_1`）。
- `Value`: 你想要显示的文本（如 `[FA-I-YA]`）。
- 支持 `#` 开头的注释行。

**示例**：
```ini
# 这是一个注释
SPELL_ARC_FIRE_1 = [FA-I-YA]
SPELL_BAS_JUMP_1 = [JUMP]
```

## 常见问题
- **Q: 修改了配置没有变化？**
  - A: 确保修改的是 `BepInEx/config` 目录下的文件，并按 F9 刷新。
- **Q: 显示乱码？**
  - A: 配置文件请使用 UTF-8 编码保存。
