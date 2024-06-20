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
monorepo="https://github.com/dotnet/runtime"
monobranch="release/8.0"

# Download .NET runtime
echo Cloning .NET runtime...
mkdir runtime
git clone "$monorepo" runtime/repo

# Build Mono
echo Building .NET runtime...
cd runtime/repo

# Prepare
echo Preparing crosscompile...
echo "$(cat eng/common/cross/build-android-rootfs.sh | sed "s/__NDK_Version=.*/__NDK_Version=r26d/g")" > eng/common/cross/build-android-rootfs.sh
bash eng/common/cross/build-android-rootfs.sh arm64 24 || exit 1
echo Switching branch...
git checkout "$monobranch" || exit 1
git pull

# Build
echo Compiling...
ROOTFS_DIR=$(realpath ./.tools/android-rootfs/android-ndk-*/sysroot) ./build.sh mono+libs --cross --arch arm64 -os android -c release /p:RunAOTCompilation=false /p:MonoForceInterpreter=true || exit 1

# Copy
echo Copying files...
mkdir ../../monolib
cp -rfv artifacts/obj/mono/android.arm64.*/out/include/. ../../monolib/include
cp -rfv artifacts/obj/mono/android.arm64.*/out/lib/. ../../monolib/lib
