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
netdownloadurl=https://download.visualstudio.microsoft.com/download/pr/eac51dde-7bac-4bdb-aacd-e8c870f29aa4/d6c945e85adab9af2446856f90f6d326/dotnet-runtime-8.0.1-win-x64.zip


# Download CoreCLR
echo Downloading CoreCLR...
mkdir coreclr
curl -L "$netdownloadurl" --output coreclr/coreclr.zip --fail || exit 1

# Extract CoreCLR
echo Extracting CoreCLR...
cd coreclr
unzip coreclr.zip
rm coreclr.zip
