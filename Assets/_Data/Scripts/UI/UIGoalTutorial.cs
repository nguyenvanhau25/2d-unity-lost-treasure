using UnityEngine;
using TMPro;

/// <summary>
/// Displays the quest goal on ForestStartScene.
/// Replaces the old princess quest text with the Lost Treasure objective.
/// </summary>
public class UIGoalTutorial : UIDisplay
{
    protected static UIGoalTutorial instance;
    public static UIGoalTutorial Instance => instance;

    protected override void Awake()
    {
        base.Awake();
        if (UIGoalTutorial.instance != null) Debug.LogWarning("Only 1 UITutorial allow to exist");
        UIGoalTutorial.instance = this;

        this.UpdateExistingGoalText();
    }

    private void UpdateExistingGoalText()
    {
        // Update the existing TMP_Text instead of creating a duplicate object
        TMP_Text existingText = GetComponentInChildren<TMP_Text>(true);
        if (existingText != null)
        {
            existingText.text = "✦ MỤC TIÊU: TRUY TÌM KHO BÁU THẤT LẠC ✦\n\n"
                              + "Lần theo tấm bản đồ cổ qua 4 vùng đất huyền bí.\n"
                              + "Vượt qua cạm bẫy gai, giải mã cơ quan và đánh bại quái vật hộ vệ để thu thập đủ 4 MẢNH CHÌA KHÓA CỔ.\n\n"
                              + "Khi nhận đủ 4 mảnh, chúng sẽ tự động hợp nhất thành CHÌA KHÓA CỔ ĐẠI HOÀN CHỈNH để mở RƯƠNG KHO BÁU BÍ ẨN!";
            existingText.fontSize = 13f;
            existingText.lineSpacing = 14f;
            existingText.color = new Color(1f, 0.95f, 0.75f, 1f);
        }
    }
}
