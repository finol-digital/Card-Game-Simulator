#! /bin/sh

echo "Setting up osx certificates..."
sleep 10

KEY_CHAIN=osx.keychain
APPLICATION_CERTIFICATE_P12=application.p12
INSTALLER_CERTIFICATE_P12=installer.p12

# Recreate the certificates from the secure environment variable
echo $OSX_APPLICATION_CERTIFICATE | base64 --decode > $APPLICATION_CERTIFICATE_P12
echo $OSX_INSTALLER_CERTIFICATE | base64 --decode > $INSTALLER_CERTIFICATE_P12

#create a keychain
security create-keychain -p travis $KEY_CHAIN

# Make the keychain the default so identities are found
security default-keychain -s $KEY_CHAIN

# Unlock the keychain
security unlock-keychain -p travis $KEY_CHAIN

security import $APPLICATION_CERTIFICATE_P12 -k $KEY_CHAIN -P $OSX_APPLICATION_PASSWORD -T /usr/bin/codesign;
security import $INSTALLER_CERTIFICATE_P12 -k $KEY_CHAIN -P $OSX_INSTALLER_PASSWORD -T /usr/bin/codesign;

security find-identity -v

security set-key-partition-list -S apple-tool:,apple: -s -k travis $KEY_CHAIN

# remove certs
rm -fr *.p12

sleep 10
echo "OSX certificate setup complete!"
echo "Signing app..."
sleep 10

codesign -f --deep -s "3rd Party Mac Developer Application: Finol Digital LLC" --entitlements "${TRAVIS_BUILD_DIR}/Assets/Editor/Card Game Simulator.entitlements" "${HOME}/unity_build_cache/OSX/Card Game Simulator.app"
productbuild --component "${HOME}/unity_build_cache/OSX/Card Game Simulator.app" /Applications --sign "3rd Party Mac Developer Installer: Finol Digital LLC" "Card Game Simulator.pkg"

sleep 10
echo "App signed!"
