using UnityEngine;

public class PlayerItemDrop : ItemDrop
{
    public override void GenerateDrop()
    {
        // Player death ember drops are intentionally disabled.
        // The upcoming Hellfire system owns death-related ember behavior.
    }
}
