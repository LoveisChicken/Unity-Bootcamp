using ProjectNOVA.PSJ;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentDetailUI : MonoBehaviour
{
    [Header("Detail Root")]
    [SerializeField] private GameObject emptyGuideRoot;
    [SerializeField] private GameObject detailContentRoot;

    [Header("Detail Header")]
    [SerializeField] private TMP_Text nameLevelText;
    [SerializeField] private TMP_Text levelBadgeText;

    [Header("Current Effect")]
    [SerializeField] private TMP_Text powerNameText;
    [SerializeField] private TMP_Text powerText;

    [Header("Next Effect")]
    [SerializeField] private TMP_Text nextEffectTitleText;
    [SerializeField] private TMP_Text nextPowerNameText;
    [SerializeField] private TMP_Text nextPowerText;

    [Header("Cost")]
    [SerializeField] private GameObject costBox;
    [SerializeField] private TMP_Text costTitleText;
    [SerializeField] private TMP_Text refinedNameText;
    [SerializeField] private TMP_Text refinedValueText;
    [SerializeField] private TMP_Text starDustNameText;
    [SerializeField] private TMP_Text starDustValueText;

    [Header("Status Badge")]
    [SerializeField] private GameObject statusBadgeBox;
    [SerializeField] private TMP_Text statusValueText;

    [Header("Action")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonLabel;

    [Header("Button Sprites")]
    [SerializeField] private Sprite actionUnlockedSprite;
    [SerializeField] private Sprite actionLockedSprite;
    [SerializeField] private Sprite actionMaxSprite;

    [Header("Equipment Icon")]
    [SerializeField] private Image equipmentIconImage;
    [SerializeField] private Sprite drillSprite;
    [SerializeField] private Sprite scannerSprite;
    [SerializeField] private Sprite purifySprite;
    [SerializeField] private Sprite droneCtrlSprite;

    private EquipmentManager equipmentManager;
    private EquipmentData.EquipmentType currentType;
    private bool hasSelected;

    public Button ActionButton => actionButton;
    public EquipmentData.EquipmentType CurrentType => currentType;
    public bool HasSelected => hasSelected;

    public void Init(EquipmentManager manager)
    {
        equipmentManager = manager;
        SetFixedCostNames();
        ShowEmptyGuide();
    }

    public void ShowEmptyGuide()
    {
        hasSelected = false;

        if (emptyGuideRoot != null)
            emptyGuideRoot.SetActive(true);

        if (detailContentRoot != null)
            detailContentRoot.SetActive(false);

        if (actionButton != null)
            actionButton.interactable = false;
    }

    public void ShowEquipment(EquipmentData.EquipmentType type)
    {
        if (equipmentManager == null)
            return;

        currentType = type;
        hasSelected = true;

        if (emptyGuideRoot != null)
            emptyGuideRoot.SetActive(false);

        if (detailContentRoot != null)
            detailContentRoot.SetActive(true);

        RefreshEquipmentIcon(type);
        Refresh();
    }

    public void Refresh()
    {
        if (equipmentManager == null || !hasSelected) return;

        RefreshEquipmentIcon(currentType);

        if (equipmentManager == null || !hasSelected)
            return;

        if (!equipmentManager.IsEquipmentUnlocked(currentType))
        {
            RefreshLockedDetail();
            return;
        }

        bool isCrafted = equipmentManager.IsCrafted(currentType);
        int level = equipmentManager.GetEquipmentLevel(currentType);
        int maxLevel = equipmentManager.GetMaxLevel(currentType);
        bool isMaxLevel = level >= maxLevel && isCrafted;

        if (!isCrafted)
        {
            RefreshUncraftedDetail();
            return;
        }

        if (isMaxLevel)
        {
            RefreshMaxLevelDetail();
            return;
        }

        RefreshUpgradeDetail();
    }

    private void RefreshLockedDetail()
    {
        string requiredPlanetName = GetRequiredPlanetDisplayName(currentType);

        if (nameLevelText != null)
            nameLevelText.text = GetEquipmentDisplayName(currentType);

        if (powerNameText != null)
            powerNameText.text = GetAbilityLabel(currentType);

        if (levelBadgeText != null)
            levelBadgeText.text = "";

        if (powerText != null)
            powerText.text = "잠김";

        if (nextEffectTitleText != null)
            nextEffectTitleText.text = "해금 조건";

        if (nextPowerNameText != null)
            nextPowerNameText.text = "";

        if (nextPowerText != null)
            nextPowerText.text = $"{requiredPlanetName} 도달 시 해금";

        SetCostBoxActive(false);
        RefreshStatusBadge();

        if (actionButtonLabel != null)
            actionButtonLabel.text = "잠김";

        if (actionButton != null)
        {
            actionButton.interactable = false;

            if (actionLockedSprite != null)
                actionButton.image.sprite = actionLockedSprite;
        }
    }

    private void RefreshUncraftedDetail()
    {
        float craftPower = equipmentManager.GetPreviewPower(currentType, 0);
        var (refined, starDust) = equipmentManager.GetCraftCost(currentType);

        if (nameLevelText != null)
            nameLevelText.text = $"{GetEquipmentDisplayName(currentType)} - 미제작";

        if (powerNameText != null)
            powerNameText.text = GetAbilityLabel(currentType);

        if (levelBadgeText != null)
            levelBadgeText.text = "Lv. 0 / " + equipmentManager.GetMaxLevel(currentType);

        if (powerText != null)
            powerText.text = "미적용";

        if (nextEffectTitleText != null)
            nextEffectTitleText.text = "제작 후 효과";

        if (nextPowerNameText != null)
            nextPowerNameText.text = GetAbilityLabel(currentType);

        if (nextPowerText != null)
            nextPowerText.text = FormatPowerText(craftPower);

        SetCostBoxActive(true);

        if (costTitleText != null)
            costTitleText.text = "제작 비용";

        RefreshCostTexts(refined, starDust);
        RefreshStatusBadge();

        if (actionButtonLabel != null)
            actionButtonLabel.text = "제작";

        if (actionButton != null)
        {
            actionButton.interactable = equipmentManager.CanCraft(currentType);

            if (actionUnlockedSprite != null)
                actionButton.image.sprite = actionUnlockedSprite;
        }
    }

    private void RefreshEquipmentIcon(EquipmentData.EquipmentType type)
    {
        if (equipmentIconImage == null) return;

        Sprite soIcon = equipmentManager.GetCurrentIcon(type);
        equipmentIconImage.sprite = soIcon != null ? soIcon : GetEquipmentSprite(type);
        equipmentIconImage.preserveAspect = true;
    }

    private Sprite GetEquipmentSprite(EquipmentData.EquipmentType type)
    {
        return type switch
        {
            EquipmentData.EquipmentType.DRILL => drillSprite,
            EquipmentData.EquipmentType.SCANNER => scannerSprite,
            EquipmentData.EquipmentType.PURIFICATION_MODULE => purifySprite,
            EquipmentData.EquipmentType.DRONE_CONTROLLER => droneCtrlSprite,
            _ => drillSprite
        };
    }

    private void RefreshUpgradeDetail()
    {
        int level = equipmentManager.GetEquipmentLevel(currentType);
        int maxLevel = equipmentManager.GetMaxLevel(currentType);

        float currentPower = equipmentManager.GetCurrentPower(currentType);
        float nextPower = equipmentManager.GetPreviewPower(currentType, level + 1);

        var (refined, starDust) = equipmentManager.GetUpgradeCost(currentType);

        if (levelBadgeText != null)
            levelBadgeText.text = $"Lv. {level} / {maxLevel}";

        if (nameLevelText != null)
            nameLevelText.text = $"{GetEquipmentDisplayName(currentType)} Lv.{level} / {maxLevel}";

        if (powerNameText != null)
            powerNameText.text = GetAbilityLabel(currentType);

        if (powerText != null)
            powerText.text = FormatPowerText(currentPower);

        if (nextEffectTitleText != null)
            nextEffectTitleText.text = "다음 레벨 효과";

        if (nextPowerNameText != null)
            nextPowerNameText.text = GetAbilityLabel(currentType);

        if (nextPowerText != null)
            nextPowerText.text = FormatPowerText(nextPower);

        SetCostBoxActive(true);

        if (costTitleText != null)
            costTitleText.text = "강화 비용";

        RefreshCostTexts(refined, starDust);
        RefreshStatusBadge();

        if (actionButtonLabel != null)
            actionButtonLabel.text = "업그레이드";

        if (actionButton != null)
        {
            actionButton.interactable = equipmentManager.CanUpgrade(currentType);

            if (actionUnlockedSprite != null)
                actionButton.image.sprite = actionUnlockedSprite;
        }
    }

    private void RefreshMaxLevelDetail()
    {
        int level = equipmentManager.GetEquipmentLevel(currentType);
        int maxLevel = equipmentManager.GetMaxLevel(currentType);
        float currentPower = equipmentManager.GetCurrentPower(currentType);
        bool canTierUp = equipmentManager.CanUpgradeTier(currentType);

        if (levelBadgeText != null)
            levelBadgeText.text = $"Lv. {level}/{maxLevel}";

        if (nameLevelText != null)
            nameLevelText.text = $"{GetEquipmentDisplayName(currentType)} Lv.{level}/{maxLevel}";

        if (powerNameText != null) powerNameText.text = GetAbilityLabel(currentType);

        if (powerText != null) powerText.text = FormatPowerText(currentPower);

        if (nextEffectTitleText != null)
            nextEffectTitleText.text = canTierUp ? "등급업 효과" : "다음 레벨 효과";

        if (nextPowerNameText != null)
            nextPowerNameText.text = "";

        if (nextPowerText != null)
            nextPowerText.text = canTierUp
                ? $"{equipmentManager.GetNextTierName(currentType)} 장비로 교체"
                : "최고 레벨";

        SetCostBoxActive(false);
        RefreshStatusBadge();

        if (actionButtonLabel != null)
            actionButtonLabel.text = canTierUp
                ? $"{equipmentManager.GetNextTierName(currentType)}로 등급업"
                : "최고 레벨";

        if (actionButton != null)
        {
            actionButton.interactable = canTierUp;
            actionButton.image.sprite = canTierUp
                ? actionUnlockedSprite
                : actionMaxSprite;
        }
    }

    private void SetFixedCostNames()
    {
        if (refinedNameText != null)
            refinedNameText.text = "정제된 크리스탈";

        if (starDustNameText != null)
            starDustNameText.text = "별가루";
    }

    private void SetCostBoxActive(bool active)
    {
        if (costBox != null)
            costBox.SetActive(active);
    }

    private void RefreshCostTexts(float requiredRefined, float requiredStarDust)
    {
        if (refinedValueText != null)
            refinedValueText.text = FormatCostValue(GetCurrentRefinedResource(), requiredRefined);

        if (starDustValueText != null)
            starDustValueText.text = FormatCostValue(GetCurrentStarDust(), requiredStarDust);
    }

    private string FormatCostValue(float current, float required)
    {
        string color = current >= required ? "#66FF99" : "#FF6666";
        return $"<color={color}>{current:F0}</color> / {required:F0}";
    }

    private float GetCurrentRefinedResource()
    {
        return ResourceManager.Instance != null ? ResourceManager.Instance.RefinedResource : 0f;
    }

    private float GetCurrentStarDust()
    {
        return ResourceManager.Instance != null ? ResourceManager.Instance.StarDust : 0f;
    }

    private void RefreshStatusBadge()
    {
        if (statusBadgeBox != null)
            statusBadgeBox.SetActive(true);

        if (statusValueText != null)
            statusValueText.text = GetEquipmentStateText(currentType);
    }

    private string GetEquipmentStateText(EquipmentData.EquipmentType type)
    {
        if (!equipmentManager.IsEquipmentUnlocked(type))
            return "<color=#B0B0B0>잠김</color>";

        if (!equipmentManager.IsCrafted(type))
            return "<color=#FFD166>미제작</color>";

        int level = equipmentManager.GetEquipmentLevel(type);
        int maxLevel = equipmentManager.GetMaxLevel(type);

        if (level >= maxLevel)
            return "<color=#FFD700>최고 레벨</color>";

        return "<color=#66FF99>제작 완료</color>";
    }

    private string GetEquipmentDisplayName(EquipmentData.EquipmentType type)
    {
        return type switch
        {
            EquipmentData.EquipmentType.DRILL => "드릴",
            EquipmentData.EquipmentType.SCANNER => "스캐너",
            EquipmentData.EquipmentType.PURIFICATION_MODULE => "정화 모듈",
            EquipmentData.EquipmentType.DRONE_CONTROLLER => "드론 컨트롤러",
            _ => type.ToString()
        };
    }

    private string GetAbilityLabel(EquipmentData.EquipmentType type)
    {
        return type switch
        {
            EquipmentData.EquipmentType.DRILL => "채굴 속도",
            EquipmentData.EquipmentType.SCANNER => "희귀 광맥 발견",
            EquipmentData.EquipmentType.PURIFICATION_MODULE => "오염도 감소율",
            EquipmentData.EquipmentType.DRONE_CONTROLLER => "오프라인 보상",
            _ => "능력치"
        };
    }

    private string FormatPowerText(float value)
    {
        return $"<color=#66FF99>+{value:F2}</color>";
    }

    private string GetRequiredPlanetDisplayName(EquipmentData.EquipmentType type)
    {
        int requiredPlanetID = equipmentManager.GetRequiredPlanetID(type);

        if (PlanetDataManager.Instance == null)
            return $"행성 {requiredPlanetID}";

        PlanetData requiredPlanet = PlanetDataManager.Instance.GetPlanetByID(requiredPlanetID);

        if (requiredPlanet == null)
            return $"행성 {requiredPlanetID}";

        return GetPlanetDisplayName(requiredPlanet);
    }

    private string GetPlanetDisplayName(PlanetData planet)
    {
        if (planet == null)
            return "";

        return planet.planetID switch
        {
            1 => "테라스",
            2 => "리온",
            3 => "베가스",
            4 => "아이리스",
            _ => planet.planetName
        };
    }
}
