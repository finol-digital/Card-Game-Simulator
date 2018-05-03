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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace PlayInstant.Editor
{
    public static class AndroidManifestHelper
    {
        private const string AndroidManifestAssetsDirectory = "Assets/Plugins/Android/";
        private const string AndroidManifestAssetsPath = AndroidManifestAssetsDirectory + "AndroidManifest.xml";

        private const string AndroidNamespaceAlias = "android";
        private const string AndroidNamespaceUrl = "http://schemas.android.com/apk/res/android";
        private static readonly XNamespace AndroidXNamespace = XNamespace.Get(AndroidNamespaceUrl);

        private static readonly XName AndroidAutoVerifyXName = XName.Get("autoVerify", AndroidNamespaceUrl);
        private static readonly XName AndroidHostXName = XName.Get("host", AndroidNamespaceUrl);
        private static readonly XName AndroidNameXName = XName.Get("name", AndroidNamespaceUrl);
        private static readonly XName AndroidPathXName = XName.Get("path", AndroidNamespaceUrl);
        private static readonly XName AndroidSchemeXName = XName.Get("scheme", AndroidNamespaceUrl);
        private static readonly XName AndroidValueXName = XName.Get("value", AndroidNamespaceUrl);

        private static readonly XName AndroidTargetSandboxVersionXName =
            XName.Get("targetSandboxVersion", AndroidNamespaceUrl);

        private const string Action = "action";
        private const string ActionMain = "android.intent.action.MAIN";
        private const string ActionView = "android.intent.action.VIEW";
        private const string Activity = "activity";
        private const string Application = "application";
        private const string Category = "category";
        private const string CategoryLauncher = "android.intent.category.LAUNCHER";
        private const string Data = "data";
        private const string DefaultUrl = "default-url";
        private const string IntentFilter = "intent-filter";
        private const string Manifest = "manifest";
        private const string MetaData = "meta-data";

        public static bool HasExistingAndroidManifest()
        {
            return File.Exists(AndroidManifestAssetsPath);
        }

        public static string GetExistingUrl()
        {
            try
            {
                var doc = XDocument.Load(AndroidManifestAssetsPath);
                var defaultUrls =
                    from metaData in doc.Descendants(MetaData)
                    where (string) metaData.Attribute(AndroidNameXName) == DefaultUrl
                    select metaData.Attribute(AndroidValueXName);
                return defaultUrls.First().Value.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Failed to find existing default url {0}", ex);
            }

            return string.Empty;
        }

        public static bool SwitchToInstant(Uri uri)
        {
            XDocument doc;
            if (File.Exists(AndroidManifestAssetsPath))
            {
                Debug.LogFormat("Loading existing file {0}", AndroidManifestAssetsPath);
                doc = XDocument.Load(AndroidManifestAssetsPath);
            }
            else
            {
                Debug.LogFormat("Creating new file {0}", AndroidManifestAssetsPath);
                doc = new XDocument(
                    new XElement(Manifest,
                        new XAttribute(XNamespace.Xmlns + AndroidNamespaceAlias, AndroidXNamespace),
                        new XElement(Application,
                            new XElement(Activity,
                                new XAttribute(AndroidNameXName, "com.unity3d.player.UnityPlayerActivity"),
                                new XElement(IntentFilter,
                                    new XElement(Action, new XAttribute(AndroidNameXName, ActionMain)),
                                    new XElement(Category, new XAttribute(AndroidNameXName, CategoryLauncher)))))));
            }

            var manifestElement = GetExactlyOne(doc.Elements(Manifest));
            if (manifestElement == null)
            {
                LogError("expect 1 manifest element");
                return false;
            }

            var applicationElement = GetExactlyOne(manifestElement.Elements(Application));
            if (applicationElement == null)
            {
                LogError("expect 1 application element");
                return false;
            }

            var mainActivity = GetExactlyOne(GetMainActivities(applicationElement));
            if (mainActivity == null)
            {
                LogError("expect 1 activity element with action MAIN and category LAUNCHER");
                return false;
            }

            var androidAttribute = manifestElement.Attribute(XNamespace.Xmlns + AndroidNamespaceAlias);
            if (androidAttribute == null)
            {
                LogError("missing manifest attribute xmlns:android");
                return false;
            }

            if (androidAttribute.Value != AndroidNamespaceUrl)
            {
                LogError("invalid value for xmlns:android");
                return false;
            }

            // Updates: TSV2, VIEW intent-filter, and defulat-url
            manifestElement.SetAttributeValue(AndroidTargetSandboxVersionXName, "2");

            {
                var actionViewIntentFilters = GetActionViewIntentFilters(mainActivity);
                XElement viewIntentFilter;
                switch (actionViewIntentFilters.Count())
                {
                    case 0:
                        viewIntentFilter = new XElement(IntentFilter);
                        mainActivity.Add(viewIntentFilter);
                        break;
                    case 1:
                        viewIntentFilter = actionViewIntentFilters.First();
                        // TODO: preserve existing elements and just update
                        viewIntentFilter.RemoveAll();
                        break;
                    default:
                        LogError("more than one VIEW intent-filter");
                        return false;
                }

                viewIntentFilter.SetAttributeValue(AndroidAutoVerifyXName, "true");
                viewIntentFilter.Add(CreateElementWithAttribute(Action, AndroidNameXName, ActionView));
                viewIntentFilter.Add(
                    CreateElementWithAttribute(Category, AndroidNameXName, "android.intent.category.BROWSABLE"));
                viewIntentFilter.Add(CreateElementWithAttribute(Category, AndroidNameXName,
                    "android.intent.category.DEFAULT"));
                viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "http"));
                viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidSchemeXName, "https"));
                viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidHostXName, uri.Host));
                var path = uri.AbsolutePath;
                if (!string.IsNullOrEmpty(path) && path != "/")
                {
                    viewIntentFilter.Add(CreateElementWithAttribute(Data, AndroidPathXName, path));
                }
            }

            {
                var metaDataElements = GetDefaultUrlMetaDataElements(mainActivity);
                XElement defaultUrlMetaData;
                switch (metaDataElements.Count())
                {
                    case 0:
                        defaultUrlMetaData = new XElement(MetaData);
                        mainActivity.Add(defaultUrlMetaData);
                        break;
                    case 1:
                        defaultUrlMetaData = metaDataElements.First();
                        defaultUrlMetaData.RemoveAttributes();
                        break;
                    default:
                        LogError("more than one meta-data element for default-url");
                        return false;
                }

                defaultUrlMetaData.SetAttributeValue(AndroidNameXName, DefaultUrl);
                defaultUrlMetaData.SetAttributeValue(AndroidValueXName, uri);
            }

            // TODO: check for android:extractNativeLibs="false" on application element

            if (!Directory.Exists(AndroidManifestAssetsDirectory))
            {
                Directory.CreateDirectory(AndroidManifestAssetsDirectory);
            }

            // TODO: create backup of existing file first?
            // NOTE: Save includes a Byte Order Mark (BOM)
            doc.Save(AndroidManifestAssetsPath);

            Debug.LogFormat("Successfully updated {0}", AndroidManifestAssetsPath);
            return true;
        }

        public static void SwitchToInstalled()
        {
            if (!File.Exists(AndroidManifestAssetsPath))
            {
                Debug.LogFormat("Nothing to do for {0} since file does not exist", AndroidManifestAssetsPath);
                return;
            }

            Debug.LogFormat("Loading existing file {0}", AndroidManifestAssetsPath);
            var doc = XDocument.Load(AndroidManifestAssetsPath);
            foreach (var manifestElement in doc.Elements(Manifest))
            {
                manifestElement.Attributes(AndroidTargetSandboxVersionXName).Remove();
                foreach (var applicationElement in manifestElement.Elements(Application))
                {
                    foreach (var mainActivity in GetMainActivities(applicationElement))
                    {
                        GetDefaultUrlMetaDataElements(mainActivity).Remove();
                        GetActionViewIntentFilters(mainActivity).Remove();
                    }
                }
            }

            // TODO: create backup of existing file first?
            doc.Save(AndroidManifestAssetsPath);
        }

        private static IEnumerable<XElement> GetMainActivities(XContainer applicationElement)
        {
            // See https://developer.android.com/topic/instant-apps/getting-started/prepare.html#default-url
            // Find all activities with an <intent-filter> that has
            //  <action android:name="android.intent.action.MAIN" />
            //  <category android:name="android.intent.category.LAUNCHER" />
            return
                from activityElement in applicationElement.Elements(Activity)
                where
                    (from intentFilter in activityElement.Elements(IntentFilter)
                        where
                            intentFilter.Elements(Action)
                                .Any(e => (string) e.Attribute(AndroidNameXName) == ActionMain) &&
                            intentFilter.Elements(Category).Any(e =>
                                (string) e.Attribute(AndroidNameXName) == CategoryLauncher)
                        select intentFilter)
                    .Any()
                select activityElement;
        }

        private static IEnumerable<XElement> GetDefaultUrlMetaDataElements(XContainer mainActivity)
        {
            return from metaData in mainActivity.Elements(MetaData)
                where (string) metaData.Attribute(AndroidNameXName) == DefaultUrl
                select metaData;
        }

        private static IEnumerable<XElement> GetActionViewIntentFilters(XContainer mainActivity)
        {
            return from intentFilter in mainActivity.Elements(IntentFilter)
                where
                    intentFilter.Elements(Action).Any(e => (string) e.Attribute(AndroidNameXName) == ActionView)
                select intentFilter;
        }

        private static XElement CreateElementWithAttribute(string elementName, XName attributeName,
            string attributeValue)
        {
            var element = new XElement(elementName);
            element.SetAttributeValue(attributeName, attributeValue);
            return element;
        }

        private static void LogError(string message)
        {
            Debug.LogErrorFormat("AndroidManifest.xml error: {0}", message);
            EditorUtility.DisplayDialog("Error updating", message, "OK");
        }

        private static XElement GetExactlyOne(IEnumerable<XElement> elements)
        {
            return elements.Count() == 1 ? elements.Last() : null;
        }
    }
}