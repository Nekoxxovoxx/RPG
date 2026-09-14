using UnityEngine;

public class MagicCircleEffect : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifeTime;
    [SerializeField] private bool destroyAfterLifeTime;
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followOffset;

    private float timer;

    public void Play(float duration = 0f, bool destroyWhenFinished = true)
    {
        lifeTime = Mathf.Max(0f, duration);
        destroyAfterLifeTime = destroyWhenFinished && lifeTime > 0f;
        timer = 0f;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public void Follow(Transform target, Vector3 offset)
    {
        followTarget = target;
        followOffset = offset;
    }

    public void StopFollowing()
    {
        followTarget = null;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void LateUpdate()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followOffset;

        if (!destroyAfterLifeTime || lifeTime <= 0f)
            return;

        timer += Time.deltaTime;

        if (timer >= lifeTime)
            Destroy(gameObject);
    }
}
