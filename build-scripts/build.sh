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
ANDROID_LOG_FILE=$UNITY_BUILD_DIR/android.log

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
      -username "$UNITY_EMAIL" \
      -password "$UNITY_PASSWORD"
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
echo "Attempting to build $UNITY_PROJECT_NAME for Android"
$UNITY_PATH \
    -batchmode \
    -logfile $ANDROID_LOG_FILE \
    -quit \
    -customBuildName "$UNITY_PROJECT_NAME" \
    -projectPath $(pwd) \
    -buildTarget "Android" \
    -customBuildTarget "Android" \
    -customBuildPath "$UNITY_BUILD_DIR/android/$UNITY_PROJECT_NAME.apk" \
    -executeMethod "BuildCGS.BuildProject" \
    -androidAppBundle -keystorePass "$UNITY_ANDROID_KEYSTORE_PASS" -keyaliasPass "$UNITY_ANDROID_KEYSTORE_PASS"
rc1=$?
echo 'Android build logs:'
cat $ANDROID_LOG_FILE

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
