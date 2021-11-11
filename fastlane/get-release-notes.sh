#! /bin/sh

echo "Release Notes: "
export RELEASE_NOTES="$(cat fastlane/metadata/en-US/release_notes.txt)"
RELEASE_NOTES="${RELEASE_NOTES//'%'/'%25'}"
RELEASE_NOTES="${RELEASE_NOTES//$'\n'/'%0A'}"
RELEASE_NOTES="${RELEASE_NOTES//$'\r'/'%0D'}"
echo "$RELEASE_NOTES"
echo "::set-output name=RELEASE_NOTES::$RELEASE_NOTES"
