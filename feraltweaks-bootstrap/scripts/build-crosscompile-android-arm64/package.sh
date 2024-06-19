#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build

# Package
echo Creating FTL build package...
mkdir -v package

# Copy windowsill
echo Copying Windowsill...
cp -rfv work/windowsill/. package/

# Copy FTL
echo Copying FTL...
mkdir -v package/FeralTweaks
cp -rfv work/ftl/. package/FeralTweaks/

# Copy funchook
echo Copying Funchook...
mkdir -vp package/FeralTweaks/lib/android
cp -rfv work/libfunchook/*.so package/
echo Zipping...

# Copy CoreCLR
echo Copying CoreCLR...
mkdir package/CoreCLR
cp -rfv work/coreclrlib/. package/CoreCLR/

# Zip
cd package
zip -r ftl-android-arm64-latest.zip *
mv ftl-android-arm64-latest.zip ..
