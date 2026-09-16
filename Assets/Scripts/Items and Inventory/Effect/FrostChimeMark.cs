using UnityEngine;

public class FrostChimeMark : MonoBehaviour
{
    [SerializeField, Min(0)] private int stacks;

    public int Stacks => stacks;

    public bool AddStack(int requiredStacks)
    {
        requiredStacks = Mathf.Max(1, requiredStacks);
        stacks++;

        if (stacks < requiredStacks)
            return false;

        stacks = 0;
        return true;
    }

    public void Clear()
    {
        stacks = 0;
    }
}
