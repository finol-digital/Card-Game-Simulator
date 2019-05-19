#! /bin/sh

UNITY_PROJECT_NAME="Card Game Simulator"
UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
OSX_LOG_FILE=$(pwd)/builds/osx.log

echo "Attempting to build $UNITY_PROJECT_NAME for OSX"
${UNITY_PATH} \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile "$OSX_LOG_FILE" \
  -projectPath "$(pwd)" \
  -buildOSXUniversalPlayer "$(pwd)/builds/osx/$UNITY_PROJECT_NAME.app" \
  -quit
STATUS_CODE=$?
echo 'OSX build logs:'
cat $OSX_LOG_FILE

echo "Finishing with code $STATUS_CODE"
exit $STATUS_CODE