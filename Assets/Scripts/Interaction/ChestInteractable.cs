using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInteractable : Interactable
{
    [Header("宝箱")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Sprite> openAnimationFrames = new List<Sprite>();
    [SerializeField] private float frameDuration = 0.08f;
    [SerializeField] private bool disableAfterOpen = true;

    [Header("掉落")]
    [SerializeField] private GameObject dropPrefab;
    [SerializeField] private List<InventoryItem> dropItems = new List<InventoryItem>();
    [SerializeField] private Vector2 dropVelocityXRange = new Vector2(-4f, 4f);
    [SerializeField] private Vector2 dropVelocityYRange = new Vector2(8f, 12f);

    [Header("音效")]
    [SerializeField] private AudioEventPlayer audioEventPlayer;
    [SerializeField] private string openCueName = "ChestOpen";
    [SerializeField] private string dropCueName = "ChestDrop";

    private bool opened;
    private Coroutine openCoroutine;

    public override bool CanInteract(Player player)
    {
        return base.CanInteract(player) && !opened;
    }

    protected override void OnInteract(Player player)
    {
        opened = true;

        PlayAudioCue(openCueName);

        if (openCoroutine != null)
            StopCoroutine(openCoroutine);

        openCoroutine = StartCoroutine(OpenRoutine());

        if (disableAfterOpen)
            SetCanInteract(false);
    }

    private IEnumerator OpenRoutine()
    {
        PlayOpenFrame(0);

        for (int i = 0; i < openAnimationFrames.Count; i++)
        {
            PlayOpenFrame(i);
            yield return new WaitForSeconds(frameDuration);
        }

        DropItems();

        PlayAudioCue(dropCueName);

        openCoroutine = null;
    }

    private void PlayOpenFrame(int frameIndex)
    {
        if (spriteRenderer == null || openAnimationFrames == null || openAnimationFrames.Count == 0)
            return;

        frameIndex = Mathf.Clamp(frameIndex, 0, openAnimationFrames.Count - 1);
        spriteRenderer.sprite = openAnimationFrames[frameIndex];
    }

    private void DropItems()
    {
        if (dropPrefab == null || dropItems == null)
            return;

        for (int i = 0; i < dropItems.Count; i++)
        {
            InventoryItem dropItem = dropItems[i];

            if (dropItem == null || dropItem.data == null)
                continue;

            int stackSize = Mathf.Max(1, dropItem.stackSize);

            for (int j = 0; j < stackSize; j++)
                DropItem(dropItem.data);
        }
    }

    private void DropItem(ItemData itemData)
    {
        GameObject newDrop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
        ItemObject itemObject = newDrop.GetComponent<ItemObject>();

        if (itemObject == null)
            itemObject = newDrop.GetComponentInChildren<ItemObject>();

        if (itemObject == null)
        {
            Debug.LogWarning("宝箱掉落物预制体缺少 ItemObject 组件: " + dropPrefab.name, this);
            return;
        }

        Vector2 velocity = new Vector2(
            Random.Range(dropVelocityXRange.x, dropVelocityXRange.y),
            Random.Range(dropVelocityYRange.x, dropVelocityYRange.y));

        itemObject.SetupItem(itemData, velocity);
    }

    private void PlayAudioCue(string cueName)
    {
        if (string.IsNullOrWhiteSpace(cueName))
            return;

        if (audioEventPlayer == null)
            TryGetComponent(out audioEventPlayer);

        if (audioEventPlayer == null)
            return;

        audioEventPlayer.PlayCue(cueName);
    }
}