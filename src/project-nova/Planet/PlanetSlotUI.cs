using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlanetSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlanetData _planetData;
    [SerializeField] private TextMeshProUGUI _planetNameText;
    [SerializeField] private TextMeshProUGUI _planetCoreText;
    [SerializeField] private GameObject _lockScreen;

    [SerializeField] private Image _outline;
    private PlanetListUI _listPlanets;
    private bool _isInLobby; // [PSJ] 매칭 상태일 때만 오염도 표시

    // YJ added
    [Header("Pollution UI")]
    [SerializeField] private GameObject pollutionRoot;
    [SerializeField] private TMP_Text pollutionLabelText;
    [SerializeField] private TMP_Text pollutionPercentText;
    [SerializeField] private Image pollutionBarFill;

    private void Awake()
    {
        SetOutline(false);
        HidePollutionGaugeUI();
    }

    private void OnEnable()
    {
        EventManager.OnUpdatePlanetData += RefreshSlot;
        EventManager.OnMatchingStateChanged += OnMatchingStateChanged; // [PSJ]
        if (MatchingManager.Instance != null) // [PSJ]
            MatchingManager.Instance.OnPollutionUpdated += OnPollutionUpdated;

        // [PSJ] 탭 복귀 시 현재 상태 즉시 반영
        _isInLobby = MatchingManager.Instance != null && MatchingManager.Instance.CurrentLobby != null;
        UpdatePollutionGaugeUI();
    }

    private void OnDisable()
    {
        EventManager.OnUpdatePlanetData -= RefreshSlot;
        EventManager.OnMatchingStateChanged -= OnMatchingStateChanged; // [PSJ]
        if (MatchingManager.Instance != null) // [PSJ]
            MatchingManager.Instance.OnPollutionUpdated -= OnPollutionUpdated;
    }

    // [PSJ] 오염도 변경 시 슬롯의 오염도 게이지 갱신
    private void OnPollutionUpdated(float _) => UpdatePollutionGaugeUI();

    // [PSJ] 매칭 상태 변경 시 오염도 게이지 갱신
    private void OnMatchingStateChanged(bool inLobby)
    {
        _isInLobby = inLobby;
        UpdatePollutionGaugeUI();
    }

    public void Setup(PlanetData data, PlanetListUI parent)
    {
        _planetData = data;
        _listPlanets = parent;

        if (_planetData == null)
        {
            Debug.LogWarning($"[{gameObject.name}] PlanetData가 null입니다.");

            if (_lockScreen != null)
                _lockScreen.SetActive(true);

            HidePollutionGaugeUI();
            SetOutline(false);
            return;
        }

        if (_planetData.unlockRequiredPlanetID == 0)
        {
            _planetData.isUnlocked = true;
        }

        if (_planetData.isUnlocked)
        {
            RefreshUnlockedPlanetUI();
        }
        else
        {
            RefreshLockedPlanetUI();
        }
    }

    private void RefreshUnlockedPlanetUI()
    {
        if (_lockScreen != null)
            _lockScreen.SetActive(false);
        else
            Debug.LogWarning($"[{gameObject.name}] _lockScreen 연결되지 않았습니다.");
        
        if(_planetNameText != null)
            _planetNameText.text = _planetData.planetName;
        else
            Debug.LogWarning($"[{gameObject.name}] _planetNameText 연결되지 않았습니다.");

        if (_planetCoreText != null)
            _planetCoreText.text = $"행성 코어 : {_planetData.currentPlanetFragment} %";
        else
            Debug.LogWarning($"[{gameObject.name}] _planetCoreText 연결되지 않았습니다.");

        UpdatePollutionGaugeUI();
    }

    private void RefreshLockedPlanetUI()
    {
        if(_lockScreen != null)
        {
            _lockScreen.SetActive(true);

            TextMeshProUGUI lockText = _lockScreen.GetComponentInChildren<TextMeshProUGUI>(true);

            if (lockText != null)
            {
                string requiredPlanetName = GetRequiredPlanetName();
                lockText.text = $"{requiredPlanetName} 행성 복구 필요";
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] _lockScreen 아래에 TextMeshProUGUI가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] _lockScreen이 연결되지 않았습니다.");
        }

        if (_planetNameText != null)
            _planetNameText.text = "잠긴 행성";

        if (_planetCoreText != null)
            _planetCoreText.text = "행성 코어 : -";

        HidePollutionGaugeUI();
    }

    private string GetRequiredPlanetName()
    {
        int requiredID = _planetData.unlockRequiredPlanetID;

        PlanetData requiredPlanet = FindPlanetByID(requiredID);

        if (requiredPlanet == null)
        {
            Debug.LogWarning($"[{gameObject.name}] unlockRequiredPlanetID {requiredID}에 해당하는 PlanetData가 없습니다.");
            return $"행성 {requiredID}";
        }

        return requiredPlanet.planetName;
    }

    private PlanetData FindPlanetByID(int planetID)
    {
        // YJ: 현재 행성 데이터는 ClusterManager의 ClusterList 기준으로 구성되어 있으므로
        // PlanetDataManager보다 ClusterManager 전체 클러스터를 우선 검색한다.
        if (ClusterManager.Instance != null &&
            ClusterManager.Instance.ClusterList != null)
        {
            foreach (ClusterData cluster in ClusterManager.Instance.ClusterList)
            {
                if (cluster == null || cluster.planetsInCluster == null)
                    continue;

                foreach (PlanetData planet in cluster.planetsInCluster)
                {
                    if (planet == null)
                        continue;

                    if (planet.planetID == planetID)
                        return planet;
                }
            }
        }

        // YJ: 기존 구조와의 호환용 fallback
        if (PlanetDataManager.Instance != null)
            return PlanetDataManager.Instance.GetPlanetByID(planetID);

        return null;
    }

    public void SetOutline(bool isTrue)
    {
        if (_outline != null)
            _outline.enabled = isTrue;
    }

    // YJ: 선택 상태 표시
    public void SetSelected(bool selected)
    {
        SetOutline(selected);
    }

    // YJ: PlanetData 참조 반환
    public PlanetData GetPlanetData()
    {
        return _planetData;
    }

    // YJ: UI 전체 갱신
    public void RefreshUI()
    {
        if (_planetData == null) return;
        Setup(_planetData, _listPlanets);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_planetData == null || !_planetData.isUnlocked) return;
        _listPlanets.OnSlotClicked(this, _planetData);
    }

    public void RefreshSlot(PlanetData planet)
    {
        if (_planetData == null || planet.planetID != _planetData.planetID)
            return;

        if (_planetNameText != null)
            _planetNameText.text = _planetData.planetName;

        if(_planetCoreText != null)
            _planetCoreText.text = $"행성 코어 : {_planetData.currentPlanetFragment} %";


        //_planetNameText.text = _planetData.planetName;
        //_planetCoreText.text = $"행성 코어 : {_planetData.currentPlanetFragment} %";

        UpdatePollutionGaugeUI();
    }

    // YJ: 매칭 상태와 현재 행성 기준으로 오염도 게이지 갱신
    private void UpdatePollutionGaugeUI()
    {
        if (_planetData == null)
        {
            HidePollutionGaugeUI();
            return;
        }

        // YJ: 잠긴 행성은 오염도 UI 숨김
        if (!_planetData.isUnlocked)
        {
            HidePollutionGaugeUI();
            return;
        }

        // YJ: 해금된 행성이지만 아직 매칭 전이면 기본 오염도 UI 표시
        if (!TryGetCurrentPollution(out float pollution))
        {
            ClearPollutionGaugeUI();
            return;
        }

        RefreshPollutionGaugeUI(pollution);
    }

    // YJ: 현재 슬롯이 매칭 중인 행성과 같을 때만 오염도 반환
    private bool TryGetCurrentPollution(out float pollution)
    {
        pollution = 0f;

        if (_isInLobby &&
            MatchingManager.Instance != null &&
            MatchingManager.Instance.GetCurrentPlanetIndex() == _planetData.planetID)
        {
            pollution = MatchingManager.Instance.GetCurrentPollution();
            return true;
        }

        return false;
    }

    // YJ: 오염도 게이지 UI 적용
    private void RefreshPollutionGaugeUI(float pollution)
    {
        pollution = Mathf.Clamp(pollution, 0f, 100f);

        if (pollutionRoot != null)
            pollutionRoot.SetActive(true);

        Color pollutionColor = GetPollutionColor(pollution);

        if (pollutionLabelText != null)
        {
            pollutionLabelText.text = "오염도";
            pollutionLabelText.color = Color.white;
        }

        if (pollutionPercentText != null)
        {
            pollutionPercentText.text = $"{pollution:F0}%";
            pollutionPercentText.color = pollutionColor;
        }

        if (pollutionBarFill != null)
        {
            pollutionBarFill.fillAmount = Mathf.Clamp01(pollution / 100f);
            pollutionBarFill.color = pollutionColor;
        }
    }

    // YJ: 해금된 행성이지만 매칭 전일 때 오염도 UI 기본 상태 표시
    private void ClearPollutionGaugeUI()
    {
        if (pollutionRoot != null)
            pollutionRoot.SetActive(true);

        if (pollutionLabelText != null)
        {
            pollutionLabelText.text = "오염도";
            pollutionLabelText.color = Color.white;
        }

        if (pollutionPercentText != null)
        {
            pollutionPercentText.text = "-";
            pollutionPercentText.color = Color.white;
        }

        if (pollutionBarFill != null)
        {
            pollutionBarFill.fillAmount = 0f;
            pollutionBarFill.color = new Color(0.2f, 1f, 0.7f);
        }
    }

    // YJ: 잠긴 행성 또는 데이터가 없을 때 오염도 UI 숨김
    private void HidePollutionGaugeUI()
    {
        if (pollutionRoot != null)
            pollutionRoot.SetActive(false);
    }

    // [PSJ] 오염도 수치에 따라 흰색→노랑→주황→빨강 부드럽게 보간
    private Color GetPollutionColor(float pollution)
    {
        Color white  = Color.white;
        Color yellow = new Color(1f, 0.95f, 0f);
        Color orange = new Color(1f, 0.5f, 0f);
        Color red    = new Color(1f, 0.15f, 0.15f);

        if (pollution <= 33f)
            return Color.Lerp(white,  yellow, pollution / 33f);
        if (pollution <= 66f)
            return Color.Lerp(yellow, orange, (pollution - 33f) / 33f);
        return     Color.Lerp(orange, red,    (pollution - 66f) / 34f);
    }

}
