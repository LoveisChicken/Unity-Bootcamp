using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSelectSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Content UI")]
    [SerializeField] private Image equipmentImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;

    [Header("State UI")]
    [SerializeField] private GameObject selectedOutline;
    [SerializeField] private GameObject lockOverlayRoot;
    [SerializeField] private TMP_Text lockConditionText;

    [Header("Tier Card VFX")]
    [SerializeField] private GameObject normalVfxRoot;
    [SerializeField] private GameObject rareVfxRoot;
    [SerializeField] private GameObject uniqueVfxRoot;

    //[SerializeField] private Image iconImage;

    private bool isUnlocked = true;
    private System.Action onClickAction;

    private void Awake()
    {
        InitTierVfx();
    }

    private void OnDisable()
    {
        HideAllTierVfx();
    }

    public void SetIcon(Sprite sprite)
    {
        if (equipmentImage != null && sprite != null)
            equipmentImage.sprite = sprite;
    }

    public void SetState(bool unlocked, string equipmentName, string levelLabel, string lockMessage = "")
    {
        isUnlocked = unlocked;

        if (equipmentImage != null)
            equipmentImage.gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = equipmentName;

        if (levelText != null)
        {
            levelText.gameObject.SetActive(unlocked);
            levelText.text = levelLabel;
        }

        if (lockOverlayRoot != null)
            lockOverlayRoot.SetActive(!unlocked);

        if (lockConditionText != null)
        {
            lockConditionText.gameObject.SetActive(!unlocked);
            lockConditionText.text = lockMessage;
        }

        if (!unlocked)
            SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedOutline != null)
            selectedOutline.SetActive(selected && isUnlocked);
    }

    public void SetOnClick(System.Action action)
    {
        onClickAction = action;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isUnlocked)
            return;

        onClickAction?.Invoke();
    }

    public void SetTierCardVfx(bool show, int tierIndex)
    {
        if (!show)
        {
            HideAllTierVfx();
            return;
        }

        GameObject targetRoot = GetTierVfxRoot(tierIndex);

        SetTierRoot(normalVfxRoot, targetRoot == normalVfxRoot);
        SetTierRoot(rareVfxRoot, targetRoot == rareVfxRoot);
        SetTierRoot(uniqueVfxRoot, targetRoot == uniqueVfxRoot);
    }

    private void InitTierVfx()
    {
        HideAllTierVfx();
    }

    private void HideAllTierVfx()
    {
        SetTierRoot(normalVfxRoot, false);
        SetTierRoot(rareVfxRoot, false);
        SetTierRoot(uniqueVfxRoot, false);
    }

    private GameObject GetTierVfxRoot(int tierIndex)
    {
        return tierIndex switch
        {
            0 => normalVfxRoot,
            1 => rareVfxRoot,
            2 => uniqueVfxRoot,
            _ => uniqueVfxRoot
        };
    }

    private void SetTierRoot(GameObject root, bool active)
    {
        if (root == null)
            return;

        // 같은 상태면 건드리지 않는다.
        // RefreshSelectButtons가 자주 호출될 수 있어서 VFX가 계속 리셋되는 것을 방지.
        if (root.activeSelf == active)
            return;

        root.SetActive(active);

        if (active)
            PlayParticlesInRoot(root);
    }

    private void PlayParticlesInRoot(GameObject root)
    {
        if (root == null)
            return;

        ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in particles)
        {
            if (particle == null)
                continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play(true);
        }
    }
}
