#!/bin/bash

# Prepare
chmod +x build.sh
chmod +x buildpackage.sh

# Download NDK if needed
NDK_URL="https://dl.google.com/android/repository/android-ndk-r26b-linux.zip"
if [ ! -f ndk/complete ]; then
    echo Downloading NDK...
    mkdir tmp4
    curl "$NDK_URL" --output tmp/android-ndk.zip
    echo Extracting NDK...
    cd tmp
    unzip android-ndk.zip
    rm android-ndk.zip
    cd ..
    rm -rf ndk
    cp -rfv tmp/* ndk
    rm -rf tmp
    touch ndk/complete
fi

# Copy headers
echo Copying headers...
cp -rfv ../android-launcher/build/generated/sources/headers/java/main/* src/includes

# Done
echo Done.

