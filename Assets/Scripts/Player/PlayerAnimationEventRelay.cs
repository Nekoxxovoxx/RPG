using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
    }

    // 向后兼容：无参数版本默认为主药瓶
    public void HealEvent()
    {
        player.HealEvent(0);
    }

    // 新版本：带 flaskIndex 参数 (0=主药瓶, 1-4=副药瓶)
    public void HealEvent(int flaskIndex)
    {
        player.HealEvent(flaskIndex);
    }
}
