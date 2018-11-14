#! /bin/sh

project="Card Game Simulator"

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile $(pwd)/unity.log \
  -projectPath $(pwd) \
  -buildOSXUniversalPlayer "$(pwd)/builds/osx/$project.app" \
  -quit

echo 'Logs from build'
cat $(pwd)/unity.log

echo 'Attempting to zip build'
zip -r $(pwd)/builds/mac.zip $(pwd)/builds/osx/