using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MonoBehaviour), true)]
public class ChineseMonoBehaviourInspector : Editor
{
    private static readonly Dictionary<string, string> ExactLabels = new Dictionary<string, string>
    {
        { "m_Script", "脚本" },
        { "baseValue", "基础数值" },
        { "modifiers", "加成列表" },
        { "characterUI", "角色界面" },
        { "skillTreeUI", "技能树界面" },
        { "craftUI", "制造界面" },
        { "optionsUI", "设置界面" },
        { "levelUpUI", "升级界面" },
        { "inGameUI", "游戏内界面" },
        { "deathUI", "死亡界面" },
        { "pauseGameWhenMenuOpen", "打开菜单时暂停游戏" },
        { "skillToolTip", "技能提示框" },
        { "skillText", "技能描述文本" },
        { "skillName", "技能名称" },
        { "skillDescription", "技能描述" },
        { "unlockCondition", "解锁条件" },
        { "unlockCost", "解锁消耗" },
        { "unlockConditionText", "解锁条件文本" },
        { "unlockCostText", "解锁消耗文本" },
        { "lockedSkillColor", "未解锁颜色" },
        { "unlocked", "已解锁" },
        { "shouldBeUnlocked", "前置已解锁技能" },
        { "shouldBeLocked", "互斥未解锁技能" },
        { "itemTooltip", "物品提示框" },
        { "statToolTip", "属性提示框" },
        { "interactionPrompt", "交互提示框" },
        { "craftWindow", "制造窗口" },
        { "fadeOverlay", "黑屏遮罩" },
        { "fadeImage", "黑屏图片" },
        { "defaultFadeDuration", "默认渐变时长" },
        { "sortingOrder", "显示层级" },
        { "startFadeToBlackDuration", "开始游戏黑屏时长" },
        { "startFadeFromBlackDuration", "开始游戏显现时长" },
        { "openingSceneControlsFadeIn", "开幕剧情控制显现" },
        { "useScreenFade", "启用黑屏转场" },
        { "fadeFromBlackDuration", "黑屏消失时长" },
        { "textBoxDelayAfterFade", "黑屏后文字框延迟" },
        { "titleGroup", "标题显现组" },
        { "returnFireGroup", "归火显现组" },
        { "returnFireButton", "归火按钮" },
        { "maxBlackAlpha", "黑屏最大透明度" },
        { "fadeToBlackDuration", "黑屏渐变时长" },
        { "contentDelay", "文字按钮延迟" },
        { "contentFadeDuration", "文字按钮显现时长" },
        { "clickFeedbackDelay", "点击反馈延迟" },

        { "attackMovement", "攻击位移" },
        { "counterAttackDuration", "反击持续时间" },
        { "moveSpeed", "移动速度" },
        { "jumpForce", "跳跃力度" },
        { "swordReturnImpact", "飞剑返回冲击力" },
        { "doubleJumpForce", "二段跳力度" },
        { "extraJumpCount", "额外跳跃次数" },
        { "dashSpeed", "冲刺速度" },
        { "dashDuration", "冲刺持续时间" },
        { "sword", "当前飞剑" },
        { "canHeal", "可以治疗" },
        { "deathUiFallbackDelay", "死亡UI兜底延迟" },
        { "enableStairAssist", "启用楼梯辅助" },
        { "stairCheckWidthMultiplier", "楼梯检测宽度比例" },
        { "stairCheckHeight", "楼梯检测高度" },
        { "stairCheckExtraDistance", "楼梯额外检测距离" },
        { "stairVerticalSpeedRatio", "楼梯垂直速度比例" },
        { "stairMaxVerticalSpeed", "楼梯最大垂直速度" },
        { "stairGroundMemoryTime", "楼梯接地缓冲" },
        { "stairTopInsetRatio", "楼梯碰撞顶部下沉比例" },
        { "stairBottomLiftRatio", "楼梯碰撞底部抬高比例" },
        { "disableOriginalSlopeTileCollider", "关闭原楼梯Tile碰撞" },
        { "upDirection", "上楼方向" },

        { "knockbackDirection", "击退方向" },
        { "knockbackDuration", "击退持续时间" },
        { "attackCheak", "攻击检测点" },
        { "attackCheakRidus", "攻击检测半径" },
        { "groundCheak", "地面检测点" },
        { "groundCheakDistance", "地面检测距离" },
        { "wallCheak", "墙体检测点" },
        { "wallCheakDistance", "墙体检测距离" },
        { "whatIsGround", "地面图层" },
        { "cameraTransform", "摄像机" },
        { "ParallaxEffect", "横向跟随比例" },
        { "verticalParallaxEffect", "纵向跟随比例" },
        { "followVerticalMovement", "跟随纵向移动" },
        { "loopHorizontally", "横向循环" },
        { "keyText", "按键文本" },
        { "promptText", "提示文本" },
        { "worldOffset", "世界偏移" },
        { "screenOffset", "屏幕偏移" },
        { "screenPadding", "屏幕边距" },
        { "promptFont", "提示字体" },
        { "promptFontResourcePath", "字体资源路径" },
        { "backgroundColor", "背景颜色" },
        { "frameColor", "边框颜色" },
        { "keyBackgroundColor", "按键背景颜色" },
        { "textColor", "文字颜色" },
        { "revealRange", "显现范围" },
        { "hideRange", "消失范围" },
        { "entranceDuration", "出场动画时长" },
        { "disappearDuration", "消失动画时长" },
        { "reverseEntranceSpeedMultiplier", "倒放出场速度倍率" },
        { "revealConfirmSeconds", "显现确认时间" },
        { "hideConfirmSeconds", "消失确认时间" },
        { "entranceAnimation", "出场动画名" },
        { "idleAnimation", "待机动画名" },
        { "disappearAnimation", "消失动画名" },
        { "startHidden", "开始时隐藏" },
        { "npcName", "NPC 名称" },
        { "playerSpeakerName", "玩家说话者名称" },
        { "enabledFunctions", "启用功能" },
        { "dialogueOptionText", "对话选项文字" },
        { "upgradeOptionText", "祭礼选项文字" },
        { "returnFireOptionText", "归火选项文字" },
        { "flaskUpgradeOptionText", "凝露选项文字" },
        { "conversationLines", "对话流程" },
        { "monologueText", "独白文本" },
        { "monologueLines", "独白文本列表" },
        { "pages", "剧情页列表" },
        { "backgroundRoot", "背景对象" },
        { "dialogueBoxRoot", "独白框对象" },
        { "lines", "本页独白列表" },
        { "autoCollectPagesWhenEmpty", "空列表时自动收集背景" },
        { "disableChildSinglePageControllers", "禁用子背景单页控制器" },
        { "hideAllWhenFinished", "结束后隐藏全部" },
        { "loadNextSceneWhenFinished", "结束后加载下一场景" },
        { "nextSceneName", "下一场景名称" },
        { "finishFadeToBlackDuration", "结尾黑屏时长" },
        { "finishFadeFromBlackDuration", "结尾显现时长" },
        { "skipButton", "跳过按钮" },
        { "skipButtonObjectName", "跳过按钮对象名" },
        { "showSkipButtonWithDialogue", "随文字框显示跳过按钮" },
        { "playOnStart", "开始时播放" },
        { "hideWhenFinished", "结束后隐藏" },
        { "inputDelay", "输入延迟" },
        { "speaker", "说话者" },
        { "text", "对话文本" },
        { "useTypewriter", "逐字显示" },
        { "charactersPerSecond", "每秒显示字数" },
        { "dialogueDisplaySeconds", "对话显示时间" },
        { "returnFireCompleteMessage", "归火完成提示文本" },
        { "useReturnFireFade", "归火启用黑屏" },
        { "returnFireFadeToBlackDuration", "归火黑屏淡入时长" },
        { "returnFireBlackHoldDuration", "归火全黑停留时间" },
        { "returnFireFadeFromBlackDuration", "归火黑屏淡出时长" },
        { "pauseWorldDuringReturnFireFade", "归火黑屏时暂停世界" },
        { "onDialogue", "对话事件" },
        { "onUpgrade", "祭礼事件" },
        { "onReturnFire", "归火事件" },
        { "onFlaskUpgrade", "凝露事件" },
        { "titleText", "标题文本" },
        { "nameText", "名称文本" },
        { "menuFont", "菜单字体" },
        { "dialogueFont", "对话字体" },
        { "fontResourcePath", "字体资源路径" },
        { "npcStyle", "NPC UI 样式" },
        { "styleResourcePath", "样式资源路径" },
        { "panelSprite", "面板图片" },
        { "buttonSprite", "按钮图片" },
        { "panelSize", "面板尺寸" },
        { "bottomOffset", "底部偏移" },
        { "optionSize", "选项尺寸" },
        { "optionSpacing", "选项间距" },
        { "normalOptionColor", "普通选项颜色" },
        { "selectedOptionColor", "选中选项颜色" },
        { "normalTextColor", "普通文字颜色" },
        { "selectedTextColor", "选中文字颜色" },
        { "borderColor", "边框颜色" },
        { "nameColor", "名称颜色" },

        { "strength", "力量" },
        { "agility", "敏捷" },
        { "intelligence", "智慧" },
        { "vitality", "活力" },
        { "damage", "攻击力" },
        { "critChance", "暴击几率" },
        { "critPower", "暴击伤害" },
        { "isPercent", "百分比显示" },
        { "maxHealth", "最大生命值" },
        { "armor", "护甲" },
        { "evasion", "闪避" },
        { "magicResistance", "魔法抗性" },
        { "fireDamage", "火焰伤害" },
        { "iceDamage", "冰霜伤害" },
        { "lightingDamage", "雷电伤害" },
        { "isIgnited", "燃烧状态" },
        { "isChilled", "冰冷状态" },
        { "isShocked", "感电状态" },
        { "ailmentsDuration", "异常状态持续时间" },
        { "shockStrikePrefab", "感电打击预制体" },
        { "currentHealth", "当前生命值" },

        { "flashDuration", "闪烁持续时间" },
        { "hitMat", "受击材质" },
        { "igniteColor", "燃烧颜色" },
        { "chillColor", "冰冷颜色" },
        { "shockColor", "感电颜色" },

        { "cooldown", "冷却时间" },
        { "dodgeDuration", "闪避持续时间" },
        { "dodgeSpeed", "闪避速度" },
        { "timeStopDuration", "时间停止持续时间" },
        { "inputBufferDuration", "输入缓冲时间" },
        { "followUpWindow", "追击窗口" },
        { "followUpUnlocked", "追击已解锁" },
        { "followUpAttackDuration", "追击持续时间" },
        { "followUpSegmentDurations", "追击每段持续时间" },
        { "followUpSegmentHitTimes", "追击每段命中时间" },
        { "followUpSegmentLungeDurations", "追击每段突进时间" },
        { "followUpSegmentLungeSpeeds", "追击每段突进速度" },
        { "followUpLungeDuration", "追击突进时间" },
        { "followUpLungeSpeed", "追击突进速度" },
        { "followUpHitRadiusMultiplier", "追击命中半径倍率" },
        { "followUpHitForwardOffset", "追击命中前移距离" },
        { "followUpCanHitSameEnemyMultipleTimes", "可重复命中同一敌人" },
        { "castDuration", "施法时间" },
        { "sunPrefab", "太阳预制体" },
        { "spawnOffsetFromPlayerTop", "从玩家顶部生成偏移" },
        { "maxSunDiameter", "太阳最大直径" },
        { "growDuration", "变大持续时间" },
        { "lifeTime", "存在时间" },
        { "duration", "持续时间" },
        { "statBonusPercent", "属性提升比例" },
        { "auraPrefab", "光环预制体" },
        { "auraPixelWidth", "描边像素宽度" },
        { "auraInnerGold", "光环内侧金色" },
        { "auraOuterGold", "光环外侧金色" },
        { "outlinePixelWidth", "描边像素宽度" },
        { "innerGold", "内侧金色" },
        { "outerGold", "外侧金色" },
        { "pulseAlpha", "呼吸透明度" },
        { "sortingOrderOffset", "渲染层级偏移" },
        { "invisibleAlpha", "隐身透明度" },

        { "swordType", "飞剑类型" },
        { "bounceAmount", "弹跳次数" },
        { "bounceGravity", "弹跳重力" },
        { "bounceSpeed", "弹跳速度" },
        { "pierceAmount", "穿透次数" },
        { "pierceGravity", "穿透重力" },
        { "hitCooldown", "命中冷却" },
        { "maxTraveDistance", "最大飞行距离" },
        { "spinDuration", "旋转持续时间" },
        { "SpinGravity", "旋转重力" },
        { "swordPrefab", "飞剑预制体" },
        { "launchForce", "发射力度" },
        { "swordGravity", "飞剑重力" },
        { "freezeTimeDuration", "冻结时间" },
        { "returnSpeed", "返回速度" },
        { "numberOfDots", "指示点数量" },
        { "spaceBeetwenDots", "指示点间距" },
        { "dotPrefab", "指示点预制体" },
        { "dotsParent", "指示点父对象" },

        { "itemName", "物品名称" },
        { "itemDescription", "物品描述" },
        { "itemIcon", "物品图标" },
        { "itemType", "物品类型" },
        { "equipmentType", "装备类型" },
        { "itemCooldown", "物品冷却" },
        { "maxStackSize", "最大堆叠数量" },
        { "dropChance", "掉落几率" },
        { "displayName", "显示名称" },
        { "icon", "图标" },
        { "description", "描述" },
        { "saveKey", "存档键名" },
        { "startingAmount", "初始数量" },
        { "emberData", "余烬数据" },
        { "currentEmbers", "当前余烬" },

        { "defaultLevel", "默认等级" },
        { "defaultRank", "默认等阶" },
        { "applyToExistingEnemiesOnAwake", "启动时应用到场景敌人" },
        { "enemyLevelRules", "敌人等级规则" },
        { "rankEmberRewards", "等阶余烬掉落" },
        { "kind", "敌人种类" },
        { "level", "等级" },
        { "rank", "等阶" },
        { "useManagerSettings", "使用管理器配置" },
        { "percantageModifier", "每级属性成长比例" },
        { "baseReward", "基础余烬" },
        { "rewardPerLevel", "每级增加余烬" },
        { "rankMultiplier", "等阶倍率" },
        { "useManagerFormula", "使用管理器公式" },
        { "fixedReward", "固定余烬" },
        { "randomBonusMax", "随机额外余烬上限" },
        { "rewardMultiplier", "掉落倍率" },
        { "logReward", "输出掉落日志" },
        { "defaultDropPrefab", "默认掉落物预制体" },
        { "useLegacyDropPrefabFallback", "使用旧掉落预制体兜底" },
        { "dropVelocityXRange", "掉落横向速度范围" },
        { "dropVelocityYRange", "掉落纵向速度范围" },
        { "randomSpawnXOffset", "随机生成横向偏移" },
        { "enemyDropRules", "怪物掉落规则" },
        { "enabled", "启用" },
        { "dropPrefabOverride", "掉落物预制体覆盖" },
        { "dropEntries", "掉落物列表" },
        { "item", "掉落物品" },
        { "minAmount", "最小数量" },
        { "maxAmount", "最大数量" },
    };

    private static readonly Dictionary<string, string> WordLabels = new Dictionary<string, string>
    {
        { "Base", "基础" },
        { "Value", "数值" },
        { "Values", "数值" },
        { "Modifier", "加成" },
        { "Modifiers", "加成列表" },
        { "Player", "玩家" },
        { "Enemy", "敌人" },
        { "Attack", "攻击" },
        { "Counter", "反击" },
        { "Damage", "伤害" },
        { "Crit", "暴击" },
        { "Chance", "几率" },
        { "Power", "倍率" },
        { "Health", "生命值" },
        { "Max", "最大" },
        { "Min", "最小" },
        { "Move", "移动" },
        { "Speed", "速度" },
        { "Duration", "持续时间" },
        { "Time", "时间" },
        { "Cooldown", "冷却" },
        { "Force", "力度" },
        { "Jump", "跳跃" },
        { "Dash", "冲刺" },
        { "Dodge", "闪避" },
        { "Precise", "精准" },
        { "Follow", "追击" },
        { "Up", "" },
        { "Window", "窗口" },
        { "Input", "输入" },
        { "Buffer", "缓冲" },
        { "Range", "范围" },
        { "Radius", "半径" },
        { "Multiplier", "倍率" },
        { "Offset", "偏移" },
        { "Forward", "前方" },
        { "Hit", "命中" },
        { "Ground", "地面" },
        { "Wall", "墙体" },
        { "Check", "检测" },
        { "Cheak", "检测" },
        { "Layer", "图层" },
        { "Mask", "遮罩" },
        { "Sword", "飞剑" },
        { "Prefab", "预制体" },
        { "Material", "材质" },
        { "Mat", "材质" },
        { "Color", "颜色" },
        { "Colors", "颜色" },
        { "Sprite", "精灵图" },
        { "Icon", "图标" },
        { "Name", "名称" },
        { "Description", "描述" },
        { "Type", "类型" },
        { "Amount", "数量" },
        { "Count", "数量" },
        { "Size", "大小" },
        { "Parent", "父对象" },
        { "Target", "目标" },
        { "Armor", "护甲" },
        { "Evasion", "闪避" },
        { "Magic", "魔法" },
        { "Resistance", "抗性" },
        { "Fire", "火焰" },
        { "Ice", "冰霜" },
        { "Lighting", "雷电" },
        { "Lightning", "雷电" },
        { "Ignite", "燃烧" },
        { "Chill", "冰冷" },
        { "Shock", "感电" },
        { "Ailment", "异常状态" },
        { "Ailments", "异常状态" },
        { "Slow", "减速" },
        { "Percent", "百分比" },
        { "Percentage", "百分比" },
        { "Gravity", "重力" },
        { "Launch", "发射" },
        { "Return", "返回" },
        { "Dot", "指示点" },
        { "Dots", "指示点" },
        { "Space", "间距" },
        { "Between", "" },
        { "Beetwen", "" },
        { "Spin", "旋转" },
        { "Bounce", "弹跳" },
        { "Pierce", "穿透" },
        { "Life", "存在" },
        { "Grow", "变大" },
        { "Aura", "光环" },
        { "Gold", "金色" },
        { "Inner", "内侧" },
        { "Outer", "外侧" },
    };

    public override void OnInspectorGUI()
    {
        if (!IsProjectScript())
        {
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script"))
            {
                EditorGUILayout.PropertyField(property, GetLabel(property), true);
            }

            enterChildren = false;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private bool IsProjectScript()
    {
        MonoBehaviour behaviour = target as MonoBehaviour;

        if (behaviour == null)
            return false;

        MonoScript script = MonoScript.FromMonoBehaviour(behaviour);

        if (script == null)
            return false;

        string path = AssetDatabase.GetAssetPath(script);
        return path.StartsWith("Assets/Scripts/");
    }

    private static GUIContent GetLabel(SerializedProperty property)
    {
        if (ExactLabels.TryGetValue(property.propertyPath, out string exactByPath))
            return new GUIContent(exactByPath, property.tooltip);

        if (ExactLabels.TryGetValue(property.name, out string exactByName))
            return new GUIContent(exactByName, property.tooltip);

        return new GUIContent(TranslateDisplayName(property.displayName), property.tooltip);
    }

    private static string TranslateDisplayName(string displayName)
    {
        string[] words = displayName.Split(' ');
        bool translatedAny = false;

        for (int i = 0; i < words.Length; i++)
        {
            if (!WordLabels.TryGetValue(words[i], out string translated))
                continue;

            words[i] = translated;
            translatedAny = true;
        }

        if (!translatedAny)
            return displayName;

        return string.Join("", words);
    }
}

[CustomPropertyDrawer(typeof(Stat))]
public class StatPropertyDrawer : PropertyDrawer
{
    private const float ButtonWidth = 28f;
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty baseValue = property.FindPropertyRelative("baseValue");
        SerializedProperty modifiers = property.FindPropertyRelative("modifiers");

        Rect foldoutRect = NextLine(position, 0);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            EditorGUI.PropertyField(NextLine(position, 1), baseValue, new GUIContent("基础数值"));

            Rect modifiersRect = NextLine(position, 2);
            modifiers.isExpanded = EditorGUI.Foldout(modifiersRect, modifiers.isExpanded, "加成列表", true);

            if (modifiers.isExpanded)
            {
                EditorGUI.indentLevel++;

                DrawListSize(NextLine(position, 3), modifiers);
                int line = 4;

                if (modifiers.arraySize == 0)
                {
                    EditorGUI.LabelField(NextLine(position, line), "列表为空");
                    line++;
                }
                else
                {
                    for (int i = 0; i < modifiers.arraySize; i++)
                    {
                        EditorGUI.PropertyField(NextLine(position, line), modifiers.GetArrayElementAtIndex(i), new GUIContent($"加成 {i + 1}"));
                        line++;
                    }
                }

                DrawListButtons(NextLine(position, line), modifiers);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 1;

        if (property.isExpanded)
        {
            lines += 2;

            SerializedProperty modifiers = property.FindPropertyRelative("modifiers");

            if (modifiers.isExpanded)
                lines += 2 + Mathf.Max(1, modifiers.arraySize);
        }

        return lines * (EditorGUIUtility.singleLineHeight + Spacing);
    }

    private Rect NextLine(Rect position, int line)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        return new Rect(position.x, position.y + line * (lineHeight + Spacing), position.width, lineHeight);
    }

    private void DrawListSize(Rect rect, SerializedProperty modifiers)
    {
        int newSize = Mathf.Max(0, EditorGUI.IntField(rect, "数量", modifiers.arraySize));

        if (newSize != modifiers.arraySize)
            modifiers.arraySize = newSize;
    }

    private void DrawListButtons(Rect rect, SerializedProperty modifiers)
    {
        Rect addRect = new Rect(rect.xMax - ButtonWidth * 2f - Spacing, rect.y, ButtonWidth, rect.height);
        Rect removeRect = new Rect(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);

        if (GUI.Button(addRect, "+"))
            modifiers.arraySize++;

        EditorGUI.BeginDisabledGroup(modifiers.arraySize <= 0);

        if (GUI.Button(removeRect, "-"))
            modifiers.arraySize--;

        EditorGUI.EndDisabledGroup();
    }
}
