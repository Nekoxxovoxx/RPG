using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 8f;
    [SerializeField] private bool randomizeStartFrame = true;
    [SerializeField] private bool useUnscaledTime;

    private SpriteRenderer spriteRenderer;
    private float timer;
    private int currentFrame;

    public void SetFrames(Sprite[] animationFrames, float fps, bool randomStart)
    {
        frames = animationFrames;
        framesPerSecond = fps;
        randomizeStartFrame = randomStart;
        ApplyFrame(0);
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (frames == null || frames.Length == 0)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        currentFrame = randomizeStartFrame ? Random.Range(0, frames.Length) : 0;
        timer = 0;
        ApplyFrame(currentFrame);
    }

    private void OnValidate()
    {
        if (frames == null || frames.Length == 0)
            return;

        ApplyFrame(0);
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1 || framesPerSecond <= 0)
            return;

        timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / framesPerSecond;

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrame = (currentFrame + 1) % frames.Length;
            ApplyFrame(currentFrame);
        }
    }

    private void ApplyFrame(int frameIndex)
    {
        if (frames == null || frames.Length == 0)
            return;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
    }
}
