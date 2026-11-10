using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Displays the "Finding the Ancient Map" prologue and backstory on MainMenu.
/// Gives players the narrative hook: finding the map and beginning the quest.
/// </summary>
public class MainMenuStoryPrologue : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttachOnMainMenu()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;

        if (FindFirstObjectByType<MainMenuStoryPrologue>() == null)
        {
            GameObject obj = new GameObject("MainMenuStoryPrologue");
            obj.AddComponent<MainMenuStoryPrologue>();
        }
    }

    private GameObject prologueModal;

    private void Start()
    {
        this.BuildPrologueUI();
    }

    private void BuildPrologueUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Modal container
        this.prologueModal = new GameObject("StoryPrologueModal", typeof(RectTransform));
        this.prologueModal.transform.SetParent(canvas.transform, false);

        RectTransform rect = this.prologueModal.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 25f);
        rect.sizeDelta = new Vector2(620f, 320f);

        // Background box
        Image bg = this.prologueModal.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        // Border
        Outline outline = this.prologueModal.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.82f, 0.28f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(this.prologueModal.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.78f);
        titleRect.anchorMax = new Vector2(1f, 0.96f);
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) titleText.font = TMP_Settings.defaultFontAsset;
        titleText.text = "✦ BỐI CẢNH: BẢN ĐỒ KHO BÁU THẤT LẠC ✦";
        titleText.fontSize = 18f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.88f, 0.35f);

        // Lore Story text
        GameObject bodyObj = new GameObject("StoryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(this.prologueModal.transform, false);
        RectTransform bodyRect = bodyObj.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.06f, 0.26f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.76f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI bodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) bodyText.font = TMP_Settings.defaultFontAsset;
        bodyText.text = "Tại một phế tích hoang tàn, bạn vô tình nhặt được một tấm <color=#FFDD55>Bản Đồ Da Thú Cổ Xưa</color> đã nhuốm màu thời gian.\n\n"
                      + "Bản đồ chỉ lối qua <b>4 vùng đất hiểm trở</b>, nơi cất giữ <b>4 Mảnh Chìa Khóa Cổ</b>. Hãy vượt qua cạm bẫy, giải mã cơ quan, đánh bại các Hộ Vệ Thần để ghép thành <color=#FFDD55>Chìa Khóa Hoàn Chỉnh</color> và mở <color=#FFAA33>Rương Kho Báu Bí Ẩn</color>!";
        bodyText.fontSize = 14f;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.lineSpacing = 16f;
        bodyText.color = new Color(0.92f, 0.92f, 0.95f);

        // Close / Confirm button
        GameObject btnObj = new GameObject("BtnStartAdventure", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(this.prologueModal.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.28f, 0.06f);
        btnRect.anchorMax = new Vector2(0.72f, 0.22f);
        btnRect.anchoredPosition = Vector2.zero;
        btnRect.sizeDelta = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.75f, 0.55f, 0.12f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(this.OnConfirmPrologue);

        GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.anchoredPosition = Vector2.zero;
        btnTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnText = btnTextObj.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) btnText.font = TMP_Settings.defaultFontAsset;
        btnText.text = "NHẬN BẢN ĐỒ & KHỞI HÀNH ▶";
        btnText.fontSize = 15f;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
    }

    private void OnConfirmPrologue()
    {
        if (this.prologueModal != null)
        {
            this.prologueModal.SetActive(false);
        }

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.2f);
        }
    }
}
