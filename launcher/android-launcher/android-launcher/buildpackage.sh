#!/bin/bash

dex2jarDownload="https://github.com/pxb1988/dex2jar/releases/download/v2.1/dex2jar-2.1.zip"
dex2jarInnerFolder="dex-tools-2.1"
minSdk="29"

if [ ! -d work-temp ]; then
    mkdir work-temp
fi

# Determine platform
unameOut="$(uname -s)"
case "${unameOut}" in
    Linux*)
        plat=linux
        ;;
    Darwin*)
        plat=macosx
        ;;
    CYGWIN*)
        plat=windows
        ;;
    MINGW*)
        plat=windows
        ;;
    MSYS_NT*)
        plat=windows
        ;;
    *)
        1>&2 echo Error: unknown platform: $unameOut, cannot run the build tool
        exit 1
        ;;
esac
echo "Platform: $plat"

# Download dex2jar
if [ ! -f work-temp/dex2jar/complete ]; then
    if [ ! -d work-temp/d2j ]; then
        mkdir -p work-temp/d2j
    fi

    # Download
    echo 'Downloading dex2jar... (note this package is owned by pxb1988)'
    curl -L "$dex2jarDownload" --output "work-temp/d2j/d2j.zip" || exit 1

    # Unzip
    echo 'Extracting dex2jar...'
    rm -rf 'work-temp/d2j/d2j-ext'
    mkdir 'work-temp/d2j/d2j-ext'
    cd 'work-temp/d2j/d2j-ext'
    unzip '../../../work-temp/d2j/d2j.zip' || exit 1
    cd ../../..

    # Rename
    mv 'work-temp/d2j/d2j-ext/'"$dex2jarInnerFolder" 'work-temp/dex2jar'
    rm -rfv 'work-temp/d2j'
    touch work-temp/dex2jar/complete
fi

# Download tools
if [ ! -f work-temp/build-tools/complete ]; then
    if [ ! -d work-temp/buildtools ]; then
        mkdir -p work-temp/buildtools
    fi

    # Download
    echo 'Downloading Android Build Tools... (note this package is owned by google)'
    curl "https://dl.google.com/android/repository/build-tools_r33-$plat.zip" --output "work-temp/buildtools/build-tools.zip" || exit 1

    # Unzip
    echo 'Extracting buildtools...'
    rm -rf 'work-temp/buildtools/build-tools-ext'
    mkdir 'work-temp/buildtools/build-tools-ext'
    cd 'work-temp/buildtools/build-tools-ext'
    unzip '../../../work-temp/buildtools/build-tools.zip' || exit 1
    cd ../../..

    # Rename
    mv 'work-temp/buildtools/build-tools-ext/android-13' 'work-temp/build-tools'
    rm -rfv 'work-temp/buildtools'
    touch work-temp/build-tools/complete
fi

# Build
echo Building...
../gradlew build || exit 1
rm -rfv build/launcher-binaries

# Prepare output
echo
echo Creating output...
mkdir -p build/launcher-binaries
cp -v launcherconfig.json build/launcher-binaries || exit 1

# Create dex
echo Creating launcher dex...

# Find libraries for dx
libs=$(find work-temp/dex2jar/lib -name '*.jar' -exec echo -n :{} \;)
libs=${libs:1}

# Find jars
jars=()
echo Locating built jars...
for file in build/ftlauncher/core/*; do
    echo "Found: $file"
    jars+=("$file")
done
if [ -d build/ftlauncher/libs ]; then
    for file in build/ftlauncher/libs/*; do
        echo "Found: $file"
        jars+=("$file")
    done
fi

# Run dx
echo Running DX...
java -cp "$libs" com.android.dx.command.Main --dex --no-strict --min-sdk-version "$minSdk" --output build/launcher-binaries/launcher-binary.dex $jars || exit 1
if [ ! -f build/launcher-binaries/launcher-binary.dex ]; then
    1>&2 echo Error: DX did not create build/launcher-binaries/launcher-binary.dex!
    exit 1
fi
echo

# Windowsill
#echo Building windowsill...
#cd ../windowsill-cpp/
#chmod +x buildpackage.sh
#./buildpackage.sh || exit 1
cd ../android-launcher
echo Copying libraries...
cp -v ../windowsill-cpp/src/build/*.so build/launcher-binaries


# Create zip
echo Creating zip...
cd build/launcher-binaries
zip ../launcher-binaries.zip * || exit 1
