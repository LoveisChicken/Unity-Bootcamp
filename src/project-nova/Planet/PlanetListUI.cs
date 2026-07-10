using System.Collections;
using UnityEngine;

public class PlanetListUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private PlanetSlotUI[] slots;

    [Header("Selection Feedback")]
    [SerializeField] private float selectedPulseScale = 1.04f;
    [SerializeField] private float selectedPulseDuration = 0.08f;

    private PlanetSlotUI selectedSlot;
    private bool isLocked = false;

    private Coroutine selectedPulseRoutine;
    private RectTransform pulsingRect;

    private void OnEnable()
    {
        EventManager.OnMatchingStateChanged += SetLocked;
        EventManager.OnClusterChanged += OnClusterChanged;

        RefreshForCurrentCluster();
    }

    private void OnDisable()
    {
        EventManager.OnMatchingStateChanged -= SetLocked;
        EventManager.OnClusterChanged -= OnClusterChanged;

        if (selectedPulseRoutine != null)
        {
            StopCoroutine(selectedPulseRoutine);
            selectedPulseRoutine = null;
        }

        if (pulsingRect != null)
        {
            pulsingRect.localScale = Vector3.one;
            pulsingRect = null;
        }
    }

    private void Start()
    {
        RefreshForCurrentCluster();
    }

    private void SetLocked(bool inLobby)
    {
        isLocked = inLobby;
    }

    private void OnClusterChanged()
    {
        RefreshForCurrentCluster();
    }

    public void RefreshForCurrentCluster()
    {
        RecalculatePlanetUnlockStates();

        SetupSlotsFromCurrentCluster();
        SelectFirstUnlockedPlanet();
    }

    private void RecalculatePlanetUnlockStates()
    {
        if (ClusterManager.Instance == null ||
            ClusterManager.Instance.ClusterList == null)
        {
            return;
        }

        foreach (ClusterData cluster in ClusterManager.Instance.ClusterList)
        {
            if (cluster == null || cluster.planetsInCluster == null)
                continue;

            foreach (PlanetData planet in cluster.planetsInCluster)
            {
                if (planet == null)
                    continue;

                // YJ: 해금 조건이 없는 행성은 기본 해금
                if (planet.unlockRequiredPlanetID == 0)
                {
                    planet.isUnlocked = true;
                    continue;
                }

                PlanetData requiredPlanet = FindPlanetByID(planet.unlockRequiredPlanetID);

                if (requiredPlanet == null)
                {
                    Debug.LogWarning(
                        $"[PlanetListUI] planetID {planet.planetID}의 unlockRequiredPlanetID {planet.unlockRequiredPlanetID}를 찾지 못했습니다."
                    );
                    continue;
                }

                // YJ: 이전 행성 코어가 100% 이상이면 현재 행성 해금
                if (requiredPlanet.currentPlanetFragment >= 100f)
                    planet.isUnlocked = true;
            }
        }
    }

    private PlanetData FindPlanetByID(int planetID)
    {
        if (ClusterManager.Instance == null ||
            ClusterManager.Instance.ClusterList == null)
            return null;

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

        return null;
    }

    private void SetupSlotsFromCurrentCluster()
    {
        if (ClusterManager.Instance == null)
        {
            Debug.LogWarning("[PlanetListUI] ClusterManager.Instance가 없습니다.");
            return;
        }

        ClusterData currentCluster = ClusterManager.Instance.GetCurrentCluster();

        if (currentCluster == null || currentCluster.planetsInCluster == null)
        {
            Debug.LogWarning("[PlanetListUI] 현재 ClusterData 또는 planetsInCluster가 없습니다.");
            return;
        }

        var planetList = currentCluster.planetsInCluster;

        selectedSlot = null;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (i >= planetList.Count)
            {
                slots[i].gameObject.SetActive(false);
                continue;
            }

            if (planetList[i] == null)
            {
                slots[i].gameObject.SetActive(false);
                continue;
            }

            slots[i].gameObject.SetActive(true);
            slots[i].Setup(planetList[i], this);
            slots[i].SetSelected(false);
        }
    }

    public void OnSlotClicked(PlanetSlotUI clickedSlot, PlanetData planetData)
    {
        OnSlotClicked(clickedSlot, planetData, true);
    }

    private void OnSlotClicked(PlanetSlotUI clickedSlot, PlanetData planetData, bool playFeedback)
    {
        if (isLocked)
            return;

        if (clickedSlot == null || planetData == null)
            return;

        if (!planetData.isUnlocked)
            return;

        bool isSelectionChanged = selectedSlot != clickedSlot;

        if (selectedSlot != null)
            selectedSlot.SetSelected(false);

        selectedSlot = clickedSlot;
        selectedSlot.SetSelected(true);

        if (playFeedback && isSelectionChanged)
        {
            ExploreUISoundController.Instance?.PlayMenuSelect();
            PlaySelectedPulse(clickedSlot);
        }

        if (PlanetDataManager.Instance != null)
            PlanetDataManager.Instance.SetSelectedPlanet(planetData);

        if (PlanetDataManager.Instance != null &&
            PlanetDataManager.Instance._selectPlanet != null &&
            PlanetDataManager.Instance._selectPlanet.pollutionProgress >= 100f)
        {
            EventManager.BtnActiveChanged(false);
        }
        else
        {
            EventManager.BtnActiveChanged(true);
        }
    }

    private void SelectFirstUnlockedPlanet()
    {
        foreach (PlanetSlotUI slot in slots)
        {
            if (slot == null || !slot.gameObject.activeSelf)
                continue;

            PlanetData data = slot.GetPlanetData();

            if (data == null || !data.isUnlocked)
            {
                slot.SetSelected(false);
                continue;
            }

            // 자동 선택이므로 소리/펄스 없음
            OnSlotClicked(slot, data, false);
            return;
        }

        EventManager.BtnActiveChanged(false);
    }

    public void RefreshAllSlots()
    {
        RefreshForCurrentCluster();
    }

    private void PlaySelectedPulse(PlanetSlotUI slot)
    {
        if (slot == null)
            return;

        RectTransform targetRect = slot.GetComponent<RectTransform>();

        if (targetRect == null)
            return;

        if (selectedPulseRoutine != null)
        {
            StopCoroutine(selectedPulseRoutine);
            selectedPulseRoutine = null;
        }

        if (pulsingRect != null)
            pulsingRect.localScale = Vector3.one;

        pulsingRect = targetRect;
        selectedPulseRoutine = StartCoroutine(SelectedPulseCo(targetRect));
    }

    private IEnumerator SelectedPulseCo(RectTransform targetRect)
    {
        Vector3 normalScale = Vector3.one;
        Vector3 targetScale = Vector3.one * selectedPulseScale;

        float elapsed = 0f;

        while (elapsed < selectedPulseDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / selectedPulseDuration);
            float smoothT = Smooth(t);

            targetRect.localScale = Vector3.Lerp(normalScale, targetScale, smoothT);

            yield return null;
        }

        targetRect.localScale = targetScale;

        elapsed = 0f;

        while (elapsed < selectedPulseDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / selectedPulseDuration);
            float smoothT = Smooth(t);

            targetRect.localScale = Vector3.Lerp(targetScale, normalScale, smoothT);

            yield return null;
        }

        targetRect.localScale = normalScale;

        if (pulsingRect == targetRect)
            pulsingRect = null;

        selectedPulseRoutine = null;
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
