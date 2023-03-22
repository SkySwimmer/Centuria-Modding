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
netdownloadurl=https://download.visualstudio.microsoft.com/download/pr/17309c0b-8c70-467b-9f95-a4f7ee8bd095/e6ccec507628a50cd81caef510b6fe76/dotnet-runtime-6.0.15-win-x64.zip


# Download CoreCLR
echo Downloading CoreCLR...
mkdir coreclr
curl -L "$netdownloadurl" --output coreclr/coreclr.zip --fail || exit 1

# Extract CoreCLR
echo Extracting CoreCLR...
cd coreclr
unzip coreclr.zip
rm coreclr.zip
