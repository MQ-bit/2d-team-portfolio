#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DamageNumberTests
{
    [Test]
    public void EnemyReportsActualDamageBeforeDeathWithoutChangingHitEvents()
    {
        GameObject target = new GameObject("Damage test enemy");
        EnemyStatus status = target.AddComponent<EnemyStatus>();
        status.Initialize(10f, 1f);
        target.transform.position = new Vector3(2f, 3f, 0f);
        List<float> amounts = new();
        bool died = false;
        int hitCount = 0;
        System.Action<Vector3, float, FloatingNumberKind> listener = (position, amount, kind) =>
        {
            Assert.That(died, Is.False, "사망/풀 반환 전에 위치와 피해를 전달해야 합니다.");
            Assert.That(position, Is.EqualTo(target.transform.position));
            Assert.That(kind, Is.EqualTo(FloatingNumberKind.EnemyDamage));
            amounts.Add(amount);
        };
        DamageFeedback.OnValueChanged += listener;
        status.OnDeath += () => died = true;
        status.OnDamaged += () => hitCount++;
        try
        {
            status.DecreaseHealth(0f);
            status.DecreaseHealth(-5f);
            status.RestoreHealth(5f);
            Assert.That(amounts, Is.Empty);
            status.DecreaseHealth(3f);
            status.DecreaseHealth(50f);
            status.DecreaseHealth(1f);
            Assert.That(amounts, Is.EqualTo(new[] { 3f, 7f }));
            Assert.That(died, Is.True);
            Assert.That(hitCount, Is.EqualTo(1), "기존 OnDamaged는 생존 타격에만 발생해야 합니다.");
        }
        finally
        {
            DamageFeedback.OnValueChanged -= listener;
            Object.DestroyImmediate(target);
        }
    }

    [TestCase(10.4f, FloatingNumberKind.EnemyDamage, "10")]
    [TestCase(10.5f, FloatingNumberKind.PlayerDamage, "11")]
    [TestCase(12.5f, FloatingNumberKind.Healing, "+13")]
    [TestCase(2.5f, FloatingNumberKind.ThirstChange, "+3")]
    [TestCase(-2.5f, FloatingNumberKind.ThirstChange, "-3")]
    [TestCase(0.05f, FloatingNumberKind.PlayerDamage, "0")]
    [TestCase(-0.05f, FloatingNumberKind.ThirstChange, "0")]
    public void NumbersAreRoundedIntegers(float amount, FloatingNumberKind kind, string expected)
    {
        MethodInfo formatter = typeof(DamageNumberPresenter).GetMethod("FormatAmount", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(formatter.Invoke(null, new object[] { amount, kind }), Is.EqualTo(expected));
    }

    [Test]
    public void RecoveryAndThirstUseActualClampedChanges()
    {
        GameObject target = new GameObject("Recovery test player");
        PlayerStatus status = target.AddComponent<PlayerStatus>();
        SetField(status, "_healthInstance", new EntityHealth(95f, 100f));
        SetField(status, "_thirstInstance", new EntityThirst(90f, 100f));
        List<(float, FloatingNumberKind)> changes = new();
        System.Action<Vector3, float, FloatingNumberKind> listener = (position, amount, kind) => changes.Add((amount, kind));
        DamageFeedback.OnValueChanged += listener;
        try
        {
            status.RestoreHealth(20f);
            status.RestoreHealth(20f); // 최대 체력에서는 표시하지 않는다.
            status.RestoreThirst(30f);
            status.RestoreThirst(30f); // 최대 금단증상에서도 표시하지 않는다.
            status.DecreaseThirst(150f);
            status.DecreaseThirst(10f);
            Assert.That(changes, Is.EqualTo(new[]
            {
                (5f, FloatingNumberKind.Healing),
                (10f, FloatingNumberKind.ThirstChange),
                (-100f, FloatingNumberKind.ThirstChange)
            }));

            SetField(status, "_thirstInstance", new EntityThirst(5f, 100f, 1f, 0.01f));
            EntityThirst thirst = (EntityThirst)typeof(PlayerStatus).GetField("_thirstInstance",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(status);
            Assert.That(thirst.IncreaseTick(0.02f), Is.True);
            typeof(PlayerStatus).GetMethod("NotifyThirstChanged", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(status, new object[] { 5f });
            Assert.That(changes[changes.Count - 1], Is.EqualTo((1f, FloatingNumberKind.ThirstChange)));
        }
        finally
        {
            DamageFeedback.OnValueChanged -= listener;
            Object.DestroyImmediate(target);
        }
    }

    [UnityTest]
    public IEnumerator PopupSurvivesTargetAndReusesBoundedPool()
    {
        yield return new EnterPlayMode();
        GameObject cameraObject = new GameObject("Damage test camera", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(0f, 0f, -10f);

        GameObject canvasObject = new GameObject("Damage test canvas", typeof(Canvas));
        canvasObject.SetActive(false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        DamageNumberPresenter presenter = canvasObject.AddComponent<DamageNumberPresenter>();
        SetField(presenter, "_font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/11.Fonts/NeoDunggeunmoPro-Regular SDF.asset"));
        SetField(presenter, "_worldCamera", camera);
        SetField(presenter, "_prewarmCount", 2);
        SetField(presenter, "_maxVisible", 3);
        SetField(presenter, "_outlineColor", Color.black);
        SetField(presenter, "_outlineWidth", 0.5f);
        canvasObject.SetActive(true);
        yield return null;

        GameObject target = new GameObject("Pooled damage target");
        try
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>(true).Length, Is.EqualTo(2));
            EnemyStatus status = target.AddComponent<EnemyStatus>();
            status.Initialize(10f, 1f);
            status.DecreaseHealth(10f);
            Object.Destroy(target);
            yield return null;
            TextMeshProUGUI[] visible = canvasObject.GetComponentsInChildren<TextMeshProUGUI>();
            Assert.That(visible.Length, Is.EqualTo(1));
            Assert.That(visible[0].text, Is.EqualTo("10"));
            Assert.That(visible[0].raycastTarget, Is.False);

            // RASTER_HINTED 폰트에서도 실제 외곽선 메시가 생성되고 재생성 시 누적되지 않아야 한다.
            visible[0].ForceMeshUpdate();
            TMP_MeshInfo outlinedMesh = visible[0].textInfo.meshInfo[0];
            Assert.That(outlinedMesh.vertexCount, Is.EqualTo(2 * 4 * 9));
            int faceStart = 2 * 4 * 8;
            Assert.That(outlinedMesh.colors32[0], Is.EqualTo((Color32)Color.black));
            Assert.That(outlinedMesh.colors32[faceStart], Is.EqualTo((Color32)visible[0].color));
            Assert.That(outlinedMesh.vertices[faceStart].x - outlinedMesh.vertices[0].x,
                Is.EqualTo(visible[0].fontSize * 0.05f).Within(0.001f));
            visible[0].ForceMeshUpdate();
            Assert.That(visible[0].textInfo.meshInfo[0].vertexCount, Is.EqualTo(2 * 4 * 9));

            // 실행 중 변경도 반영되고, 두께 0에서는 본문만 남아야 한다.
            SetField(presenter, "_outlineWidth", 0f);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(visible[0].textInfo.meshInfo[0].vertexCount, Is.EqualTo(2 * 4));
            Assert.That(visible[0].textInfo.meshInfo[0].vertices[faceStart], Is.EqualTo(Vector3.zero));
            SetField(presenter, "_outlineWidth", 0.5f);

            yield return new WaitForSeconds(0.2f);
            Time.timeScale = 0f;
            yield return null;
            Vector2 beforePause = visible[0].rectTransform.anchoredPosition;
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(visible[0].rectTransform.anchoredPosition, Is.EqualTo(beforePause));
            Time.timeScale = 1f;

            for (int i = 0; i < 10; i++) DamageFeedback.Report(Vector3.zero, i + 1, false);
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>().Length, Is.EqualTo(3));
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>(true).Length, Is.EqualTo(3));
            yield return new WaitForSeconds(0.9f);
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>(), Is.Empty);

            DamageFeedback.Report(Vector3.zero, 0.05f, true);
            visible = canvasObject.GetComponentsInChildren<TextMeshProUGUI>();
            Assert.That(visible.Length, Is.EqualTo(1));
            Assert.That(visible[0].text, Is.EqualTo("0"));
            Assert.That(visible[0].color.g, Is.EqualTo(0.35f).Within(0.01f));
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>(true).Length, Is.EqualTo(3));

            DamageFeedback.ReportHealing(Vector3.zero, 12.5f);
            DamageFeedback.ReportThirstChange(Vector3.zero, -29.6f);
            visible = canvasObject.GetComponentsInChildren<TextMeshProUGUI>();
            Assert.That(System.Array.Exists(visible, text => text.text == "+13" && text.color.g > 0.9f), Is.True);
            Assert.That(System.Array.Exists(visible, text => text.text == "-30" && text.color.b > 0.9f), Is.True);

            canvasObject.SetActive(false);
            canvasObject.SetActive(true);
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>(), Is.Empty);
            DamageFeedback.Report(Vector3.zero, 2f, false);
            Assert.That(canvasObject.GetComponentsInChildren<TextMeshProUGUI>().Length, Is.EqualTo(1));
        }
        finally
        {
            Time.timeScale = 1f;
            Object.Destroy(canvasObject);
            Object.Destroy(cameraObject);
            if (target != null) Object.Destroy(target);
        }
        yield return null;
        yield return new ExitPlayMode();
    }

    private static void SetField(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, value);
    }
}
#endif
