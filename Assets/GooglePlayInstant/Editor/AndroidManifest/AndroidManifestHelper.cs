// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace GooglePlayInstant.Editor.AndroidManifest
{
    /// <summary>
    /// A helper class for updating the AndroidManifest to target installed vs instant apps.
    /// </summary>
    public static class AndroidManifestHelper
    {
        private const string Action = "action";
        private const string Activity = "activity";
        private const string Application = "application";
        private const string Category = "category";
        private const string Data = "data";
        private const string DefaultUrl = "default-url";
        private const string IntentFilter = "intent-filter";
        private const string Manifest = "manifest";
        private const string MetaData = "meta-data";
        private const string PlayInstantUnityPluginVersion = "play-instant-unity-plugin.version";
        private const string ValueTrue = "true";
        private const string AndroidNamespaceAlias = "android";
        private const string AndroidNamespaceUrl = "http://schemas.android.com/apk/res/android";
        private const string DistributionNamespaceAlias = "dist";
        private const string DistributionNamespaceUrl = "http://schemas.android.com/apk/distribution";

        private static readonly XName AndroidXmlns = XNamespace.Xmlns + AndroidNamespaceAlias;
        private static readonly XName AndroidAutoVerifyXName = XName.Get("autoVerify", AndroidNamespaceUrl);
        private static readonly XName AndroidHostXName = XName.Get("host", AndroidNamespaceUrl);
        private static readonly XName AndroidNameXName = XName.Get("name", AndroidNamespaceUrl);
        private static readonly XName AndroidPathXName = XName.Get("path", AndroidNamespaceUrl);
        private static readonly XName AndroidSchemeXName = XName.Get("scheme", AndroidNamespaceUrl);
        private static readonly XName AndroidValueXName = XName.Get("value", AndroidNamespaceUrl);
        private static readonly XName DistributionXmlns = XNamespace.Xmlns + DistributionNamespaceAlias;
        private static readonly XName DistributionModuleXName = XName.Get("module", DistributionNamespaceUrl);
        private static readonly XName DistributionInstantXName = XName.Get("instant", DistributionNamespaceUrl);

        private static readonly XName AndroidTargetSandboxVersionXName =
            XName.Get("targetSandboxVersion", AndroidNamespaceUrl);

        // These precondition check strings are visibile for testing.
        internal const string PreconditionOneManifestElement = "expect 1 manifest element";
        internal const string PreconditionMissingXmlnsAndroid = "missing manifest attribute xmlns:android";
        internal const string PreconditionInvalidXmlnsAndroid = "invalid value for xmlns:android";
        internal const string PreconditionInvalidXmlnsDistribution = "invalid value for xmlns:dist";
        internal const string PreconditionOneApplicationElement = "expect 1 application element";
        internal const string PreconditionOneMainActivity = "expect 1 activity with action MAIN and category LAUNCHER";
        internal const string PreconditionOneViewIntentFilter = "more than one VIEW intent-filter";
        internal const string PreconditionOneMetaDataDefaultUrl = "more than one meta-data element for default-url";
        internal const string PreconditionOneModuleInstant = "more than one dist:module element with dist:instant";
        internal const string PreconditionOnePluginVersion = "more than one meta-data element for plugin version";

        private delegate IEnumerable<XElement> ElementFinder(XElement element);

        /// <summary>
        /// Returns the <see cref="IAndroidManifestUpdater"/> appropriate for the version of Unity.
        /// </summary>
        public static IAndroidManifestUpdater GetAndroidManifestUpdater()
        {
#if UNITY_2018_1_OR_NEWER
            return new PostGenerateGradleProjectAndroidManifestUpdater();
#else
            return new LegacyAndroidManifestUpdater();
#endif
        }

        /// <summary>
        /// Creates a new XDocument representing a basic Unity AndroidManifest XML file.
        /// </summary>
        public static XDocument CreateManifestXDocument()
        {
            return new XDocument(new XElement(
                Manifest,
                new XAttribute(AndroidXmlns, XNamespace.Get(AndroidNamespaceUrl)),
                new XElement(Application,
                    new XElement(Activity,
                        new XAttribute(AndroidNameXName, "com.unity3d.player.UnityPlayerActivity"),
                        new XElement(IntentFilter,
                            new XElement(Action, new XAttribute(AndroidNameXName, Android.IntentActionMain)),
                            new XElement(Category, new XAttribute(AndroidNameXName, Android.IntentCategoryLauncher))
                        )))));
        }

        /// <summary>
        /// Returns true if the specified XDocument representing an AndroidManifest has the correct plugin version.
        /// </summary>
        public static bool HasCurrentPluginVersion(XDocument doc, out string errorMessage)
        {
            var manifestElement = GetExactlyOne(doc.Elements(Manifest));
            if (manifestElement == null)
            {
                errorMessage = PreconditionOneManifestElement;
                return false;
            }

            var applicationElement = GetExactlyOne(manifestElement.Elements(Application));
            if (applicationElement == null)
            {
                errorMessage = PreconditionOneApplicationElement;
                return false;
            }

            var elements = FindPluginVersionElements(applicationElement);
            var hasCurrentPluginVersion = elements.Count() == 1 &&
                                          (string) elements.First().Attribute(AndroidValueXName) ==
                                          GooglePlayInstantUtils.PluginVersion;
            errorMessage = hasCurrentPluginVersion ? null : UpdatePluginVersion(applicationElement);
            return hasCurrentPluginVersion;
        }

        /// <summary>
        /// Converts the specified XDocument representing an AndroidManifest to support an installed app build.
        /// </summary>
        public static void ConvertManifestToInstalled(XDocument doc)
        {
            foreach (var manifestElement in doc.Elements(Manifest))
            {
                manifestElement.Attributes(AndroidTargetSandboxVersionXName).Remove();
                FindDistributionModuleInstantElements(manifestElement).Remove();
                // TODO: it may not always be safe to remove the "dist" namespace.
                manifestElement.Attributes(DistributionXmlns).Remove();
                foreach (var applicationElement in manifestElement.Elements(Application))
                {
                    FindPluginVersionElements(applicationElement).Remove();
                    foreach (var mainActivity in FindMainActivities(applicationElement))
                    {
                        // TODO: also remove view intent filters?
                        FindDefaultUrlMetaDataElements(mainActivity).Remove();
                    }
                }
            }
        }

        /// <summary>
        /// Converts the specified XDocument representing an AndroidManifest to support an instant app build.
        /// </summary>
        /// <param name="doc">An XDocument representing an AndroidManifest.</param>
        /// <param name="uri">The Default URL to use, or null for a URL-less instant app.</param>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        public static string ConvertManifestToInstant(XDocument doc, Uri uri)
        {
            var manifestElement = GetExactlyOne(doc.Elements(Manifest));
            if (manifestElement == null)
            {
                return PreconditionOneManifestElement;
            }

            // Verify that "xmlns:android" is already present and correct.
            var androidAttribute = manifestElement.Attribute(AndroidXmlns);
            if (androidAttribute == null)
            {
                return PreconditionMissingXmlnsAndroid;
            }

            if (androidAttribute.Value != AndroidNamespaceUrl)
            {
                return PreconditionInvalidXmlnsAndroid;
            }

            // Don't assume that "xmlns:dist" is already present. If it is present, verify that it's correct.
            var distributionAttribute = manifestElement.Attribute(DistributionXmlns);
            if (distributionAttribute == null)
            {
                manifestElement.SetAttributeValue(DistributionXmlns, DistributionNamespaceUrl);
            }
            else if (distributionAttribute.Value != DistributionNamespaceUrl)
            {
                return PreconditionInvalidXmlnsDistribution;
            }

            // The manifest element <dist:module dist:instant="true" /> is required for AppBundles.
            var moduleInstantResult = UpdateDistributionModuleInstantElement(manifestElement);
            if (moduleInstantResult != null)
            {
                return moduleInstantResult;
            }

            // TSV2 is required for instant apps starting with Android Oreo.
            manifestElement.SetAttributeValue(AndroidTargetSandboxVersionXName, "2");

            var applicationElement = GetExactlyOne(manifestElement.Elements(Application));
            if (applicationElement == null)
            {
                return PreconditionOneApplicationElement;
            }

            var updatePluginVersionResult = UpdatePluginVersion(applicationElement);
            if (updatePluginVersionResult != null)
            {
                return updatePluginVersionResult;
            }

            return uri == null ? null : AddDefaultUrl(applicationElement, uri);
        }

        /// <summary>
        /// Adds the specified default URL to manifest's main activity.
        /// </summary>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        private static string AddDefaultUrl(XContainer applicationElement, Uri uri)
        {
            var mainActivity = GetExactlyOne(FindMainActivities(applicationElement));
            if (mainActivity == null)
            {
                return PreconditionOneMainActivity;
            }

            var updateViewIntentFilterResult = UpdateViewIntentFilter(mainActivity, uri);
            if (updateViewIntentFilterResult != null)
            {
                return updateViewIntentFilterResult;
            }

            return UpdateMetaDataElement(
                FindDefaultUrlMetaDataElements, mainActivity, PreconditionOneMetaDataDefaultUrl, DefaultUrl, uri);
        }

        /// <summary>
        /// Updates the specified main activity to have a view intent filter for the specified default URL.
        /// </summary>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        private static string UpdateViewIntentFilter(XElement mainActivity, Uri uri)
        {
            // Find the Activity's Intent Filter with action type VIEW to update, or create a new Intent Filter.
            var viewIntentFilter = GetElement(FindActionViewIntentFilters, mainActivity, IntentFilter);
            if (viewIntentFilter == null)
            {
                // TODO: add support for activities with multiple VIEW intent filters
                return PreconditionOneViewIntentFilter;
            }

            // See https://developer.android.com/topic/google-play-instant/getting-started/game-instant-app#app-links
            // and https://developer.android.com/training/app-links/verify-site-associations for info on "autoVerify".
            viewIntentFilter.SetAttributeValue(AndroidAutoVerifyXName, ValueTrue);
            viewIntentFilter.Add(CreateElementWithAttribute(Action, AndroidNameXName, Android.IntentActionView));
            viewIntentFilter.Add(
                CreateElementWithAttribute(Category, AndroidNameXName, Android.IntentCategoryBrowsable));
            viewIntentFilter.Add(CreateElementWithAttribute(Category, AndroidNameXName,
                Android.IntentCategoryDefault));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "http"));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "https"));
            viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidHostXName, uri.Host));
            var path = uri.AbsolutePath;
            if (!string.IsNullOrEmpty(path) && path != "/")
            {
                viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidPathXName, path));
            }

            return null;
        }

        /// <summary>
        /// Updates the specified application element to include meta-data with the current version of the plugin.
        /// </summary>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        private static string UpdatePluginVersion(XElement applicationElement)
        {
            return UpdateMetaDataElement(FindPluginVersionElements, applicationElement, PreconditionOnePluginVersion,
                PlayInstantUnityPluginVersion, GooglePlayInstantUtils.PluginVersion);
        }

        /// <summary>
        /// Updates the specified meta-data element to the specified value.
        /// </summary>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        private static string UpdateMetaDataElement(
            ElementFinder finder, XElement parentElement, string errorMessage, string name, object value)
        {
            var metaDataElement = GetElement(finder, parentElement, MetaData);
            if (metaDataElement == null)
            {
                return errorMessage;
            }

            metaDataElement.SetAttributeValue(AndroidNameXName, name);
            metaDataElement.SetAttributeValue(AndroidValueXName, value);

            return null;
        }

        /// <summary>
        /// Updates the specified manifest to indicate that it is an instant module, necessary for building app bundles.
        /// </summary>
        /// <returns>An error message if there was a problem updating the manifest, or null if successful.</returns>
        private static string UpdateDistributionModuleInstantElement(XElement manifestElement)
        {
            var moduleInstant =
                GetElement(FindDistributionModuleInstantElements, manifestElement, DistributionModuleXName);
            if (moduleInstant == null)
            {
                return PreconditionOneModuleInstant;
            }

            moduleInstant.SetAttributeValue(DistributionInstantXName, ValueTrue);

            return null;
        }

        private static IEnumerable<XElement> FindMainActivities(XContainer applicationElement)
        {
            // Find all activities with an <intent-filter> that contains
            //  <action android:name="android.intent.action.MAIN" />
            //  <category android:name="android.intent.category.LAUNCHER" />
            return
                from activityElement in applicationElement.Elements(Activity)
                where
                    (from intentFilter in activityElement.Elements(IntentFilter)
                        where
                            intentFilter.Elements(Action)
                                .Any(e => (string) e.Attribute(AndroidNameXName) == Android.IntentActionMain) &&
                            intentFilter.Elements(Category)
                                .Any(e => (string) e.Attribute(AndroidNameXName) == Android.IntentCategoryLauncher)
                        select intentFilter)
                    .Any()
                select activityElement;
        }

        private static IEnumerable<XElement> FindActionViewIntentFilters(XContainer mainActivity)
        {
            // Find all intent filters that contain <action android:name="android.intent.action.VIEW" />
            return from intentFilter in mainActivity.Elements(IntentFilter)
                where intentFilter.Elements(Action)
                    .Any(e => (string) e.Attribute(AndroidNameXName) == Android.IntentActionView)
                select intentFilter;
        }

        private static IEnumerable<XElement> FindDefaultUrlMetaDataElements(XContainer mainActivity)
        {
            // Find all elements of the form <meta-data android:name="default-url" />
            return from metaData in mainActivity.Elements(MetaData)
                where (string) metaData.Attribute(AndroidNameXName) == DefaultUrl
                select metaData;
        }

        private static IEnumerable<XElement> FindDistributionModuleInstantElements(XContainer manifestElement)
        {
            // Find all elements of the form <dist:module dist:instant="..." />
            return from moduleElement in manifestElement.Elements(DistributionModuleXName)
                where moduleElement.Attribute(DistributionInstantXName) != null
                select moduleElement;
        }

        private static IEnumerable<XElement> FindPluginVersionElements(XContainer applicationElement)
        {
            // Find all elements of the form
            //   <meta-data android:name="play-instant-unity-plugin.version" android:value="1.0"/>
            return from metaData in applicationElement.Elements(MetaData)
                where (string) metaData.Attribute(AndroidNameXName) == PlayInstantUnityPluginVersion
                select metaData;
        }

        private static XElement CreateElementWithAttribute(
            XName elementName, XName attributeName, string attributeValue)
        {
            var element = new XElement(elementName);
            element.SetAttributeValue(attributeName, attributeValue);
            return element;
        }

        private static XElement GetExactlyOne(IEnumerable<XElement> elements)
        {
            // If the IEnumerable has exactly 1 element, return it. If the IEnumerable has 0 or 2+ elements, return
            // null. Cannot use FirstOrDefault() here since that will return the first element if 2+ elements.
            return elements.Count() == 1 ? elements.First() : null;
        }

        /// <summary>
        /// Uses the specified delegate to find all matching elements attached to the specified parent element. If
        /// no matches are found, create a new element and return it. If one match is found, remove any existing
        /// elements/attributes and return it. If more than one match is found, return null to indicate an error.
        /// </summary>
        private static XElement GetElement(ElementFinder finder, XElement parentElement, XName elementName)
        {
            var elements = finder(parentElement);
            switch (elements.Count())
            {
                case 0:
                    var createdElement = new XElement(elementName);
                    parentElement.Add(createdElement);
                    return createdElement;
                case 1:
                    var existingElement = elements.First();
                    existingElement.RemoveAll();
                    return existingElement;
                default:
                    // This is unexpected. The caller is responsible for logging an error.
                    return null;
            }
        }
    }
}