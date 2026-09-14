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
            if (!CanRunColorFx(igniteColor))
                return;

            InvokeRepeating("IgniteColorFx", 0, 0.3f);
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

    }
