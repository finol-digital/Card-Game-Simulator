#! /bin/sh

UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
project="Card Game Simulator"

echo "Activating Unity license"
${UNITY_PATH} \
    -logFile "${TRAVIS_BUILD_DIR}/unity.activation.log" \
    -username ${UNITY_USER} \
    -password ${UNITY_PWD} \
    -batchmode \
    -noUpm \
    -quit
echo "Unity activation log"
cat "${TRAVIS_BUILD_DIR}/unity.activation.log"

echo "Attempting to build $project for OS X"
${UNITY_PATH} \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile "$(TRAVIS_BUILD_DIR)/unity.build.osx.log" \
  -projectPath "$(TRAVIS_BUILD_DIR)" \
  -buildOSXUniversalPlayer "$(TRAVIS_BUILD_DIR)/builds/osx/$project.app" \
  -quit
echo 'Logs from build'
cat "$(TRAVIS_BUILD_DIR)/unity.build.osx.log"

echo "Returning Unity license"
${UNITY_PATH} \
    -logFile "${TRAVIS_BUILD_DIR}/unity.returnlicense.log" \
    -batchmode \
    -returnlicense \
    -quit
cat "$(TRAVIS_BUILD_DIR)/unity.returnlicense.log"

echo 'Attempting to zip OS X build'
zip -r "$(TRAVIS_BUILD_DIR)/builds/mac.zip" "$(TRAVIS_BUILD_DIR)/builds/osx/"