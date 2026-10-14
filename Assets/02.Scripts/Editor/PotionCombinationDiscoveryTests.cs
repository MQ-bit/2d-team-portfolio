#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class PotionCombinationDiscoveryTests
{
    private GameObject _catalogObject;
    private PotionRunCatalog _catalog;
    private PotionDatabase _potions;
    private PotionCombinationDatabase _combinations;
    private Sprite[] _sprites;

    [SetUp]
    public void SetUp()
    {
        _potions = AssetDatabase.LoadAssetAtPath<PotionDatabase>(
            AssetDatabase.GUIDToAssetPath("13a82fb1c7534d0aa31716c2263ee944"));
        _combinations = AssetDatabase.LoadAssetAtPath<PotionCombinationDatabase>(
            AssetDatabase.GUIDToAssetPath("1d5af485a8dda49468ad0f9fd6517fc2"));
        _catalogObject = new GameObject("Combination discovery test");
        _catalogObject.SetActive(false);
        _catalog = _catalogObject.AddComponent<PotionRunCatalog>();
        _sprites = new Sprite[6];
        for (int i = 0; i < _sprites.Length; i++)
            _sprites[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        Set(_catalog, "_database", _potions);
        Set(_catalog, "_potionSprites", _sprites);
        _catalog.Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_catalogObject);
        foreach (Sprite sprite in _sprites) Object.DestroyImmediate(sprite);
    }

    [Test]
    public void IdentifyingEveryIngredientDoesNotRevealCombinations()
    {
        foreach (PotionData potion in _potions.Potions)
            _catalog.TryIdentify(_catalog.GetPotionItemByData(potion));
        foreach (PotionCombinationData combination in _combinations.Combinations)
            Assert.That(_catalog.IsCombinationDiscovered(combination), Is.False);
    }

    [Test]
    public void WorldDiscoveryIsDeduplicatedForBothOrdersAndClearedForANewRun()
    {
        PotionCombinationData combination = _combinations.Combinations[0];
        List<PotionCombinationData> discoveries = new();
        _catalog.OnCombinationDiscovered += discoveries.Add;
        Assert.That(_catalog.TryDiscoverCombination(null), Is.False);
        Assert.That(_catalog.TryDiscoverCombination(combination), Is.True);
        Assert.That(_combinations.TryGetCombination(combination.ElementB, combination.ElementA,
            out PotionCombinationData reverse), Is.True);
        Assert.That(_catalog.TryDiscoverCombination(reverse), Is.False);
        Assert.That(discoveries, Is.EqualTo(new[] { combination }));
        Assert.That(_catalog.IsCombinationDiscovered(combination), Is.True);
        _catalog.Initialize();
        Assert.That(_catalog.IsCombinationDiscovered(combination), Is.False);
    }

    [Test]
    public void BookCannotRevealAnUntestedRecipeAndUnlocksAfterWorldDiscovery()
    {
        GameObject hud = PrefabUtility.LoadPrefabContents(
            "Assets/03.Prefabs/UI/IngameUI/PotionInformationHUD.prefab");
        CombinationCatalogPresenter presenter = hud.GetComponent<CombinationCatalogPresenter>();
        bool subscribed = false;
        try
        {
            Invoke(presenter, "Awake");
            Set(presenter, "_runCatalog", _catalog);
            Invoke(presenter, "OnEnable");
            subscribed = true;
            PotionCombinationData combination = _combinations.Combinations[0];
            Assert.That(_potions.TryGetByElement(combination.ElementA, out PotionData first), Is.True);
            Assert.That(_potions.TryGetByElement(combination.ElementB, out PotionData second), Is.True);
            _catalog.TryIdentify(_catalog.GetPotionItemByData(first));
            _catalog.TryIdentify(_catalog.GetPotionItemByData(second));
            var cards = Get<DictionaryCardSlotView[]>(presenter, "_cards");
            cards[0].SetRevealed(first, _sprites[0], _sprites[0]);
            cards[1].SetRevealed(second, _sprites[1], _sprites[1]);
            Invoke(presenter, "HandleCardClicked", cards[0]);
            Invoke(presenter, "HandleCardClicked", cards[1]);
            Button button = Get<Button>(presenter, "_confirmButton");
            CombinationInfoSlot slot = Get<CombinationInfoSlot[]>(presenter, "_slots")[0];
            Assert.That(button.interactable, Is.False);
            Invoke(presenter, "ConfirmCombination"); // Even a direct invocation must not reveal the result.
            Assert.That(Get<Image>(slot, "_resultIcon").sprite, Is.Null);
            Assert.That(Get<TMP_Text>(slot, "_statusLabel").text, Is.EqualTo("아직 남겨진 기록이 없다"));
            presenter.ShowTableCombination(combination);
            Assert.That(Get<Image>(slot, "_resultIcon").sprite, Is.Null);

            Invoke(presenter, "HandleCardClicked", cards[0]);
            Invoke(presenter, "HandleCardClicked", cards[1]);
            _catalog.TryDiscoverCombination(combination);
            Assert.That(button.interactable, Is.True);
            Assert.That(Get<Image>(slot, "_resultIcon").sprite, Is.Null, "Discovery enables record access without opening it automatically.");
            button.onClick.Invoke();
            Assert.That(Get<TMP_Text>(slot, "_descriptionText").text, Is.EqualTo(combination.Description));
            _catalog.Initialize();
            presenter.Refresh();
            Assert.That(button.interactable, Is.False);
            Assert.That(Get<Image>(slot, "_resultIcon").sprite, Is.Null);
        }
        finally
        {
            if (subscribed) Invoke(presenter, "OnDisable");
            PrefabUtility.UnloadPrefabContents(hud);
        }
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static T Get<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
}
#endif
