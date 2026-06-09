#!/bin/bash

# Requirements
require targetbuildercommands

# Builder
function build() {
    # Arguments
    local type="$1"
    local target="$2"
    local profile="$3"

    # Check arguments
    if [ "$type" == "" ]; then
        1>&2 echo "Error: missing argument for build: target type: base folder for targets"
        1>&2 echo "Usage: build <type> <target> <profile>"
        printStackTrace
        exit 1
    fi
    if [ "$target" == "" ]; then
        1>&2 echo "Error: missing argument for build: target name: build target name"
        1>&2 echo "Usage: build <type> <target> <profile>"
        printStackTrace
        exit 1
    fi
    if [ "$profile" == "" ]; then
        1>&2 echo "Error: missing argument for build: profile: build target profile function name"
        1>&2 echo "Usage: build <type> <target> <profile>"
        printStackTrace
        exit 1
    fi
        
    # Run build
    local prefcwd="$PWD"
    export BUILD_TARGET="$BUILD_TARGET_BASE_OUT/$type/$target"
    export BUILD_TARGET_NAME="$target"
    export BUILD_TARGETTYPE_NAME="$type"
    export BUILD_TEMP="$BUILD_SOURCETEMP/$type/$target"
    export BUILD_TEMP_DOWNLOADS="$BUILD_SOURCETEMP_DOWNLOADS/$type/$target"
    local BUILD_TARGET_BAK="$BUILD_TARGET"
    local BUILD_TARGETTYPE_NAME_BAK="$BUILD_TARGETTYPE_NAME"
    local BUILD_TARGET_NAME_BAK="$BUILD_TARGET_NAME"
    local BUILD_TEMP_BAK="$BUILD_TEMP"
    local BUILD_TEMP_DOWNLOADS_BAK="$BUILD_TEMP_DOWNLOADS"
    if [ ! -d "$BUILD_TARGET" ]; then
        mkdir -p "$BUILD_TARGET" || exit 1
    fi
    cd "$BUILD_TARGET"
    TARGET_BUILDING_LIST+=("$type/$target")

    # Run function
    runFunctionSafe "$profile" || exit 1

    # Return
    unset BUILD_TARGET
    unset BUILD_TARGET_NAME
    unset BUILD_TARGETTYPE_NAME
    unset BUILD_TEMP
    unset BUILD_TEMP_DOWNLOADS
    unset BUILD_TARGET_BAK
    unset BUILD_TARGET_NAME_BAK
    unset BUILD_TARGETTYPE_NAME_BAK
    unset BUILD_TEMP_BAK
    unset BUILD_TEMP_DOWNLOADS_BAK
    cd "$prefcwd"
}

# Artifact producer
function produce-artifact-result() {
    # Arguments
    local mode="$1"
    local type="$2"
    local target="$3"
    local outname="$4"
    local src="$5"

    # Check args
    if [ "$mode" == "" ]; then
        1>&2 echo "Error: missing argument for produce-artifact-result: store mode: mode for storing the artifact, accepts: raw, compressed-zip, compressed-targz"
        1>&2 echo "Usage: produce-artifact-result <store mode> <target type> <target name> <output path> [<source files in target>]"
        printStackTrace
        exit 1
    fi
    if [ "$type" == "" ]; then
        1>&2 echo "Error: missing argument for produce-artifact-result: target type: base folder for targets"
        1>&2 echo "Usage: produce-artifact-result <store mode> <target type> <target name> <output path> [<source files in target>]"
        printStackTrace
        exit 1
    fi
    if [ "$target" == "" ]; then
        1>&2 echo "Error: missing argument for produce-artifact-result: target name: build target name"
        1>&2 echo "Usage: produce-artifact-result <store mode> <target type> <target name> <output path> [<source files in target>]"
        printStackTrace
        exit 1
    fi
    if [ "$outname" == "" ]; then
        1>&2 echo "Error: missing argument for produce-artifact-result: output path: output artifact name"
        1>&2 echo "Usage: produce-artifact-result <store mode> <target type> <target name> <output path> [<source files in target>]"
        printStackTrace
        exit 1
    fi

    # Setup
    local source="$BUILD_TARGET_BASE_OUT/$type/$target/$src"
    if [ ! -d "$source" ] && [ ! -f "$source" ]; then
        # Invalid target
        if [ "$src" == "" ]; then
            1>&2 echo "Error: target data not found for build target $type -> $target"
        else
            1>&2 echo "Error: target data not found for build target $type -> $target -> $src"
        fi
        printStackTrace
        exit 1
    fi
    local dest="$BUILD_ARTIFACT_OUT/$outname"
    if [ ! -d "$(dirname "$dest")" ]; then mkdir -p "$(dirname "$dest")" ; fi

    # Create artifact
    echo "Creating artifact $outname..."
    rm -rf "$dest"
    if [ "$mode" == "raw" ]; then
        # Raw mode
        copyContentsInternal "$(readlink -f "$source")" "$(readlink -f "$dest")" "<targets>/$type/$target$destN" "<artifacts>/$outname" || exit 1
    elif [ "$mode" == "compressed-targz" ]; then
        # Tar mode
        (cd "$(readlink -f "$source")" ; tar -czvf "$dest" .) || exit 1
    elif [ "$mode" == "compressed-zip" ]; then
        # Tar mode
        (cd "$(readlink -f "$source")" ; zip -r "$dest" .) || exit 1
    else
        crash unimplemented mode $mode
    fi
}