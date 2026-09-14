using UnityEngine;

[DisallowMultipleComponent]
public class ChaosWolfLordIntroDirector : BossIntroSequenceDirector
{
    private const string WolfBossUiGroupName = "\u6df7\u6c8c\u72fc\u4e3b";
    private const string WolfPortalNameToken = "\u72fc\u4e3b";

    protected override void Awake()
    {
        ConfigureWolfDefaults();
        base.Awake();
    }

    protected override void OnEnable()
    {
        ConfigureWolfDefaults();
        base.OnEnable();
    }

    public override void AutoConfigure()
    {
        ConfigureWolfDefaults();
        base.AutoConfigure();
    }

    private void ConfigureWolfDefaults()
    {
        ConfigureAutoResolveNames(WolfBossUiGroupName, WolfPortalNameToken);
    }
}
