#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build/work
set -e

# Properties
monorepo="https://github.com/dotnet/runtime"
monobranch="release/8.0"
ndkver="r26d"
apilevel=26

# Download .NET runtime
echo Cloning .NET runtime...
mkdir runtime
git clone "$monorepo" runtime/repo

# Build Mono
echo Building .NET runtime...
cd runtime/repo

# Prepare
echo Preparing crosscompile...

echo "Setting up toolchains and rootfs..."
mkdir -p "./.tools/android-rootfs/android-ndk-$ndkver"
mkdir -p "./.tools/android-rootfs/lldb"
CROSS_DIR="$(realpath ./.tools/android-rootfs)"
NDK_DIR="$(realpath ./.tools/android-rootfs/android-ndk-$ndkver)"
LLDB_DIR="$(realpath ./.tools/android-rootfs/lldb)"

echo "Downloading Android NDK $ndkver..."
curl -L "https://dl.google.com/android/repository/android-ndk-$ndkver-linux.zip" --output "$NDK_DIR.zip" -f || exit 1
echo "Extracting Android NDK..."
LAST="$PWD"
cd "$CROSS_DIR"
unzip "$NDK_DIR.zip" || exit 1
cd "$LAST"

echo "Downloading Android LLDB..."
curl -L "https://dl.google.com/android/repository/lldb-2.3.3614996-linux-x86_64.zip" --output "$LLDB_DIR.zip" -f || exit 1
echo "Extracting Android LLDB..."
LAST="$PWD"
cd "$LLDB_DIR"
unzip "$LLDB_DIR.zip" || exit 1
cd "$LAST"

TOOLCHAIN_DIR="$(realpath ./.tools/android-rootfs/android-ndk-$ndkver/toolchains/llvm/prebuilt/linux-x86_64)"
echo "Copying sysroot..."
cp -rfv "./.tools/android-rootfs/android-ndk-$ndkver/toolchains/llvm/prebuilt/linux-x86_64/sysroot" "./.tools/android-rootfs/android-ndk-$ndkver/sysroot"

# Taken from eng/common/cross/build-android-rootfs.sh
echo "Downloading dependencies..."
TMP=$CROSS_DIR/tmp/arm64/
mkdir -p "$TMP"
__AndroidPackages="libicu"
__AndroidPackages+=" libandroid-glob"
__AndroidPackages+=" liblzma"
__AndroidPackages+=" krb5"
__AndroidPackages+=" openssl"
for path in $(wget -qO- https://packages.termux.dev/termux-main-21/dists/stable/main/binary-aarch64/Packages |\
    grep -A15 "Package: \(${__AndroidPackages// /\\|}\)" | grep -v "static\|tool" | grep Filename); do

    if [[ "$path" != "Filename:" ]]; then
        echo "Working on: $path"
        wget -qO- https://packages.termux.dev/termux-main-21/$path | dpkg -x - "$TMP"
    fi
done
cp -Rv "$TMP/data/data/com.termux/files/usr/"* "$TOOLCHAIN_DIR/sysroot/usr/"
echo "Generating platform file..."
echo "RID=android.$apilevel-arm64" > "$TOOLCHAIN_DIR/sysroot/android_platform"
# End ported code

echo Switching branch...
git checkout "$monobranch" || exit 1
git pull

# Build
echo Compiling...
ROOTFS_DIR="$PWD/.tools/android-rootfs/android-ndk-$ndkver/sysroot" ./build.sh mono+libs --cross --arch arm64 -os android -c release /p:RunAOTCompilation=false /p:MonoForceInterpreter=true || exit 1

# Copy
echo Copying files...
mkdir ../../monolib
cp -rfv artifacts/obj/mono/android.arm64.*/out/include/. ../../monolib/include
cp -rfv artifacts/obj/mono/android.arm64.*/out/lib/. ../../monolib/lib
