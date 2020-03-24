#! /bin/sh

UNITY_PROJECT_NAME="Card Game Simulator"

if [ -z ${CI} ]; then
  UNITY_PATH="/home/david/Unity/Hub/Editor/2018.4.19f1/Editor/Unity"
  UNITY_BUILD_DIR=$(pwd)/builds
else
  UNITY_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
  UNITY_BUILD_DIR=${UNITY_BUILD_CACHE}
fi
UNITY_ACTIVATION_LOG_FILE=$UNITY_BUILD_DIR/unity.activation.log
UNITY_RETURN_LOG_FILE=$UNITY_BUILD_DIR/unity.returnlicense.log
IOS_LOG_FILE=$UNITY_BUILD_DIR/iOS.log
OSX_LOG_FILE=$UNITY_BUILD_DIR/OSX.log
LINUX_LOG_FILE=$UNITY_BUILD_DIR/linux.log

echo "Activating Unity license"
#$UNITY_PATH \
#  -quit \
#  -batchmode \
#  -logFile $UNITY_ACTIVATION_LOG_FILE \
#  -silent-crashes \
#  -serial ${UNITY_SERIAL} \
#  -username ${UNITY_USER} \
#  -password ${UNITY_PWD} \
#  -noUpm
$UNITY_PATH \
      -batchmode \
      -nographics \
      -logFile $UNITY_ACTIVATION_LOG_FILE \
      -quit \
      -serial "$UNITY_SERIAL" \
      -username "$UNITY_USER" \
      -password "$UNITY_PWD"
echo "Unity activation log:"
cat $UNITY_ACTIVATION_LOG_FILE

#echo "Attempting to build $UNITY_PROJECT_NAME for iOS"
#$UNITY_PATH \
#  -quit \
#  -batchmode \
#  -logFile $IOS_LOG_FILE \
#  -silent-crashes \
#  -projectPath $(pwd) \
#  -buildTarget iOS \
#  -executeMethod BuildCGS.iOS "$UNITY_BUILD_DIR/iOS"
#rc0=$?
#echo 'iOS build logs:'
#cat $IOS_LOG_FILE

#echo "Attempting to build $UNITY_PROJECT_NAME for OSX"
#$UNITY_PATH \
#  -quit \
#  -batchmode \
#  -logFile $OSX_LOG_FILE \
#  -projectPath $(pwd) \
#  -nographics \
#  -silent-crashes \
#  -buildOSXUniversalPlayer "$UNITY_BUILD_DIR/OSX/$UNITY_PROJECT_NAME.app"
echo "Attempting to build $UNITY_PROJECT_NAME for Linux"
$UNITY_PATH \
    -batchmode \
    -logfile $LINUX_LOG_FILE \
    -quit \
    -customBuildName "$UNITY_PROJECT_NAME" \
    -projectPath $(pwd) \
    -buildTarget "StandaloneLinux64" \
    -customBuildTarget "StandaloneLinux64" \
    -customBuildPath "$UNITY_BUILD_DIR/linux/$UNITY_PROJECT_NAME.x86_64" \
    -executeMethod "UnityBuilderAction.Builder.BuildProject" 
rc1=$?
echo 'Linux build logs:'
cat $LINUX_LOG_FILE

echo "Returning Unity license"
$UNITY_PATH \
  -quit \
  -batchmode \
  -logFile $UNITY_RETURN_LOG_FILE \
  -silent-crashes \
  -returnlicense
echo "Unity return log:"
cat $UNITY_RETURN_LOG_FILE

#STATUS_CODE=$(($rc0|$rc1))
STATUS_CODE=$rc1
echo "Finishing with code $STATUS_CODE"
exit $STATUS_CODE
