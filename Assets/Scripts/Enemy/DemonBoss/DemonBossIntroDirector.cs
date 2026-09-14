using UnityEngine;

[DisallowMultipleComponent]
public class DemonBossIntroDirector : BossIntroSequenceDirector
{
    private const string DemonBossUiGroupName = "\u8d64\u9ad3boss";
    private const string DemonPortalNameToken = "\u8d64\u9ad3";

    protected override void Awake()
    {
        ConfigureDemonDefaults();
        base.Awake();
    }

    protected override void OnEnable()
    {
        ConfigureDemonDefaults();
        base.OnEnable();
    }

    public override void AutoConfigure()
    {
        ConfigureDemonDefaults();
        base.AutoConfigure();
    }

    private void ConfigureDemonDefaults()
    {
        ConfigureAutoResolveNames(DemonBossUiGroupName, DemonPortalNameToken);
    }
}
