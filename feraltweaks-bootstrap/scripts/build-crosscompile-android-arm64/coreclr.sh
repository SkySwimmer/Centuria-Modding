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
coreclrbranch="release/8.0"

# Download .NET runtime
echo Cloning .NET runtime...
mkdir runtime
git clone "$coreclrrepo" runtime/repo

# Build CoreCLR
echo Building .NET runtime...
cd runtime/repo

# Prepare
echo Preparing crosscompile...
bash eng/common/cross/build-android-rootfs.sh || exit 1
echo Switching branch...
git checkout "$coreclrbranch" || exit 1
git pull

# Build
echo Compiling...
ROOTFS_DIR=$(realpath ./.tools/android-rootfs/android-ndk-*/sysroot) ./build.sh coreclr+libs --cross --arch arm64 -os android -c release /p:RunAOTCompilation=false /p:MonoForceInterpreter=true || exit 1

# Copy
echo Copying files...
mkdir ../../coreclrlib
cp -rfv artifacts/obj/mono/*nux.arm64.*/out/include/. ../../coreclrlib/include
cp -rfv artifacts/bin/microsoft.netcore.app.runtime.*/*/runtimes/*/native/. ../../coreclrlib/lib
cp -rfv artifacts/bin/microsoft.netcore.app.runtime.*/*/runtimes/*/lib/*/. ../../coreclrlib/lib
