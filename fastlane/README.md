fastlane documentation
================
# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```
xcode-select --install
```

Install _fastlane_ using
```
[sudo] gem install fastlane -NV
```
or alternatively using `brew install fastlane`

# Available Actions
### fixversion
```
fastlane fixversion
```
Hack so that Apple doesn't reject the mac build due to a mistake in versioning

----

## Android
### android playprod
```
fastlane android playprod
```
Upload a new Android version to the Google Play Store

----

## iOS
### ios release
```
fastlane ios release
```
Push a new release build to the App Store
### ios beta
```
fastlane ios beta
```
Submit a new Beta Build to Apple TestFlight
### ios build
```
fastlane ios build
```
Create .ipa

----

## Mac
### mac macupload
```
fastlane mac macupload
```
Upload a new Mac version to the Mac App Store

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.
More information about fastlane can be found on [fastlane.tools](https://fastlane.tools).
The documentation of fastlane can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
