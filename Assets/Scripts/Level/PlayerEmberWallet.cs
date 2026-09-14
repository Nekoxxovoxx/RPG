using System;
using UnityEngine;

public class PlayerEmberWallet : MonoBehaviour
{
    private const string RuntimeObjectName = "Player Ember Wallet";
    private const string DefaultSaveKey = "PlayerEmbers";

    public static PlayerEmberWallet instance;

    [SerializeField] private LevelCurrencyData emberData;
    [SerializeField, Min(0)] private int currentEmbers;

    private bool loaded;

    public event Action<int> OnEmbersChanged;

    public LevelCurrencyData EmberData => emberData;
    public int CurrentEmbers => currentEmbers;

    private string SaveKey
    {
        get
        {
            if (emberData != null && !string.IsNullOrWhiteSpace(emberData.SaveKey))
                return emberData.SaveKey;

            return DefaultSaveKey;
        }
    }

    private int StartingAmount => emberData != null ? emberData.StartingAmount : 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolveEmberData();
        Load();

        if (gameObject.name == RuntimeObjectName && transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    public static PlayerEmberWallet GetOrCreate()
    {
        if (instance != null)
            return instance;

        PlayerEmberWallet existingWallet = FindObjectOfType<PlayerEmberWallet>(true);

        if (existingWallet != null)
        {
            instance = existingWallet;
            return instance;
        }

        GameObject walletObject = new GameObject(RuntimeObjectName);
        return walletObject.AddComponent<PlayerEmberWallet>();
    }

    public void AddEmbers(int amount)
    {
        if (amount <= 0)
            return;

        Load();
        SetEmbers(currentEmbers + amount);
    }

    public bool TrySpendEmbers(int amount)
    {
        if (amount <= 0)
            return true;

        Load();

        if (currentEmbers < amount)
            return false;

        SetEmbers(currentEmbers - amount);
        return true;
    }

    public void SetEmbers(int amount)
    {
        currentEmbers = Mathf.Max(0, amount);
        PlayerPrefs.SetInt(SaveKey, currentEmbers);
        PlayerPrefs.Save();
        OnEmbersChanged?.Invoke(currentEmbers);
    }

    public void Load()
    {
        if (loaded)
            return;

        ResolveEmberData();
        currentEmbers = PlayerPrefs.GetInt(SaveKey, StartingAmount);
        loaded = true;
        OnEmbersChanged?.Invoke(currentEmbers);
    }

    private void ResolveEmberData()
    {
        if (emberData != null)
            return;

#if UNITY_EDITOR
        emberData = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelCurrencyData>("Assets/Data/Level/余烬.asset");
#endif
    }
}
