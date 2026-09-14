using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class PixelPerfectCameraSnapper : MonoBehaviour
{
    [SerializeField] private float pixelsPerUnit = 16f;
    [SerializeField] private bool snapX = true;
    [SerializeField] private bool snapY = true;
    [SerializeField] private bool quantizeOrthographicSize = true;

    private Camera targetCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedInstaller()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnInitialSceneCamera()
    {
        InstallOnMainCamera();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallOnMainCamera();
    }

    private static void InstallOnMainCamera()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null || mainCamera.GetComponent<PixelPerfectCameraSnapper>() != null)
            return;

        mainCamera.gameObject.AddComponent<PixelPerfectCameraSnapper>();
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (targetCamera != null)
        {
            targetCamera.allowHDR = false;
            targetCamera.allowMSAA = false;
        }
    }

    private void LateUpdate()
    {
        SnapCameraPosition();
    }

    private void OnPreCull()
    {
        SnapCameraPosition();
    }

    private void SnapCameraPosition()
    {
        if (targetCamera == null || !targetCamera.orthographic || pixelsPerUnit <= 0f)
            return;

        if (quantizeOrthographicSize)
            QuantizeOrthographicSize();

        Vector3 position = transform.position;
        float unitPerPixel = GetWorldUnitsPerScreenPixel();

        if (snapX)
            position.x = Mathf.Round(position.x / unitPerPixel) * unitPerPixel;

        if (snapY)
            position.y = Mathf.Round(position.y / unitPerPixel) * unitPerPixel;

        transform.position = position;
    }

    private void QuantizeOrthographicSize()
    {
        int pixelHeight = Mathf.Max(1, targetCamera.pixelHeight);
        float currentVisibleSpritePixels = targetCamera.orthographicSize * 2f * pixelsPerUnit;

        if (currentVisibleSpritePixels <= 0f)
            return;

        int nearestIntegerScale = Mathf.Max(1, Mathf.RoundToInt(pixelHeight / currentVisibleSpritePixels));
        targetCamera.orthographicSize = pixelHeight / (pixelsPerUnit * nearestIntegerScale * 2f);
    }

    private float GetWorldUnitsPerScreenPixel()
    {
        int pixelHeight = Mathf.Max(1, targetCamera.pixelHeight);
        return targetCamera.orthographicSize * 2f / pixelHeight;
    }
}
