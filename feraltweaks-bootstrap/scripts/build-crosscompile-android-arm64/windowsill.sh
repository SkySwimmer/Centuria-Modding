#!/bin/bash

if [ -f scriptdir ]; then
    cd ..
elif [ -f ../scriptdir ]; then
    cd ../..
elif [ -d launcher/feraltweaks-launcher ]; then
    cd feraltweaks-bootstrap
fi
cd build/work

# Create folder
mkdir windowsill
cd windowsill

# Set up
echo Setting up windowsill...
echo '{
    "mainClass": "Doorstop.Entrypoint",
    "mainMethod": "Start",

    "mainAssembly": "FeralTweaks/FeralTweaksBootstrap.dll",
    "monoAssembly": "Mono/coreclr.dll",
    "monoDir": "Mono"
}
' > windowsil.config.json
echo Done.
