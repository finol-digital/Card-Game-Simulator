#! /bin/sh

echo "Setting up osx certificates..."
sleep 10

KEY_CHAIN=osx.keychain
MAC_APPLICATION_CERTIFICATE_P12=mac_application.p12
MAC_INSTALLER_CERTIFICATE_P12=mac_installer.p12

# Recreate the certificates from the secure environment variable
echo $MAC_APPLICATION_CERTIFICATE | base64 --decode > $MAC_APPLICATION_CERTIFICATE_P12
echo $MAC_INSTALLER_CERTIFICATE | base64 --decode > $MAC_INSTALLER_CERTIFICATE_P12

#create a keychain
security create-keychain -p travis $KEY_CHAIN

# Make the keychain the default so identities are found
security default-keychain -s $KEY_CHAIN

# Unlock the keychain
security unlock-keychain -p travis $KEY_CHAIN

security import $MAC_APPLICATION_CERTIFICATE_P12 -k $KEY_CHAIN -P $MAC_APPLICATION_PASSWORD -T /usr/bin/codesign;
security import $MAC_INSTALLER_CERTIFICATE_P12 -k $KEY_CHAIN -P $MAC_INSTALLER_PASSWORD -T /usr/bin/codesign;

security find-identity -v

security set-key-partition-list -S apple-tool:,apple: -s -k travis $KEY_CHAIN

# remove certs
rm -fr *.p12

sleep 10
echo "OSX certificate setup complete!"
echo "Signing app..."
sleep 10

codesign -f --deep -s "3rd Party Mac Developer Application: Finol Digital LLC (49G524X5NY)" --entitlements "${TRAVIS_BUILD_DIR}/Assets/Editor/Card Game Simulator.entitlements" "${HOME}/unity_build_cache/OSX/Card Game Simulator.app"
productbuild --component "${HOME}/unity_build_cache/OSX/Card Game Simulator.app" /Applications --sign "3rd Party Mac Developer Installer: Finol Digital LLC (49G524X5NY)" "Card Game Simulator.pkg"
STATUS_CODE=$?

sleep 10
echo "App signed! Finishing with code $STATUS_CODE"
exit $STATUS_CODE