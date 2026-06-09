#!/bin/bash

# Building cache
CALLED_TARGETS=()

# Target loading
function locateAllTargets() {
    # Find all
    local dir="$1"
    local pref="$2"
    for target in "$dir"/*; do
        if [ -f "$target" ]; then
            targets+=("$pref$(basename "$target")")
        fi
    done
    for target in "$dir"/*/; do
        if [ -d "$target" ]; then
            locateAllTargets "$target" "$pref$(basename "$target")/"
        fi
    done
}

# Main tool
function artifacttool_main() {
    # Check arguments
    local basedir="$1"
    if [ "$basedir" == "" ]; then
        1>&2 echo "Error: missing argument: artifact base dir"
        1>&2 echo "Usage: buildartifacts <basedir> [artifacts relative paths to call...]"
        exit 1
    fi

    # Check folder
    if [ ! -d "$basedir/artifacts" ]; then
        1>&2 echo "Error: invalid argument: artifact base dir: nested 'artifacts' folder does not exist"
        1>&2 echo "Usage: buildartifacts <basedir> [artifacts relative paths to call...]"
        exit 1
    fi
    basedir="$(readlink -f "$basedir")"

    # Enter
    export BASE_DIR="$basedir"
    export ASSETS_DIR="$basedir"
    export ARTIFACT_SCRIPTS_DIR="$basedir/artifacts"
    export BUILD_CWD="$PWD"
    export BUILD_BASEDIR="$BUILD_CWD/build"
    export BUILD_ARTIFACT_OUT="$BUILD_BASEDIR/artifacts"
    export BUILD_TARGET_BASE_OUT="$BUILD_BASEDIR/targets"
    export BUILD_SOURCETEMP="$BUILD_BASEDIR/tmp"
    export BUILD_SOURCETEMP_DOWNLOADS="$BUILD_BASEDIR/tmp/downloads"
    if [ ! -d "$BUILD_BASEDIR" ]; then
        mkdir "$BUILD_BASEDIR" || exit 1
    fi
    if [ ! -d "$BUILD_SOURCETEMP" ]; then
        mkdir "$BUILD_SOURCETEMP" || exit 1
    fi
    if [ ! -d "$BUILD_SOURCETEMP_DOWNLOADS" ]; then
        mkdir "$BUILD_SOURCETEMP_DOWNLOADS" || exit 1
    fi
    if [ -d "$BUILD_TARGET_BASE_OUT" ]; then
        rm -rf "$BUILD_TARGET_BASE_OUT"
    fi
    mkdir "$BUILD_TARGET_BASE_OUT" || exit 1
    if [ ! -d "$BUILD_ARTIFACT_OUT" ]; then
        mkdir "$BUILD_ARTIFACT_OUT" || exit 1
    fi
    cd "$basedir"

    # Collect targets
    local targets=()
    local first=true
    for arg in "$@"; do
        if [ "$first" == "true" ]; then
            first=false
            continue
        fi
        targets+=("$arg")
    done

    # Check targets
    echo Loading targets...
    for target in "${targets[@]}"; do
        # Check existing
        if [ -d "artifacts/$target" ]; then
            # Collect targets
            arrayRemove "$target" "targets"
            locateAllTargets "$basedir/artifacts/$target" "$target/"
            continue
        fi
        if [ ! -f "artifacts/$target" ]; then
            # Check
            if [ -f "artifacts/$target.artifact" ]; then
                target="$target.artifact"
            fi

            # Check
            if [ ! -f "artifacts/$target" ]; then
                1>&2 echo "Error: invalid target '$target': file not found in targets base folder, make sure to use a relative path and not only a name if the file is nested"
                1>&2 echo "Usage: buildartifacts <basedir> [artifacts relative paths to call...]"
            fi
        fi
    done

    # Load targets if empty
    if [ "${#targets[@]}" == 0 ]; then
        locateAllTargets "$basedir/artifacts" ""
    fi

    # Log start
    echo Preparing build...
    echo "Targets being built:"
    echo " - ${targets[@]}"
    echo Note: additional targets may be built if other targets specify dependencies
    echo
    
    # Begin build
    echo Building...
    for target in "${targets[@]}"; do
        runTarget "$ARTIFACT_SCRIPTS_DIR/$target" "$target"
    done
    echo Finished.
}
