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
mkdir -vp package/FeralTweaks/lib/win
cp -rfv work/libfunchook/*.dll package/FeralTweaks/lib/win/
echo Zipping...

# Copy CoreCLR
echo Copying CoreCLR...
mkdir package/CoreCLR
cp -rfv work/coreclr/shared/Microsoft.NETCore.App/*/. package/CoreCLR/

# Zip
cd package
zip ftl-win64-latest.zip *
mv ftl-win64-latest.zip ..
