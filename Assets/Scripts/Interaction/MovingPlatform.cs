using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public float speed = 2f;

    [Header("停顿时间")]
    public float waitTime = 0.5f;

    private Vector3 target; // 当前目标点(终点)
    private float waitCounter; // 计时器：记录停顿时间

    void Start()
    {
        target = pointB.position; // 初始目标点设为 B
    }

    void Update()
    {
        // 第一部分：处理停顿逻辑
        if (waitCounter > 0) //如果在等待
        {
            waitCounter -= Time.deltaTime;
            return;
        }

        // 第二部分：移动平台
        transform.position = Vector3.MoveTowards(
            transform.position,// 当前位置
            target,// 目标点
            speed * Time.deltaTime // 每帧移动
        );

        // 第三部分：到达目标点切换
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            // 开始等待
            waitCounter = waitTime;

            // 切换目标点
            if (target == pointB.position)
                target = pointA.position;
            else
                target = pointB.position;
        }
    }
}