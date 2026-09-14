using UnityEngine;

[AddComponentMenu("Environment/Stair Surface")]
public class StairSurface : MonoBehaviour
{
    [SerializeField] private int upDirection = 1;

    public int UpDirection => upDirection >= 0 ? 1 : -1;

    public void Setup(int direction)
    {
        upDirection = direction >= 0 ? 1 : -1;
    }
}
