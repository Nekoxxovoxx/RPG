using System.Collections;
using Cinemachine;
using UnityEngine;

public class PlayerOpeningAwakingCameraFocus
{
    private readonly Transform playerTransform;
    private readonly float focusOrthographicSize;
    private readonly bool useCameraZoom;

    private CinemachineVirtualCamera virtualCamera;
    private Transform originalFollowTarget;
    private float originalVirtualCameraSize;
    private bool hasVirtualCamera;

    private Camera mainCamera;
    private float originalCameraSize;
    private Vector3 originalCameraPosition;
    private bool hasFallbackCamera;

    public PlayerOpeningAwakingCameraFocus(Transform playerTransform, float focusOrthographicSize, bool useCameraZoom)
    {
        this.playerTransform = playerTransform;
        this.focusOrthographicSize = focusOrthographicSize;
        this.useCameraZoom = useCameraZoom;
    }

    public void ApplyImmediateFocus()
    {
        CaptureVirtualCamera();
        CaptureFallbackCamera();

        if (hasVirtualCamera)
        {
            originalFollowTarget = virtualCamera.Follow;
            originalVirtualCameraSize = virtualCamera.m_Lens.OrthographicSize;
            virtualCamera.Follow = playerTransform;

            if (useCameraZoom)
                SetVirtualCameraSize(focusOrthographicSize);

            ForceCameraToPlayer();
            return;
        }

        if (hasFallbackCamera)
        {
            originalCameraSize = mainCamera.orthographicSize;
            originalCameraPosition = mainCamera.transform.position;

            if (useCameraZoom)
                mainCamera.orthographicSize = focusOrthographicSize;

            mainCamera.transform.position = GetPlayerCameraPosition(mainCamera.transform.position.z);
        }
    }

    public IEnumerator Restore(float duration)
    {
        if (hasVirtualCamera && virtualCamera != null)
        {
            if (useCameraZoom)
                SetVirtualCameraSize(originalVirtualCameraSize);

            virtualCamera.Follow = originalFollowTarget;
            yield return null;
            yield break;
        }

        if (hasFallbackCamera && mainCamera != null)
        {
            Vector3 startPosition = mainCamera.transform.position;

            yield return Lerp(duration, t =>
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, originalCameraPosition, t);
            });

            mainCamera.orthographicSize = originalCameraSize;
            mainCamera.transform.position = originalCameraPosition;
        }
    }

    private void CaptureVirtualCamera()
    {
        CinemachineVirtualCamera[] cameras = Object.FindObjectsOfType<CinemachineVirtualCamera>(true);

        if (cameras == null || cameras.Length == 0)
            return;

        CinemachineVirtualCamera bestCamera = null;

        for (int i = 0; i < cameras.Length; i++)
        {
            CinemachineVirtualCamera candidate = cameras[i];

            if (candidate == null)
                continue;

            if (candidate.Follow == playerTransform)
            {
                bestCamera = candidate;
                break;
            }

            if (bestCamera == null || candidate.Priority > bestCamera.Priority)
                bestCamera = candidate;
        }

        if (bestCamera == null)
            return;

        virtualCamera = bestCamera;
        hasVirtualCamera = true;
    }

    private void CaptureFallbackCamera()
    {
        mainCamera = Camera.main;
        hasFallbackCamera = mainCamera != null && mainCamera.orthographic;

        if (!hasFallbackCamera)
            return;

        originalCameraSize = mainCamera.orthographicSize;
        originalCameraPosition = mainCamera.transform.position;
    }

    private void SetVirtualCameraSize(float size)
    {
        LensSettings lens = virtualCamera.m_Lens;
        lens.OrthographicSize = size;
        virtualCamera.m_Lens = lens;
    }

    private void ForceCameraToPlayer()
    {
        if (virtualCamera == null)
            return;

        virtualCamera.PreviousStateIsValid = false;

        if (mainCamera == null)
            return;

        if (useCameraZoom)
            mainCamera.orthographicSize = focusOrthographicSize;

        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();

        if (brain != null)
            brain.ManualUpdate();
    }

    private Vector3 GetPlayerCameraPosition(float z)
    {
        if (playerTransform == null)
            return new Vector3(0f, 0f, z);

        Vector3 playerPosition = playerTransform.position;
        return new Vector3(playerPosition.x, playerPosition.y + 1f, z);
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
