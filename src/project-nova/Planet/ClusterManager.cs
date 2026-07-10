using UnityEngine;
using System.Collections.Generic;

public class ClusterManager : MonoBehaviour
{
    public static ClusterManager Instance;

    [SerializeField] private List<ClusterData> _allClusters = new List<ClusterData>();

    [SerializeField] private int _currentClusterIndex = 0;
    private int _currentPlanetIndex = 0;
    private int _collectedCorePieces = 0;

    public int CurrentClusterIndex
    {
        get => _currentClusterIndex;
        set
        {
            int targetIndex = Mathf.Clamp(value, 0, _allClusters.Count - 1);

            _currentClusterIndex = targetIndex;
            EventManager.ClusterChanged();
        }
    }

    public int CurrentPlanetIndex => _currentPlanetIndex;
    public int CollectedCorePieces => _collectedCorePieces;
    public List<ClusterData> ClusterList => _allClusters;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public ClusterData GetCurrentCluster()
    {
        if (_allClusters == null || _allClusters.Count == 0)
            return null;

        if (_currentClusterIndex < 0 || _currentClusterIndex >= _allClusters.Count)
            return null;

        return _allClusters[_currentClusterIndex];
    }

    public void AdvanceToNextCluster()
    {
        int nextIndex = _currentClusterIndex + 1;

        if (nextIndex >= _allClusters.Count)
            return;

        if (!IsClusterUnlocked(nextIndex))
            return;

        _currentClusterIndex = nextIndex;

        ClusterData nextCluster = GetCurrentCluster();

        EventManager.ClusterChanged();
    }

    // YJ: 특정 계층이 해금되어 있는지 확인
    public bool IsClusterUnlocked(int clusterIndex)
    {
        if (_allClusters == null || _allClusters.Count == 0)
            return false;

        if (clusterIndex < 0 || clusterIndex >= _allClusters.Count)
            return false;

        // 첫 번째 계층은 기본 해금
        if (clusterIndex == 0)
            return true;

        // n번째 계층은 이전 계층의 마지막 행성 코어가 100%일 때 해금
        int previousClusterIndex = clusterIndex - 1;
        PlanetData lastPlanet = GetLastPlanetOfCluster(previousClusterIndex);

        if (lastPlanet == null)
            return false;

        return lastPlanet.currentPlanetFragment >= 100;
    }

    // YJ: 현재 계층 기준 다음 계층이 해금 가능한지 확인
    public bool CanUnlockNextCluster()
    {
        int nextClusterIndex = _currentClusterIndex + 1;

        if (nextClusterIndex >= _allClusters.Count)
            return false;

        return IsClusterUnlocked(nextClusterIndex);
    }

    // YJ: 현재 계층 기준 다음 계층 데이터 반환
    public ClusterData GetNextCluster()
    {
        int nextClusterIndex = _currentClusterIndex + 1;

        if (nextClusterIndex < 0 || nextClusterIndex >= _allClusters.Count)
            return null;

        return _allClusters[nextClusterIndex];
    }

    // YJ: 특정 계층의 마지막 행성 반환
    private PlanetData GetLastPlanetOfCluster(int clusterIndex)
    {
        if (_allClusters == null || clusterIndex < 0 || clusterIndex >= _allClusters.Count)
            return null;

        ClusterData cluster = _allClusters[clusterIndex];

        if (cluster == null || cluster.planetsInCluster == null || cluster.planetsInCluster.Count == 0)
            return null;

        int lastPlanetIndex = cluster.planetsInCluster.Count - 1;

        return cluster.planetsInCluster[lastPlanetIndex];
    }

    // YJ: 외부에서 계층 이동 요청 시 잠금 여부까지 확인
    public bool TryMoveToCluster(int clusterIndex)
    {
        if (!IsClusterUnlocked(clusterIndex))
            return false;

        CurrentClusterIndex = clusterIndex;
        return true;
    }
}
