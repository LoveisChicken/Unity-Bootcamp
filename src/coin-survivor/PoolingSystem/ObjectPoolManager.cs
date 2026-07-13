using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance { get; private set; }

    private Dictionary<GameObject, Queue<GameObject>> poolDict;


    //[Header("Pool Settings")]
    //[SerializeField] private int maxCount = 300; count 설정은 Manager에서 연결

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        poolDict = new Dictionary<GameObject, Queue<GameObject>>();
    }
    /// <summary>
    /// 처음에 미리 count개를 만들어 두고 비활성 상태로 큐에 넣어 둡니다.
    /// </summary>
    public void InitPool(GameObject prefab, int count, Transform parent = null)
    {
        if(prefab == null)
        {
            //Debug.LogWarning("InitPool 실패 : prefab이 null입니다.");
            return;
        }

        if(!poolDict.ContainsKey(prefab))
        {
            poolDict[prefab] = new Queue<GameObject>();
        }

        for(int i =0; i<count; i++)
        {
            GameObject obj = CreateNewObject(prefab, parent);
            poolDict[prefab].Enqueue(obj);
        }
    }
    /// <summary>
    /// 스폰용
    /// prefab용 큐를 찾음
    /// 남아 있으면 하나 꺼냄
    /// 없으면 새로 하나 만듦
    /// 위치/회전 세팅
    /// 활성화해서 반환
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if(prefab == null)
        {
            Debug.LogWarning("Get 실패 : prefab이 null입니다.");
            return null;
        }

        if(!poolDict.ContainsKey(prefab))
        {
            InitPool(prefab, 1);
        }
        GameObject obj;

        if (poolDict[prefab].Count > 0)
        {
            obj = poolDict[prefab].Dequeue();
        }
        else
        {
            obj = CreateNewObject(prefab);
        }

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);


        return obj;
    }
    /// <summary>
    /// 죽었을 때 / 총알 수명이 끝났을 때 사용하는 메서드
    /// 반납할 오브젝트에서 PooledObject 찾음
    /// 저장된 originPrefab 확인
    /// 원래 prefab 큐로 다시 넣음
    /// Destroy() 안 하고, 비활성화만 함
    /// </summary>
    public void ReturnToPool(GameObject instance)
    {
        if(instance == null)
        {
            Debug.LogWarning("ReturnToPool 실패 : instance가 null입니다.");
            return;
        }

        PooledObject pooledObject = instance.GetComponent<PooledObject>();

        if(pooledObject == null || pooledObject.originPrefab == null)
        {
            Debug.LogWarning($"{instance.name} 은(는) 풀 정보가 없습니다.");
            instance.SetActive(false);
            return;
        }

        GameObject originPrefab = pooledObject.originPrefab;

        if(!poolDict.ContainsKey(originPrefab))
        {
            poolDict[originPrefab] = new Queue<GameObject>();
        }

        instance.SetActive(false);
        poolDict[originPrefab].Enqueue(instance);

    }
    /// <summary>
    /// 다른 스크립트가 Instantiate()를 안 쓰고, ObjectPoolManager.Get()만 쓰게
    /// 실제 Instantiate()가 발생
    /// 풀 내부에서만 사용
    /// </summary>
    private GameObject CreateNewObject(GameObject prefab, Transform parent = null)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.SetActive(false);
        
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if(pooledObject == null)
        {
            pooledObject = obj.AddComponent<PooledObject>();
        }

        pooledObject.originPrefab = prefab;

        return obj;
    }
}
