# Google Play Instant Plugin for Unity

## Overview

The Google Play Instant Plugin for Unity simplifies conversion of a Unity-based Android app into an instant app that can be deployed through Google Play Instant.

The plugin’s features include:
 * Settings to switch between Installed and Instant build modes.
 * Recommendations of Unity Build Settings and Android Player Settings to change.
 * An action to build and run the instant app on an adb connected Android device.

## Importing the Plugin
Import the .unitypackage by clicking the Unity IDE menu option _Assets > Import package > Custom Package_ and importing all items.

## Using the Plugin
After import there will be a “PlayInstant” menu in Unity providing several options described below.

### Documentation
Includes links to developer documentation.

### Configure Instant or Installed...
Opens a window that enables switching between "Installed" and "Instant" development modes. Switching to "Instant" performs the following changes:
 * Creates a Scripting Define Symbol called PLAY_INSTANT that can be used with #if / #endif.
 * Provides a text box for entering a URL that can be used to launch the instant app once it's published in Google Play. (Note that this URL does not need to point to a real website during development.)
 * Manages updates to the AndroidManifest.xml for certain required changes such as [android:targetSandboxVersion](https://developer.android.com/guide/topics/manifest/manifest-element#targetSandboxVersion).

### Check Player Settings...
Opens a window that indicates Unity Build Settings and Android Player Settings that should be changed in order for the app to be Play Instant compatible. Click on an “Update” button to change a setting.

### Set up Play Instant SDK...
Installs or updates the “Instant Apps Development SDK” using [sdkmanager](https://developer.android.com/studio/command-line/sdkmanager). If there is a license that needs to be accepted, the plugin will prompt for acceptance.

### Build and Run

Invokes Unity's BuildPlayer method to create an APK containing all scenes that are currently selected in “Build Settings” and runs the APK as an instant app on the adb connected Android device.

If the device is an Android version before Oreo, this step will also provision the device for Instant App development by installing "Google Play Services for Instant Apps" and "Instant Apps Development Manger if necessary.
