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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GooglePlayInstant.Editor.GooglePlayServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GooglePlayInstant.Editor
{
    /// <summary>
    /// Provides methods that call the Android SDK build tool "apksigner" to verify whether an APK complies with
    /// <a href="https://source.android.com/security/apksigning/v2">APK Signature Scheme V2</a> and to re-sign
    /// the APK if not. Instant apps require Signature Scheme V2 starting with Android O, however some Unity versions
    /// do not produce compliant APKs. Without this "adb install --ephemeral" on an Android O device will fail with
    /// "INSTALL_PARSE_FAILED_NO_CERTIFICATES: No APK Signature Scheme v2 signature in ephemeral package".
    /// </summary>
    public static class ApkSigner
    {
        private static readonly string AndroidDebugKeystore = Path.Combine(".android", "debug.keystore");

        /// <summary>
        /// Returns true if apksigner is available to call, false otherwise.
        /// </summary>
        public static bool IsAvailable()
        {
            return GetApkSignerJarPath() != null;
        }

        /// <summary>
        /// Synchronously calls the apksigner tool to verify whether the specified APK uses APK Signature Scheme V2.
        /// </summary>
        /// <returns>true if the specified APK uses APK Signature Scheme V2, false otherwise</returns>
        public static bool Verify(string apkPath)
        {
            var arguments = string.Format(
                "-jar {0} verify {1}",
                CommandLine.QuotePath(GetApkSignerJarPath()),
                CommandLine.QuotePath(apkPath));

            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments);
            if (result.exitCode == 0)
            {
                Debug.Log("Verified APK Signature Scheme V2.");
                return true;
            }

            // Logging at info level since the most common failure (V2 signature missing) is normal.
            Debug.LogFormat("APK Signature Scheme V2 verification failed: {0}", result.message);
            return false;
        }

        /// <summary>
        /// Synchronously calls the apksigner tool to sign the specified APK using APK Signature Scheme V2.
        /// </summary>
        /// <returns>An error message if there was a problem running apksigner, or null if successful.</returns>
        public static string SignApk(string apkFilePath)
        {
            return SignFile(apkFilePath, string.Empty);
        }

        /// <summary>
        /// Synchronously calls the apksigner tool to sign the specified ZIP file using APK Signature Scheme V1,
        /// simulating jarsigner behavior. This can be used to sign an Android App Bundles.
        /// </summary>
        /// <returns>An error message if there was a problem running apksigner, or null if successful.</returns>
        public static string SignZip(string zipFilePath)
        {
            return SignFile(zipFilePath, "--min-sdk-version 1 --v1-signing-enabled true --v2-signing-enabled false ");
        }

        private static string SignFile(string filePath, string additionalArguments)
        {
            string keystoreName;
            string keystorePass;
            string keyaliasName;
            string keyaliasPass;

            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) ||
                string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
            {
                Debug.Log("No keystore and/or no keyalias specified. Signing using Android debug keystore.");
                var homePath =
                    Application.platform == RuntimePlatform.WindowsEditor
                        ? Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")
                        : Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(homePath))
                {
                    return "Failed to locate directory that contains Android debug keystore.";
                }

                keystoreName = Path.Combine(homePath, AndroidDebugKeystore);
                keystorePass = "android";
                keyaliasName = "androiddebugkey";
                keyaliasPass = "android";
            }
            else
            {
                keystoreName = PlayerSettings.Android.keystoreName;
                keystorePass = PlayerSettings.Android.keystorePass;
                keyaliasName = PlayerSettings.Android.keyaliasName;
                keyaliasPass = PlayerSettings.Android.keyaliasPass;
            }

            if (!File.Exists(keystoreName))
            {
                return string.Format("Failed to locate keystore file: {0}", keystoreName);
            }

            // Sign the file {4} using key {2} contained in keystore file {1} using additional arguments {3}.
            // ApkSignerResponder will provide passwords using stdin; this is the default for apksigner
            // so there is no need to specify "--ks-pass" or "--key-pass" arguments.
            // ApkSignerResponder will encode the passwords with UTF8, so we specify "--pass-encoding utf-8" here.
            var arguments = string.Format(
                "-jar {0} sign --ks {1} --ks-key-alias {2} --pass-encoding utf-8 {3}{4}",
                CommandLine.QuotePath(GetApkSignerJarPath()),
                CommandLine.QuotePath(keystoreName),
                keyaliasName,
                additionalArguments,
                CommandLine.QuotePath(filePath));

            var promptToPasswordDictionary = new Dictionary<string, string>
            {
                // Example keystore password prompt: "Keystore password for signer #1: "
                {"Keystore password for signer", keystorePass},
                // Example keyalias password prompt: "Key \"androiddebugkey\" password for signer #1: "
                {"Key .+ password for signer", keyaliasPass}
            };
            var responder = new ApkSignerResponder(promptToPasswordDictionary);
            var result = CommandLine.Run(JavaUtilities.JavaBinaryPath, arguments, ioHandler: responder.AggregateLine);
            return result.exitCode == 0 ? null : result.message;
        }

        private static string GetApkSignerJarPath()
        {
            var newestBuildToolsVersion = AndroidBuildTools.GetNewestBuildToolsVersion();
            if (newestBuildToolsVersion == null)
            {
                return null;
            }

            var newestBuildToolsPath = Path.Combine(AndroidBuildTools.GetBuildToolsPath(), newestBuildToolsVersion);
            var apkSignerJarPath = Path.Combine(newestBuildToolsPath, Path.Combine("lib", "apksigner.jar"));
            if (File.Exists(apkSignerJarPath))
            {
                return apkSignerJarPath;
            }

            Debug.LogErrorFormat("Failed to locate apksigner.jar at path: {0}", apkSignerJarPath);
            return null;
        }

        /// <summary>
        /// Checks apksigner's stdout for password prompts and provides the associated password to apksigner's stdin.
        /// This is more secure than providing passwords on the command line (where passwords are visible to process
        /// listing tools like "ps") or using file-based password input (where passwords are written to disk).
        /// </summary>
        private class ApkSignerResponder : CommandLine.LineReader
        {
            private readonly Dictionary<Regex, string> _promptToPasswordDictionary;

            public ApkSignerResponder(Dictionary<string, string> promptToPasswordDictionary)
            {
                _promptToPasswordDictionary =
                    promptToPasswordDictionary.ToDictionary(
                        kvp => new Regex(kvp.Key, RegexOptions.Compiled), kvp => kvp.Value);
                LineHandler += CheckAndRespond;
            }

            private void CheckAndRespond(Process process, StreamWriter stdin, CommandLine.StreamData data)
            {
                if (process.HasExited)
                {
                    return;
                }

                // The password prompt text won't have a trailing newline, so read ahead on stdout to locate it.
                var stdoutData = GetBufferedData(CommandLine.StandardOutputStreamDataHandle);
                var stdoutText = Aggregate(stdoutData).text;
                var password = _promptToPasswordDictionary
                    .Where(kvp => kvp.Key.IsMatch(stdoutText))
                    .Select(kvp => kvp.Value)
                    .FirstOrDefault();
                if (password == null)
                {
                    return;
                }

                Flush();
                // UTF8 to match "--pass-encoding utf-8" argument passed to apksigner.
                var passwordBytes = Encoding.UTF8.GetBytes(password + Environment.NewLine);
                stdin.BaseStream.Write(passwordBytes, 0, passwordBytes.Length);
                stdin.BaseStream.Flush();
            }
        }
    }
}