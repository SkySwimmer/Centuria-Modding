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
netdownloadurl=https://download.visualstudio.microsoft.com/download/pr/220d43f7-eb7f-470d-a80b-b30210adbbf2/dbfa691328557ee9888a1f38a29f72bd/dotnet-runtime-8.0.1-osx-x64.tar.gz


# Download CoreCLR
echo Downloading CoreCLR...
mkdir coreclr
curl -L "$netdownloadurl" --output coreclr/coreclr.tar.gz --fail || exit 1

# Extract CoreCLR
echo Extracting CoreCLR...
cd coreclr
tar -xvf coreclr.tar.gz
rm coreclr.tar.gz
