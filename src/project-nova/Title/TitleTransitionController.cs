using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleTransitionController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    [Header("Skip")]
    [SerializeField] private Button skipButton;
    [SerializeField] private bool allowSkip = true;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup introTitleCanvasGroup;
    [SerializeField] private CanvasGroup doorCanvasGroup;
    [SerializeField] private CanvasGroup wormholeCanvasGroup;
    [SerializeField] private CanvasGroup warpCanvasGroup;
    [SerializeField] private CanvasGroup whiteFlashCanvasGroup;

    [Header("Transition Overlay")]
    [SerializeField] private CanvasGroup transitionOverlayCanvasGroup;

    [Header("Login")]
    [SerializeField] private GameObject signInPopupRoot;
    [SerializeField] private LoginUIController loginUIController;

    [Header("Doors")]
    [SerializeField] private RectTransform leftDoor;
    [SerializeField] private RectTransform rightDoor;

    [Header("Effect Images")]
    [SerializeField] private RectTransform wormholeImage;
    [SerializeField] private RectTransform warpImage;

    [Header("Door Positions")]
    [SerializeField] private Vector2 leftDoorClosedPos = new Vector2(-463f, 0f);
    [SerializeField] private Vector2 rightDoorClosedPos = new Vector2(463f, 0f);

    [SerializeField] private Vector2 leftDoorTeaseOpenPos = new Vector2(-560f, 0f);
    [SerializeField] private Vector2 rightDoorTeaseOpenPos = new Vector2(560f, 0f);

    [SerializeField] private Vector2 leftDoorOpenedPos = new Vector2(-1450f, 0f);
    [SerializeField] private Vector2 rightDoorOpenedPos = new Vector2(1450f, 0f);

    [Header("Dark Transition Timing")]
    [SerializeField] private float darkFadeInDuration = 0.9f;
    [SerializeField] private float darkHoldDuration = 0.4f;
    [SerializeField] private float darkFadeOutDuration = 0.7f;

    [Header("Door Timing")]
    [SerializeField] private float doorTeaseOpenDuration = 0.8f;
    [SerializeField] private float doorTeaseCloseDuration = 0.3f;
    [SerializeField] private float wormholeAppearDuration = 2f;
    [SerializeField] private float doorOpenDuration = 2.2f;
    [SerializeField] private float wormholeEnterDuration = 2f;

    [Header("Warp Timing")]
    [SerializeField] private float warpFadeInDuration = 0.8f;
    [SerializeField] private float warpAccelerationDuration = 3.5f;
    [SerializeField] private float whiteFlashInDuration = 0.12f;
    [SerializeField] private float whiteFlashHoldDuration = 0.12f;
    [SerializeField] private float whiteFlashOutDuration = 0.35f;

    [Header("Wormhole Scale")]
    [SerializeField] private float wormholeStartScale = 1.0f;
    [SerializeField] private float wormholeDoorOpenScale = 1.08f;
    [SerializeField] private float wormholeEnterScale = 1.25f;

    [Header("Warp Scale")]
    [SerializeField] private float warpStartScale = 1.0f;
    [SerializeField] private float warpEndScale = 1.65f;

    [Header("Rotation")]
    [SerializeField] private float wormholeRotationAmount = 8f;
    [SerializeField] private float warpRotationAmount = 24f;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [SerializeField] private AudioClip startSfx;
    [SerializeField] private AudioClip exitSfx;  // [PSJ]
    [SerializeField] private AudioClip warpSfx;

    [SerializeField] private float bgmFadeOutDuration = 1.0f;

    [SerializeField] private bool syncLoginWithWarpSfx = true;

    private bool isTransitioning = false;

    private Coroutine transitionRoutine;
    private bool isSkipped = false;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClickStart);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnClickExit);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnClickSkip);
            skipButton.gameObject.SetActive(false);
        }

        InitState();
    }

    private void InitState()
    {
        isTransitioning = false;

        if (leftDoor != null)
            leftDoor.anchoredPosition = leftDoorClosedPos;

        if (rightDoor != null)
            rightDoor.anchoredPosition = rightDoorClosedPos;

        if (introTitleCanvasGroup != null)
        {
            introTitleCanvasGroup.gameObject.SetActive(true);
            introTitleCanvasGroup.alpha = 1f;
            introTitleCanvasGroup.interactable = true;
            introTitleCanvasGroup.blocksRaycasts = true;
        }

        /*
         * 중요:
         * DoorRoot는 처음부터 Alpha 1로 둔다.
         * IntroTitleRoot가 DoorRoot 위에 덮여 있기 때문에,
         * 시작 화면에서는 타이틀 이미지만 보이고 문은 뒤에 대기한다.
         */
        if (doorCanvasGroup != null)
        {
            doorCanvasGroup.alpha = 1f;
            doorCanvasGroup.interactable = false;
            doorCanvasGroup.blocksRaycasts = false;
        }

        if (wormholeCanvasGroup != null)
        {
            wormholeCanvasGroup.alpha = 0f;
            wormholeCanvasGroup.interactable = false;
            wormholeCanvasGroup.blocksRaycasts = false;
        }

        if (warpCanvasGroup != null)
        {
            warpCanvasGroup.alpha = 0f;
            warpCanvasGroup.interactable = false;
            warpCanvasGroup.blocksRaycasts = false;
        }

        if (whiteFlashCanvasGroup != null)
        {
            whiteFlashCanvasGroup.alpha = 0f;
            whiteFlashCanvasGroup.interactable = false;
            whiteFlashCanvasGroup.blocksRaycasts = false;
        }

        if (transitionOverlayCanvasGroup != null)
        {
            transitionOverlayCanvasGroup.alpha = 0f;
            transitionOverlayCanvasGroup.interactable = false;
            transitionOverlayCanvasGroup.blocksRaycasts = false;
        }

        if (wormholeImage != null)
        {
            wormholeImage.localScale = Vector3.one * wormholeStartScale;
            wormholeImage.localRotation = Quaternion.identity;
        }

        if (warpImage != null)
        {
            warpImage.localScale = Vector3.one * warpStartScale;
            warpImage.localRotation = Quaternion.identity;
        }

        if (signInPopupRoot != null)
            signInPopupRoot.SetActive(false);
    }

    private void OnClickStart()
    {
        if (isTransitioning)
            return;

        transitionRoutine = StartCoroutine(StartTransitionCo());
    }

    private IEnumerator StartTransitionCo()
    {
        isTransitioning = true;
        isSkipped = false;

        if (skipButton != null && allowSkip)
            skipButton.gameObject.SetActive(true);

        if (startButton != null)
            startButton.interactable = false;

        if (exitButton != null)
            exitButton.interactable = false;

        if (introTitleCanvasGroup != null)
        {
            introTitleCanvasGroup.interactable = false;
            introTitleCanvasGroup.blocksRaycasts = false;
        }

        // 시작 피드백 사운드
        PlaySfx(startSfx);

        // 타이틀 BGM 서서히 종료
        StartCoroutine(FadeOutBgm());

        // 1. 암전 전환: 타이틀 → 문
        yield return PlayDarkTransitionToDoor();

        // 2. 문 살짝 열림/닫힘
        yield return DoorTease();

        // 3. 웜홀 등장
        yield return ShowWormhole();

        // 4. 문 본격 오픈 + 웜홀 확대
        yield return OpenDoorsWithWormhole();

        float warpSfxStartTime = Time.time;
        PlaySfx(warpSfx);

        // 5. 웜홀 진입
        yield return EnterWormhole();

        // 6. 워프 가속
        yield return PlayAcceleratingWarp();

        if (syncLoginWithWarpSfx && warpSfx != null)
        {
            yield return WaitUntilWarpSfxAlmostDone(warpSfxStartTime);
        }

        // 7. 흰색 플래시
        yield return WhiteFlash();

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // 8. 로그인 팝업 표시
        if (loginUIController != null)
        {
            loginUIController.OpenLoginPopupFromTitle();
        }
        else if (signInPopupRoot != null)
        {
            signInPopupRoot.SetActive(true);
        }
    }

    private IEnumerator PlayDarkTransitionToDoor()
    {
        if (transitionOverlayCanvasGroup == null)
        {
            if (introTitleCanvasGroup != null)
            {
                introTitleCanvasGroup.alpha = 0f;
                introTitleCanvasGroup.gameObject.SetActive(false);
            }

            yield break;
        }

        float elapsed = 0f;

        // 1. 타이틀 화면을 검게 덮음
        while (elapsed < darkFadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / darkFadeInDuration);
            float smoothT = Smooth(t);

            transitionOverlayCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);

            yield return null;
        }

        transitionOverlayCanvasGroup.alpha = 1f;

        /*
         * 검은 화면으로 완전히 덮인 시점에서 타이틀을 끈다.
         * 뒤에는 이미 DoorRoot가 Alpha 1로 대기하고 있으므로,
         * Overlay가 사라지면 문 이미지가 보인다.
         */
        if (introTitleCanvasGroup != null)
        {
            introTitleCanvasGroup.alpha = 0f;
            introTitleCanvasGroup.gameObject.SetActive(false);
        }

        // 2. 아주 짧게 암전 유지
        if (darkHoldDuration > 0f)
            yield return new WaitForSeconds(darkHoldDuration);

        // 3. 검은 화면이 걷히면서 문이 드러남
        elapsed = 0f;

        while (elapsed < darkFadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / darkFadeOutDuration);
            float smoothT = Smooth(t);

            transitionOverlayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);

            yield return null;
        }

        transitionOverlayCanvasGroup.alpha = 0f;
    }

    private IEnumerator DoorTease()
    {
        float elapsed = 0f;

        while (elapsed < doorTeaseOpenDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / doorTeaseOpenDuration);
            float smoothT = Smooth(t);

            if (leftDoor != null)
                leftDoor.anchoredPosition = Vector2.Lerp(leftDoorClosedPos, leftDoorTeaseOpenPos, smoothT);

            if (rightDoor != null)
                rightDoor.anchoredPosition = Vector2.Lerp(rightDoorClosedPos, rightDoorTeaseOpenPos, smoothT);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < doorTeaseCloseDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / doorTeaseCloseDuration);
            float smoothT = Smooth(t);

            if (leftDoor != null)
                leftDoor.anchoredPosition = Vector2.Lerp(leftDoorTeaseOpenPos, leftDoorClosedPos, smoothT);

            if (rightDoor != null)
                rightDoor.anchoredPosition = Vector2.Lerp(rightDoorTeaseOpenPos, rightDoorClosedPos, smoothT);

            yield return null;
        }

        if (leftDoor != null)
            leftDoor.anchoredPosition = leftDoorClosedPos;

        if (rightDoor != null)
            rightDoor.anchoredPosition = rightDoorClosedPos;
    }

    private IEnumerator ShowWormhole()
    {
        if (wormholeCanvasGroup == null)
            yield break;

        float elapsed = 0f;

        if (wormholeImage != null)
        {
            wormholeImage.localScale = Vector3.one * wormholeStartScale;
            wormholeImage.localRotation = Quaternion.identity;
        }

        while (elapsed < wormholeAppearDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / wormholeAppearDuration);
            float smoothT = Smooth(t);

            wormholeCanvasGroup.alpha = Mathf.Lerp(0f, 0.55f, smoothT);

            yield return null;
        }

        wormholeCanvasGroup.alpha = 0.55f;
    }

    private IEnumerator OpenDoorsWithWormhole()
    {
        float elapsed = 0f;

        Vector2 leftStart = leftDoor != null ? leftDoor.anchoredPosition : Vector2.zero;
        Vector2 rightStart = rightDoor != null ? rightDoor.anchoredPosition : Vector2.zero;

        Vector3 wormholeStartScaleVector = wormholeImage != null
            ? wormholeImage.localScale
            : Vector3.one * wormholeStartScale;

        Vector3 wormholeTargetScaleVector = Vector3.one * wormholeDoorOpenScale;

        while (elapsed < doorOpenDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / doorOpenDuration);
            float smoothT = Smooth(t);

            if (leftDoor != null)
                leftDoor.anchoredPosition = Vector2.Lerp(leftStart, leftDoorOpenedPos, smoothT);

            if (rightDoor != null)
                rightDoor.anchoredPosition = Vector2.Lerp(rightStart, rightDoorOpenedPos, smoothT);

            if (wormholeCanvasGroup != null)
                wormholeCanvasGroup.alpha = Mathf.Lerp(0.55f, 0.85f, smoothT);

            if (wormholeImage != null)
            {
                wormholeImage.localScale = Vector3.Lerp(
                    wormholeStartScaleVector,
                    wormholeTargetScaleVector,
                    smoothT
                );

                float rotationZ = Mathf.Lerp(0f, wormholeRotationAmount * 0.5f, smoothT);
                wormholeImage.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            yield return null;
        }

        if (leftDoor != null)
            leftDoor.anchoredPosition = leftDoorOpenedPos;

        if (rightDoor != null)
            rightDoor.anchoredPosition = rightDoorOpenedPos;

        if (wormholeCanvasGroup != null)
            wormholeCanvasGroup.alpha = 0.85f;

        if (wormholeImage != null)
        {
            wormholeImage.localScale = wormholeTargetScaleVector;
            wormholeImage.localRotation = Quaternion.Euler(0f, 0f, wormholeRotationAmount * 0.5f);
        }
    }

    private IEnumerator EnterWormhole()
    {
        if (wormholeCanvasGroup == null)
            yield break;

        float elapsed = 0f;

        Vector3 startScale = wormholeImage != null
            ? wormholeImage.localScale
            : Vector3.one * wormholeDoorOpenScale;

        Vector3 targetScale = Vector3.one * wormholeEnterScale;

        Quaternion startRotation = wormholeImage != null
            ? wormholeImage.localRotation
            : Quaternion.identity;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, wormholeRotationAmount);

        while (elapsed < wormholeEnterDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / wormholeEnterDuration);
            float smoothT = Smooth(t);

            wormholeCanvasGroup.alpha = Mathf.Lerp(0.85f, 1f, smoothT);

            if (wormholeImage != null)
            {
                wormholeImage.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                wormholeImage.localRotation = Quaternion.Lerp(startRotation, targetRotation, smoothT);
            }

            yield return null;
        }

        wormholeCanvasGroup.alpha = 1f;

        if (wormholeImage != null)
        {
            wormholeImage.localScale = targetScale;
            wormholeImage.localRotation = targetRotation;
        }
    }

    private IEnumerator PlayAcceleratingWarp()
    {
        if (warpCanvasGroup == null)
            yield break;

        if (warpImage != null)
        {
            warpImage.localScale = Vector3.one * warpStartScale;
            warpImage.localRotation = Quaternion.identity;
        }

        float elapsed = 0f;

        while (elapsed < warpFadeInDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / warpFadeInDuration);
            float smoothT = Smooth(t);

            warpCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);

            if (warpImage != null)
            {
                float scale = Mathf.Lerp(warpStartScale, warpStartScale + 0.1f, smoothT);
                warpImage.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        warpCanvasGroup.alpha = 1f;

        if (wormholeCanvasGroup != null)
            wormholeCanvasGroup.alpha = 0f;

        elapsed = 0f;

        while (elapsed < warpAccelerationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / warpAccelerationDuration);
            float accelT = t * t;

            if (warpImage != null)
            {
                float scale = Mathf.Lerp(warpStartScale + 0.1f, warpEndScale, accelT);
                warpImage.localScale = Vector3.one * scale;

                float rotationZ = Mathf.Lerp(0f, warpRotationAmount, accelT);
                warpImage.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            warpCanvasGroup.alpha = Mathf.Lerp(1f, 0.85f, t);

            yield return null;
        }
    }

    private IEnumerator WhiteFlash()
    {
        if (whiteFlashCanvasGroup == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < whiteFlashInDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / whiteFlashInDuration);
            float smoothT = Smooth(t);

            whiteFlashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);

            if (warpCanvasGroup != null)
                warpCanvasGroup.alpha = Mathf.Lerp(0.85f, 0f, smoothT);

            yield return null;
        }

        whiteFlashCanvasGroup.alpha = 1f;

        if (warpCanvasGroup != null)
            warpCanvasGroup.alpha = 0f;

        yield return new WaitForSeconds(whiteFlashHoldDuration);

        elapsed = 0f;

        while (elapsed < whiteFlashOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / whiteFlashOutDuration);
            float smoothT = Smooth(t);

            whiteFlashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);

            yield return null;
        }

        whiteFlashCanvasGroup.alpha = 0f;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    private IEnumerator FadeOutBgm()
    {
        if (bgmSource == null)
            yield break;

        float startVolume = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < bgmFadeOutDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / bgmFadeOutDuration);
            float smoothT = Smooth(t);

            bgmSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);

            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
    }

    private IEnumerator WaitUntilWarpSfxAlmostDone(float warpSfxStartTime)
    {
        if (warpSfx == null)
            yield break;

        float whiteFlashTotalDuration =
            whiteFlashInDuration +
            whiteFlashHoldDuration +
            whiteFlashOutDuration;

        float targetWaitTime = Mathf.Max(0f, warpSfx.length - whiteFlashTotalDuration);

        while (Time.time - warpSfxStartTime < targetWaitTime)
        {
            // 기다리는 동안 워프 이미지가 완전히 멈춰 보이지 않도록 아주 약하게 회전 유지
            if (warpImage != null)
            {
                float rotationZ = warpImage.localEulerAngles.z + Time.deltaTime * 8f;
                warpImage.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            }

            yield return null;
        }
    }

    private void OnClickSkip()
    {
        if (!isTransitioning)
            return;

        if (!allowSkip)
            return;

        isSkipped = true;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        StopAllCoroutines();

        SkipToLogin();
    }

    private void SkipToLogin()
    {
        isTransitioning = false;

        if (skipButton != null)
            skipButton.gameObject.SetActive(false);

        // 버튼 입력 비활성화 유지
        if (startButton != null)
            startButton.interactable = false;

        if (exitButton != null)
            exitButton.interactable = false;

        // 타이틀 화면 숨김
        if (introTitleCanvasGroup != null)
        {
            introTitleCanvasGroup.alpha = 0f;
            introTitleCanvasGroup.interactable = false;
            introTitleCanvasGroup.blocksRaycasts = false;
            introTitleCanvasGroup.gameObject.SetActive(false);
        }

        // 문/웜홀/워프 연출 숨김
        if (doorCanvasGroup != null)
            doorCanvasGroup.alpha = 0f;

        if (wormholeCanvasGroup != null)
            wormholeCanvasGroup.alpha = 0f;

        if (warpCanvasGroup != null)
            warpCanvasGroup.alpha = 0f;

        if (transitionOverlayCanvasGroup != null)
            transitionOverlayCanvasGroup.alpha = 0f;

        if (whiteFlashCanvasGroup != null)
            whiteFlashCanvasGroup.alpha = 0f;

        // 사운드 정리
        if (sfxSource != null)
            sfxSource.Stop();

        if (bgmSource != null)
            bgmSource.Stop();

        // 로그인 팝업 표시
        if (loginUIController != null)
        {
            loginUIController.OpenLoginPopupFromTitle();
        }
        else if (signInPopupRoot != null)
        {
            signInPopupRoot.SetActive(true);
        }
    }

    private float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void OnClickExit()
    {
        PlaySfx(exitSfx); // [PSJ]
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
