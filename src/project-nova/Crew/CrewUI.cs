using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrewUI : MonoBehaviour
{
    private CrewManager crewManager;

    [Header("Data")]
    [SerializeField] private CrewRole role;

    [Header("Card Group")]
    [SerializeField] private CrewCardGroup cardGroup;

    [Header("Texts")]
    [SerializeField] private TMP_Text nameLevelText;
    [SerializeField] private TMP_Text gradeText;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text levelUpButtonText;
    [SerializeField] private TMP_Text upgradeButtonText;

    [Header("Buttons")]
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button upgradeButton;

    [Header("Panels")]
    [SerializeField] private CrewDetailPanel detailPanel;

    [Header("Icons")]
    [SerializeField] private GameObject lockIcon;

    [Header("Character")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Sprite lockedCharacterSprite;
    [SerializeField] private Sprite unlockedCharacterSprite;

    [Header("Card Summary")]
    [SerializeField] private TMP_Text summaryBonusText;
    [SerializeField] private GameObject selectedOutline;


    [Header("Selection Feedback")]
    [SerializeField] private float selectedPulseScale = 1.04f;
    [SerializeField] private float selectedPulseDuration = 0.15f;

    [Header("Unlock UI Particle")]
    [SerializeField] private GameObject unlockVfxRoot;
    [SerializeField] private bool playUnlockVfx = true;
    [SerializeField] private float unlockVfxDisableDelay = 1.5f;
    [SerializeField] private float unlockRevealDelay = 0.45f;

    private ParticleSystem[] _unlockParticles;
    private Coroutine _unlockVfxDisableRoutine;

    private bool _wasUnlocked = false;
    private Coroutine _unlockVFXRoutine;

    private Coroutine _pulseRoutine;
    private void Awake()
    {
        TrySetCrewManager();

        InitUnlockVfx();
    }

    private void OnEnable()
    {
        TrySetCrewManager();

        if (crewManager != null)
            _wasUnlocked = crewManager.IsCrewUnlocked(role);

        EventManager.OnCrewLevelUp += RefreshUI;
        EventManager.OnCrewUpgrade += RefreshUI;

        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(OnLevelUpClicked);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        RefreshUI(0);
    }

    private void Start()
    {
        TrySetCrewManager();

        SetSelected(false);
        RefreshUI(0);
    }

    private void OnDisable()
    {
        EventManager.OnCrewLevelUp -= RefreshUI;
        EventManager.OnCrewUpgrade -= RefreshUI;

        if (levelUpButton != null)
            levelUpButton.onClick.RemoveAllListeners();

        if (upgradeButton != null)
            upgradeButton.onClick.RemoveAllListeners();

        if (_pulseRoutine != null)
        {
            StopCoroutine(_pulseRoutine);
            GetComponent<RectTransform>().localScale = Vector3.one;
            _pulseRoutine = null;
        }

        /*if (_unlockVFXRoutine != null)
        {
            StopCoroutine(_unlockVFXRoutine);
            _unlockVFXRoutine = null;
        }*/
        if (_unlockVfxDisableRoutine != null)
        {
            StopCoroutine(_unlockVfxDisableRoutine);
            _unlockVfxDisableRoutine = null;
        }

        if (unlockVfxRoot != null)
            unlockVfxRoot.SetActive(false);
    }

    private void InitUnlockVfx()
    {
        if (unlockVfxRoot == null)
            return;

        _unlockParticles = unlockVfxRoot.GetComponentsInChildren<ParticleSystem>(true);
        unlockVfxRoot.SetActive(false);
    }

    private void TrySetCrewManager()
    {
        if (CrewManager.Instance != null)
            crewManager = CrewManager.Instance;
    }

    private void SelectThisCard()
    {
        if (cardGroup != null)
            cardGroup.SelectCard(this);
        else
            SetSelected(true);
    }

    private void OnLevelUpClicked()
    {
        TrySetCrewManager();

        if (crewManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: CrewManager가 연결되지 않아 레벨업할 수 없습니다.");
            return;
        }

        crewManager.LevelUpCrew(role);
        SoundManager.Instance?.PlaySfx("crew_levelup");

        if (detailPanel != null)
            detailPanel.Show(role);

        SelectThisCard();
        RefreshUI(0);
    }

    private void OnUpgradeClicked()
    {
        TrySetCrewManager();

        if (crewManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: CrewManager가 연결되지 않아 업그레이드할 수 없습니다.");
            return;
        }

        crewManager.UpgradeCrew(role);
        SoundManager.Instance?.PlaySfx("crew_upgrade");

        if (detailPanel != null)
            detailPanel.Show(role);

        SelectThisCard();
        RefreshUI(0);
    }

    private void RefreshUI(int _)
    {
        TrySetCrewManager();

        if (crewManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: CrewManager가 연결되지 않았습니다.");
            return;
        }

        bool unlocked = crewManager.IsCrewUnlocked(role);
        //add
        /*if (unlocked && !_wasUnlocked)
        {
            _wasUnlocked = true;
            if (_unlockVFXRoutine != null) StopCoroutine(_unlockVFXRoutine);
            _unlockVFXRoutine = StartCoroutine(UnlockVFXRoutine());
            return;  // VFX 끝난 후 UI 갱신
        }*/

        // ver2
        /*if (unlocked && !_wasUnlocked)
        {
            _wasUnlocked = true;
            if (_unlockVFXRoutine != null) StopCoroutine(_unlockVFXRoutine);
            _unlockVFXRoutine = StartCoroutine(UnlockVFXRoutine());
            return;
        }
        */
        // ver3
        if (unlocked && !_wasUnlocked)
        {
            _wasUnlocked = true;

            if (_unlockVFXRoutine != null)
                StopCoroutine(_unlockVFXRoutine);

            _unlockVFXRoutine = StartCoroutine(UnlockVFXRoutine());
            return;
        }
        // end of ver3
        _wasUnlocked = unlocked;

        if (!unlocked)
        {
            RefreshLockedUI();
            return;
        }

        RefreshUnlockedUI();
    }

    private void RefreshLockedUI()
    {
        if (characterImage != null)
            characterImage.sprite = lockedCharacterSprite;

        if (nameLevelText != null)
            nameLevelText.text = $"{GetRoleName(role)}\n잠김";

        if (gradeText != null)
            gradeText.text = "등급 -";

        if (levelUpButtonText != null)
            levelUpButtonText.text = "레벨업";

        if (upgradeButtonText != null)
            upgradeButtonText.text = "업그레이드";

        if (lockIcon != null)
            lockIcon.SetActive(true);

        if (levelUpButton != null)
            levelUpButton.interactable = false;

        if (upgradeButton != null)
            upgradeButton.interactable = false;

        RefreshSummaryBonus(false);
    }

    private void RefreshUnlockedUI()
    {
        if (characterImage != null)
            characterImage.sprite = unlockedCharacterSprite;

        int level = crewManager.GetCrewLevel(role);
        int maxLevel = crewManager.GetMaxLevel(role);
        int nextUnlock = crewManager.GetNextUnlockLevel(role);
        int currentGrade = crewManager.GetCrewGrade(role);

        if (nameLevelText != null)
            nameLevelText.text = $"{GetRoleName(role)}\nLv.{level}/{maxLevel}";

        if (gradeText != null)
            gradeText.text = $"등급 {currentGrade + 1}";

        RefreshSummaryBonus(true);

        SetLevelUpButtonState(level, maxLevel);
        SetUpgradeButtonState(level, nextUnlock);
    }

    private IEnumerator UnlockVFXRoutine()
    {
        /*if (characterImage != null)
        {
            characterImage.transform.DOKill();

            //잠금 스프라이트 유지한 채로 엄청 크게
            characterImage.transform.localScale = Vector3.one;
            yield return characterImage.transform
                .DOScale(Vector3.one * 2.5f, 0.4f)
                .SetEase(Ease.OutExpo)
                .WaitForCompletion();

            //살짝 멈추는 느낌
            yield return new WaitForSeconds(0.2f);

            //해금 스프라이트로 교체 + 빠르게 0으로
            characterImage.sprite = unlockedCharacterSprite;
            characterImage.transform.localScale = Vector3.zero;

            SoundManager.Instance?.PlaySfx("crew_select");

            //튀어나오는 느낌으로 원래 크기로
            yield return characterImage.transform
                .DOScale(Vector3.one, 0.35f)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }
        else
        {
            SoundManager.Instance?.PlaySfx("crew_select");
            yield return new WaitForSeconds(0.5f);
        }

        RefreshUnlockedUI();
        _unlockVFXRoutine = null;
        */
        /*
        // 먼저 카드 UI를 정상 해금 상태로 변경 ver2
        RefreshUnlockedUI();

        // 해금 VFX 재생
        PlayUnlockUIParticle();

        // 기존 해금 사운드는 유지
        SoundManager.Instance?.PlaySfx("crew_select");

        // VFX가 보일 시간을 조금 확보
        yield return new WaitForSeconds(unlockVfxDisableDelay);

        _unlockVFXRoutine = null;
        */
        // YJ: 해금 VFX 먼저 재생
        PlayUnlockUIParticle();

        // YJ: 기존 해금 사운드는 유지
        SoundManager.Instance?.PlaySfx("crew_unlock");

        // YJ: VFX가 먼저 보이도록 잠깐 대기
        yield return new WaitForSeconds(unlockRevealDelay);

        // YJ: VFX가 재생된 뒤 카드 UI를 해금 상태로 변경
        RefreshUnlockedUI();

        _unlockVFXRoutine = null;
    }

    private void PlayUnlockUIParticle()
    {
        /*
        if (!playUnlockVfx || unlockVfxRoot == null)
            return;

        unlockVfxRoot.SetActive(true);

        if (_unlockParticles == null || _unlockParticles.Length == 0)
            _unlockParticles = unlockVfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in _unlockParticles)
        {
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        if (_unlockVfxDisableRoutine != null)
            StopCoroutine(_unlockVfxDisableRoutine);

        _unlockVfxDisableRoutine = StartCoroutine(DisableUnlockVfxAfterDelay());
    */
        if (!playUnlockVfx || unlockVfxRoot == null)
            return;

        unlockVfxRoot.SetActive(true);

        if (_unlockParticles == null || _unlockParticles.Length == 0)
            _unlockParticles = unlockVfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in _unlockParticles)
        {
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }

        if (_unlockVfxDisableRoutine != null)
            StopCoroutine(_unlockVfxDisableRoutine);

        _unlockVfxDisableRoutine = StartCoroutine(DisableUnlockVfxAfterDelay());
    }

    private IEnumerator DisableUnlockVfxAfterDelay()
    {
        yield return new WaitForSeconds(unlockVfxDisableDelay);

        if (unlockVfxRoot != null)
            unlockVfxRoot.SetActive(false);

        _unlockVfxDisableRoutine = null;
    }

    private void SetLevelUpButtonState(int level, int maxLevel)
    {
        bool isMaxLevel = level >= maxLevel;
        bool canLevelUp = !isMaxLevel && crewManager.CanLevelUp(role);

        if (levelUpButtonText != null)
            levelUpButtonText.text = isMaxLevel ? "최고 레벨" : "레벨업";

        if (levelUpButton != null)
            levelUpButton.interactable = canLevelUp;
    }

    private void SetUpgradeButtonState(int level, int nextUnlock)
    {
        bool isMaxGrade = nextUnlock < 0;
        bool isLockedByLevel = !isMaxGrade && level < nextUnlock;
        bool canUpgrade = !isMaxGrade && !isLockedByLevel && crewManager.CanUpgrade(role);

        if (upgradeButtonText != null)
            upgradeButtonText.text = isMaxGrade ? "최고 등급" : "업그레이드";

        if (lockIcon != null)
            lockIcon.SetActive(isLockedByLevel);

        if (upgradeButton != null)
            upgradeButton.interactable = canUpgrade;
    }

    public void OnSelectCrew()
    {
        TrySetCrewManager();

        if (crewManager == null)
        {
            Debug.LogWarning($"{gameObject.name}: CrewManager가 연결되지 않아 승무원을 선택할 수 없습니다.");
            return;
        }

        SoundManager.Instance?.PlaySfx("equipment_select");
        PlayPulse();

        if (detailPanel != null)
            detailPanel.Show(role);

        SelectThisCard();
    }

    public void SetSelected(bool selected)
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(selected);
    }

    private void PlayPulse()
    {
        if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseCo());
    }

    private IEnumerator PulseCo()
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector3 normalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * selectedPulseScale;
        float elapsed = 0f;

        while (elapsed < selectedPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / selectedPulseDuration));
            rect.localScale = Vector3.Lerp(normalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < selectedPulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Smooth(Mathf.Clamp01(elapsed / selectedPulseDuration));
            rect.localScale = Vector3.Lerp(targetScale, normalScale, t);
            yield return null;
        }

        rect.localScale = normalScale;
        _pulseRoutine = null;
    }

    private float Smooth(float t) => t * t * (3f - 2f * t);

    private void RefreshSummaryBonus(bool unlocked)
    {
        if (summaryBonusText == null)
            return;

        if (!unlocked)
        {
            summaryBonusText.text = "주 보너스 ???";
            summaryBonusText.color = Color.gray;
            return;
        }

        summaryBonusText.color = Color.white;

        switch (role)
        {
            case CrewRole.Engineer:
                summaryBonusText.text = $"정제 효율 +{crewManager.GetRefineEfficiencyBonus() * 100f:F1}%";
                break;

            case CrewRole.Explorer:
                summaryBonusText.text = $"채굴량 +{crewManager.GetMiningAmountBonus() * 100f:F1}%";
                break;

            case CrewRole.SecurityOfficer:
                summaryBonusText.text = $"이벤트 성공률 +{crewManager.GetEventSuccessRateBonus() * 100f:F1}%";
                break;

            default:
                summaryBonusText.text = "-";
                break;
        }
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
}
