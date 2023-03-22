#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi

# Build FTL
echo Building FTL...
dotnet build || exit 1

# Copy results
echo Copying assemblies...
mkdir build/work/ftl
cp -rfv run/FeralTweaks/*.dll build/work/ftl
