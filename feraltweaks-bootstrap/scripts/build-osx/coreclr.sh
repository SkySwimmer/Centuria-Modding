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
netdownloadurl=https://download.visualstudio.microsoft.com/download/pr/002ce092-a45c-4c52-baae-067879173e64/a6b706f9b30cb74210ce87ca651b3f4b/dotnet-runtime-6.0.15-osx-x64.tar.gz


# Download CoreCLR
echo Downloading CoreCLR...
mkdir coreclr
curl -L "$netdownloadurl" --output coreclr/coreclr.tar.gz --fail || exit 1

# Extract CoreCLR
echo Extracting CoreCLR...
cd coreclr
tar -xvf coreclr.tar.gz
rm coreclr.tar.gz
