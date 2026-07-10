using ProjectNOVA.PSJ;
using System.Collections.Generic;
using UnityEngine;

public class CrewManager : MonoBehaviour
{
    public static CrewManager Instance { get; private set; }

    [SerializeField] private CrewData engineerData;
    [SerializeField] private CrewData explorerData;
    [SerializeField] private CrewData securityData;

    //해금 여부
    public bool IsEngineerUnlocked => IsCrewUnlocked(CrewRole.Engineer);
    public bool IsExplorerUnlocked => IsCrewUnlocked(CrewRole.Explorer);
    public bool IsSecurityUnlocked => IsCrewUnlocked(CrewRole.SecurityOfficer);

    //--해금 여부 끝
    private int levelUpAmount = 1;

    private Dictionary<CrewRole, int> crewLevels = new Dictionary<CrewRole, int>();
    private Dictionary<CrewRole, int> crewGrades = new Dictionary<CrewRole, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCrewStatus();
    }

    private void EnsureCrewKey(CrewRole role)
    {
        if (!crewLevels.ContainsKey(role))
            crewLevels[role] = 0;

        if (!crewGrades.ContainsKey(role))
            crewGrades[role] = 0;
    }

    private void InitializeCrewStatus()
    {
        EnsureCrewKey(CrewRole.Engineer);
        EnsureCrewKey(CrewRole.Explorer);
        EnsureCrewKey(CrewRole.SecurityOfficer);
    }

    // Unlocked crew members
    public bool IsCrewUnlocked(CrewRole role)
    {
        InitializeCrewStatus();

        return role switch
        {
            CrewRole.Engineer => true,
            CrewRole.Explorer => crewLevels[CrewRole.Engineer] >= 10,
            CrewRole.SecurityOfficer => crewLevels[CrewRole.Explorer] >= 10,
            _ => false
        };
    }

    public bool IsAbilityUnlocked(CrewRole role, int abilityIndex)
    {
        return crewGrades[role] > abilityIndex;
    }

    // LevelUp
    public bool CanLevelUp(CrewRole role)
    {
        var data = GetCrewData(role);
        return crewLevels[role] < data.maxLevel && ResourceManager.Instance.Crystal >= GetLevelUpCost(role);
    }

    public bool CanUpgrade(CrewRole role)
    {
        var data = GetCrewData(role);
        int grade = crewGrades[role];
        if (grade >= data.abilityUnlockLevels.Length) return false;

        return crewLevels[role] >= data.abilityUnlockLevels[grade]
            && ResourceManager.Instance.RefinedResource >= GetUpgradeCost(role);
    }

    // float을 int로 바꿔야할지도 모름? 나중에 바꿔야 되는 일이 있을 때 바꿀듯? crystal?
    public float GetLevelUpCost(CrewRole role)
    {
        EnsureCrewKey(role);

        var data = GetCrewData(role);
        return data.levelUpCostBase * Mathf.Pow(data.levelUpCostRate, crewLevels[role]);
    }

    public float GetUpgradeCost(CrewRole role)
    {
        EnsureCrewKey(role);

        var data = GetCrewData(role);
        return data.upgradeBaseCost * Mathf.Pow(data.upgradeCostRate, crewGrades[role]);
    }

    public int GetNextUnlockLevel(CrewRole role)
    {
        var data = GetCrewData(role);
        int grade = crewGrades[role];
        if (grade >= data.abilityUnlockLevels.Length) return -1;
        return data.abilityUnlockLevels[grade];
    }

    public void LevelUpCrew(CrewRole role)
    {
        /*
        if (!CanLevelUp(role)) return;

        ResourceManager.Instance.TrySpendCrystal(GetLevelUpCost(role));
        crewLevels[role] += levelUpAmount;
        EventManager.CrewLevelChanged(crewLevels[role]);
        */
        EnsureCrewKey(role);

        int beforeLevel = crewLevels[role];
        bool canLevelUp = CanLevelUp(role);

        //Debug.Log(
        //    $"[CrewManager] LevelUpCrew 호출 / Role:{role} / " +
        //    $"BeforeLv:{beforeLevel} / CanLevelUp:{canLevelUp} / " +
        //    $"ManagerID:{GetInstanceID()}"
        //);

        if (!canLevelUp)
        {
            Debug.LogWarning($"[CrewManager] LevelUp 실패 / Role:{role} / Lv:{beforeLevel}");
            return;
        }

        ResourceManager.Instance.TrySpendCrystal(GetLevelUpCost(role));
        crewLevels[role] += levelUpAmount;

        //Debug.Log(
        //    $"[CrewManager] LevelUp 성공 / Role:{role} / " +
        //    $"BeforeLv:{beforeLevel} → AfterLv:{crewLevels[role]} / " +
        //    $"ManagerID:{GetInstanceID()}"
        //);

        EventManager.CrewLevelChanged(crewLevels[role]);
    }

    public void UpgradeCrew(CrewRole role)
    {
        // 스탯 조회
        if (!CanUpgrade(role)) return;

        ResourceManager.Instance.TrySpendRefinedResource(GetUpgradeCost(role));
        crewGrades[role]++;
        EventManager.CrewGradeChanged(crewGrades[role]);
        
    }

    public int GetCrewLevel(CrewRole role)
    {
        EnsureCrewKey(role);
        return crewLevels[role];
    }

    public int GetMaxLevel(CrewRole role)
    {
        return GetCrewData(role).maxLevel;
    }

    // Calculate Stats - each crew member

    public float GetCurrentStat(CrewRole role)
    {
        EnsureCrewKey(role);

        var data = GetCrewData(role);
        /*float levelEfficiency = data.baseEfficiency + (data.perLevelBonus * crewLevels[role]);
        float gradeMultiplier = data.statPerGrade[crewGrades[role]];
        return levelEfficiency * gradeMultiplier;*/ // 덧셈방식

        float levelEfficiency = data.baseEfficiency * Mathf.Pow(data.perLevelBonus, crewLevels[role]);
        float gradeMultiplier = data.statPerGrade[crewGrades[role]];
        return levelEfficiency * gradeMultiplier;
    }

    public int GetCrewGrade(CrewRole role)
    {
        EnsureCrewKey(role);
        return crewGrades[role];
    }

    private CrewData GetCrewData(CrewRole role)
    {
        return role switch
        {
            CrewRole.Engineer => engineerData,
            CrewRole.Explorer => explorerData,
            CrewRole.SecurityOfficer => securityData,
            _ => engineerData
        };
    }
    // 현재 등급 배율 확인
    public float GetGradeMultiplier(CrewRole role)
    {
        var data = GetCrewData(role);
        return data.statPerGrade[crewGrades[role]];
    }

    public Dictionary<string, int> GetAllLevels()
    {
        return new Dictionary<string, int>
    {
        { CrewRole.Engineer.ToString(),        crewLevels[CrewRole.Engineer]        },
        { CrewRole.Explorer.ToString(),         crewLevels[CrewRole.Explorer]         },
        { CrewRole.SecurityOfficer.ToString(), crewLevels[CrewRole.SecurityOfficer] }
    };
    }

    public Dictionary<string, int> GetAllGrades()
    {
        return new Dictionary<string, int>
    {
        { CrewRole.Engineer.ToString(),        crewGrades[CrewRole.Engineer]        },
        { CrewRole.Explorer.ToString(),         crewGrades[CrewRole.Explorer]         },
        { CrewRole.SecurityOfficer.ToString(), crewGrades[CrewRole.SecurityOfficer] }
    };
    }

    public void LoadData(Dictionary<string, int> levels, Dictionary<string, int> grades)
    {
        Debug.Log(
        $"[CrewManager] LoadData 호출 / " +
        $"ManagerID:{GetInstanceID()} / " +
        $"LevelsNull:{levels == null} / GradesNull:{grades == null}"
    );

        if (levels != null)
        {
            foreach (var kvp in levels)
            {
                if (System.Enum.TryParse<CrewRole>(kvp.Key, out var role))
                    crewLevels[role] = kvp.Value;
            }
        }

        if (grades != null)
        {
            foreach (var kvp in grades)
            {
                if (System.Enum.TryParse<CrewRole>(kvp.Key, out var role))
                    crewGrades[role] = kvp.Value;
            }
        }
    }
    public float GetPreviewStat(CrewRole role, int previewLevel, int previewGrade)
    {
        var data = GetCrewData(role);

        previewLevel = Mathf.Clamp(previewLevel, 0, data.maxLevel);
        previewGrade = Mathf.Clamp(previewGrade, 0, data.statPerGrade.Length - 1);

        float levelEfficiency = data.baseEfficiency * Mathf.Pow(data.perLevelBonus, previewLevel);
        float gradeMultiplier = data.statPerGrade[previewGrade];

        return levelEfficiency * gradeMultiplier;
    }

    // Engineer
    public float GetRefineEfficiencyBonus()
    {
        return GetCurrentStat(CrewRole.Engineer) - 1f;
    }

    public float GetEquipmentEfficiencyBonus()
    {
        return GetCurrentStat(CrewRole.Engineer) - 1f;
    }

    public float GetDronePerformanceBonus()
    {
        return GetCurrentStat(CrewRole.Engineer) - 1f;
    }

    // Explorer
    public float GetMiningAmountBonus()
    {
        return GetCurrentStat(CrewRole.Explorer) - 1f;
    }

    public float GetRareResourceBonus()
    {
        //return GetCurrentStat(CrewRole.Explorer) - 1f;
        float crewBonus = GetCurrentStat(CrewRole.Explorer) - 1f;

        float scannerBonus = 0f;
        if (EquipmentManager.Instance != null &&
            EquipmentManager.Instance.IsCrafted(EquipmentData.EquipmentType.SCANNER))
        {
            scannerBonus = EquipmentManager.Instance.GetCurrentPower(
                EquipmentData.EquipmentType.SCANNER) - 1f;
        }

        return crewBonus + scannerBonus;
    }

    public float GetCoreFragmentDropBonus()
    {
        if (!IsAbilityUnlocked(CrewRole.Explorer, 1)) return 0f;  // grade > 1 = 등급 3+
        return GetCurrentStat(CrewRole.Explorer) - 1f;
    }

    // Security Officer
    public float GetEventSuccessRateBonus()
    {
        return GetCurrentStat(CrewRole.SecurityOfficer) - 1f;
    }
}
