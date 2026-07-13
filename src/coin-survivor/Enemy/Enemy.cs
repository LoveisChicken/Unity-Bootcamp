using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    protected EnemyData myData;
    protected float currentHp;
    protected float maxHp;
    protected float attack;
    protected float moveSpeed;
    protected int rewardGold;
    protected bool isDead;

    public bool IsDead => isDead;

    // 추가: EnemyFactory aliveCount 관리용
    private EnemyFactory enemyFactory;
    private int enemyID;
    private bool isRegisteredToFactory;

    protected float attackCooldown;
    protected float lastAttackTime = 0f;

    [Header("Hit Feedback")]
    public GameObject hitParticlePrefab;
    public float knockbackForce = 0.5f;
    public float knockbackDuration = 0.1f;

    [Header("Boss Settings")]
    public bool isBoss = false;
    [Tooltip("체크하면 피격 시 넉백을 받지 않음")]
    [SerializeField] protected bool immuneToKnockback = false;

    [Tooltip("기본 스프라이트가 오른쪽을 보고 있으면 true, 왼쪽을 보고 있으면 false")]
    [SerializeField] protected bool defaultFacingRight = true;

    private Coroutine knockbackCoroutine;
    protected bool isKnockbacked = false;
    protected Rigidbody2D rb;
    protected Animator anim;

    protected static Transform cachedPlayer;
    protected Transform player;

    private static readonly WaitForFixedUpdate waitFixedUpdate = new WaitForFixedUpdate();

    protected EnemyHitFlash hitFlash;
    private Vector2Int lastGridPos;

    protected readonly int hashIsMoving = Animator.StringToHash("1_Move");
    protected readonly int hashAttack = Animator.StringToHash("2_Attack");
    protected readonly int hashDie = Animator.StringToHash("4_Death");
    protected readonly int hashIsDeath = Animator.StringToHash("isDeath");

    public Vector3 WorldPosition => transform.position;

    // Seperation
    protected float separationTimer = 0f;
    [SerializeField] protected float separationInterval = 0.1f;
    protected Vector2 cachedSeparation = Vector2.zero;
    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationWeight = 0.5f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitFlash = GetComponent<EnemyHitFlash>();
        anim = GetComponentInChildren<Animator>();

        if(anim != null)
            anim.keepAnimatorStateOnDisable = true;     // Animator 재초기화 막기

        if (cachedPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                cachedPlayer = playerObj.transform;
        }

        player = cachedPlayer;
    }

    // 변경됨:
    // 기존 Initialize(EnemyData enemyData)에서
    // Initialize(EnemyData enemyData, int id, EnemyFactory factory)로 변경
    public virtual void Initialize(EnemyData enemyData, int id, EnemyFactory factory)
    {
        if (enemyData == null)
        {
            Debug.LogWarning("[Enemy] Initialize 실패 : enemyData is null.");
            return;
        }

        myData = enemyData;

        enemyID = id;
        enemyFactory = factory;
        isRegisteredToFactory = true;

        maxHp = enemyData.maxHp;
        currentHp = maxHp;
        attack = enemyData.attack;
        moveSpeed = enemyData.moveSpeed;
        rewardGold = enemyData.rewardGold;
        attackCooldown = Mathf.Max(0.1f, enemyData.cooldown);

        isDead = false;
        isKnockbacked = false;

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null)
            coll.enabled = true;

        if (anim != null)
        {
            anim.SetBool(hashIsDeath, false);
        }

        if (SpatialGrid.Instance != null)
            lastGridPos = SpatialGrid.Instance.WorldToGrid(WorldPosition);
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || player == null || isKnockbacked)
            return;

        separationTimer += Time.fixedDeltaTime;
        if(separationTimer >= separationInterval)
        {
            cachedSeparation = CalculateSeparation();
            separationTimer = 0f;
        }

            MoveTowardsPlayer();
    }

    // adding YJ: Separation 계산 함수 추가
    private Vector2 CalculateSeparation()
    {
        if(SpatialGrid.Instance == null)
            return Vector2.zero;

        var nearby = SpatialGrid.Instance.GetNearby(transform.position, separationRadius);
        Vector2 separation = Vector2.zero;
        int count = 0;

        foreach(Enemy other in nearby)
        {
            if(other == this || other.isDead)
                continue;

            Vector2 toOther = rb.position - (Vector2)other.transform.position;
            float dist = toOther.magnitude;
            if (dist > 0f)
            {
                separation += toOther.normalized / dist;
                count++;
            }
        }

        return count > 0 ? separation / count : Vector2.zero;
    }

    protected void MoveTowardsPlayer()
    {
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        Vector2 finalDirection = (direction + cachedSeparation * separationWeight).normalized;
        //Vector2 nextPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime; //기존꺼
        Vector2 nextPosition = rb.position + finalDirection * moveSpeed * Time.fixedDeltaTime;


        rb.MovePosition(nextPosition);

        //UpdateFacingDirection(direction);
        UpdateFacingDirection(finalDirection);


        if (anim != null)
        {
            bool isMoving = direction.sqrMagnitude > 0.001f;
            anim.SetBool(hashIsMoving, isMoving);
        }

        if (SpatialGrid.Instance != null)
        {
            Vector2Int currentGridPos = SpatialGrid.Instance.WorldToGrid(WorldPosition);

            if (lastGridPos != currentGridPos)
            {
                SpatialGrid.Instance.UpdatePosition(this, lastGridPos, currentGridPos);
                lastGridPos = currentGridPos;
            }
        }
    }

    protected void UpdateFacingDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) < 0.01f)
            return;

        float yRotation = direction.x > 0 ? 180f : 0f;

        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    protected void PlayAttackAnimation()
    {
        if (anim != null)
            anim.SetTrigger(hashAttack);
    }

    public void TakeDamage(float damage, Vector2 hitDirection = default)
    {
        if (isDead)
            return;

        currentHp -= damage;

        GameEventBus.PublishDamageTaken(damage, transform.position);

        if (hitFlash != null)
            hitFlash.PlayFlash();

        if (DamageTextPool.Instance != null)
        {
            Vector3 textPos = transform.position + Vector3.up * 0.5f;
            DamageTextPool.Instance.GetText(textPos, damage);
        }

        if (currentHp > 0)
        {
            if (hitDirection != Vector2.zero)
            {
                SpawnDirectionalParticle(hitDirection);
                ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
            }
        }
        else
        {
            Die();
        }
    }

    private void SpawnDirectionalParticle(Vector2 direction)
    {
        if (hitParticlePrefab == null || ObjectPoolManager.Instance == null)
            return;

        GameObject particle = ObjectPoolManager.Instance.Get(
            hitParticlePrefab,
            transform.position,
            Quaternion.identity
        );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        particle.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (isDead || immuneToKnockback)
            return;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(
            KnockbackRoutine(direction, force, duration)
        );
    }

    private IEnumerator KnockbackRoutine(Vector2 direction, float force, float duration)
    {
        isKnockbacked = true;

        Vector2 startPos = rb.position;
        Vector2 targetPos = startPos + direction.normalized * force;

        float timer = 0f;
        float actualDuration = duration > 0f ? duration : 0.1f;

        while (timer < actualDuration)
        {
            timer += Time.fixedDeltaTime;

            rb.MovePosition(
                Vector2.Lerp(startPos, targetPos, timer / actualDuration)
            );

            yield return waitFixedUpdate;
        }

        isKnockbacked = false;
        knockbackCoroutine = null;
    }

    protected virtual void Die()
    {
        if (isDead)
            return;

        isDead = true;

        GameEventBus.PublishEnemyKilled(transform.position, rewardGold);

        rb.linearVelocity = Vector2.zero;

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null)
            coll.enabled = false;

        if (anim != null)
        {
            anim.SetTrigger(hashDie);
            anim.SetBool(hashIsDeath, true);
        }

        StartCoroutine(WaitDeathAnimationAndPool());
    }

    private IEnumerator WaitDeathAnimationAndPool()
    {
        yield return new WaitForSeconds(1.2f);

        if (ObjectPoolManager.Instance != null)
        {
            transform.SetParent(EnemyFactory.EnemyRoot);
            ObjectPoolManager.Instance.ReturnToPool(gameObject);

        }
        else
            gameObject.SetActive(false);
    }

    protected void UpdateSeparationCache()
    {
        separationTimer += Time.fixedDeltaTime;

        if (separationTimer >= separationInterval)
        {
            cachedSeparation = CalculateSeparation();
            separationTimer = 0f;
        }
    }

    private void OnDisable()
    {
        if (isRegisteredToFactory)
        {
            enemyFactory?.UnregisterEnemy(enemyID);
            isRegisteredToFactory = false;
        }

        if (EnemyRuntimeManager.Instance != null)
        {
            EnemyRuntimeManager.Instance.Unregister(this);
        }

        isKnockbacked = false;

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
    }
}
