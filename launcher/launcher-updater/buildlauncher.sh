#!/bin/bash
chmod +x tools/buildartifacts.sh

# Build artifacts
rm -rf build/artifacts
tools/buildartifacts.sh assets/stable "$@" || exit
mv build/artifacts build/artifactdata-stable
tools/buildartifacts.sh assets/earlyaccess-1.8 "$@" || exit
mv build/earlyaccess-1.8 build/artifactdata-earlyaccess-1.8
mkdir build/artifacts
mv build/artifactdata-stable build/artifacts/stable
mv build/artifactdata-earlyaccess-1.8 build/artifacts/earlyaccess-1.8
