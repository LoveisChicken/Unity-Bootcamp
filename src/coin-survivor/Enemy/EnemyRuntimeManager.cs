using System.Collections.Generic;
using UnityEngine;

public class EnemyRuntimeManager : MonoBehaviour
{
    // 플레이어와 너무 멀어진 몬스터를 풀로 반환하는 구조
    public static EnemyRuntimeManager Instance { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private float despawnDistance = 15f;
    [SerializeField] private float checkInterval = 0.5f;

    private float checkTimer;
    private List<Enemy> activeEnemies;

    private HashSet<Enemy> enemySet = new HashSet<Enemy>();     // 중복 체크용

    // 외부에서 읽기전용
    public List<Enemy> ActiveEnemies => activeEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (player == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
            }
            else
            {
                return;
            }
        }

        checkTimer = 0f;
        activeEnemies = new List<Enemy>();  // Initialize List
    }

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer < checkInterval)
        {
            return;
        }

        checkTimer = 0f;
        DespawnFarEnemies();
    }

    public void Register(Enemy enemy)
    {
        if (enemy == null) return;      // if enemy is nothing, stop

        if(!enemySet.Add(enemy))    // if enemy already exist in set, stop
            return;
        activeEnemies.Add(enemy);   // then, add enemy in dict

        if (SpatialGrid.Instance != null)
            SpatialGrid.Instance.Add(enemy);
    }

    public void Unregister(Enemy enemy)
    {
        // if monster was killed & monster is far away from player & pooling was inactive
        activeEnemies.Remove(enemy);
        enemySet.Remove(enemy);
    }

    public void DespawnFarEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            // enemy = null remove the list in dict
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }
            if (!activeEnemies[i].isActiveAndEnabled)
            {
                enemySet.Remove(activeEnemies[i]);
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (activeEnemies[i].isBoss)
            {
                continue;
            }

            if (IsTooFar(activeEnemies[i]))
            {
                Enemy enemy = activeEnemies[i];
                activeEnemies.RemoveAt(i);
                enemySet.Remove(enemy);
                ReturnEnemy(enemy);
            }
        }
    }

    public bool IsTooFar(Enemy enemy)   // calculate for a direction between monster and player
    {
        return (player.position - enemy.transform.position).sqrMagnitude > despawnDistance * despawnDistance;
    }

    public void ReturnEnemy(Enemy enemy)
    {
        if (ObjectPoolManager.Instance != null)
        {
            enemy.transform.SetParent(EnemyFactory.EnemyRoot);
            ObjectPoolManager.Instance.ReturnToPool(enemy.gameObject);
        }
        else
        {
            enemy.gameObject.SetActive(false);
        }
    }
}
