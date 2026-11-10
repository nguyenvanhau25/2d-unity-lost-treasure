using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Core Manager for "Lost Treasure - Truy Tìm Kho Báu Thất Lạc".
/// Handles cross-scene persistence for the 4 Key Fragments, Master Key assembly,
/// and the Mystery Treasure Chest unlocking sequence.
/// </summary>
public class LostTreasureManager : MonoBehaviour
{
    public const int TotalRequiredFragments = 4;

    private static LostTreasureManager instance;
    public static LostTreasureManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("LostTreasureManager");
                instance = obj.AddComponent<LostTreasureManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    [Header("Key Fragments State")]
    [SerializeField] private bool[] collectedFragments = new bool[TotalRequiredFragments];
    [SerializeField] private bool isMasterKeyCrafted;
    [SerializeField] private bool isChestOpened;

    public bool[] CollectedFragments => this.collectedFragments;
    public bool IsMasterKeyCrafted => this.isMasterKeyCrafted;
    public bool IsChestOpened => this.isChestOpened;

    public int TotalCollectedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < this.collectedFragments.Length; i++)
            {
                if (this.collectedFragments[i]) count++;
            }
            return count;
        }
    }

    public static readonly string[] LandNames = new string[]
    {
        "Rừng Khởi Đầu",
        "Rừng Hắc Ám",
        "Hang Động Cổ Xưa",
        "Đền Cổ Bị Nguyền Rủa"
    };

    public static readonly string[] FragmentNames = new string[]
    {
        "Mảnh Chìa Khóa Rừng Xanh",
        "Mảnh Chìa Khóa Hắc Ám",
        "Mảnh Chìa Khóa Lòng Đất",
        "Mảnh Chìa Khóa Đền Cổ"
    };

    public event Action<int, string> OnFragmentCollectedEvent;
    public event Action OnMasterKeyCraftedEvent;
    public event Action OnChestUnlockedEvent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += this.OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Auto reset progress if returning to Main Menu
        if (scene.name == "MainMenu")
        {
            this.ResetProgress();
        }
    }

    public bool HasFragment(int index)
    {
        if (index < 0 || index >= this.collectedFragments.Length) return false;
        return this.collectedFragments[index];
    }

    public bool CollectFragment(int index, string customFragmentName = null, string customMessage = null)
    {
        if (index < 0 || index >= this.collectedFragments.Length) return false;
        if (this.collectedFragments[index]) return false; // Already collected

        this.collectedFragments[index] = true;
        string fragmentName = !string.IsNullOrEmpty(customFragmentName) ? customFragmentName : FragmentNames[index];
        string landName = LandNames[index];

        this.OnFragmentCollectedEvent?.Invoke(index, fragmentName);

        if (this.TotalCollectedCount >= TotalRequiredFragments)
        {
            this.CraftMasterKey();
        }
        else
        {
            // Show cinematic banner
            string defaultMsg = $"Bạn đã thu thập được {fragmentName} tại {landName}.\nHãy tiếp tục hành trình giải mã kho báu!";
            StartCoroutine(this.ShowFragmentBanner(index, fragmentName, customMessage ?? defaultMsg));
        }

        return true;
    }

    private void CraftMasterKey()
    {
        if (this.isMasterKeyCrafted) return;
        this.isMasterKeyCrafted = true;
        this.OnMasterKeyCraftedEvent?.Invoke();

        StartCoroutine(this.RunMasterKeyAssemblyCutscene());
    }

    private IEnumerator ShowFragmentBanner(int index, string fragmentName, string message)
    {
        yield return new WaitForSeconds(0.2f);
        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
        {
            string title = $"MẢNH CHÌA KHÓA CỔ [{this.TotalCollectedCount}/{TotalRequiredFragments}]";
            Color accent = new Color(1f, 0.85f, 0.3f);
            yield return overlay.ShowMessage(title, message, 3.2f, accent);
        }
    }

    private IEnumerator RunMasterKeyAssemblyCutscene()
    {
        yield return new WaitForSeconds(0.3f);
        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("ResonanceSkill", 1.5f);
        }

        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
        {
            string title = "★ HỢP NHẤT CHÌA KHÓA HOÀN CHỈNH ★";
            string msg = "Bốn mảnh chìa khóa cổ đại hòa quyện vào nhau, tái sinh thành [CHÌA KHÓA CỔ ĐẠI HOÀN CHỈNH]!\nGiờ đây bạn đã có thể mở Rương Kho Báu Bí Ẩn!";
            Color masterKeyColor = new Color(1f, 0.9f, 0.4f);
            yield return overlay.ShowMessage(title, msg, 4.0f, masterKeyColor);
        }
    }

    public void UnlockTreasureChest()
    {
        if (this.isChestOpened) return;
        this.isChestOpened = true;
        this.OnChestUnlockedEvent?.Invoke();
    }

    public void ResetProgress()
    {
        for (int i = 0; i < this.collectedFragments.Length; i++)
        {
            this.collectedFragments[i] = false;
        }
        this.isMasterKeyCrafted = false;
        this.isChestOpened = false;
    }

    /// <summary>
    /// Helper to get current scene index mapped to fragment index:
    /// ForestStartScene -> 0
    /// DarkForestScene  -> 1
    /// InsideCave       -> 2
    /// RecusePrincess   -> 3
    /// </summary>
    public static int GetCurrentSceneFragmentIndex()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (LostTreasureSceneHelper.IsForestStart(sceneName)) return 0;
        if (LostTreasureSceneHelper.IsDarkForest(sceneName)) return 1;
        if (LostTreasureSceneHelper.IsAncientCave(sceneName)) return 2;
        if (LostTreasureSceneHelper.IsCursedTemple(sceneName)) return 3;
        return -1;
    }
}
