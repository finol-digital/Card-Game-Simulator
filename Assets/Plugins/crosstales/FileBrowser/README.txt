# FileBrowser PRO 2020.2.8

Thank you for buying our asset "File Browser PRO"! 
If you have any questions about this asset, send us an email at [fb@crosstales.com](mailto:fb@crosstales.com). 



## Description
A wrapper for native file dialogs on Windows, macOS, Linux and UWP.

* Works with Windows, macOS, Linux and UWP in editor and runtime
* Full IL2CPP-support
* Open file/folder, save file dialogs supported
* Multiple file selection
* Multiple folder selection on macOS and Linux
* File extension filters
* Other platforms are currently not supported

Please install "Assets/Plugins/crosstales/Common/UI.unitypackage" first and afterwards "Demos.unitypackage" to use the demo scenes!



## Upgrade to new version
Follow this steps to upgrade the version of "File Browser PRO":

1. Update "File Browser PRO" to the latest version from the "Unity AssetStore"
2. Delete the "Assets/Plugins/crosstales/FileBrowser" folder from the Project-view
3. Import the latest version from the "Unity AssetStore"



## Notes:

### macOS
* Sync calls can throw exceptions in development builds after the panel loses and gains focus. Use async calls to avoid this.
* Notarization and Mac App Store; to get the app through the Apples signing process, do one of the following things:

1) Add the following key to the entitlement-file:
<key>com.apple.security.cs.disable-library-validation</key><true/>

2) Sign the library after building:
codesign --deep --force --verify --verbose --timestamp --sign "Developer ID Application : YourCompanyName (0123456789)" "YourApp.app/Contents/Plugins/FileBrowser.bundle"


### Linux
The library is tested under Ubuntu 18.04 with GTK3+.
Since there are so many different Linux distributions and configurations, we simply can't test and support them all.
Therefore, we included the whole source code; please follow the README in the "Linux-Source.zip".



## Release notes

See "VERSIONS.txt" for details.



## Credits

The icons are based on [Font Awesome](http://fontawesome.io/).

Code partially based on:
https://github.com/gkngkc/UnityStandaloneFileBrowser

Improvements for the Linux version:
Yinon Oshrat



## Contact

crosstales LLC
Schanzeneggstrasse 1
CH-8002 Zürich

* [Homepage](https://www.crosstales.com/)
* [Email](mailto:fb@crosstales.com)

### Social media
* [Discord](https://discord.gg/ZbZ2sh4)
* [Facebook](https://www.facebook.com/crosstales/)
* [Twitter](https://twitter.com/crosstales)
* [LinkedIN](https://www.linkedin.com/company/crosstales)



## More information
* [Homepage](https://www.crosstales.com/)
* [AssetStore](https://assetstore.unity.com/lists/crosstales-42213?aid=1011lNGT)
* [Forum](https://forum.unity.com/threads/file-browser-native-file-browser-for-windows-and-macos.510403/)
* [Documentation](https://www.crosstales.com/media/data/assets/FileBrowser/FileBrowser-doc.pdf)
* [API](https://www.crosstales.com/media/data/assets/FileBrowser/api/)

### Videos
[Youtube-channel](https://www.youtube.com/c/Crosstales)

### Demos
* [Windows-Demo](https://drive.google.com/file/d/1sE-6uhp2nk_5B85jvoiMWdk__HqUPSek/view?usp=sharing)
* [macOS-Demo](https://drive.google.com/file/d/1sAB953F-fpRmTSks9f2ZM0sMV7CEyyUA/view?usp=sharing)
* [Linux-Demo](https://drive.google.com/file/d/1LAm9v8Mu9jvF_8ZU0X3UU8nLKCdobzrj/view?usp=sharing)


`Version: 17.06.2020`