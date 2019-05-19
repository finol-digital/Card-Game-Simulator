#! /bin/sh

UNITY_PROJECT_NAME="Card Game Simulator"
UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
UNITY_ACTIVATION_LOG_FILE=$(pwd)/build/unity.activation.log
UNITY_RETURN_LOG_FILE=$(pwd)/build/unity.returnlicense.log
OSX_LOG_FILE=$(pwd)/build/osx.log

echo "Activating Unity license"
${UNITY_PATH} \
    -logFile "$UNITY_ACTIVATION_LOG_FILE" \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USER} \
    -password ${UNITY_PWD} \
    -batchmode \
    -noUpm \
    -quit
echo "Unity activation log:"
cat $UNITY_ACTIVATION_LOG_FILE

echo "Attempting to build $UNITY_PROJECT_NAME for OSX"
${UNITY_PATH} \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile "$OSX_LOG_FILE" \
  -projectPath "$(pwd)" \
  -buildOSXUniversalPlayer "$(pwd)/build/osx/$UNITY_PROJECT_NAME.app" \
  -quit
STATUS_CODE=$?
echo 'OSX build logs:'
cat $OSX_LOG_FILE

echo "Returning Unity license"
${UNITY_PATH} \
    -logFile "$UNITY_RETURN_LOG_FILE" \
    -batchmode \
    -returnlicense \
    -quit
echo "Unity return log:"
cat $UNITY_RETURN_LOG_FILE

echo "Finishing with code $STATUS_CODE"
exit $STATUS_CODE