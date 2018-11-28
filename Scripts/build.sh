#! /bin/sh

UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
project="Card Game Simulator"

echo "Activating Unity license"
${UNITY_PATH} \
    -logFile "${pwd}/unity.activation.log" \
    -serial ${UNITY_SERIAL} \
    -username ${UNITY_USER} \
    -password ${UNITY_PWD} \
    -batchmode \
    -noUpm \
    -quit
echo "Unity activation log"
cat "${pwd}/unity.activation.log"

echo "Attempting to build $project for OS X"
${UNITY_PATH} \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile "$(pwd)/unity.build.osx.log" \
  -projectPath "$(pwd)" \
  -buildOSXUniversalPlayer "$(pwd)/builds/osx/$project.app" \
  -quit
echo 'Logs from build'
cat "$(pwd)/unity.build.osx.log"

echo "Returning Unity license"
${UNITY_PATH} \
    -logFile "${pwd}/unity.returnlicense.log" \
    -batchmode \
    -returnlicense \
    -quit
cat "$(pwd)/unity.returnlicense.log"

echo 'Attempting to zip OS X build'
zip -r "$(pwd)/builds/mac.zip" "$(pwd)/builds/osx/"