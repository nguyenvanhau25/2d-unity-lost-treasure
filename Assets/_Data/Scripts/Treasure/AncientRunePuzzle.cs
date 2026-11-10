using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hệ thống Giải 3 Câu Đố Cổ Đại (Ancient Temple Riddle Puzzle) để mở Cửa Phong Ấn trong Ngôi Đền Cổ Bị Nguyền Rủa.
/// Thay vì phải đi tìm các cột đá cổ ngữ rải rác, người chơi chỉ cần tiến đến Bia Đá Phong Ấn hoặc Cửa Phong Ấn
/// và giải 3 câu đố bất kỳ (bằng chuột hoặc phím 1, 2, 3) để phá vỡ phong ấn Bắc Điện.
/// </summary>
public class AncientRunePuzzle : MonoBehaviour
{
    private struct RiddleData
    {
        public string Question;
        public string OptionA;
        public string OptionB;
        public string OptionC;
        public int CorrectOptionIndex; // 0 = A, 1 = B, 2 = C
        public string Hint;

        public RiddleData(string question, string optionA, string optionB, string optionC, int correctOptionIndex, string hint)
        {
            this.Question = question;
            this.OptionA = optionA;
            this.OptionB = optionB;
            this.OptionC = optionC;
            this.CorrectOptionIndex = correctOptionIndex;
            this.Hint = hint;
        }
    }

    private static readonly RiddleData[] RiddlePool = new RiddleData[]
    {
        new RiddleData(
            "“Bốn mảnh phân ly khắp bốn phương,\nHợp thành một vật mở ngàn rương.\nRừng sâu, hang tối, đền nguyền rủa,\nThợ săn tìm kiếm suốt dặm trường.”\n— Vật cổ đại đó là gì?",
            "A. Chìa Khóa Cổ Đại (Master Key)",
            "B. Chiếc Khiên Đá Ngủ Quên",
            "C. Ngọn Đuốc Gió Bấc",
            0,
            "Bốn mảnh ghép lại dùng để mở Rương Kho Báu Thất Lạc."),
        new RiddleData(
            "“Càng lấy đi lại càng lớn thêm,\nCàng lấp đầy lại càng nhỏ lại.\nẨn dưới nền đá đền cổ đại,\nKẻ thiếu quan sát dễ sụp chân.”\n— Đó là thứ gì?",
            "A. Ngọn núi đá",
            "B. Cái hố / Hầm bẫy cổ đại",
            "C. Dòng thác bạc",
            1,
            "Đào đất lấy đi thì nó rộng ra, lấp đất vào thì nó nhỏ lại."),
        new RiddleData(
            "“Không chân mà chạy khắp rừng sâu,\nKhông miệng mà thét gào đêm thâu,\nCây cối nghiêng mình khi nó đến,\nChẳng ai nhìn thấy hình dáng đâu.”\n— Đó là hiện tượng gì?",
            "A. Cơn Gió Ngàn Năm",
            "B. Tảng Đá Cổ Ngữ",
            "C. Bóng Đêm Tĩnh Lặng",
            0,
            "Thổi qua làm cây cối nghiêng mình nhưng vô hình không ai thấy."),
        new RiddleData(
            "“Có mũi mà chẳng biết thở than,\nBay đi vun vút tựa sao băng,\nRời khỏi cánh cung người lữ khách,\nXuyên thủng bóng tối giữ bình an.”\n— Vũ khí nào của bạn?",
            "A. Bình Máu Cổ Xưa",
            "B. Mũi Tên Cung Thủ",
            "C. Tấm Bản Đồ Da Dê",
            1,
            "Vũ khí tầm xa được bắn ra từ cánh cung của nhân vật."),
        new RiddleData(
            "“Có sông mà chẳng có nước trôi,\nCó rừng, có núi, có đền đồi,\nDẫn đường lữ khách tìm kho báu,\nGấp gọn trong tay khắp mọi nơi.”\n— Đó là vật gì mở đầu hành trình?",
            "A. Thanh Kiếm Thép Xanh",
            "B. Viên Đá Phong Ấn",
            "C. Tấm Bản Đồ Kho Báu Cổ Đại",
            2,
            "Vật phẩm xuất hiện từ đầu hành trình chỉ đường tới 4 vùng đất."),
        new RiddleData(
            "“Nuôi nó bằng củi thì nó sống,\nCho nó uống nước thì nó tan,\nThắp sáng đêm đen nơi hang thẳm,\nXua tan lạnh giá chốn hoang tàn.”\n— Đó là thứ gì?",
            "A. Ngọn Lửa Thiêng",
            "B. Cánh Cửa Đá",
            "C. Giọt Sương Đêm",
            0,
            "Có củi thì cháy sáng rực rỡ, gặp nước thì tắt ngấm.")
    };

    [Header("Puzzle Elements")]
    [SerializeField] private List<AncientRunePillar> runePillars = new List<AncientRunePillar>();
    [SerializeField] private GameObject objectToReveal;
    [SerializeField] private GameObject barrierToDisable;

    private readonly List<RiddleData> activeRiddles = new List<RiddleData>();
    private int solvedCount = 0;
    private const int RequiredRiddlesToSolve = 3;
    private bool isSolved;
    private bool isModalOpen;
    private bool isTransitioningRiddle;
    private float modalReopenCooldownUntil;

    private TextMeshPro gateStatusLabel;
    private GameObject modalCanvasObj;
    private TextMeshProUGUI headerProgressText;
    private TextMeshProUGUI questionBodyText;
    private TextMeshProUGUI feedbackText;
    private Button[] optionButtons = new Button[3];
    private TextMeshProUGUI[] optionButtonLabels = new TextMeshProUGUI[3];
    private Image[] progressDots = new Image[RequiredRiddlesToSolve];

    public bool IsSolved => this.isSolved;
    public bool IsPuzzleSolved => this.isSolved;
    public int SolvedCount => this.solvedCount;
    public int TotalRequired => RequiredRiddlesToSolve;

    public void Configure(IReadOnlyList<AncientRunePillar> pillars, GameObject revealTarget, GameObject barrierTarget, TextMeshPro gateLabel = null)
    {
        this.runePillars.Clear();
        if (pillars != null)
        {
            for (int i = 0; i < pillars.Count; i++)
            {
                if (pillars[i] != null)
                {
                    pillars[i].Configure(i, this);
                    this.runePillars.Add(pillars[i]);
                }
            }
        }

        this.objectToReveal = revealTarget;
        this.barrierToDisable = barrierTarget;
        this.gateStatusLabel = gateLabel;
        this.PrepareThreeRandomRiddles();
        this.UpdateGateLabel();
    }

    private void Start()
    {
        if (this.activeRiddles.Count == 0)
        {
            this.PrepareThreeRandomRiddles();
        }

        if (this.objectToReveal != null && !this.isSolved)
        {
            this.objectToReveal.SetActive(false);
        }

        this.UpdateGateLabel();
    }

    private void Update()
    {
        if (this.isSolved) return;

        if (this.isModalOpen)
        {
            this.KeepPlayerStillWhileSolving();

            if (!this.isTransitioningRiddle)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    this.OnSelectOption(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    this.OnSelectOption(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    this.OnSelectOption(2);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    this.CloseRiddleModal();
                }
            }
            return;
        }

        // Cho phép người chơi nhấn phím E hoặc F khi đứng gần Bia Đá hoặc Cửa Phong Ấn để mở bảng câu đố
        if (Time.time >= this.modalReopenCooldownUntil && PlayerCtrl.Instance != null)
        {
            Vector3 playerPos = PlayerCtrl.Instance.transform.position;
            bool nearGate = this.barrierToDisable != null &&
                            Mathf.Abs(playerPos.x) <= 5.5f &&
                            playerPos.y >= 1.5f &&
                            playerPos.y <= 6.8f;

            if (nearGate && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return)))
            {
                this.OpenRiddleModal();
            }
        }
    }

    private void PrepareThreeRandomRiddles()
    {
        this.activeRiddles.Clear();
        List<RiddleData> poolCopy = new List<RiddleData>(RiddlePool);
        for (int i = 0; i < poolCopy.Count; i++)
        {
            int swapIdx = Random.Range(i, poolCopy.Count);
            RiddleData temp = poolCopy[i];
            poolCopy[i] = poolCopy[swapIdx];
            poolCopy[swapIdx] = temp;
        }

        int count = Mathf.Min(RequiredRiddlesToSolve, poolCopy.Count);
        for (int i = 0; i < count; i++)
        {
            this.activeRiddles.Add(poolCopy[i]);
        }
    }

    public void OnPillarActivated(AncientRunePillar pillar)
    {
        if (this.isSolved) return;
        this.OpenRiddleModal();
    }

    public void OpenRiddleModal()
    {
        if (this.isSolved || this.isModalOpen || Time.time < this.modalReopenCooldownUntil) return;

        this.EnsureEventSystem();
        if (this.modalCanvasObj == null)
        {
            this.BuildRiddleModalUI();
        }

        this.isModalOpen = true;
        this.isTransitioningRiddle = false;
        this.modalCanvasObj.SetActive(true);
        this.RefreshCurrentRiddleUI();

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.0f);
        }
    }

    public void CloseRiddleModal()
    {
        this.isModalOpen = false;
        this.isTransitioningRiddle = false;
        this.modalReopenCooldownUntil = Time.time + 1.1f;
        if (this.modalCanvasObj != null)
        {
            this.modalCanvasObj.SetActive(false);
        }
    }

    private void KeepPlayerStillWhileSolving()
    {
        if (PlayerCtrl.Instance == null || PlayerCtrl.Instance.PlayerMovement == null) return;
        PlayerCtrl.Instance.PlayerMovement.SetHorizontal(0f);
        PlayerCtrl.Instance.PlayerMovement.SetVertical(0f);
        if (PlayerCtrl.Instance.PlayerMovement._Rb != null)
        {
            PlayerCtrl.Instance.PlayerMovement._Rb.linearVelocity = Vector2.zero;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
    }

    private void BuildRiddleModalUI()
    {
        this.modalCanvasObj = new GameObject("TempleRiddleModalCanvas");
        this.modalCanvasObj.transform.SetParent(transform, false);

        Canvas canvas = this.modalCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;

        CanvasScaler scaler = this.modalCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        this.modalCanvasObj.AddComponent<GraphicRaycaster>();

        // Lớp nền mờ phía sau
        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(this.modalCanvasObj.transform, false);
        RectTransform backdropRect = backdrop.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0.02f, 0.04f, 0.09f, 0.82f);

        // Khung viền vàng cổ đại ở chính giữa màn hình
        GameObject borderObj = new GameObject("RiddleFrameBorder");
        borderObj.transform.SetParent(this.modalCanvasObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.5f, 0.5f);
        borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(980f, 650f);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.92f, 0.74f, 0.28f, 0.98f);

        GameObject innerPanel = new GameObject("RiddleInnerPanel");
        innerPanel.transform.SetParent(borderObj.transform, false);
        RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(5f, 5f);
        innerRect.offsetMax = new Vector2(-5f, -5f);
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = new Color(0.07f, 0.10f, 0.17f, 0.98f);

        // Tiêu đề trên cùng
        this.headerProgressText = this.CreateUIText(
            innerPanel.transform,
            "HeaderTitle",
            new Vector2(0f, 255f),
            new Vector2(880f, 54f),
            28f,
            FontStyles.Bold,
            new Color(0.38f, 0.94f, 1f, 1f));

        // 3 dấu chấm tiến độ câu đố
        for (int i = 0; i < RequiredRiddlesToSolve; i++)
        {
            GameObject dotObj = new GameObject($"ProgressDot_{i + 1}");
            dotObj.transform.SetParent(innerPanel.transform, false);
            RectTransform dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = new Vector2((i - 1) * 140f, 206f);
            dotRect.sizeDelta = new Vector2(115f, 28f);

            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = new Color(0.2f, 0.26f, 0.35f, 0.95f);
            this.progressDots[i] = dotImg;

            TextMeshProUGUI dotLabel = this.CreateUIText(
                dotObj.transform,
                "DotLabel",
                Vector2.zero,
                new Vector2(115f, 28f),
                15f,
                FontStyles.Bold,
                Color.white);
            dotLabel.text = $"CÂU ĐỐ #{i + 1}";
        }

        // Khung chứa nội dung câu đố
        GameObject questionBox = new GameObject("QuestionBox");
        questionBox.transform.SetParent(innerPanel.transform, false);
        RectTransform qBoxRect = questionBox.AddComponent<RectTransform>();
        qBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
        qBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
        qBoxRect.pivot = new Vector2(0.5f, 0.5f);
        qBoxRect.anchoredPosition = new Vector2(0f, 88f);
        qBoxRect.sizeDelta = new Vector2(880f, 175f);
        Image qBoxImg = questionBox.AddComponent<Image>();
        qBoxImg.color = new Color(0.11f, 0.16f, 0.25f, 0.96f);

        this.questionBodyText = this.CreateUIText(
            questionBox.transform,
            "QuestionBody",
            Vector2.zero,
            new Vector2(840f, 155f),
            22f,
            FontStyles.Normal,
            new Color(0.98f, 0.95f, 0.86f, 1f));

        // 3 Nút đáp án (A, B, C) hỗ trợ cả Click chuột và nhấn phím [1], [2], [3]
        for (int i = 0; i < 3; i++)
        {
            int optionIndex = i;
            float yPos = -42f - i * 68f;

            GameObject btnObj = new GameObject($"OptionButton_{i + 1}");
            btnObj.transform.SetParent(innerPanel.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0f, yPos);
            btnRect.sizeDelta = new Vector2(840f, 54f);

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.16f, 0.24f, 0.36f, 0.98f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.16f, 0.24f, 0.36f, 0.98f);
            cb.highlightedColor = new Color(0.25f, 0.42f, 0.62f, 1f);
            cb.pressedColor = new Color(0.35f, 0.62f, 0.85f, 1f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;
            btn.onClick.AddListener(() => this.OnSelectOption(optionIndex));

            TextMeshProUGUI btnText = this.CreateUIText(
                btnObj.transform,
                "OptionLabel",
                Vector2.zero,
                new Vector2(800f, 48f),
                21f,
                FontStyles.Bold,
                new Color(1f, 0.94f, 0.72f, 1f));
            btnText.alignment = TextAlignmentOptions.MidlineLeft;

            this.optionButtons[i] = btn;
            this.optionButtonLabels[i] = btnText;
        }

        // Dòng phản hồi / gợi ý phía dưới
        this.feedbackText = this.CreateUIText(
            innerPanel.transform,
            "FeedbackText",
            new Vector2(0f, -248f),
            new Vector2(860f, 44f),
            19f,
            FontStyles.Bold,
            new Color(0.85f, 0.9f, 0.98f, 1f));

        // Nút đóng tạm thời góc trên phải
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(innerPanel.transform, false);
        RectTransform closeRect = closeBtnObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-18f, -16f);
        closeRect.sizeDelta = new Vector2(110f, 38f);
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = new Color(0.45f, 0.18f, 0.2f, 0.95f);
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(this.CloseRiddleModal);

        TextMeshProUGUI closeTxt = this.CreateUIText(
            closeBtnObj.transform,
            "CloseLabel",
            Vector2.zero,
            new Vector2(104f, 34f),
            15f,
            FontStyles.Bold,
            new Color(1f, 0.88f, 0.88f, 1f));
        closeTxt.text = "ĐÓNG [ESC]";
    }

    private TextMeshProUGUI CreateUIText(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        float fontSize,
        FontStyles style,
        Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private void RefreshCurrentRiddleUI()
    {
        if (this.activeRiddles.Count == 0)
        {
            this.PrepareThreeRandomRiddles();
        }

        int currentIdx = Mathf.Clamp(this.solvedCount, 0, this.activeRiddles.Count - 1);
        RiddleData current = this.activeRiddles[currentIdx];

        if (this.headerProgressText != null)
        {
            this.headerProgressText.text = $"📜 GIẢI ĐỐ MỞ PHONG ẤN ĐỀN CỔ ({this.solvedCount}/{RequiredRiddlesToSolve} ĐÃ GIẢI)";
        }

        for (int i = 0; i < this.progressDots.Length; i++)
        {
            if (this.progressDots[i] == null) continue;
            if (i < this.solvedCount)
            {
                this.progressDots[i].color = new Color(0.18f, 0.72f, 0.36f, 1f);
            }
            else if (i == this.solvedCount)
            {
                this.progressDots[i].color = new Color(0.88f, 0.68f, 0.18f, 1f);
            }
            else
            {
                this.progressDots[i].color = new Color(0.2f, 0.26f, 0.35f, 0.95f);
            }
        }

        if (this.questionBodyText != null)
        {
            this.questionBodyText.text = $"<color=#FFD966><b>CÂU ĐỐ THỨ {currentIdx + 1}/{RequiredRiddlesToSolve}:</b></color>\n{current.Question}";
        }

        string[] options = { current.OptionA, current.OptionB, current.OptionC };
        for (int i = 0; i < 3; i++)
        {
            if (this.optionButtonLabels[i] != null)
            {
                this.optionButtonLabels[i].text = $"  [Phím {i + 1}]  {options[i]}";
            }
            if (this.optionButtons[i] != null)
            {
                this.optionButtons[i].interactable = true;
            }
        }

        if (this.feedbackText != null)
        {
            this.feedbackText.text = "💡 Chọn đáp án bằng CHUỘT hoặc nhấn phím số [1], [2], [3] trên bàn phím.";
            this.feedbackText.color = new Color(0.82f, 0.9f, 1f, 0.95f);
        }
    }

    private void OnSelectOption(int chosenOptionIndex)
    {
        if (this.isSolved || !this.isModalOpen || this.isTransitioningRiddle) return;

        int currentIdx = Mathf.Clamp(this.solvedCount, 0, this.activeRiddles.Count - 1);
        RiddleData current = this.activeRiddles[currentIdx];

        if (chosenOptionIndex == current.CorrectOptionIndex)
        {
            StartCoroutine(this.HandleCorrectAnswer());
        }
        else
        {
            if (AudioManagerr.Instance != null)
            {
                AudioManagerr.Instance.PlaySFX("SelectSound", 0.75f);
            }

            if (this.feedbackText != null)
            {
                this.feedbackText.text = $"✗ Chưa chính xác! Gợi ý: {current.Hint}";
                this.feedbackText.color = new Color(1f, 0.55f, 0.45f, 1f);
            }
        }
    }

    private IEnumerator HandleCorrectAnswer()
    {
        this.isTransitioningRiddle = true;
        this.solvedCount++;

        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("SelectSound", 1.2f);
        }

        for (int i = 0; i < this.runePillars.Count; i++)
        {
            if (this.runePillars[i] != null)
            {
                this.runePillars[i].SetSolvedProgress(this.solvedCount, RequiredRiddlesToSolve);
            }
        }

        this.UpdateGateLabel();

        if (this.feedbackText != null)
        {
            this.feedbackText.text = $"✓ CHÍNH XÁC! Đã hóa giải {this.solvedCount}/{RequiredRiddlesToSolve} tầng phong ấn cổ đại!";
            this.feedbackText.color = new Color(0.35f, 1f, 0.62f, 1f);
        }

        yield return new WaitForSeconds(0.55f);

        if (this.solvedCount >= RequiredRiddlesToSolve)
        {
            this.isSolved = true;
            this.CloseRiddleModal();
            this.OnPuzzleSolved();
        }
        else
        {
            this.isTransitioningRiddle = false;
            this.RefreshCurrentRiddleUI();
        }
    }

    private void UpdateGateLabel()
    {
        if (this.gateStatusLabel == null) return;
        this.gateStatusLabel.text =
            $"🔒 CỬA PHONG ẤN ĐỀN CỔ [{this.solvedCount}/{RequiredRiddlesToSolve} CÂU ĐỐ]\n" +
            $"<size=2.0>[Chạm vào Bia Đá Cổ Ngữ hoặc nhấn phím E để giải 3 câu đố mở cửa]</size>";
    }

    private void OnPuzzleSolved()
    {
        if (AudioManagerr.Instance != null)
        {
            AudioManagerr.Instance.PlaySFX("StartSound", 1f);
        }

        if (this.objectToReveal != null)
        {
            this.objectToReveal.SetActive(true);
        }

        if (this.barrierToDisable != null)
        {
            this.barrierToDisable.SetActive(false);
        }

        SceneStoryOverlay overlay = SceneStoryOverlay.GetOrCreate();
        if (overlay != null)
        {
            StartCoroutine(overlay.ShowMessage(
                "ĐÃ GIẢI ĐỦ 3 CÂU ĐỐ — PHONG ẤN ĐÃ MỞ!",
                "Cánh cửa Bắc Điện đã khai mở! Hãy đánh bại Guardian Đền Cổ để nhận Mảnh Chìa Khóa Thứ Tư!",
                2.8f,
                new Color(0.35f, 0.95f, 1f, 1f)));
        }
    }
}
