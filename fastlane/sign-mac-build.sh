#! /bin/sh

echo "Setting up macOS certificates..."
sleep 10

KEYCHAIN_FILE=macos.keychain
KEYCHAIN_PASSWORD=$(openssl rand -base64 12)
APPLE_DISTRIBUTION_CERTIFICATE_P12=apple_distribution.p12
MAC_INSTALLER_CERTIFICATE_P12=mac_installer.p12

# Recreate the certificates from the secure environment variable
echo $APPLE_DISTRIBUTION_CERTIFICATE | base64 --decode > $APPLE_DISTRIBUTION_CERTIFICATE_P12
echo $MAC_INSTALLER_CERTIFICATE | base64 --decode > $MAC_INSTALLER_CERTIFICATE_P12

# Create a keychain
security create-keychain -p $KEYCHAIN_PASSWORD $KEYCHAIN_FILE

# Make the keychain the default so identities are found
security default-keychain -s $KEYCHAIN_FILE

# Unlock the keychain
security unlock-keychain -p $KEYCHAIN_PASSWORD $KEYCHAIN_FILE

# Import the certificates
security import $APPLE_DISTRIBUTION_CERTIFICATE_P12 -k $KEYCHAIN_FILE -P $APPLE_DISTRIBUTION_PASSWORD -A
security import $MAC_INSTALLER_CERTIFICATE_P12 -k $KEYCHAIN_FILE -P $MAC_INSTALLER_PASSWORD -A

# Fix for hanging in the codesign step
security set-key-partition-list -S apple-tool:,apple: -s -k $KEYCHAIN_PASSWORD $KEYCHAIN_FILE > /dev/null

# Confirm the certificate imports
security find-identity -v

sleep 10
echo "Finished setting up macOS certificate!"
echo "Signing app..."
sleep 10

mv "${MAC_BUILD_PATH}/StandaloneOSX.app" "${MAC_BUILD_PATH}/${PROJECT_NAME}.app"

chmod -R a+xr "${MAC_BUILD_PATH}/${PROJECT_NAME}.app"
bundlepaths=$(echo $MAC_APP_BUNDLE_PATHS | tr ";" "\n")
for bundlepath in $bundlepaths
do
    codesign --deep --force --verbose --sign "Apple Distribution: ${APPLE_TEAM_NAME} (${APPLE_TEAM_ID})" "${MAC_BUILD_PATH}/${PROJECT_NAME}.app/$bundlepath"
done
codesign --deep --force --verbose --sign "Apple Distribution: ${APPLE_TEAM_NAME} (${APPLE_TEAM_ID})" --entitlements "fastlane/${PROJECT_NAME}.entitlements" "${MAC_BUILD_PATH}/${PROJECT_NAME}.app"

sleep 10
echo "Packaging app..."
sleep 10

productbuild --component "${MAC_BUILD_PATH}/${PROJECT_NAME}.app" /Applications --sign "3rd Party Mac Developer Installer: ${APPLE_TEAM_NAME} (${APPLE_TEAM_ID})" "${MAC_BUILD_PATH}/${PROJECT_NAME}.pkg"
STATUS_CODE=$?

sleep 10
echo "App ready! Finishing with code $STATUS_CODE"
exit $STATUS_CODE
