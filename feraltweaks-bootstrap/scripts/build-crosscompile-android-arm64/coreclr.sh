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
git clone "$coreclrrepo" coreclr/repo

# Build CoreCLR
echo Building CoreCLR...
cd coreclr/repo

# Prepare
echo Preparing crosscompile...
bash eng/common/cross/build-android-rootfs.sh || exit 1
echo Switching branch...
git checkout "$coreclrbranch" || exit 1
git pull

# Build
echo Compiling...
ROOTFS_DIR=$(realpath ./.tools/android-rootfs/android-ndk-*/sysroot) ./build.sh clr+libs --cross --arch arm64 || exit 1
