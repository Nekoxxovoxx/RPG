    using System.Collections;
    using UnityEngine;

    public class EntityFX : MonoBehaviour
    {
        private SpriteRenderer sr;

        [Header("Flash FX")]
        [SerializeField] private float flashDuration = 0.12f;
        [SerializeField] private Material hitMat;
        private Material originalMat;
        private Color originalColor;
        private Coroutine flashCoroutine;

        [Header("Ailment colors")]
        [SerializeField] private Color[] igniteColor;
        [SerializeField] private Color[] chillColor;
        [SerializeField] private Color[] shockColor;
        [SerializeField] private Color[] freezeColor =
        {
            new Color(0.3f, 0.85f, 1f, 1f),
            Color.white
        };
        [SerializeField, Min(0.02f)] private float freezeBlinkInterval = 0.18f;

        private Coroutine freezeColorCoroutine;
        private float igniteVisualUntil;

        public void ResetStatusFx()
        {
            CancelInvoke();
            StopAllCoroutines();
            flashCoroutine = freezeColorCoroutine = null;
            igniteVisualUntil = 0f;
            CacheRenderer();
            if (sr != null)
            {
                sr.color = originalColor;
                if (originalMat != null)
                    sr.material = originalMat;
            }
        }

        private void OnDisable() => ResetStatusFx();

        private void LateUpdate()
        {
            if (sr == null)
                return;
            if (freezeColorCoroutine != null)
                sr.color = GetFreezeColor(0, new Color(0.3f, 0.85f, 1f, 1f));
            else if (Time.time < igniteVisualUntil)
            {
                sr.color = Color.red;
                if (originalMat != null)
                    sr.material = originalMat;
            }
        }

        private void Awake()
        {
            CacheRenderer();
        }

        private void Start()
        {
            CacheRenderer();
        }

        private void CacheRenderer()
        {
            if (sr != null)
                return;

            sr = GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                originalMat = sr.material;
                originalColor = sr.color;
            }
        }

        public IEnumerator FlashFX()
        {
            CacheRenderer();

            if (sr == null)
                yield break;

            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(FlashRoutine());
            yield break;
        }

        private IEnumerator FlashRoutine()
        {
            if (hitMat != null)
                sr.material = hitMat;

            sr.color = Color.white;

            yield return new WaitForSeconds(flashDuration);

            sr.color = originalColor;

            if (originalMat != null)
                sr.material = originalMat;

            flashCoroutine = null;
        }

        public void CreateDodgeText()
        {
            Vector3 spawnPosition = transform.position + Vector3.up * 1.5f;
            DamagePopup.Create(spawnPosition, "闪避", Color.white);
        }


        private void RedColorBlink()
        {
            if (sr.color != Color.white)
            { sr.color = Color.white; }
            else { sr.color = Color.red; }
        }

        private void CancelColorChange()
        {
            CancelInvoke();
            CacheRenderer();

            if (sr != null)
                sr.color = originalColor;
        }
        public void IgniteFxFor(float _seconds)
        {
            CacheRenderer();
            if (sr == null || _seconds <= 0f)
                return;

            igniteVisualUntil = Time.time + _seconds;
            sr.color = Color.red;
            Invoke("CancelColorChange", _seconds);
        }

        public void ChillFxFor(float _seconds)
        {
            if (!CanRunColorFx(chillColor))
                return;

            InvokeRepeating("ChillColorFx", 0, 0.3f);
            Invoke("CancelColorChange", _seconds);
        }

        public void ShockFxFor(float _seconds)
        {
            if (!CanRunColorFx(shockColor))
                return;

            InvokeRepeating("ShockColorFx", 0, 0.3f);
            Invoke("CancelColorChange", _seconds);
        }

        public void FreezeFxFor(float _seconds)
        {
            CacheRenderer();

            if (sr == null || _seconds <= 0f)
                return;

            if (freezeColorCoroutine != null)
                StopCoroutine(freezeColorCoroutine);

            freezeColorCoroutine = StartCoroutine(FreezeColorRoutine(_seconds));
        }

        private void IgniteColorFx()
        {
            if (!CanRunColorFx(igniteColor))
                return;

            if (sr.color != igniteColor[0])
                sr.color = igniteColor[0];
            else
                sr.color = igniteColor[1];
        }
        private void ChillColorFx()
        {
            if (!CanRunColorFx(chillColor))
                return;

            if (sr.color != chillColor[0])
                sr.color = chillColor[0];
            else
                sr.color = chillColor[1];
        }

        private void ShockColorFx()
        {
            if (!CanRunColorFx(shockColor))
                return;

            if (sr.color != shockColor[0])
                sr.color = shockColor[0];
            else
                sr.color = shockColor[1];
        }

        private bool CanRunColorFx(Color[] colors)
        {
            CacheRenderer();

            if (sr == null || colors == null || colors.Length < 2)
            {
                CancelInvoke("IgniteColorFx");
                CancelInvoke("ChillColorFx");
                CancelInvoke("ShockColorFx");
                return false;
            }

            return true;
        }

        private IEnumerator FreezeColorRoutine(float seconds)
        {
            float timer = seconds;
            bool usePrimary = true;
            Color primary = GetFreezeColor(0, new Color(0.3f, 0.85f, 1f, 1f));
            Color secondary = GetFreezeColor(1, Color.white);

            while (timer > 0f && sr != null)
            {
                sr.color = usePrimary ? primary : secondary;
                usePrimary = !usePrimary;

                float wait = Mathf.Min(Mathf.Max(0.02f, freezeBlinkInterval), timer);
                timer -= wait;
                yield return new WaitForSeconds(wait);
            }

            if (sr != null)
                sr.color = originalColor;

            freezeColorCoroutine = null;
        }

        private Color GetFreezeColor(int index, Color fallback)
        {
            if (freezeColor == null || index < 0 || index >= freezeColor.Length)
                return fallback;

            return freezeColor[index];
        }

    }
