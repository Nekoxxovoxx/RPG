using UnityEngine;

public class PlayerPlatformStick : MonoBehaviour
{
    private Transform originalParent; // 玩家原始父物体
    private bool usingLegacyParenting;

    void Start()
    {
        originalParent = transform.parent; // 记录玩家当前的原始父物体
    }

    void OnCollisionEnter2D(Collision2D collision) //当玩家触碰到碰撞体时触发
    {
        if (collision.gameObject.CompareTag("MovingPlatform")) // 如果带有 "MovingPlatform" tag标签
        {
            if (collision.gameObject.GetComponent<Assets.Scripts.Environment.MovingPlatform>() != null)
                return;

            usingLegacyParenting = true;
            transform.SetParent(collision.transform, true); // 旧平台兜底：没有新 MovingPlatform 组件时才使用父子绑定
        }
    }

    void OnCollisionExit2D(Collision2D collision) //当玩家离开碰撞体时触发
    {
        if (collision.gameObject.CompareTag("MovingPlatform")) // 如果带有 "MovingPlatform" tag标签
        {
            if (collision.gameObject.GetComponent<Assets.Scripts.Environment.MovingPlatform>() != null)
                return;

            if (!usingLegacyParenting)
                return;

            transform.SetParent(originalParent, true); // 将玩家的父物体重置为原始父物体
            usingLegacyParenting = false;
        }
    }
}
