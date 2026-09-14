using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;

public class BossPhaseTransitionDirector : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float promptCharactersPerSecond = 18f;

    private CinemachineVirtualCamera virtualCamera;
    private CinemachineFramingTransposer framingTransposer;
    private Transform originalFollow;
    private float originalVirtualCameraSize;
    private float originalScreenX;
    private float originalScreenY;
    private Vector3 originalTrackedObjectOffset;
    private bool hasFramingTransposerState;
    private bool hasVirtualCamera;

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private float originalCameraSize;
    private bool hasFallbackCamera;
    private Transform temporaryFollowTarget;
    private TextMeshPro activePromptText;
    private Coroutine cameraShakeRoutine;
    private CinemachineFramingTransposer shakeFramingTransposer;
    private Vector3 shakeOriginalTrackedObjectOffset;
    private CinemachineVirtualCamera shakeVirtualCamera;
    private CinemachineBasicMultiChannelPerlin shakeNoise;
    private NoiseSettings shakeOriginalNoiseProfile;
    private float shakeOriginalNoiseAmplitude;
    private float shakeOriginalNoiseFrequency;
    private bool createdShakeNoiseComponent;
    private bool hasShakeNoise;
    private NoiseSettings runtimeShakeNoiseProfile;
    private Camera shakeFallbackCamera;
    private Vector3 shakeAppliedFallbackOffset;
    private bool hasShakeFramingTransposer;
    private bool hasShakeFallbackCamera;

    public IEnumerator ShowPromptAndFocus(
        Transform target,
        string prompt,
        Color promptColor,
        float promptDuration,
        Vector3 promptOffset,
        float focusOrthographicSize,
        float focusDuration)
    {
        if (target == null)
            yield break;

        yield return ShowPromptAtPosition(target.position + promptOffset, prompt, promptColor, promptDuration);

        CaptureCameraState();
        yield return FocusOn(target, focusOrthographicSize, focusDuration);
    }

    public void BeginCameraSequence()
    {
        CaptureCameraState();
    }

    public IEnumerator ShowPromptAtPosition(Vector3 position, string prompt, Color promptColor, float duration)
    {
        ClearPromptText();

        activePromptText = CreatePromptText(position, string.Empty, promptColor);

        if (activePromptText == null)
            yield break;

        string fullText = prompt ?? string.Empty;

        if (string.IsNullOrEmpty(fullText))
        {
            ClearPromptText();
            yield break;
        }

        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            activePromptText.text = fullText;
            yield return null;
            ClearPromptText();
            yield break;
        }

        float elapsed = 0f;
        int visibleCharacters = 0;
        float secondsPerCharacter = 1f / Mathf.Max(promptCharactersPerSecond, fullText.Length / safeDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            int targetVisibleCharacters = Mathf.Clamp(
                Mathf.FloorToInt(elapsed / secondsPerCharacter),
                0,
                fullText.Length);

            if (targetVisibleCharacters != visibleCharacters)
            {
                visibleCharacters = targetVisibleCharacters;

                if (activePromptText != null)
                    activePromptText.text = fullText.Substring(0, visibleCharacters);
            }

            yield return null;
        }

        ClearPromptText();
    }

    public void ClearPromptText()
    {
        if (activePromptText == null)
            return;

        Destroy(activePromptText.gameObject);
        activePromptText = null;
    }

    public IEnumerator PanHorizontallyTo(Transform target, float duration)
    {
        if (target == null)
            yield break;

        if (!HasCapturedCameraState)
            CaptureCameraState();

        if (hasVirtualCamera && virtualCamera != null)
        {
            Vector3 startPosition = GetCameraReferencePosition();
            Vector3 targetPosition = new Vector3(target.position.x, startPosition.y, startPosition.z);
            Transform followTarget = EnsureTemporaryFollowTarget(startPosition);
            virtualCamera.Follow = followTarget;

            yield return Lerp(duration, t =>
            {
                if (followTarget != null)
                    followTarget.position = Vector3.Lerp(startPosition, targetPosition, t);
            });

            yield break;
        }

        if (hasFallbackCamera && mainCamera != null)
        {
            Vector3 startPosition = mainCamera.transform.position;
            Vector3 targetPosition = new Vector3(target.position.x, startPosition.y, startPosition.z);

            yield return Lerp(duration, t =>
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            });
        }
    }

    public IEnumerator FocusOnTarget(Transform target, float focusOrthographicSize, float duration)
    {
        if (target == null)
            yield break;

        if (!HasCapturedCameraState)
            CaptureCameraState();

        yield return FocusOn(target, focusOrthographicSize, duration);
    }

    public IEnumerator FocusOnTargetCentered(Transform target, float focusOrthographicSize, float duration)
    {
        if (target == null)
            yield break;

        yield return FocusOnPositionCentered(target.position, focusOrthographicSize, duration);
    }

    public IEnumerator FocusOnPositionCentered(Vector3 targetPosition, float focusOrthographicSize, float duration)
    {
        if (!HasCapturedCameraState)
            CaptureCameraState();

        yield return FocusOnPosition(targetPosition, focusOrthographicSize, duration);
    }

    public IEnumerator RestoreCamera(float duration)
    {
        ClearPromptText();

        if (hasVirtualCamera && virtualCamera != null)
        {
            float startSize = virtualCamera.m_Lens.OrthographicSize;

            yield return Lerp(duration, t =>
            {
                SetVirtualCameraSize(Mathf.Lerp(startSize, originalVirtualCameraSize, t));
            });

            virtualCamera.Follow = originalFollow;
            RestoreFramingTransposerState();
            hasVirtualCamera = false;
            hasFallbackCamera = false;
            DestroyTemporaryFollowTarget();
            yield break;
        }

        if (hasFallbackCamera && mainCamera != null)
        {
            Vector3 startPosition = mainCamera.transform.position;
            float startSize = mainCamera.orthographicSize;

            yield return Lerp(duration, t =>
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, originalCameraPosition, t);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, originalCameraSize, t);
            });

            mainCamera.transform.position = originalCameraPosition;
            mainCamera.orthographicSize = originalCameraSize;
            hasFallbackCamera = false;
        }

        DestroyTemporaryFollowTarget();
    }

    public void StartCameraShake(float amplitude, float frequency)
    {
        StopCameraShake();
        CaptureCameraShakeTarget();

        if (!hasShakeFramingTransposer && !hasShakeNoise && !hasShakeFallbackCamera)
            return;

        cameraShakeRoutine = StartCoroutine(CameraShakeRoutine(
            Mathf.Max(0f, amplitude),
            Mathf.Max(0.1f, frequency)));
    }

    public void StopCameraShake()
    {
        if (cameraShakeRoutine != null)
        {
            StopCoroutine(cameraShakeRoutine);
            cameraShakeRoutine = null;
        }

        RestoreCameraShakeTarget();
    }

    private bool HasCapturedCameraState => hasVirtualCamera || hasFallbackCamera;

    private void CaptureCameraState()
    {
        CaptureVirtualCamera();
        CaptureFallbackCamera();
    }

    private void CaptureVirtualCamera()
    {
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>(true);

        if (cameras == null || cameras.Length == 0)
            return;

        CinemachineVirtualCamera bestCamera = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            CinemachineVirtualCamera candidate = cameras[i];

            if (candidate == null)
                continue;

            if (!candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
                continue;

            if (bestCamera == null || candidate.Priority > bestCamera.Priority)
                bestCamera = candidate;
        }

        if (bestCamera == null)
            return;

        virtualCamera = bestCamera;
        originalFollow = virtualCamera.Follow;
        originalVirtualCameraSize = virtualCamera.m_Lens.OrthographicSize;
        CaptureFramingTransposerState();
        hasVirtualCamera = true;
    }

    private void CaptureFramingTransposerState()
    {
        framingTransposer = virtualCamera != null
            ? virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>()
            : null;

        if (framingTransposer == null)
        {
            hasFramingTransposerState = false;
            return;
        }

        originalScreenX = framingTransposer.m_ScreenX;
        originalScreenY = framingTransposer.m_ScreenY;
        originalTrackedObjectOffset = framingTransposer.m_TrackedObjectOffset;
        hasFramingTransposerState = true;
    }

    private void CenterFramingTransposer()
    {
        if (!hasFramingTransposerState || framingTransposer == null)
            return;

        framingTransposer.m_ScreenX = 0.5f;
        framingTransposer.m_ScreenY = 0.5f;
        framingTransposer.m_TrackedObjectOffset = Vector3.zero;
    }

    private void RestoreFramingTransposerState()
    {
        if (!hasFramingTransposerState || framingTransposer == null)
            return;

        framingTransposer.m_ScreenX = originalScreenX;
        framingTransposer.m_ScreenY = originalScreenY;
        framingTransposer.m_TrackedObjectOffset = originalTrackedObjectOffset;
        hasFramingTransposerState = false;
    }

    private void CaptureFallbackCamera()
    {
        mainCamera = Camera.main;
        hasFallbackCamera = mainCamera != null && mainCamera.orthographic;

        if (!hasFallbackCamera)
            return;

        originalCameraPosition = mainCamera.transform.position;
        originalCameraSize = mainCamera.orthographicSize;
    }

    private Vector3 GetCameraReferencePosition()
    {
        if (mainCamera != null)
            return mainCamera.transform.position;

        if (temporaryFollowTarget != null)
            return temporaryFollowTarget.position;

        if (originalFollow != null)
            return originalFollow.position;

        return Vector3.zero;
    }

    private Transform EnsureTemporaryFollowTarget(Vector3 position)
    {
        if (temporaryFollowTarget != null)
        {
            temporaryFollowTarget.position = position;
            return temporaryFollowTarget;
        }

        GameObject targetObject = new GameObject("Boss Camera Temporary Follow Target");
        targetObject.hideFlags = HideFlags.HideAndDontSave;
        targetObject.transform.position = position;
        temporaryFollowTarget = targetObject.transform;
        return temporaryFollowTarget;
    }

    private void DestroyTemporaryFollowTarget()
    {
        if (temporaryFollowTarget == null)
            return;

        Destroy(temporaryFollowTarget.gameObject);
        temporaryFollowTarget = null;
    }

    private void OnDisable()
    {
        StopCameraShake();
        ClearPromptText();
        DestroyTemporaryFollowTarget();
    }

    private void CaptureCameraShakeTarget()
    {
        hasShakeFramingTransposer = false;
        hasShakeFallbackCamera = false;
        hasShakeNoise = false;
        shakeFramingTransposer = null;
        shakeVirtualCamera = null;
        shakeNoise = null;
        shakeFallbackCamera = null;
        shakeAppliedFallbackOffset = Vector3.zero;
        createdShakeNoiseComponent = false;

        CinemachineVirtualCamera bestCamera = FindHighestPriorityVirtualCamera();

        if (bestCamera != null)
        {
            if (TryCaptureCinemachineNoiseShake(bestCamera))
                return;

            shakeFramingTransposer = bestCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

            if (shakeFramingTransposer != null)
            {
                shakeOriginalTrackedObjectOffset = shakeFramingTransposer.m_TrackedObjectOffset;
                hasShakeFramingTransposer = true;
                return;
            }
        }

        shakeFallbackCamera = Camera.main;
        hasShakeFallbackCamera = shakeFallbackCamera != null;
    }

    private bool TryCaptureCinemachineNoiseShake(CinemachineVirtualCamera targetCamera)
    {
        if (targetCamera == null)
            return false;

        CinemachineBasicMultiChannelPerlin noise =
            targetCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise == null)
        {
            noise = targetCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            createdShakeNoiseComponent = true;
        }

        if (noise == null)
            return false;

        shakeVirtualCamera = targetCamera;
        shakeNoise = noise;
        shakeOriginalNoiseProfile = noise.m_NoiseProfile;
        shakeOriginalNoiseAmplitude = noise.m_AmplitudeGain;
        shakeOriginalNoiseFrequency = noise.m_FrequencyGain;

        if (noise.m_NoiseProfile == null)
            noise.m_NoiseProfile = GetOrCreateRuntimeShakeNoiseProfile();

        noise.m_AmplitudeGain = 0f;
        noise.m_FrequencyGain = 0f;
        noise.ReSeed();
        hasShakeNoise = noise.m_NoiseProfile != null;
        return hasShakeNoise;
    }

    private NoiseSettings GetOrCreateRuntimeShakeNoiseProfile()
    {
        if (runtimeShakeNoiseProfile != null)
            return runtimeShakeNoiseProfile;

        runtimeShakeNoiseProfile = ScriptableObject.CreateInstance<NoiseSettings>();
        runtimeShakeNoiseProfile.name = "Runtime Cave Collapse Shake";
        runtimeShakeNoiseProfile.hideFlags = HideFlags.HideAndDontSave;
        runtimeShakeNoiseProfile.PositionNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 0.75f, Amplitude = 1f, Constant = false },
                Y = new NoiseSettings.NoiseParams { Frequency = 0.85f, Amplitude = 1f, Constant = false },
                Z = new NoiseSettings.NoiseParams { Frequency = 0f, Amplitude = 0f, Constant = true }
            },
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 2.2f, Amplitude = 0.45f, Constant = false },
                Y = new NoiseSettings.NoiseParams { Frequency = 2.6f, Amplitude = 0.45f, Constant = false },
                Z = new NoiseSettings.NoiseParams { Frequency = 0f, Amplitude = 0f, Constant = true }
            }
        };
        runtimeShakeNoiseProfile.OrientationNoise = System.Array.Empty<NoiseSettings.TransformNoiseParams>();
        return runtimeShakeNoiseProfile;
    }

    private CinemachineVirtualCamera FindHighestPriorityVirtualCamera()
    {
        CinemachineVirtualCamera[] cameras = FindObjectsOfType<CinemachineVirtualCamera>(true);

        if (cameras == null || cameras.Length == 0)
            return null;

        CinemachineVirtualCamera bestCamera = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            CinemachineVirtualCamera candidate = cameras[i];

            if (candidate == null)
                continue;

            if (!candidate.isActiveAndEnabled || !candidate.gameObject.activeInHierarchy)
                continue;

            if (bestCamera == null || candidate.Priority > bestCamera.Priority)
                bestCamera = candidate;
        }

        return bestCamera;
    }

    private IEnumerator CameraShakeRoutine(float amplitude, float frequency)
    {
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(100f, 200f);

        while (true)
        {
            float time = Time.unscaledTime * frequency;
            Vector3 offset = new Vector3(
                (Mathf.PerlinNoise(seedX, time) - 0.5f) * 2f * amplitude,
                (Mathf.PerlinNoise(seedY, time) - 0.5f) * 2f * amplitude,
                0f);

            if (hasShakeFramingTransposer && shakeFramingTransposer != null)
            {
                shakeFramingTransposer.m_TrackedObjectOffset = shakeOriginalTrackedObjectOffset + offset;
            }
            else if (hasShakeNoise && shakeNoise != null)
            {
                shakeNoise.m_AmplitudeGain = amplitude;
                shakeNoise.m_FrequencyGain = frequency;
            }
            else if (hasShakeFallbackCamera && shakeFallbackCamera != null)
            {
                shakeFallbackCamera.transform.position -= shakeAppliedFallbackOffset;
                shakeFallbackCamera.transform.position += offset;
                shakeAppliedFallbackOffset = offset;
            }

            yield return null;
        }
    }

    private void RestoreCameraShakeTarget()
    {
        if (hasShakeFramingTransposer && shakeFramingTransposer != null)
            shakeFramingTransposer.m_TrackedObjectOffset = shakeOriginalTrackedObjectOffset;

        if (hasShakeNoise && shakeNoise != null)
        {
            shakeNoise.m_NoiseProfile = shakeOriginalNoiseProfile;
            shakeNoise.m_AmplitudeGain = shakeOriginalNoiseAmplitude;
            shakeNoise.m_FrequencyGain = shakeOriginalNoiseFrequency;

            if (createdShakeNoiseComponent && shakeVirtualCamera != null)
                shakeVirtualCamera.DestroyCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }

        if (hasShakeFallbackCamera && shakeFallbackCamera != null)
            shakeFallbackCamera.transform.position -= shakeAppliedFallbackOffset;

        shakeFramingTransposer = null;
        shakeVirtualCamera = null;
        shakeNoise = null;
        shakeOriginalNoiseProfile = null;
        shakeFallbackCamera = null;
        shakeAppliedFallbackOffset = Vector3.zero;
        hasShakeFramingTransposer = false;
        hasShakeNoise = false;
        hasShakeFallbackCamera = false;
        createdShakeNoiseComponent = false;
    }

    private IEnumerator FocusOn(Transform target, float focusOrthographicSize, float duration)
    {
        if (target == null)
            yield break;

        if (hasVirtualCamera && virtualCamera != null)
        {
            CenterFramingTransposer();
            virtualCamera.Follow = target;
            float startSize = virtualCamera.m_Lens.OrthographicSize;
            float targetSize = Mathf.Max(0.5f, focusOrthographicSize);

            yield return Lerp(duration, t =>
            {
                SetVirtualCameraSize(Mathf.Lerp(startSize, targetSize, t));
            });

            yield break;
        }

        if (hasFallbackCamera && mainCamera != null)
        {
            Vector3 startPosition = mainCamera.transform.position;
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y + 1f, startPosition.z);
            float startSize = mainCamera.orthographicSize;
            float targetSize = Mathf.Max(0.5f, focusOrthographicSize);

            yield return Lerp(duration, t =>
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            });
        }
    }

    private IEnumerator FocusOnPosition(Vector3 targetPosition, float focusOrthographicSize, float duration)
    {
        if (hasVirtualCamera && virtualCamera != null)
        {
            CenterFramingTransposer();
            Vector3 startPosition = GetCameraReferencePosition();
            Vector3 centeredTargetPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);
            Transform followTarget = EnsureTemporaryFollowTarget(startPosition);
            virtualCamera.Follow = followTarget;

            float startSize = virtualCamera.m_Lens.OrthographicSize;
            float targetSize = Mathf.Max(0.5f, focusOrthographicSize);

            yield return Lerp(duration, t =>
            {
                if (followTarget != null)
                    followTarget.position = Vector3.Lerp(startPosition, centeredTargetPosition, t);

                SetVirtualCameraSize(Mathf.Lerp(startSize, targetSize, t));
            });

            yield break;
        }

        if (hasFallbackCamera && mainCamera != null)
        {
            Vector3 startPosition = mainCamera.transform.position;
            Vector3 centeredTargetPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);
            float startSize = mainCamera.orthographicSize;
            float targetSize = Mathf.Max(0.5f, focusOrthographicSize);

            yield return Lerp(duration, t =>
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, centeredTargetPosition, t);
                mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            });
        }
    }

    private TextMeshPro CreatePromptText(Vector3 position, string prompt, Color color)
    {
        GameObject promptObject = new GameObject("Boss Phase Prompt");
        promptObject.transform.position = position;

        TextMeshPro text = promptObject.AddComponent<TextMeshPro>();
        text.text = prompt ?? string.Empty;
        text.fontSize = 2.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.sortingOrder = 100;
        return text;
    }

    private void SetVirtualCameraSize(float size)
    {
        if (virtualCamera == null)
            return;

        LensSettings lens = virtualCamera.m_Lens;
        lens.OrthographicSize = size;
        virtualCamera.m_Lens = lens;
    }

    private IEnumerator Lerp(float duration, System.Action<float> apply)
    {
        if (duration <= 0f)
        {
            apply?.Invoke(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);
            apply?.Invoke(t);
            yield return null;
        }

        apply?.Invoke(1f);
    }
}
