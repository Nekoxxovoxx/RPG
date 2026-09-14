using UnityEngine;

[DefaultExecutionOrder(10100)]
public class ParallaBackground : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float ParallaxEffect = 1f;
    [SerializeField] private float verticalParallaxEffect = 1f;
    [SerializeField] private bool followVerticalMovement = true;
    [SerializeField] private bool loopHorizontally = true;
    [SerializeField] private bool pixelSnap = true;
    [SerializeField] private float pixelsPerUnit = 16f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;
    private float length;
    private bool hasCameraStartPosition;
    private Camera cachedCamera;

    private void Start()
    {
        ResolveCamera();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        length = spriteRenderer != null ? spriteRenderer.bounds.size.x : 0f;
        startPosition = transform.position;
        CacheCameraStartPosition();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null && !ResolveCamera())
            return;

        if (!hasCameraStartPosition)
            CacheCameraStartPosition();

        Vector3 cameraDelta = cameraTransform.position - cameraStartPosition;
        Vector3 targetPosition = startPosition;

        targetPosition.x += cameraDelta.x * ParallaxEffect;

        if (followVerticalMovement)
            targetPosition.y += cameraDelta.y * verticalParallaxEffect;

        transform.position = SnapToPixelGrid(targetPosition);

        if (loopHorizontally)
            WrapHorizontally(cameraDelta);
    }

    private bool ResolveCamera()
    {
        if (cameraTransform != null)
            return true;

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            cachedCamera = Camera.main;
            return true;
        }

        GameObject mainCamera = GameObject.Find("Main Camera");

        if (mainCamera == null)
            return false;

        cameraTransform = mainCamera.transform;
        cachedCamera = mainCamera.GetComponent<Camera>();
        return true;
    }

    private void CacheCameraStartPosition()
    {
        if (cameraTransform == null)
            return;

        cameraStartPosition = cameraTransform.position;
        hasCameraStartPosition = true;
    }

    private void WrapHorizontally(Vector3 cameraDelta)
    {
        if (length <= 0.01f || cameraTransform == null)
            return;

        float distanceFromCamera = cameraTransform.position.x - transform.position.x;

        if (distanceFromCamera > length)
        {
            startPosition.x += length;
        }
        else if (distanceFromCamera < -length)
        {
            startPosition.x -= length;
        }
        else
        {
            return;
        }

        transform.position = SnapToPixelGrid(new Vector3(
            startPosition.x + cameraDelta.x * ParallaxEffect,
            transform.position.y,
            startPosition.z));
    }

    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        if (!pixelSnap || pixelsPerUnit <= 0f)
            return position;

        float unitPerPixel = GetPixelSnapUnit();
        position.x = Mathf.Round(position.x / unitPerPixel) * unitPerPixel;
        position.y = Mathf.Round(position.y / unitPerPixel) * unitPerPixel;
        return position;
    }

    private float GetPixelSnapUnit()
    {
        if (cachedCamera == null && cameraTransform != null)
            cachedCamera = cameraTransform.GetComponent<Camera>();

        if (cachedCamera != null && cachedCamera.orthographic && cachedCamera.pixelHeight > 0)
            return cachedCamera.orthographicSize * 2f / cachedCamera.pixelHeight;

        return 1f / pixelsPerUnit;
    }
}
