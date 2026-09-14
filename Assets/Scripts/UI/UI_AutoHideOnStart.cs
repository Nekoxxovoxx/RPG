using System.Collections;
using UnityEngine;

public class UI_AutoHideOnStart : MonoBehaviour
{
    [SerializeField] private float visibleDuration = 10f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine hideRoutine;

    private void OnEnable()
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private void OnDisable()
    {
        if (hideRoutine == null)
            return;

        StopCoroutine(hideRoutine);
        hideRoutine = null;
    }

    private IEnumerator HideAfterDelay()
    {
        float elapsed = 0f;

        while (elapsed < visibleDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        hideRoutine = null;
        gameObject.SetActive(false);
    }
}
