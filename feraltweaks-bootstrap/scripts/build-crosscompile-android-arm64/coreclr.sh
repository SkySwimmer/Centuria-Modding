#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build/work


# Properties
coreclrrepo="https://github.com/dotnet/runtime"
coreclrbranch="release/6.0"

# Download CoreCLR
echo Cloning CoreCLR...
mkdir coreclr
git clone "$coreclrrepo" --branch "$coreclrbranch" coreclr/repo

# Build CoreCLR
echo Building CoreCLR...
cd coreclr/repo

# Prepare
echo Preparing crosscompile...
bash eng/common/cross/build-android-rootfs.sh || exit 1

# Build
echo Compiling...
