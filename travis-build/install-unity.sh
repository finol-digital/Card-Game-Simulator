#! /bin/sh

# Refer to https://unity3d.com/get-unity/download/archive for unityhub://$VERSION/$HASH link
BASE_URL=https://download.unity3d.com/download_unity
VERSION=2018.4.0f1
HASH=b6ffa8986c8d

getFileName() {
  echo "${UNITY_DOWNLOAD_CACHE}/`basename "$1"`"
}

download() {
  file=$1
  url="$BASE_URL/$HASH/$file"
  filePath=$(getFileName $file)
  fileName=`basename "$file"`

  if [ ! -e $filePath ] ; then
    echo "Downloading $filePath from $url: "
    curl --retry 5 -o "$filePath" "$url"
  else
    echo "$fileName exists in cache. Skipping download."
  fi
}

install() {
  package=$1
  filePath=$(getFileName $package)
  
  download "$package"

  echo "Installing $filePath"
  sudo installer -dumplog -package "$filePath" -target /
}

# See $BASE_URL/$HASH/unity-$VERSION-osx.ini for a complete list of packages
install "MacEditorInstaller/Unity-$VERSION.pkg"
install "MacEditorTargetInstaller/UnitySetup-Mac-IL2CPP-Support-for-Editor-$VERSION.pkg"
install "MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-$VERSION.pkg"