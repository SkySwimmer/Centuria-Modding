#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build/work

#
# Funchook
#

# Download funchook
echo Downloading funchook...
git clone https://github.com/kubo/funchook || exit 1
cd funchook

# Compile funchook
echo Compiling funchook...
mkdir build
cd build

# Prepare
cmake -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=install .. -DFUNCHOOK_DISASM=capstone

# Build
make
make install

# Copy result
echo Copying results...
mkdir ../../libfunchook
cp -rfv install/bin/* ../../libfunchook
cp -rfv install/include/* ../../libfunchook
cd ../..
rm -rf funchook
