/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace Tests.EditMode
{
    public class PdfDependencyTests
    {
        [TestCase(BuildTarget.Android)]
        [TestCase(BuildTarget.iOS)]
        [TestCase(BuildTarget.StandaloneLinux64)]
        [TestCase(BuildTarget.StandaloneOSX)]
        [TestCase(BuildTarget.StandaloneWindows64)]
        [TestCase(BuildTarget.WSAPlayer)]
        public void ImageSharpDependencies_AreAvailableToPlayerLinker(BuildTarget target)
        {
            var plugins = PluginImporter.GetImporters(target);
            var imageSharp = Assembly.Load("SixLabors.ImageSharp");
            foreach (var dependency in imageSharp.GetReferencedAssemblies())
            {
                if (dependency.Name == "netstandard")
                    continue;

                var plugin = plugins.FirstOrDefault(importer =>
                    Path.GetFileNameWithoutExtension(importer.assetPath) == dependency.Name);
                Assert.That(plugin, Is.Not.Null,
                    $"{target}: PDF export requires the {dependency.Name} plugin.");
                var bundled = AssemblyName.GetAssemblyName(plugin.assetPath);
                Assert.That(bundled.Version, Is.GreaterThanOrEqualTo(dependency.Version),
                    $"{target}: {dependency.Name} must satisfy ImageSharp's assembly reference.");
            }
        }
    }
}
