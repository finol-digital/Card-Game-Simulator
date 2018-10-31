#! /bin/sh

# Download Unity3D installer into the container
#  Refer to https://unity3d.com/get-unity/download/archive and find the link pointed to by Mac "Unity Editor"
echo 'Downloading Unity 2018.1.2 pkg:'
curl --retry 5 -o Unity.pkg https://download.unity3d.com/download_unity/a46d718d282d/MacEditorInstaller/Unity-2018.1.2f1.pkg
if [ $? -ne 0 ]; then { echo "Download failed"; exit $?; } fi

# Run installer(s)
echo 'Installing Unity.pkg'
sudo installer -dumplog -package Unity.pkg -target /
