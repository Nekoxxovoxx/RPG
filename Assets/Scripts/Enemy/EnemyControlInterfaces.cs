using UnityEngine;

public interface IGenericControlImmuneEnemy
{
    bool IsImmuneToGenericEnemyControl { get; }
}

public interface IPreciseDodgeTimeStopTarget
{
    void SetPreciseDodgeTimeStop(bool timeStopped);
}

public interface IBossIntroPresentationTarget
{
    bool IsIntroUnavailable { get; }

    Vector3 GetIntroFocusPosition();

    void SetIntroPresentationLocked(bool locked);

    void ForceIntroIdle();
}

public interface IBossCombatActivationTarget
{
    bool HasBossCombatStarted { get; }

    void BeginBossCombat(Player player);
}
