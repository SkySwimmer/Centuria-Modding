#!/bin/bash

echo Building...
cd src

# Prepare
rm -rf build
mkdir build
cd build
cmake .. -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-21 -DANDROID_NDK="$(realpath ../../ndk)" -DCMAKE_TOOLCHAIN_FILE="$(realpath ../../ndk/build/cmake/android.toolchain.cmake)" -G Ninja || exit 1

# Build
ninja || exit 1
