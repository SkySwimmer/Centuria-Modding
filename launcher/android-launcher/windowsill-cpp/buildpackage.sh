#!/bin/bash

# Configure
echo Configuring...
chmod +x configure.sh
chmod +x build.sh
./configure.sh || exit 1

# Build
echo Building...
./build.sh || exit 1
rm -rfv build

# Prepare output
echo
echo Creating output...
mkdir -p build

# Create zip
echo Creating zip...
rm -rf src/build/windowsill
mkdir -p src/build/windowsill/lib/arm64-v8a
cp -rf src/build/*.so src/build/windowsill/lib/arm64-v8a
cd src/build/windowsill
zip -r ../../../build/windowsill.zip * || exit 1
