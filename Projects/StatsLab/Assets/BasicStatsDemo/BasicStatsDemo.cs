using System;
using System.Collections.Generic;
using Computerzhuxi.Stats;
using UnityEngine;

/// <summary>展示组件式和纯 C# 两名角色的隔离、修正及成长。</summary>
[DefaultExecutionOrder(100)]
public sealed class BasicStatsDemo : MonoBehaviour
{
    [SerializeField] private StatCollectionComponent componentRole;
    [SerializeField] private SampleCharacterStats componentDomain;
    private StatRegistry codeRole;
    private CodeDomain codeDomain;
    private readonly List<StatRegistration> codeRegistrations = new List<StatRegistration>();
    private IReadOnlyDictionary<string, double> componentSnapshot;
    private IReadOnlyDictionary<string, double> codeSnapshot;
    private string snapshot = "尚未拍摄基础快照";
    private bool equipmentActive;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private Font chineseFont;
    private Texture2D buttonNormal;
    private Texture2D buttonHover;
    private Texture2D buttonActive;
    private Vector2 scrollPosition;

    /// <summary>为第二名角色建立纯 C# Registry，并在 Standalone 冒烟模式校验后退出。</summary>
    private void Start()
    {
        if (componentRole == null || componentDomain == null || !componentRole.IsInitialized)
            throw new InvalidOperationException("示例组件角色尚未装配。");
        codeDomain = new CodeDomain();
        codeRole = new StatRegistry();
        codeRegistrations.Add(codeRole.Register(new StatDefinition("maxHealth", minimum: 1, rounding: StatRounding.Nearest),
            codeDomain.Binding("maxHealth")));
        codeRegistrations.Add(codeRole.Register(new StatDefinition("attack", minimum: 0, rounding: StatRounding.Nearest),
            codeDomain.Binding("attack")));
        codeRegistrations.Add(codeRole.Register(new StatDefinition("moveSpeed", minimum: 0), codeDomain.Binding("moveSpeed")));
        codeRegistrations.Add(codeRole.Register(new StatDefinition("bonus", new BonusStrategy()), codeDomain.Binding("bonus")));
        codeRole.AddModifier("bonus", new StatModifier("Bonus", 3, "demo-custom"));
        if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--stats-smoke"))
        {
            if (componentRole.Stats.GetFinal("attack") != 30 || codeRole.GetFinal("attack") != 25
                || codeRole.GetFinal("bonus") != 7)
            {
                Debug.LogError("StatsLab 冒烟初值不正确。");
                Application.Quit(1);
                return;
            }
            Debug.Log("STATS_SMOKE_PASS");
            // 冒烟播放器必须主动退出，验证脚本才能区分成功与超时。
            Application.Quit(0);
        }
    }

    /// <summary>释放纯 C# 注册以及示例面板创建的字体和按钮纹理。</summary>
    private void OnDestroy()
    {
        foreach (StatRegistration registration in codeRegistrations) registration.Dispose();
        codeRegistrations.Clear();
        if (chineseFont != null) Destroy(chineseFont);
        if (buttonNormal != null) Destroy(buttonNormal);
        if (buttonHover != null) Destroy(buttonHover);
        if (buttonActive != null) Destroy(buttonActive);
    }

    /// <summary>在两个独立角色上同时安装同一来源的装备修正。</summary>
    private void AddEquipment()
    {
        if (equipmentActive) return;
        using (componentRole.BeginBatch())
        using (codeRole.BeginBatch())
        {
            componentRole.AddModifier("attack", new StatModifier("Flat", 20, "sword"));
            componentRole.AddModifier("attack", new StatModifier("Multiplier", 1.2, "sword"));
            codeRole.AddModifier("attack", new StatModifier("Flat", 20, "sword"));
            codeRole.AddModifier("attack", new StatModifier("Multiplier", 1.2, "sword"));
        }
        equipmentActive = true;
    }

    /// <summary>按来源卸下两个角色的装备修正。</summary>
    private void RemoveEquipment()
    {
        componentRole.RemoveSource("sword");
        codeRole.RemoveSource("sword");
        equipmentActive = false;
    }

    /// <summary>通过批处理增加永久基础攻击与生命上限。</summary>
    private void ApplyGrowth()
    {
        using (componentRole.BeginBatch())
        using (codeRole.BeginBatch())
        {
            componentRole.SetBase("attack", componentRole.Stats.GetBase("attack") + 5);
            componentRole.SetBase("maxHealth", componentRole.Stats.GetBase("maxHealth") + 10);
            codeRole.SetBase("attack", codeRole.GetBase("attack") + 5);
            codeRole.SetBase("maxHealth", codeRole.GetBase("maxHealth") + 10);
        }
    }

    /// <summary>将两个角色的基础值快照展示给用户，示范存档只取基础值。</summary>
    private void CaptureSnapshot()
    {
        componentSnapshot = componentRole.CaptureBaseSnapshot();
        codeSnapshot = codeRole.CaptureBaseSnapshot();
        snapshot = "已保存基础值：组件攻击 " + componentSnapshot["attack"]
            + "；纯 C# 攻击 " + codeSnapshot["attack"];
    }

    /// <summary>批量恢复两名角色的永久基础值，保留当前装备修正。</summary>
    private void RestoreSnapshot()
    {
        if (componentSnapshot == null || codeSnapshot == null) return;
        using (componentRole.BeginBatch())
        using (codeRole.BeginBatch())
        {
            foreach (KeyValuePair<string, double> item in componentSnapshot)
                componentRole.SetBase(item.Key, item.Value);
            foreach (KeyValuePair<string, double> item in codeSnapshot)
                codeRole.SetBase(item.Key, item.Value);
        }
        snapshot = "已恢复永久基础值；装备与自定义修正仍保留";
    }

    /// <summary>在自适应高对比面板中绘制双入口状态与操作，窄窗口使用滚动避免裁切。</summary>
    private void OnGUI()
    {
        if (codeRole == null) return;
        EnsureGuiStyles();
        Rect panel = new Rect(12f, 12f,
            Mathf.Max(1f, Mathf.Min(920f, Screen.width - 24f)),
            Mathf.Max(1f, Mathf.Min(620f, Screen.height - 24f)));
        Color previousColor = GUI.color;
        try
        {
            // 深色实底让文字在明亮或复杂场景背景前保持清楚。
            GUI.color = new Color(0.035f, 0.07f, 0.12f, 0.96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previousColor;
            Rect content = new Rect(panel.x + 20f, panel.y + 16f,
                Mathf.Max(1f, panel.width - 40f), Mathf.Max(1f, panel.height - 32f));
            GUILayout.BeginArea(content);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            GUILayout.Label("Stats Demo：两名独立角色 / 组件式 + 纯 C#", titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label($"组件角色：HP {componentDomain.CurrentHealth}/{componentDomain.MaxHealth}  攻击 {componentDomain.Attack}  移速 {componentDomain.MoveSpeed}", bodyStyle);
            GUILayout.Label($"纯 C# 角色：HP {codeDomain.CurrentHealth}/{codeDomain.MaxHealth}  攻击 {codeDomain.Attack}  移速 {codeDomain.MoveSpeed}  自定义 Bonus {codeRole.GetFinal("bonus")}", bodyStyle);
            GUILayout.Space(12f);
            if (GUILayout.Button("安装同来源装备", buttonStyle, GUILayout.MinHeight(52f))) AddEquipment();
            if (GUILayout.Button("卸下同来源装备", buttonStyle, GUILayout.MinHeight(52f))) RemoveEquipment();
            if (GUILayout.Button("两名角色永久成长", buttonStyle, GUILayout.MinHeight(52f))) ApplyGrowth();
            if (GUILayout.Button("查看基础快照", buttonStyle, GUILayout.MinHeight(52f))) CaptureSnapshot();
            if (GUILayout.Button("恢复基础快照", buttonStyle, GUILayout.MinHeight(52f))) RestoreSnapshot();
            GUILayout.Space(8f);
            GUILayout.Label(snapshot, bodyStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        finally
        {
            GUI.color = previousColor;
        }
    }

    /// <summary>首次绘制时建立固定字号和高对比样式，避免每帧分配 GUI 资源。</summary>
    private void EnsureGuiStyles()
    {
        if (bodyStyle != null) return;
        chineseFont = FindChineseFont();
        Font displayFont = chineseFont != null ? chineseFont : GUI.skin.font;
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = displayFont, fontSize = 24, fontStyle = FontStyle.Bold,
            wordWrap = true, normal = { textColor = new Color(0.82f, 0.94f, 1f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            font = displayFont, fontSize = 20, wordWrap = true,
            normal = { textColor = Color.white },
            margin = new RectOffset(0, 8, 6, 6)
        };
        buttonNormal = CreateSolidTexture(new Color(0.09f, 0.36f, 0.62f));
        buttonHover = CreateSolidTexture(new Color(0.14f, 0.48f, 0.78f));
        buttonActive = CreateSolidTexture(new Color(0.06f, 0.27f, 0.47f));
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            font = displayFont, fontSize = 20, fontStyle = FontStyle.Bold,
            wordWrap = true, alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(14, 14, 9, 9),
            margin = new RectOffset(0, 8, 5, 5)
        };
        buttonStyle.normal.background = buttonNormal;
        buttonStyle.hover.background = buttonHover;
        buttonStyle.active.background = buttonActive;
        buttonStyle.focused.background = buttonHover;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
        buttonStyle.focused.textColor = Color.white;
    }

    /// <summary>从已安装字体中选择确实包含常用汉字的字体，缺失时保留 Unity 默认字体。</summary>
    private static Font FindChineseFont()
    {
        string[] installed = Font.GetOSInstalledFontNames();
        string[] preferred = { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei",
            "PingFang SC", "Noto Sans CJK SC", "WenQuanYi Micro Hei" };
        foreach (string family in preferred)
        {
            if (!Array.Exists(installed, available =>
                    string.Equals(available, family, StringComparison.OrdinalIgnoreCase))) continue;
            Font candidate = Font.CreateDynamicFontFromOSFont(family, 24);
            if (candidate != null && candidate.HasCharacter('中') && candidate.HasCharacter('攻'))
            {
                candidate.hideFlags = HideFlags.HideAndDontSave;
                return candidate;
            }
            if (candidate != null) Destroy(candidate);
        }
        return null;
    }

    /// <summary>为按钮状态建立可清理的纯色纹理，保持稳定对比。</summary>
    private static Texture2D CreateSolidTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    /// <summary>纯 C# 角色字段与对应绑定；不依赖 Unity 类型。</summary>
    private sealed class CodeDomain
    {
        internal double BaseMaxHealth = 80, MaxHealth = 80, CurrentHealth = 80;
        internal double BaseAttack = 25, Attack = 25;
        internal double BaseMoveSpeed = 4, MoveSpeed = 4;
        internal double BaseBonus = 1, Bonus = 1;

        /// <summary>为程序式入口提供显式业务域绑定。</summary>
        internal IStatBinding Binding(string id)
        {
            switch (id)
            {
                case "maxHealth": return new CodeBinding(() => BaseMaxHealth, v => BaseMaxHealth = v,
                    () => MaxHealth, v => { MaxHealth = v; CurrentHealth = Math.Min(CurrentHealth, v); });
                case "attack": return new CodeBinding(() => BaseAttack, v => BaseAttack = v, () => Attack, v => Attack = v);
                case "moveSpeed": return new CodeBinding(() => BaseMoveSpeed, v => BaseMoveSpeed = v, () => MoveSpeed, v => MoveSpeed = v);
                case "bonus": return new CodeBinding(() => BaseBonus, v => BaseBonus = v, () => Bonus, v => Bonus = v);
                default: throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }

    /// <summary>程序式角色的通用绑定封装。</summary>
    private sealed class CodeBinding : IStatBinding
    {
        private readonly Func<double> getBase, getFinal;
        private readonly Action<double> setBase, applyFinal;
        /// <summary>记录程序式角色字段访问器。</summary>
        internal CodeBinding(Func<double> getBase, Action<double> setBase, Func<double> getFinal, Action<double> applyFinal)
        { this.getBase = getBase; this.setBase = setBase; this.getFinal = getFinal; this.applyFinal = applyFinal; }
        /// <summary>读取基础值。</summary>
        public double GetBase() => getBase();
        /// <summary>写入基础值。</summary>
        public void SetBase(double value) => setBase(value);
        /// <summary>读取最终值。</summary>
        public double GetFinal() => getFinal();
        /// <summary>提交最终值。</summary>
        public void ApplyFinal(double value) => applyFinal(value);
    }

    /// <summary>示范第三方策略以新类别扩展计算规则。</summary>
    private sealed class BonusStrategy : IStatCalculationStrategy
    {
        /// <summary>声明自定义类别。</summary>
        public bool SupportsCategory(string category) => category == "Bonus";
        /// <summary>把 Bonus 修正放大两倍加入基础值。</summary>
        public double Calculate(double baseValue, IReadOnlyList<StatModifier> modifiers)
        {
            foreach (StatModifier modifier in modifiers) baseValue += modifier.Value * 2;
            return baseValue;
        }
    }
}
