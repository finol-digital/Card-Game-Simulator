/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Cgs.Editor
{
    public class ModifyUnityAndroidAppManifest : IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string basePath)
        {
            Debug.Log("OnPostGenerateGradleAndroidProject");
            var androidManifest = new AndroidManifest(GetManifestPath(basePath));
            androidManifest.SetDeepLinkHook();
            var androidManifestPath = androidManifest.Save();
            Debug.Log($"Updated Android Manifest {androidManifestPath}");
        }

        public int callbackOrder => 1;

        private string _manifestFilePath;

        private string GetManifestPath(string basePath)
        {
            if (!string.IsNullOrEmpty(_manifestFilePath))
                return _manifestFilePath;

            var pathBuilder = new StringBuilder(basePath);
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
            pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
            _manifestFilePath = pathBuilder.ToString();

            return _manifestFilePath;
        }
    }


    internal class AndroidXmlDocument : XmlDocument
    {
        protected const string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        protected XmlNamespaceManager NsMgr;
        private readonly string _path;

        protected AndroidXmlDocument(string path)
        {
            _path = path;
            using (var reader = new XmlTextReader(_path))
            {
                reader.Read();
                // ReSharper disable once VirtualMemberCallInConstructor
                Load(reader);
            }

            NsMgr = new XmlNamespaceManager(NameTable);
            NsMgr.AddNamespace("android", AndroidXmlNamespace);
        }

        public string Save()
        {
            return SaveAs(_path);
        }

        private string SaveAs(string path)
        {
            using var writer = new XmlTextWriter(path, new UTF8Encoding(false));
            writer.Formatting = Formatting.Indented;
            Save(writer);

            return path;
        }
    }

    internal class AndroidManifest : AndroidXmlDocument
    {
        public AndroidManifest(string path) : base(path)
        {
        }

        private XmlNode GetActivityWithLaunchIntent()
        {
            return SelectSingleNode(
                "/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", NsMgr);
        }

        private XmlAttribute CreateAndroidAttribute(string key, string value)
        {
            var xmlAttribute = CreateAttribute("android", key, AndroidXmlNamespace);
            xmlAttribute.Value = value;
            return xmlAttribute;
        }

        internal void SetDeepLinkHook()
        {
            var activityWithLaunchIntent = GetActivityWithLaunchIntent();
            XmlElement intentFilter = CreateElement("intent-filter");
            activityWithLaunchIntent?.AppendChild(intentFilter);
            XmlElement action = CreateElement("action");
            intentFilter.AppendChild(action);
            XmlAttribute actionAttribute = CreateAndroidAttribute("name", "android.intent.action.VIEW");
            action.Attributes.Append(actionAttribute);
            XmlElement category = CreateElement("category");
            intentFilter.AppendChild(category);
            XmlAttribute categoryAttribute = CreateAndroidAttribute("name", "android.intent.category.DEFAULT");
            category.Attributes.Append(categoryAttribute);
            XmlElement category2 = CreateElement("category");
            intentFilter.AppendChild(category2);
            XmlAttribute category2Attribute = CreateAndroidAttribute("name", "android.intent.category.BROWSABLE");
            category2.Attributes.Append(category2Attribute);
            XmlElement data = CreateElement("data");
            intentFilter.AppendChild(data);
            XmlAttribute dataAttribute = CreateAndroidAttribute("scheme", "cardgamesim");
            data.Attributes.Append(dataAttribute);
            XmlAttribute dataAttribute2 = CreateAndroidAttribute("host", "link");
            data.Attributes.Append(dataAttribute2);
        }
    }
}
