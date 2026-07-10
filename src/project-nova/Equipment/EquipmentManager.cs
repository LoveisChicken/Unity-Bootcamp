using ProjectNOVA.PSJ;
using System.Collections.Generic;
using UnityEngine;

// TODO: 2계(행성 5~8) 진입 시 classLevel 2 장비 필요 여부 체크 추가 예정
// PSJ와 SO 데이터 구조(등급별 SO 분리 여부) 논의 후 설계
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("SO DATAS - [0]노말 [1]레어 [2]에픽")]
    [SerializeField] private EquipmentData[] drillDatas;
    [SerializeField] private EquipmentData[] scannerDatas;
    [SerializeField] private EquipmentData[] purificationDatas;
    [SerializeField] private EquipmentData[] droneControllerDatas;

    private Dictionary<EquipmentData.EquipmentType, int> activeTier = new();

    [Header("Equipment Status")]
    private Dictionary<EquipmentData.EquipmentType, bool> isCrafted = new Dictionary<EquipmentData.EquipmentType, bool>();
    private Dictionary<EquipmentData.EquipmentType, int> equipmentLevels = new Dictionary<EquipmentData.EquipmentType, int>();

    private int levelUpAmount = 1;
    private int currentPlanetID = 1;
    private int maxPlanetReached = 1;

    private Dictionary<EquipmentData.EquipmentType, int> maxAvailableTier = new();

    private void Awake()
    {
        Debug.Log($"[EquipmentManager] Awake 호출됨: {gameObject.name}");

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[EquipmentManager] 중복 인스턴스 감지. 삭제됨.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //Debug.Log($"[EquipmentManager] Instance 세팅 완료: {Instance.GetInstanceID()}");

        DontDestroyOnLoad(gameObject);

        InitializeEquipmentStatus();
    }

    private void OnEnable()
    {
        EventManager.OnPlanetChanged += OnPlanetChanged;
        EventManager.OnClusterChanged += OnClusterChanged;
    }

    private void OnDisable()
    {
        EventManager.OnPlanetChanged -= OnPlanetChanged;
        EventManager.OnClusterChanged -= OnClusterChanged;
    }

    private void InitializeEquipmentStatus()
    {
        foreach (EquipmentData.EquipmentType type in System.Enum.GetValues(typeof(EquipmentData.EquipmentType)))
        {
            EnsureEquipmentKey(type);
        }
    }

    private void EnsureEquipmentKey(EquipmentData.EquipmentType type)
    {
        if (!isCrafted.ContainsKey(type))
            isCrafted[type] = false;

        if (!equipmentLevels.ContainsKey(type))
            equipmentLevels[type] = 0;
    }

    // Craft
    public bool CanCraft(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);

        var data = GetEquipmentData(type);
        return ((isCrafted[type] == false) && (ResourceManager.Instance.RefinedResource >= data.craftCostRefined)
                && (ResourceManager.Instance.StarDust >= data.craftCostStarDust));
    }

    public bool IsCrafted(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);
        return isCrafted[type];
    }

    public void CraftEquipment(EquipmentData.EquipmentType type)
    {
        // crafting equipment
        if (CanCraft(type))
        {
            var data = GetEquipmentData(type);
            ResourceManager.Instance.TrySpendRefinedResource(data.craftCostRefined);
            ResourceManager.Instance.TrySpendStarDust(data.craftCostStarDust);

            isCrafted[type] = true;
            EventManager.EquipmentCrafted(type);
        }
    }

    public (float refined, float starDust) GetCraftCost(EquipmentData.EquipmentType type)
    {
        var data = GetEquipmentData(type);
        return (data.craftCostRefined, data.craftCostStarDust);
    }

    public (float refinedCost, float stardustCost) GetUpgradeCost(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);

        var data = GetEquipmentData(type);
        int level = equipmentLevels[type]; 
        float refinedCost = data.upgradeCostRefinedBase * Mathf.Pow(data.upgradeCostRate, level);
        float stardustCost = data.upgradeCostStarDustBase * Mathf.Pow(data.upgradeCostRate, level);
        return (refinedCost, stardustCost);
    }

    public bool CanUpgrade(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);
        if (!isCrafted[type]) return false;

        if (equipmentLevels[type] >= GetEquipmentData(type).maxLevel) return false;

        var (refined, stardust) = GetUpgradeCost(type);
        return ResourceManager.Instance.RefinedResource >= refined
            && ResourceManager.Instance.StarDust >= stardust;
    }

    public void UpgradeEquipment(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);

        if (!isCrafted[type] || !CanUpgrade(type)) return;
        var (refined, stardust) = GetUpgradeCost(type);
        ResourceManager.Instance.TrySpendRefinedResource(refined);
        ResourceManager.Instance.TrySpendStarDust(stardust);

        equipmentLevels[type] += levelUpAmount;
        EventManager.EquipmentUpgraded(type, equipmentLevels[type]);
    }

    // Review
    public int GetEquipmentLevel(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);
        return equipmentLevels[type];
    }

    public float GetCurrentPower(EquipmentData.EquipmentType type)
    {
        EnsureEquipmentKey(type);

        var data = GetEquipmentData(type);
        int level = equipmentLevels[type];
        float power = data.basePower + data.powerMultiplierPerLevel * level;

        // 하위계 패널티
        var currentCluster = ClusterManager.Instance?.GetCurrentCluster();
        if (currentCluster != null)
        {
            int tierDiff = currentCluster.requiredEquipmentClassLevel - data.classLevel;
            if (tierDiff > 0)
                power *= Mathf.Pow(1f - currentCluster.lowerTierPenalty, tierDiff);
        }

        return power;
    }

    public int GetMaxLevel(EquipmentData.EquipmentType type)
    {
        return GetEquipmentData(type).maxLevel;
    }

    private EquipmentData GetEquipmentData(EquipmentData.EquipmentType type)
    {
        int tier = activeTier.ContainsKey(type) ? activeTier[type] : 0;

        EquipmentData[] datas = type switch
        {
            EquipmentData.EquipmentType.DRILL => drillDatas,
            EquipmentData.EquipmentType.SCANNER => scannerDatas,
            EquipmentData.EquipmentType.PURIFICATION_MODULE => purificationDatas,
            EquipmentData.EquipmentType.DRONE_CONTROLLER => droneControllerDatas,
            _ => drillDatas
        };

        if (datas == null || datas.Length == 0) return null;
        tier = Mathf.Clamp(tier, 0, datas.Length - 1);
        return datas[tier];
    }

    public static string GetEquipmentName(EquipmentData.EquipmentType type)
    {
        return type switch
        {
            EquipmentData.EquipmentType.DRILL => "Drill",
            EquipmentData.EquipmentType.SCANNER => "Scanner",
            EquipmentData.EquipmentType.PURIFICATION_MODULE => "Purification Module",
            EquipmentData.EquipmentType.DRONE_CONTROLLER => "Drone Controller",
            _ => type.ToString()
        };
    }

    public string GetActiveTierName(EquipmentData.EquipmentType type)
    {
        int tier = activeTier.ContainsKey(type) ? activeTier[type] : 0;
        return tier switch
        {
            0 => "노말",
            1 => "레어",
            2 => "에픽",
            _ => "노말"
        };
    }

    // 레어/에픽 제작 완료 시 호출
    public void SetActiveTier(EquipmentData.EquipmentType type, int tier)
    {
        activeTier[type] = tier;
        equipmentLevels[type] = 0;  // 새 티어는 레벨 0부터
        isCrafted[type] = true;
    }

    public void UpgradeTier(EquipmentData.EquipmentType type)
    {
        if (!CanUpgradeTier(type)) return;
        int current = activeTier.ContainsKey(type) ? activeTier[type] : 0;

        // SetActiveTier 호출 안 함 — isCrafted는 false 유지 (제작 필요)
        activeTier[type] = current + 1;
        isCrafted[type] = false;    // 새 티어는 제작 필요
        equipmentLevels[type] = 0;  // 레벨 리셋
    }

    public string GetNextTierName(EquipmentData.EquipmentType type)
    {
        int next = (activeTier.ContainsKey(type) ? activeTier[type] : 0) + 1;
        return next switch { 1 => "레어", 2 => "에픽", _ => "" };
    }

    public Sprite GetCurrentIcon(EquipmentData.EquipmentType type)
    {
        return GetEquipmentData(type)?.icon; // BaseItemData.icon 사용
    }

    public bool CanUpgradeTier(EquipmentData.EquipmentType type)
    {
        if (!isCrafted.ContainsKey(type) || !isCrafted[type]) return false;

        int current = activeTier.ContainsKey(type) ? activeTier[type] : 0;
        int max = maxAvailableTier.ContainsKey(type) ? maxAvailableTier[type] : 0;
        return max > current;
    }



    private void OnClusterChanged()
    {
        var currentCluster = ClusterManager.Instance?.GetCurrentCluster();
        if (currentCluster == null) return;

        int clusterTier = currentCluster.requiredEquipmentClassLevel - 1;

        foreach (EquipmentData.EquipmentType type in
                 System.Enum.GetValues(typeof(EquipmentData.EquipmentType)))
        {
            // 현재 티어에서 제작까지 완료된 장비만 다음 티어 가용 처리
            if (!isCrafted.ContainsKey(type) || !isCrafted[type]) continue;

            if (!maxAvailableTier.ContainsKey(type) || clusterTier > maxAvailableTier[type])
                maxAvailableTier[type] = clusterTier;
        }
    }

    // 초기화 단계를 없애려면 이 코드 수정하면됨
    private void OnPlanetChanged(int planetId)
    {
        currentPlanetID = planetId;
    }

    public int GetCurrentClass()
    {
        //return maxPlanetReached >= 5 ? 2 : 1;
        if (maxPlanetReached >= 9) return 3; // 3 계
        if (maxPlanetReached >= 5) return 2; // 2 계
        return 1; // 1 계
    }

    public bool CanEnterClass(int targetClass)
    {
        //TODO : SO 확정 후 classLevel 2 SO 목록 체크로 교체
        return false;
    }

    // 다음 레벨 효과
    public float GetPreviewPower(EquipmentData.EquipmentType type, int previewLevel)
    {
        var data = GetEquipmentData(type);

        previewLevel = Mathf.Clamp(previewLevel, 0, data.maxLevel);

        return data.basePower + data.powerMultiplierPerLevel * previewLevel;
    }

    public int GetRequiredPlanetID(EquipmentData.EquipmentType type)
    {
        // 현재 activeTier 기준으로 분기
        int tier = activeTier.ContainsKey(type) ? activeTier[type] : 0;

        return tier switch
        {
            0 => type switch  // 노말 (1계)
            {
                EquipmentData.EquipmentType.DRILL => 1,
                EquipmentData.EquipmentType.SCANNER => 2,
                EquipmentData.EquipmentType.PURIFICATION_MODULE => 3,
                EquipmentData.EquipmentType.DRONE_CONTROLLER => 4,
                _ => 1
            },
            1 => type switch  // 레어 (2계)
            {
                EquipmentData.EquipmentType.DRILL => 5,
                EquipmentData.EquipmentType.SCANNER => 6,
                EquipmentData.EquipmentType.PURIFICATION_MODULE => 7,
                EquipmentData.EquipmentType.DRONE_CONTROLLER => 8,
                _ => 5
            },
            2 => type switch  // 에픽 (3계)
            {
                EquipmentData.EquipmentType.DRILL => 9,
                EquipmentData.EquipmentType.SCANNER => 10,
                EquipmentData.EquipmentType.PURIFICATION_MODULE => 11,
                EquipmentData.EquipmentType.DRONE_CONTROLLER => 12,
                _ => 9
            },
            _ => 1
        };
    }

    public bool IsEquipmentUnlocked(EquipmentData.EquipmentType type)
    {
        int requiredPlanetID = GetRequiredPlanetID(type);
        return IsPlanetUnlocked(requiredPlanetID);
    }

    private bool IsPlanetUnlocked(int requiredPlanetID)
    {
        // 1번 행성 조건은 기본 해금으로 처리
        if (requiredPlanetID <= 1)
            return true;

        if (ClusterManager.Instance == null)
        {
            Debug.LogWarning("[EquipmentManager] ClusterManager.Instance가 없습니다.");
            return false;
        }

        List<ClusterData> clusters = ClusterManager.Instance.ClusterList;

        if (clusters == null || clusters.Count == 0)
        {
            Debug.LogWarning("[EquipmentManager] ClusterList가 비어 있습니다.");
            return false;
        }

        foreach (ClusterData cluster in clusters)
        {
            if (cluster == null || cluster.planetsInCluster == null)
                continue;

            foreach (PlanetData planet in cluster.planetsInCluster)
            {
                if (planet == null)
                    continue;

                if (planet.planetID == requiredPlanetID)
                {
                    return planet.isUnlocked;
                }
            }
        }

        Debug.LogWarning($"[EquipmentManager] requiredPlanetID={requiredPlanetID} 행성을 찾지 못했습니다.");
        return false;
    }

    // SaveLoadManager 연동

    /// <summary>저장 직전 호출. Key: EquipmentType.ToString()</summary>
    public Dictionary<string, bool> GetAllCraftedStatus()
    {
        var result = new Dictionary<string, bool>();
        foreach (var kvp in isCrafted)
            result[kvp.Key.ToString()] = kvp.Value;
        return result;
    }

    public Dictionary<string, int> GetAllActiveTiers()
    {
        var result = new Dictionary<string, int>();
        foreach (var kvp in activeTier)
            result[kvp.Key.ToString()] = kvp.Value;
        return result;
    }

    /// <summary>저장 직전 호출. Key: EquipmentType.ToString()</summary>
    public Dictionary<string, int> GetAllLevels()
    {
        var result = new Dictionary<string, int>();
        foreach (var kvp in equipmentLevels)
            result[kvp.Key.ToString()] = kvp.Value;
        return result;
    }

    /// <summary>로드 직후 호출. string 키를 다시 EquipmentType으로 파싱해서 복원</summary>
    public void LoadData(Dictionary<string, bool> crafted, Dictionary<string, int> levels, 
                         Dictionary<string, int> tiers = null)
    {
        if (crafted != null)
        {
            foreach (var kvp in crafted)
            {
                if (System.Enum.TryParse<EquipmentData.EquipmentType>(kvp.Key, out var type))
                    isCrafted[type] = kvp.Value;
            }
        }

        if (levels != null)
        {
            foreach (var kvp in levels)
            {
                if (System.Enum.TryParse<EquipmentData.EquipmentType>(kvp.Key, out var type))
                    equipmentLevels[type] = kvp.Value;
            }
        }

        if (tiers != null)
        {
            foreach (var kvp in tiers)
            {
                if (System.Enum.TryParse<EquipmentData.EquipmentType>(kvp.Key, out var type))
                    activeTier[type] = kvp.Value;
            }
        }

        InitMaxAvailableTier();
    }

    private void InitMaxAvailableTier()
    {
        var currentCluster = ClusterManager.Instance?.GetCurrentCluster();
        if (currentCluster == null) return;

        int clusterTier = currentCluster.requiredEquipmentClassLevel - 1;

        foreach (EquipmentData.EquipmentType type in
                 System.Enum.GetValues(typeof(EquipmentData.EquipmentType)))
        {
            if (!isCrafted.ContainsKey(type) || !isCrafted[type]) continue;
            if (!maxAvailableTier.ContainsKey(type) || clusterTier > maxAvailableTier[type])
                maxAvailableTier[type] = clusterTier;
        }
    }
}
