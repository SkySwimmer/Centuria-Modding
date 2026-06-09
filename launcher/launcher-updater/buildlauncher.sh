#!/bin/bash
chmod +x tools/buildartifacts.sh

# Build artifacts
rm -rf build/artifacts
rm -rf build/targets

# Build
args=("$@")
if [ "${#args[@]}" == "0" ]; then
    # Build stable
    tools/buildartifacts.sh assets/stable portables || exit
    rm -rf build/targets
    tools/buildartifacts.sh assets/stable installers || exit
    mv build/artifacts build/artifactdata-stable

    # Build EA18
    tools/buildartifacts.sh assets/earlyaccess-1.8 portables || exit
    rm -rf build/targets
    tools/buildartifacts.sh assets/earlyaccess-1.8 installers || exit
    mv build/earlyaccess-1.8 build/artifactdata-earlyaccess-1.8
else
    # Build stable
    tools/buildartifacts.sh assets/stable "$@" || exit
    mv build/artifacts build/artifactdata-stable

    # Build EA18
    tools/buildartifacts.sh assets/earlyaccess-1.8 "$@" || exit
    mv build/artifacts build/artifactdata-earlyaccess-1.8
fi

# Move artifacts
mkdir build/artifacts
mv build/artifactdata-stable build/artifacts/stable
mv build/artifactdata-earlyaccess-1.8 build/artifacts/earlyaccess-1.8
