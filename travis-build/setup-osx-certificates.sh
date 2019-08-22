KEY_CHAIN=osx.keychain
APPLICATION_CERTIFICATE_P12=application.p12
INSTALLER_CERTIFICATE_P12=installer.p12

sleep 10

echo "Setting up osx certificates..."

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

echo "OSX certificate setup complete!"

sleep 10