using System;

public static class DamageAttribution
{
    private static int playerDamageDepth;

    public static bool IsPlayerDamage => playerDamageDepth > 0;

    public static void RunAsPlayerDamage(Action action)
    {
        if (action == null)
            return;

        playerDamageDepth++;

        try
        {
            action();
        }
        finally
        {
            playerDamageDepth = Math.Max(0, playerDamageDepth - 1);
        }
    }
}
