using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FloorAnnouncementUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SceneVisualEffectManager _visualEffectManager;
    [SerializeField] private TMP_Text _floorText;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float _startDelay = 0.15f;
    [SerializeField, Min(0f)] private float _fadeInDuration = 0.45f;
    [SerializeField, Min(0f)] private float _holdDuration = 1.5f;
    [SerializeField, Min(0f)] private float _fadeOutDuration = 0.6f;

    private CanvasGroup _canvasGroup;
    private Coroutine _showRoutine;
    private int _lastShownFloor;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();

        // 안내 UI가 뒤에 있는 버튼이나 마우스 입력을 가로막지 않게 합니다.
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0f;

        if (_floorText == null)
        {
            Debug.LogError("FloorAnnouncementUI: FloorText를 연결하세요.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // 프리팹에는 씬 오브젝트 참조를 저장할 수 없으므로,
        // 연결되지 않았다면 씬에서 한 번 찾아 사용합니다.
        if (_visualEffectManager == null)
        {
            _visualEffectManager =
                FindAnyObjectByType<SceneVisualEffectManager>();
        }

        if (_visualEffectManager == null)
        {
            Debug.LogError(
                "FloorAnnouncementUI: SceneVisualEffectManager가 없습니다.",
                this);
            return;
        }

        _visualEffectManager.FadeInCompleted += HandleFadeInCompleted;
    }

    private void HandleFadeInCompleted()
    {
        ScoreManager scoreManager = ScoreManager.Instance;

        if (scoreManager == null || !scoreManager.IsRunActive)
        {
            return;
        }

        // 별도의 층 카운터를 만들지 않고 기존 플레이 기록을 읽습니다.
        int floor =
            scoreManager.GetCurrentScoreSnapshot().ClearedFloorCount + 1;

        // 같은 층에서 다른 페이드 인이 실행되어도 안내를 반복하지 않습니다.
        if (floor == _lastShownFloor)
        {
            return;
        }

        _lastShownFloor = floor;

        // 이전 연출이 남아 있다면 중단해서 투명도 제어가 겹치지 않게 합니다.
        StopAnnouncement();

        _floorText.text = $"{floor}층";
        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (_startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(_startDelay);
        }

        yield return FadeTo(1f, _fadeInDuration);

        if (_holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(_holdDuration);
        }

        yield return FadeTo(0f, _fadeOutDuration);

        _showRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = _canvasGroup.alpha;

        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 기존 화면 페이드와 동일하게 게임 시간 배율의 영향을 받지 않습니다.
            // 로딩 직후 긴 프레임 때문에 연출이 한 번에 끝나는 것도 완화합니다.
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            float progress = Mathf.Clamp01(elapsed / duration);

            // 시작과 끝에서 부드럽게 변하도록 투명도 곡선을 보정합니다.
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            _canvasGroup.alpha =
                Mathf.Lerp(startAlpha, targetAlpha, easedProgress);

            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
    }

    private void StopAnnouncement()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        _canvasGroup.alpha = 0f;
    }

    private void OnDisable()
    {
        // 씬 종료나 UI 비활성화 시 이벤트 연결과 진행 중인 연출을 정리합니다.
        if (_visualEffectManager != null)
        {
            _visualEffectManager.FadeInCompleted -= HandleFadeInCompleted;
        }

        StopAnnouncement();
    }
}