using ProjectNOVA.PSJ;
using System.Collections;
using UnityEngine;

public class EquipmentUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EquipmentManager equipmentManager;

    [Header("Select Slot")]
    [SerializeField] private EquipmentSelectSlot drillSlot;
    [SerializeField] private EquipmentSelectSlot scannerSlot;
    [SerializeField] private EquipmentSelectSlot purifySlot;
    [SerializeField] private EquipmentSelectSlot droneCtrlSlot;

    [Header("Detail UI")]
    [SerializeField] private EquipmentDetailUI detailUI;

    private EquipmentData.EquipmentType selectedType;
    private bool hasSelectedEquipment = false;

    //private bool isInitialized = false;
    private Coroutine waitForManagerRoutine;
    private Coroutine delayedRefreshRoutine;    // Refresh UI
    private Coroutine resourceRefreshRoutine;

    private void Awake()
    {
        TrySetEquipmentManager();
    }

    private void OnEnable()
    {
        RegisterSlotEvents();
        RegisterDetailButtonEvent();
        RegisterEquipmentEvents();

        // 정제소 실시간 반영
        RegisterResourceEvents();

        TryInitialize();

        // 2프레임 뒤 한 번 더 갱신한다.
        RequestDelayedRefresh();
    }

    private void OnDisable()
    {
        UnregisterSlotEvents();
        UnregisterDetailButtonEvent();
        UnregisterEquipmentEvents();

        UnregisterResourceEvents();

        if (waitForManagerRoutine != null)
        {
            StopCoroutine(waitForManagerRoutine);
            waitForManagerRoutine = null;
        }

        if (delayedRefreshRoutine != null)
        {
            StopCoroutine(delayedRefreshRoutine);
            delayedRefreshRoutine = null;
        }
    }

    private void TrySetEquipmentManager()
    {
        //if (EquipmentManager.Instance != null)
        //    equipmentManager = EquipmentManager.Instance;
        if (EquipmentManager.Instance != null)
        {
            equipmentManager = EquipmentManager.Instance;
            //Debug.Log($"[EquipmentUI] EquipmentManager 연결됨: {equipmentManager.GetInstanceID()}");
        }
        else
        {
            Debug.LogWarning("[EquipmentUI] EquipmentManager.Instance가 아직 null입니다.");
        }
    }

    private void TryInitialize()
    {
        TrySetEquipmentManager();

        if (equipmentManager != null)
        {
            InitializeUI();
            return;
        }

        if (waitForManagerRoutine == null)
            waitForManagerRoutine = StartCoroutine(WaitForEquipmentManagerAndInit());
    }

    private IEnumerator WaitForEquipmentManagerAndInit()
    {
        float timer = 0f;
        float timeout = 2f;

        while (equipmentManager == null && timer < timeout)
        {
            TrySetEquipmentManager();

            if (equipmentManager != null)
            {
                InitializeUI();
                waitForManagerRoutine = null;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"{gameObject.name}: EquipmentManager를 찾지 못했습니다. Scene에 EquipmentManager가 Active 상태인지 확인하세요.");
        waitForManagerRoutine = null;
    }

    private void InitializeUI()
    {
        hasSelectedEquipment = false;

        if (detailUI != null)
            detailUI.Init(equipmentManager);

        RefreshSelectButtons();
        RefreshSelectedSlot();

        //Debug.Log($"[EquipmentUI] EquipmentManager 연결 완료: {equipmentManager.GetInstanceID()}");
        RequestDelayedRefresh();
    }

    private void RegisterSlotEvents()
    {
        if (drillSlot != null)
            drillSlot.SetOnClick(() => SelectEquipment(EquipmentData.EquipmentType.DRILL));

        if (scannerSlot != null)
            scannerSlot.SetOnClick(() => SelectEquipment(EquipmentData.EquipmentType.SCANNER));

        if (purifySlot != null)
            purifySlot.SetOnClick(() => SelectEquipment(EquipmentData.EquipmentType.PURIFICATION_MODULE));

        if (droneCtrlSlot != null)
            droneCtrlSlot.SetOnClick(() => SelectEquipment(EquipmentData.EquipmentType.DRONE_CONTROLLER));
    }

    private void UnregisterSlotEvents()
    {
        if (drillSlot != null)
            drillSlot.SetOnClick(null);

        if (scannerSlot != null)
            scannerSlot.SetOnClick(null);

        if (purifySlot != null)
            purifySlot.SetOnClick(null);

        if (droneCtrlSlot != null)
            droneCtrlSlot.SetOnClick(null);
    }

    private void RegisterDetailButtonEvent()
    {
        if (detailUI != null && detailUI.ActionButton != null)
            detailUI.ActionButton.onClick.AddListener(OnActionButtonClicked);
    }

    private void UnregisterDetailButtonEvent()
    {
        if (detailUI != null && detailUI.ActionButton != null)
            detailUI.ActionButton.onClick.RemoveListener(OnActionButtonClicked);
    }

    private void RegisterEquipmentEvents()
    {
        EventManager.OnEquipmentCrafted += OnEquipmentCrafted;
        EventManager.OnEquipmentUpgraded += OnEquipmentUpgraded;
        EventManager.OnEquipmentUnlocked += OnEquipmentUnlockedHandler;

        EventManager.OnEquipmentStateChanged += OnEquipmentStateChanged;
    }

    private void UnregisterEquipmentEvents()
    {
        EventManager.OnEquipmentCrafted -= OnEquipmentCrafted;
        EventManager.OnEquipmentUpgraded -= OnEquipmentUpgraded;
        EventManager.OnEquipmentUnlocked -= OnEquipmentUnlockedHandler;

        EventManager.OnEquipmentStateChanged -= OnEquipmentStateChanged;
    }

    private void SelectEquipment(EquipmentData.EquipmentType type)
    {
        TrySetEquipmentManager();

        if (equipmentManager == null)
            return;

        if (!equipmentManager.IsEquipmentUnlocked(type))
            return;

        SoundManager.Instance?.PlaySfx("equipment_select");

        selectedType = type;
        hasSelectedEquipment = true;

        if (detailUI != null)
            detailUI.ShowEquipment(type);

        RefreshSelectedSlot();
    }

    private void OnActionButtonClicked()
    {
        TrySetEquipmentManager();
        if (equipmentManager == null || !hasSelectedEquipment) return;
        if (!equipmentManager.IsEquipmentUnlocked(selectedType)) return;

        bool isCrafted = equipmentManager.IsCrafted(selectedType);
        bool isMax = equipmentManager.GetEquipmentLevel(selectedType)
                     >= equipmentManager.GetMaxLevel(selectedType);

        if (isCrafted && isMax && equipmentManager.CanUpgradeTier(selectedType))
        {
            SoundManager.Instance?.PlaySfx("equipment_tier_up");
            equipmentManager.UpgradeTier(selectedType);   // 등급업

            RefreshSelectButtons();

            if (hasSelectedEquipment && detailUI != null)
                detailUI.Refresh();

            return;
        }
        else if (isCrafted)
            equipmentManager.UpgradeEquipment(selectedType); // 업그레이드
        else
        {
            SoundManager.Instance?.PlaySfx("equipment_craft");
            equipmentManager.CraftEquipment(selectedType);   // 제작
        }
    }

    private void RefreshSelectButtons()
    {
        TrySetEquipmentManager();

        if (equipmentManager == null)
            return;

        SetSlot(drillSlot, EquipmentData.EquipmentType.DRILL);
        SetSlot(scannerSlot, EquipmentData.EquipmentType.SCANNER);
        SetSlot(purifySlot, EquipmentData.EquipmentType.PURIFICATION_MODULE);
        SetSlot(droneCtrlSlot, EquipmentData.EquipmentType.DRONE_CONTROLLER);
    }

    private void SetSlot(EquipmentSelectSlot slot, EquipmentData.EquipmentType type)
    {
        if (slot == null || equipmentManager == null) return;

        bool unlocked = equipmentManager.IsEquipmentUnlocked(type);
        int level = equipmentManager.GetEquipmentLevel(type);
        int maxLevel = equipmentManager.GetMaxLevel(type);
        bool isCrafted = equipmentManager.IsCrafted(type);
        bool isMax = level >= maxLevel && isCrafted;

        // 해금+제작 상태일 때만 등급 태그 붙임
        string baseName = GetEquipmentDisplayName(type);
        string equipmentName = (unlocked && isCrafted)
            ? $"{baseName} [{equipmentManager.GetActiveTierName(type)}]"
            : baseName;

        string levelLabel;
        if (!unlocked) levelLabel = "";
        else if (!isCrafted) levelLabel = "미제작";
        else if (isMax && equipmentManager.CanUpgradeTier(type))
            levelLabel = "티어 상승 가능 ▲";
        else if (isMax) levelLabel = "최고 레벨";  // 기존 그대로
        else levelLabel = $"Lv.{level} / {maxLevel}";

        string requiredPlanetName = GetRequiredPlanetDisplayName(type);
        string lockMessage = unlocked ? "" : $"{requiredPlanetName} 도달 시 해금";

        slot.SetState(unlocked, equipmentName, levelLabel, lockMessage);

        // 단, 티어 상승 직후 다음 티어가 미제작 상태라면 이전 티어 VFX를 유지한다.
        int activeTierIndex = GetActiveTierIndexFromName(equipmentManager.GetActiveTierName(type));

        bool showTierVfx = false;
        int displayTierIndex = activeTierIndex;

        if (unlocked)
        {
            if (isCrafted)
            {
                // 현재 티어 장비를 제작 완료한 상태
                showTierVfx = true;
                displayTierIndex = activeTierIndex;
            }
            else if (activeTierIndex > 0)
            {
                // 티어 상승 후 다음 티어는 아직 미제작 상태
                // 그래서 이전 티어 VFX를 유지
                showTierVfx = true;
                displayTierIndex = activeTierIndex - 1;
            }
        }

        slot.SetTierCardVfx(showTierVfx, displayTierIndex);

        if (unlocked && isCrafted)
        {
            Sprite icon = equipmentManager.GetCurrentIcon(type);
            if (icon != null) slot.SetIcon(icon);
        }
    }

    private int GetActiveTierIndexFromName(string tierName)
    {
        if (string.IsNullOrEmpty(tierName))
            return 0;

        if (tierName.Contains("노말") || tierName.Contains("Normal"))
            return 0;

        if (tierName.Contains("레어") || tierName.Contains("Rare"))
            return 1;

        if (tierName.Contains("희귀") || tierName.Contains("에픽") || tierName.Contains("Epic") || tierName.Contains("Unique"))
            return 2;

        return 0;
    }

    private void RefreshSelectedSlot()
    {
        if (drillSlot != null)
            drillSlot.SetSelected(hasSelectedEquipment && selectedType == EquipmentData.EquipmentType.DRILL);

        if (scannerSlot != null)
            scannerSlot.SetSelected(hasSelectedEquipment && selectedType == EquipmentData.EquipmentType.SCANNER);

        if (purifySlot != null)
            purifySlot.SetSelected(hasSelectedEquipment && selectedType == EquipmentData.EquipmentType.PURIFICATION_MODULE);

        if (droneCtrlSlot != null)
            droneCtrlSlot.SetSelected(hasSelectedEquipment && selectedType == EquipmentData.EquipmentType.DRONE_CONTROLLER);
    }

    private void OnEquipmentCrafted(EquipmentData.EquipmentType type)
    {
        RefreshSelectButtons();

        if (hasSelectedEquipment && type == selectedType && detailUI != null)
            detailUI.Refresh();
    }

    private void OnEquipmentUpgraded(EquipmentData.EquipmentType type, int level)
    {
        RefreshSelectButtons();

        if (hasSelectedEquipment && type == selectedType && detailUI != null)
            detailUI.Refresh();
    }

    private void OnEquipmentUnlockedHandler(EquipmentData.EquipmentType type)
    {
        RefreshSelectButtons();

        if (hasSelectedEquipment && detailUI != null)
            detailUI.Refresh();
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

    private string GetRequiredPlanetDisplayName(EquipmentData.EquipmentType type)
    {
        if (equipmentManager == null)
            return "";

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

    private void OnEquipmentStateChanged()
    {
        TrySetEquipmentManager();

        if (equipmentManager == null)
            return;

        RefreshSelectButtons();
        RefreshSelectedSlot();

        if (hasSelectedEquipment && detailUI != null)
            detailUI.Refresh();

        RequestDelayedRefresh();
    }

    private void RequestDelayedRefresh()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (delayedRefreshRoutine != null)
            StopCoroutine(delayedRefreshRoutine);

        delayedRefreshRoutine = StartCoroutine(DelayedRefreshCo());
    }

    private IEnumerator DelayedRefreshCo()
    {
        // YJ: 첫 프레임은 탭 활성화 / 슬롯 오브젝트 활성화 대기
        yield return null;

        // YJ: 두 번째 프레임은 저장 데이터, EquipmentManager 상태 반영 대기
        yield return null;

        TrySetEquipmentManager();

        if (equipmentManager != null)
        {
            RefreshSelectButtons();
            RefreshSelectedSlot();

            if (hasSelectedEquipment && detailUI != null)
                detailUI.Refresh();
        }

        delayedRefreshRoutine = null;
    }

    // 자원 실시간 반영용
    private void RegisterResourceEvents()
    {
        if (ResourceManager.Instance == null)
            return;

        // YJ: 중복 구독 방지
        ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    private void UnregisterResourceEvents()
    {
        if (ResourceManager.Instance == null)
            return;

        ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    private void OnResourceChanged()
    {
        if (!gameObject.activeInHierarchy)
            return;

        // YJ: RefineryManager에서 AddRefinedResource, AddStarDust가 연속 호출될 수 있음
        // 같은 프레임에 여러 번 Refresh 되는 것을 막기 위해 한 프레임에 한 번만 갱신
        if (resourceRefreshRoutine != null)
            return;

        resourceRefreshRoutine = StartCoroutine(ResourceRefreshCo());
    }

    private IEnumerator ResourceRefreshCo()
    {
        yield return null;

        TrySetEquipmentManager();

        if (equipmentManager == null)
        {
            resourceRefreshRoutine = null;
            yield break;
        }

        RefreshSelectButtons();

        if (hasSelectedEquipment && detailUI != null)
            detailUI.Refresh();

        resourceRefreshRoutine = null;
    }
}
