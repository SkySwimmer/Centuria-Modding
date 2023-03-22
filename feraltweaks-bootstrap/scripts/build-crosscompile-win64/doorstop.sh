#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build/work


# Settings
version=4.0.0
artifact=doorstop_win_release_$version.zip


# Download and set up
echo Downloading Doorstop...
mkdir doorstop
curl -L "https://github.com/NeighTools/UnityDoorstop/releases/download/v$version/$artifact" --fail --output doorstop/doorstop.zip || exit 1

# Extract
echo Extracting Doorstop...
cd doorstop
unzip doorstop.zip || exit 1

# Clean up
echo Cleaning...
cp -rfv x64/. .
rm -rfv x86
rm -rfv x64
rm doorstop.zip

# Set up
echo Setting up Doorstop...
echo '[General]
enabled=true
target_assembly=FeralTweaks/FeralTweaksBootstrap.dll
redirect_output_log=false

[Il2Cpp]
coreclr_path=CoreCLR/coreclr.dll
corlib_dir=CoreCLR
' > doorstop_config.ini
echo Done.
