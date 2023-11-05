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
monobranch="release/6.0"

# Download .NET runtime
echo Cloning .NET runtime...
mkdir runtime
git clone "$monorepo" runtime/repo

# Build Mono
echo Building .NET runtime...
cd runtime/repo

# Prepare
echo Preparing crosscompile...
bash eng/common/cross/build-android-rootfs.sh || exit 1
echo Switching branch...
git checkout "$monobranch" || exit 1
git pull

# Build
echo Compiling...
ROOTFS_DIR=$(realpath ./.tools/android-rootfs/android-ndk-*/sysroot) ./build.sh mono+libs --cross --arch arm64 -os android -c release /p:RunAOTCompilation=false /p:MonoForceInterpreter=true || exit 1

# Copy
echo Copying files...
mkdir ../../monolib
cp -rfv artifacts/obj/mono/*nux.arm64.*/out/include/. ../../monolib/include
cp -rfv artifacts/bin/microsoft.netcore.app.runtime.*/*/runtimes/*/native/. ../../monolib/lib
cp -rfv artifacts/bin/microsoft.netcore.app.runtime.*/*/runtimes/*/lib/*/. ../../monolib/lib
