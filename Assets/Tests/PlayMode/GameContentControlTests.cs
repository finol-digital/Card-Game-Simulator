/* This Source Code Form is subject to the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Reflection;
using Cgs;
using Cgs.Cards;
using Cgs.Decks;
using Cgs.CardGameView.Viewer;
using FinolDigital.Cgs.Json;
using FinolDigital.Cgs.Json.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    public class GameContentControlTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;
        private UnityCardGame _previous;
        private UnityCardGame _game;
        private static PropertyInfo Current => typeof(CardGameManager).GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string property) => (T)target.GetType().GetProperty(property, Private).GetValue(target);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Game-content controls");
            _root.SetActive(false);
            _previous = CardGameManager.Current;
            _game = new UnityCardGame(null, "Control test");
            Current.SetValue(null, _game);
        }

        [TearDown]
        public void CleanUp()
        {
            Current.SetValue(null, _previous);
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void NameChangeBeforeShowingADeckDoesNotAccessMissingDeckOrControls()
        {
            var menu = _root.AddComponent<DeckSaveMenu>();
            Assert.DoesNotThrow(() => menu.ChangeName("New name"));
        }

        [Test]
        public void DuplicateTranslatedPropertyLabelsKeepIndependentCanonicalSelections()
        {
            _game.CardProperties.Add(new PropertyDef("first", PropertyType.String, "First"));
            _game.CardProperties.Add(new PropertyDef("second", PropertyType.String, "Second"));
            _game.CardPrimaryProperty = "second";
            _game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en"",""locales"":{""en"":{""inline"":{""properties"":{""first"":{""display"":""Same""},""second"":{""display"":""Same""}}}}}}").Value;
            var viewer = _root.AddComponent<CardViewer>();
            Set(viewer, "propertySelectors", new List<Dropdown>());
            Set(viewer, "propertyValueTexts", new List<Text>());
            var paths = Get<List<string>>(viewer, "PropertyPaths");
            var labels = Get<List<string>>(viewer, "PropertyLabels");
            var options = Get<List<Dropdown.OptionData>>(viewer, "PropertyOptions");
            paths.AddRange(new[] { "Set", "Id" });
            labels.AddRange(paths);
            foreach (var path in paths) options.Add(new Dropdown.OptionData(path));
            var addProperty = typeof(CardViewer).GetMethod("AddProperty", Private);
            foreach (var definition in _game.CardProperties)
                addProperty.Invoke(viewer, new object[] { definition, "" });
            typeof(CardViewer).GetMethod("RefreshPropertyLabels", Private).Invoke(viewer, new object[] { _game });
            Assert.That(options[2].text, Is.EqualTo("Same"));
            Assert.That(options[3].text, Is.EqualTo("Same"));
            viewer.SelectedPropertyIndex = 2;
            Assert.That(Get<string>(viewer, "SelectedPropertyName"), Is.EqualTo("first"));
            viewer.SelectedPropertyIndex = 3;
            Assert.That(Get<string>(viewer, "SelectedPropertyName"), Is.EqualTo("second"));
            Assert.That(Get<int>(viewer, "PrimaryPropertyIndex"), Is.EqualTo(3));
        }

        [Test]
        public void TranslatedEmptyEnumControlRetainsCanonicalKeyAndBit()
        {
            var property = new PropertyDef("element", PropertyType.StringEnum, "Element", "None");
            _game.CardProperties.Add(property);
            var enumDefinition = new EnumDef("element", new Dictionary<string, string> { ["0x1"] = "Fire" });
            enumDefinition.InitializeLookups();
            _game.Enums.Add(enumDefinition);
            _game.Localization = LocalizationJson.ReadManifest(@"{""defaultLocale"":""en"",""locales"":{""en"":{""inline"":{""properties"":{""element"":{""displayEmpty"":""Vacío""}}}}}}").Value;
            var menu = _root.AddComponent<CardSearchMenu>();
            menu.enabled = false;
            using var submit = new InputAction("Test submit");
            Set(menu, "_submitAction", submit);
            var panel = _root.AddComponent<SearchFilterPanel>();
            var group = new GameObject("Toggle group", typeof(RectTransform));
            group.transform.SetParent(_root.transform);
            var prototype = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            prototype.transform.SetParent(group.transform);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(prototype.transform);
            Set(panel, "toggleGroupContainer", (RectTransform)group.transform);
            Set(panel, "toggle", prototype.GetComponent<Toggle>());
            _root.SetActive(true);
            typeof(CardSearchMenu).GetMethod("CreateEmptyEnumToggle", Private).Invoke(menu,
                new object[] { "element", property, enumDefinition, panel, 2, Vector3.zero });
            Assert.That(enumDefinition.Lookups["None"], Is.EqualTo(2));
            Assert.That(enumDefinition.Lookups.ContainsKey("Vacío"), Is.False);
            Assert.That(enumDefinition.Lookups.Count, Is.EqualTo(2));
            var created = group.transform.GetChild(1).GetComponent<Toggle>();
            Assert.That(created.isOn, Is.True);
            Assert.That(created.GetComponentInChildren<Text>(true).text, Is.EqualTo("Vacío"));
            created.isOn = false;
            created.isOn = true;
            var filters = (CardSearchFilters)typeof(CardSearchMenu).GetField("_filters", Private).GetValue(menu);
            Assert.That(filters.EnumProperties["element"], Is.EqualTo(2));
        }
    }
}
