// CrewDetailPanel.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrewDetailPanel : MonoBehaviour
{
    private CrewManager crewManager;

    [Header("Panel Roots")]
    [SerializeField] private GameObject emptyGuidePanel;
    [SerializeField] private GameObject detailContentRoot;

    [Header("Top Area")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text nameLevelText;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private TMP_Text speechText;

    [Header("Character Sprites")]
    [SerializeField] private Sprite engineerSprite;
    [SerializeField] private Sprite explorerSprite;
    [SerializeField] private Sprite securitySprite;

    [Header("Locked Character Sprites")]
    [SerializeField] private Sprite lockedEngineerSprite;
    [SerializeField] private Sprite lockedExplorerSprite;
    [SerializeField] private Sprite lockedSecuritySprite;

    [Header("Current Effect Box")]
    [SerializeField] private TMP_Text currentTitleText;      // "현재 효과"
    [SerializeField] private TMP_Text currentEffectText;     // 실제 효과 내용

    [Header("Next Effect Box")]
    [SerializeField] private TMP_Text nextTitleText;         // "다음 효과"
    [SerializeField] private TMP_Text nextEffectText;        // 실제 다음 효과 내용

    [Header("Next Upgrade Box")]
    [SerializeField] private TMP_Text nextUpgradeTitleText;  // "다음 업그레이드 해금"
    [SerializeField] private TMP_Text nextUpgradeContentText;
    [SerializeField] private GameObject nextUnlockImage;

    [Header("Level Cost Box")]
    [SerializeField] private TMP_Text levelTitleText;         
    [SerializeField] private Image levelCostImage;            
    [SerializeField] private TMP_Text levelResourceNameText;  
    [SerializeField] private TMP_Text levelCostText;          

    [Header("Level Button")]
    [SerializeField] private Button levelUpButtonDetail;
    [SerializeField] private TMP_Text levelUpButtonText;

    [Header("Upgrade Cost Box")]
    [SerializeField] private TMP_Text upgradeTitleText;        
    [SerializeField] private Image upgradeCostImage;           
    [SerializeField] private TMP_Text upgradeResourceNameText; 
    [SerializeField] private TMP_Text upgradeCostText;         

    [Header("Upgrade Button")]
    [SerializeField] private Button upgradeButtonDetail;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private GameObject upgradeLockIcon;

    //[Header("Upgrade Hover")]
    //[SerializeField] private UpgradeButtonHover upgradeButtonHoverDetail;

    private CrewRole currentRole = CrewRole.Engineer;
    private bool hasSelectedRole = false;

    private void OnEnable()
    {
        TrySetCrewManager();

        if (levelUpButtonDetail != null)
        {
            levelUpButtonDetail.onClick.RemoveAllListeners();
            levelUpButtonDetail.onClick.AddListener(OnLevelUpClicked);
        }

        if (upgradeButtonDetail != null)
        {
            upgradeButtonDetail.onClick.RemoveAllListeners();
            upgradeButtonDetail.onClick.AddListener(OnUpgradeClicked);
        }

        EventManager.OnCrewLevelUp += OnCrewChanged;
        EventManager.OnCrewUpgrade += OnCrewChanged;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    private void Start()
    {
        //Show(CrewRole.Engineer);
        HideDetail();
    }

    private void HideDetail()
    {
        hasSelectedRole = false;

        if (emptyGuidePanel != null)
            emptyGuidePanel.SetActive(true);

        if (detailContentRoot != null)
            detailContentRoot.SetActive(false);
    }

    private void OnDisable()
    {
        if (levelUpButtonDetail != null)
            levelUpButtonDetail.onClick.RemoveAllListeners();

        if (upgradeButtonDetail != null)
            upgradeButtonDetail.onClick.RemoveAllListeners();

        EventManager.OnCrewLevelUp -= OnCrewChanged;
        EventManager.OnCrewUpgrade -= OnCrewChanged;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    private void TrySetCrewManager()
    {
        if (CrewManager.Instance != null)
            crewManager = CrewManager.Instance;
    }

    public void Show(CrewRole role)
    {
        TrySetCrewManager();

        currentRole = role;
        hasSelectedRole = true;

        if (emptyGuidePanel != null)
            emptyGuidePanel.SetActive(false);

        if (detailContentRoot != null)
            detailContentRoot.SetActive(true);

        if (crewManager == null)
        {
            Debug.LogWarning($"{nameof(CrewDetailPanel)}: CrewManager가 연결되지 않았습니다.");
            return;
        }

        bool unlocked = crewManager.IsCrewUnlocked(role);

        if (!unlocked)
        {
            RefreshLockedUI(role);
            return;
        }

        RefreshUnlockedUI(role);
    }

    private void OnCrewChanged(int _)
    {
        if (!hasSelectedRole)
            return;

        Show(currentRole);
    }

    private void OnLevelUpClicked()
    {
        TrySetCrewManager();

        if (crewManager == null)
            return;

        crewManager.LevelUpCrew(currentRole);
        SoundManager.Instance?.PlaySfx("crew_levelup");
        Show(currentRole);
    }

    private void OnUpgradeClicked()
    {
        TrySetCrewManager();

        if (crewManager == null)
            return;

        crewManager.UpgradeCrew(currentRole);
        SoundManager.Instance?.PlaySfx("crew_upgrade");
        Show(currentRole);
    }

    private void RefreshLockedUI(CrewRole role)
    {
        if (characterImage != null)
            characterImage.sprite = GetLockedCharacterSprite(role);

        if (nameLevelText != null)
            nameLevelText.text = $"{GetRoleName(role)}\n잠김";

        if (gradeText != null)
            gradeText.text = "등급 -";

        if (speechText != null)
            speechText.text = GetLockedSpeech(role);

        if (currentTitleText != null)
            currentTitleText.text = "현재 효과";

        if (currentEffectText != null)
            currentEffectText.text = "???";

        if (nextTitleText != null)
            nextTitleText.text = "다음 효과";

        if (nextEffectText != null)
            nextEffectText.text = "???";

        if (nextUpgradeTitleText != null)
            nextUpgradeTitleText.text = "해금 조건";

        if (nextUpgradeContentText != null)
            nextUpgradeContentText.text = GetUnlockConditionText(role);

        if (nextUnlockImage != null)
            nextUnlockImage.SetActive(true);

        RefreshLockedLevelCostBox();
        RefreshLockedUpgradeCostBox(role);
        RefreshLockedButtons();
    }

    private void RefreshUnlockedUI(CrewRole role)
    {
        int level = crewManager.GetCrewLevel(role);
        int maxLevel = crewManager.GetMaxLevel(role);
        int grade = crewManager.GetCrewGrade(role);
        int nextUnlockLevel = crewManager.GetNextUnlockLevel(role);

        if (characterImage != null)
            characterImage.sprite = GetCharacterSprite(role);

        if (nameLevelText != null)
            nameLevelText.text = $"{GetRoleName(role)}\nLv.{level}/{maxLevel}";

        if (gradeText != null)
            gradeText.text = $"등급 {grade + 1}";

        if (speechText != null)
            speechText.text = GetSpeech(role, level, nextUnlockLevel);

        RefreshCurrentEffectBox(role);
        RefreshNextEffectBox(role, level, maxLevel, grade);
        RefreshNextUpgradeBox(level, nextUnlockLevel);
        RefreshLevelCostBox(level, maxLevel);
        RefreshUpgradeCostBox(level, nextUnlockLevel);
        RefreshButtons(level, maxLevel, nextUnlockLevel);
    }

    private void RefreshCurrentEffectBox(CrewRole role)
    {
        if (currentTitleText != null)
            currentTitleText.text = "현재 효과";
        if (currentEffectText == null) return;

        switch (role)
        {
            case CrewRole.Engineer:
                {
                    float bonus = crewManager.GetRefineEfficiencyBonus() * 100f;
                    string text = $"정제 효율 +{bonus:F1}%";
                    text += crewManager.IsAbilityUnlocked(role, 0)   // grade > 0 = 등급 2+
                        ? $"\n장비 제작 효율 +{bonus:F1}%"
                        : "\n장비 제작 효율 <color=#888888>(등급 2 해금)</color>";
                    text += crewManager.IsAbilityUnlocked(role, 1)   // grade > 1 = 등급 3+
                        ? $"\n드론 성능 +{bonus:F1}%"
                        : "\n드론 성능 <color=#888888>(등급 3 해금)</color>";
                    currentEffectText.text = text;
                    break;
                }

            case CrewRole.Explorer:
                {
                    float miningBonus = crewManager.GetMiningAmountBonus() * 100f;
                    float rareBonus = crewManager.GetRareResourceBonus() * 100f;
                    string text = $"채굴량 +{miningBonus:F1}%";
                    text += crewManager.IsAbilityUnlocked(role, 0)   // grade > 0 = 등급 2+
                        ? $"\n희귀 광맥 발견 +{rareBonus:F1}%"
                        : "\n희귀 광맥 발견 <color=#888888>(등급 2 해금)</color>";
                    text += crewManager.IsAbilityUnlocked(role, 1)   // grade > 1 = 등급 3+
                        ? $"\n코어 조각 드랍률 +{miningBonus:F1}%"
                        : "\n코어 조각 드랍률 <color=#888888>(등급 3 해금)</color>";
                    currentEffectText.text = text;
                    break;
                }

            case CrewRole.SecurityOfficer:
                {
                    float bonus = crewManager.GetEventSuccessRateBonus() * 100f;
                    currentEffectText.text = $"이벤트 성공률 +{bonus:F1}%";
                    break;
                }
        }

            /*case CrewRole.SecurityOfficer:
                currentEffectText.text =
                    GetAbilityLine("이벤트 성공률", crewManager.GetEventSuccessRateBonus(), crewManager.IsAbilityUnlocked(role, 0));
                break;*/

    }

    private void RefreshNextEffectBox(CrewRole role, int level, int maxLevel, int grade)
    {
        if (nextTitleText != null)
            nextTitleText.text = "다음 효과";

        if (nextEffectText == null)
            return;

        if (level >= maxLevel)
        {
            nextEffectText.text = "최고 레벨입니다.";
            return;
        }

        float currentBonus = crewManager.GetCurrentStat(role) - 1f;
        float nextBonus = crewManager.GetPreviewStat(role, level + 1, grade) - 1f;
        float diff = nextBonus - currentBonus;

        switch (role)
        {
            case CrewRole.Engineer:
                nextEffectText.text =
                    $"정제/장비/드론 효율\n" +
                    $"+{currentBonus * 100f:F1}% → +{nextBonus * 100f:F1}%\n" +
                    $"상승량 +{diff * 100f:F1}%";
                break;

            case CrewRole.Explorer:
                nextEffectText.text =
                    $"채굴량/희귀 발견\n" +
                    $"+{currentBonus * 100f:F1}% → +{nextBonus * 100f:F1}%\n" +
                    $"상승량 +{diff * 100f:F1}%";
                break;

            case CrewRole.SecurityOfficer:
                nextEffectText.text =
                    $"이벤트 성공률\n" +
                    $"+{currentBonus * 100f:F1}% → +{nextBonus * 100f:F1}%\n" +
                    $"상승량 +{diff * 100f:F1}%";
                break;
        }
    }

    private void RefreshNextUpgradeBox(int level, int nextUnlockLevel)
    {
        if (nextUpgradeTitleText != null)
            nextUpgradeTitleText.text = "다음 업그레이드 해금";

        if (nextUpgradeContentText == null)
            return;

        if (nextUnlockLevel < 0)
        {
            nextUpgradeContentText.text = "모든 능력 해금 완료";

            // 모든 업그레이드가 끝났을 때만 이미지 숨김
            if (nextUnlockImage != null)
                nextUnlockImage.SetActive(false);

            return;
        }

        if (level < nextUnlockLevel)
        {
            int progress = Mathf.Min(level, nextUnlockLevel);
            nextUpgradeContentText.text = $"Lv.{nextUnlockLevel} 달성 시 해금\n진행도 {progress}/{nextUnlockLevel}";
        }
        else
        {
            nextUpgradeContentText.text = $"Lv.{nextUnlockLevel} 달성 완료\n업그레이드 가능";
        }

        // 조건 달성 전/후 모두 이미지 유지
        if (nextUnlockImage != null)
            nextUnlockImage.SetActive(true);
    }

    private void RefreshLevelCostBox(int level, int maxLevel)
    {
        if (levelTitleText != null)
            levelTitleText.text = "레벨업 비용";

        if (level >= maxLevel)
        {
            if (levelCostImage != null)
                levelCostImage.gameObject.SetActive(false);

            if (levelResourceNameText != null)
                levelResourceNameText.gameObject.SetActive(false);

            if (levelCostText != null)
            {
                levelCostText.text = "최고 레벨";
                levelCostText.color = Color.white;
            }

            return;
        }

        float currentCrystal = ResourceManager.Instance != null
                ? ResourceManager.Instance.Crystal
                : 0f;
        float requiredCrystal = crewManager.GetLevelUpCost(currentRole);

        if (levelCostImage != null)
            levelCostImage.gameObject.SetActive(true);

        if (levelResourceNameText != null)
        {
            levelResourceNameText.gameObject.SetActive(true);
            levelResourceNameText.text = "크리스탈";
        }

        if (levelCostText != null)
        {
            levelCostText.text = $"{FormatNumber(currentCrystal)} / {FormatNumber(requiredCrystal)}";
            levelCostText.color = currentCrystal >= requiredCrystal ? Color.white : Color.red;
        }
    }

    private void RefreshUpgradeCostBox(int level, int nextUnlockLevel)
    {
        if (nextUnlockLevel < 0)
        {
            if (upgradeTitleText != null)
                upgradeTitleText.text = "업그레이드";

            if (upgradeCostImage != null)
                upgradeCostImage.gameObject.SetActive(false);

            if (upgradeResourceNameText != null)
                upgradeResourceNameText.gameObject.SetActive(false);

            if (upgradeCostText != null)
            {
                upgradeCostText.text = "최고 등급";
                upgradeCostText.color = Color.white;
            }

            return;
        }

        if (level < nextUnlockLevel)
        {
            if (upgradeTitleText != null)
                upgradeTitleText.text = "업그레이드 조건";

            if (upgradeCostImage != null)
                upgradeCostImage.gameObject.SetActive(false);

            if (upgradeResourceNameText != null)
                upgradeResourceNameText.gameObject.SetActive(false);

            if (upgradeCostText != null)
            {
                upgradeCostText.text = $"Lv.{nextUnlockLevel} 필요";
                upgradeCostText.color = Color.gray;
            }

            return;
        }

        float currentRefined = ResourceManager.Instance != null
                ? ResourceManager.Instance.RefinedResource
                : 0f;
        float requiredRefined = crewManager.GetUpgradeCost(currentRole);

        if (upgradeTitleText != null)
            upgradeTitleText.text = "업그레이드 비용";

        if (upgradeCostImage != null)
            upgradeCostImage.gameObject.SetActive(true);

        if (upgradeResourceNameText != null)
        {
            upgradeResourceNameText.gameObject.SetActive(true);
            upgradeResourceNameText.text = "정제 크리스탈";
        }

        if (upgradeCostText != null)
        {
            upgradeCostText.text = $"{FormatNumber(currentRefined)} / {FormatNumber(requiredRefined)}";
            upgradeCostText.color = currentRefined >= requiredRefined ? Color.white : Color.red;
        }
    }

    private void RefreshButtons(int level, int maxLevel, int nextUnlockLevel)
    {
        bool isMaxLevel = level >= maxLevel;
        bool canLevelUp = !isMaxLevel && crewManager.CanLevelUp(currentRole);

        if (levelUpButtonText != null)
            levelUpButtonText.text = isMaxLevel ? "최고 레벨" : "레벨업";

        if (levelUpButtonDetail != null)
            levelUpButtonDetail.interactable = canLevelUp;

        bool isMaxGrade = nextUnlockLevel < 0;
        bool isLockedByLevel = !isMaxGrade && level < nextUnlockLevel;
        bool canUpgrade = !isMaxGrade && !isLockedByLevel && crewManager.CanUpgrade(currentRole);

        if (upgradeButtonText != null)
            upgradeButtonText.text = isMaxGrade ? "최고 등급" : "업그레이드";

        if (upgradeLockIcon != null)
            upgradeLockIcon.SetActive(isLockedByLevel);

        if (upgradeButtonDetail != null)
            upgradeButtonDetail.interactable = canUpgrade;

        /*if (upgradeButtonHoverDetail != null)
        {
            if (isMaxGrade)
                upgradeButtonHoverDetail.SetTooltip("모든 업그레이드가 완료되었습니다.");
            else if (isLockedByLevel)
                upgradeButtonHoverDetail.SetTooltip($"Lv.{nextUnlockLevel} 달성 시 업그레이드 가능");
            else if (!crewManager.CanUpgrade(currentRole))
                upgradeButtonHoverDetail.SetTooltip("정제 크리스탈이 부족합니다.");
            else
                upgradeButtonHoverDetail.SetTooltip("");
        }*/
    }

    private void RefreshLockedLevelCostBox()
    {
        if (levelTitleText != null)
            levelTitleText.text = "레벨업 비용";

        if (levelCostImage != null)
            levelCostImage.gameObject.SetActive(false);

        if (levelResourceNameText != null)
            levelResourceNameText.gameObject.SetActive(false);

        if (levelCostText != null)
        {
            levelCostText.text = "-";
            levelCostText.color = Color.gray;
        }
    }

    private void RefreshLockedUpgradeCostBox(CrewRole role)
    {
        if (upgradeTitleText != null)
            upgradeTitleText.text = "해금 조건";

        if (upgradeCostImage != null)
            upgradeCostImage.gameObject.SetActive(false);

        if (upgradeResourceNameText != null)
            upgradeResourceNameText.gameObject.SetActive(false);

        if (upgradeCostText != null)
        {
            upgradeCostText.text = GetUnlockConditionText(role);
            upgradeCostText.color = Color.gray;
        }
    }

    private void RefreshLockedButtons()
    {
        if (levelUpButtonText != null)
            levelUpButtonText.text = "레벨업";

        if (levelUpButtonDetail != null)
            levelUpButtonDetail.interactable = false;

        if (upgradeButtonText != null)
            upgradeButtonText.text = "업그레이드";

        if (upgradeLockIcon != null)
            upgradeLockIcon.SetActive(true);

        if (upgradeButtonDetail != null)
            upgradeButtonDetail.interactable = false;
    }

    private void OnResourceChanged()
    {
        if (!hasSelectedRole) return;

        // 비용 박스만 갱신 (캐릭터/효과 박스는 불필요)
        int level = crewManager.GetCrewLevel(currentRole);
        int maxLevel = crewManager.GetMaxLevel(currentRole);
        int nextUnlockLevel = crewManager.GetNextUnlockLevel(currentRole);

        RefreshLevelCostBox(level, maxLevel);
        RefreshUpgradeCostBox(level, nextUnlockLevel);
        RefreshButtons(level, maxLevel, nextUnlockLevel);
    }
    private string GetAbilityLine(string label, float bonus, bool unlocked)
    {
        return unlocked
            ? $"{label} +{bonus * 100f:F1}%"
            : $"{label} ???";
    }

    private Sprite GetCharacterSprite(CrewRole role)
    {
        return role switch
        {
            CrewRole.Engineer => engineerSprite,
            CrewRole.Explorer => explorerSprite,
            CrewRole.SecurityOfficer => securitySprite,
            _ => engineerSprite
        };
    }

    private string GetSpeech(CrewRole role, int level, int nextUnlockLevel)
    {
        if (nextUnlockLevel > 0 && level < nextUnlockLevel)
            return $"Lv.{nextUnlockLevel}이 되면 새로운 능력을 열 수 있어요.";

        return role switch
        {
            CrewRole.Engineer => "장비와 드론의 효율을 향상시켜\n탐사대의 생산력을 높입니다.",
            CrewRole.Explorer => "희귀 자원과 코어 단서를\n발견하는 데 특화되어 있습니다.",
            CrewRole.SecurityOfficer => "위험 이벤트 성공률을 높이고\n탐사 안정성을 확보합니다.",
            _ => "준비 완료입니다."
        };
    }

    private Sprite GetLockedCharacterSprite(CrewRole role)
    {
        return role switch
        {
            CrewRole.Engineer => lockedEngineerSprite,
            CrewRole.Explorer => lockedExplorerSprite,
            CrewRole.SecurityOfficer => lockedSecuritySprite,
            _ => lockedEngineerSprite
        };
    }

    private string GetLockedSpeech(CrewRole role)
    {
        return role switch
        {
            CrewRole.Explorer => "엔지니어를 성장시키면 탐사자를\n고용할 수 있습니다.",
            CrewRole.SecurityOfficer => "탐사자를 성장시키면\n보안요원을\n 고용할 수 있습니다.",
            _ => "아직 해금되지\n않았습니다."
        };
    }

    private string GetRoleName(CrewRole role)
    {
        return role switch
        {
            CrewRole.Engineer => "엔지니어",
            CrewRole.Explorer => "탐사자",
            CrewRole.SecurityOfficer => "보안요원",
            _ => role.ToString()
        };
    }

    private string GetUnlockConditionText(CrewRole role)
    {
        return role switch
        {
            CrewRole.Explorer => "엔지니어 Lv.10 필요",
            CrewRole.SecurityOfficer => "탐사자 Lv.10 필요",
            _ => ""
        };
    }

    private string FormatNumber(float value)
    {
        if (value >= 1_000_000f) return $"{value / 1_000_000f:F1}M";
        if (value >= 1_000f) return $"{value / 1_000f:F1}K";
        return value.ToString("F0");
    }
}
