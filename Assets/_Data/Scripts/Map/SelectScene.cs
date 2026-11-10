using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum SceneName
{
    ForestStartScene = 0,
    DarkForestScene = 1,
    AncientCaveScene = 2,
    GuardianBattlefieldScene = 3,
    CursedTempleScene = 4,
    MainMenu = 5,
    BeginScreen = 6,
    islandScene = 7,
    TreasureSanctumScene = 8,

    // Legacy aliases for backward compatibility
    InsideCave = AncientCaveScene,
    LastWar = GuardianBattlefieldScene,
    RecusePrincess = CursedTempleScene,
    Special = TreasureSanctumScene
}

/// <summary>
/// Đồng bộ tên các màn chơi (Map/Scene) theo đúng cốt truyện Lost Treasure - Truy Tìm Kho Báu Thất Lạc.
/// Hỗ trợ song song cả tên file mới lẫn tên cũ và tự động đổi tên file .unity trong Unity Editor.
/// </summary>
public static class LostTreasureSceneHelper
{
    public static bool IsForestStart(string sceneName)
    {
        return sceneName == "ForestStartScene";
    }

    public static bool IsDarkForest(string sceneName)
    {
        return sceneName == "DarkForestScene";
    }

    public static bool IsAncientCave(string sceneName)
    {
        return sceneName == "AncientCaveScene" || sceneName == "InsideCave";
    }

    public static bool IsGuardianBattlefield(string sceneName)
    {
        return sceneName == "GuardianBattlefieldScene" || sceneName == "LastWar";
    }

    public static bool IsCursedTemple(string sceneName)
    {
        return sceneName == "CursedTempleScene" || sceneName == "RecusePrincess";
    }

    public static bool IsTreasureSanctum(string sceneName)
    {
        return sceneName == "TreasureSanctumScene" || sceneName == "Special";
    }

    /// <summary>
    /// Trả về tên Scene thực tế có trong Build Settings (tự động khớp dù file đã đổi tên hay chưa).
    /// </summary>
    public static string ResolveLoadableSceneName(string requestedName)
    {
        if (string.IsNullOrEmpty(requestedName)) return "MainMenu";

        if (IsAncientCave(requestedName))
            return SelectAvailable("AncientCaveScene", "InsideCave");
        if (IsGuardianBattlefield(requestedName))
            return SelectAvailable("GuardianBattlefieldScene", "LastWar");
        if (IsCursedTemple(requestedName))
            return SelectAvailable("CursedTempleScene", "RecusePrincess");
        if (IsTreasureSanctum(requestedName))
            return SelectAvailable("TreasureSanctumScene", "Special");

        return requestedName;
    }

    public static string ResolveLoadableSceneName(SceneName sceneEnum)
    {
        switch (sceneEnum)
        {
            case SceneName.ForestStartScene:
                return "ForestStartScene";
            case SceneName.DarkForestScene:
                return "DarkForestScene";
            case SceneName.AncientCaveScene:
                return SelectAvailable("AncientCaveScene", "InsideCave");
            case SceneName.GuardianBattlefieldScene:
                return SelectAvailable("GuardianBattlefieldScene", "LastWar");
            case SceneName.CursedTempleScene:
                return SelectAvailable("CursedTempleScene", "RecusePrincess");
            case SceneName.TreasureSanctumScene:
                return SelectAvailable("TreasureSanctumScene", "Special");
            case SceneName.MainMenu:
                return "MainMenu";
            default:
                return sceneEnum.ToString();
        }
    }

    private static string SelectAvailable(string preferredNewName, string fallbackLegacyName)
    {
        if (Application.CanStreamedLevelBeLoaded(preferredNewName))
            return preferredNewName;
        if (Application.CanStreamedLevelBeLoaded(fallbackLegacyName))
            return fallbackLegacyName;
        return preferredNewName;
    }

    /// <summary>
    /// Tên hiển thị chuẩn cốt truyện cho từng màn Map (dùng cho Scene Tester, HUD, Cổng dịch chuyển).
    /// </summary>
    public static string GetMapDisplayName(string sceneName)
    {
        if (sceneName == "MainMenu")
            return "Màn Chính: Bản Đồ Kho Báu (MainMenu)";
        if (IsForestStart(sceneName))
            return "Màn 1: Rừng Sao Khởi Đầu (ForestOfStars)";
        if (IsDarkForest(sceneName))
            return "Màn 2: Rừng Hắc Ám (DarkForest)";
        if (IsAncientCave(sceneName))
            return "Màn 3: Hang Động Cổ Xưa (AncientCave)";
        if (IsGuardianBattlefield(sceneName))
            return "Màn 4: Chiến Trường Cổ Đại (GuardianBattlefield)";
        if (IsCursedTemple(sceneName))
            return "Màn 5: Ngôi Đền Bị Nguyền Rủa (CursedTemple)";
        if (IsTreasureSanctum(sceneName))
            return "Màn 6: Tháp Kho Báu Tối Thượng (TreasureSanctum)";

        return sceneName;
    }

    public static int GetStageOrder(string sceneName)
    {
        if (sceneName == "MainMenu") return 0;
        if (IsForestStart(sceneName)) return 1;
        if (IsDarkForest(sceneName)) return 2;
        if (IsAncientCave(sceneName)) return 3;
        if (IsGuardianBattlefield(sceneName)) return 4;
        if (IsCursedTemple(sceneName)) return 5;
        if (IsTreasureSanctum(sceneName)) return 6;
        return 99;
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RenameLegacySceneAssetsInEditor()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool changed = false;
            changed |= TryRenameSceneAsset("Assets/_Data/Scenes/InsideCave.unity", "AncientCaveScene");
            changed |= TryRenameSceneAsset("Assets/_Data/Scenes/LastWar.unity", "GuardianBattlefieldScene");
            changed |= TryRenameSceneAsset("Assets/_Data/Scenes/RecusePrincess.unity", "CursedTempleScene");
            changed |= TryRenameSceneAsset("Assets/_Data/Scenes/Special.unity", "TreasureSanctumScene");

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        };
    }

    private static bool TryRenameSceneAsset(string oldAssetPath, string newBaseName)
    {
        string newAssetPath = $"Assets/_Data/Scenes/{newBaseName}.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(newAssetPath) != null)
            return false;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(oldAssetPath) == null)
            return false;

        string err = AssetDatabase.RenameAsset(oldAssetPath, newBaseName);
        return string.IsNullOrEmpty(err);
    }
#endif
}
