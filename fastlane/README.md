fastlane documentation
----

# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```sh
xcode-select --install
```

For _fastlane_ installation instructions, see [Installing _fastlane_](https://docs.fastlane.tools/#installing-fastlane)

# Available Actions

## Android

### android playprod

```sh
[bundle exec] fastlane android playprod
```

Upload a new Android version to the Google Play Store

----


## iOS

### ios release

```sh
[bundle exec] fastlane ios release
```

Deliver a new Release build to the App Store

### ios beta

```sh
[bundle exec] fastlane ios beta
```

Deliver a new Beta build to Apple TestFlight

### ios build

```sh
[bundle exec] fastlane ios build
```

Create .ipa

----


## Mac

### mac fixversion

```sh
[bundle exec] fastlane mac fixversion
```

Hack so that Apple doesn't reject the mac build due to a mistake in versioning

### mac macupload

```sh
[bundle exec] fastlane mac macupload
```

Upload a new Mac version to the Mac App Store

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
