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

# Copy doorstop
echo Copying Doorstop...
cp -rfv work/doorstop/. package/

# Copy FTL
echo Copying FTL...
mkdir -v package/FeralTweaks
cp -rfv work/ftl/. package/FeralTweaks/

# Copy funchook
echo Copying Funchook...
cp -rfv work/libfunchook/*.dylib package/
echo Zipping...

# Copy CoreCLR
echo Copying CoreCLR...
mkdir package/CoreCLR
cp -rfv work/coreclr/shared/Microsoft.NETCore.App/*/. package/CoreCLR/

# Zip
cd package
zip -r ftl-osx-latest.zip *
mv ftl-osx-latest.zip ..
