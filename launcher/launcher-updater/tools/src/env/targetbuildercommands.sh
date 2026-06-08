#!/bin/bash

# Requirements
require artifactcommands

# File copy
function copyContentsInternal() {
    # Args
    local source="$1"
    local dest="$2"
    local prefixSrc="$3"
    local prefixD="$4"
    local pref="$5"
    if [ -f "$source" ]; then
        local destf="$dest"
        echo "Writing $prefixSrc -> $prefixD"
        local destp="$(dirname "$destf")"
        if [ ! -d "$destp" ]; then mkdir -p "$distp" ; fi
        cp "$source" "$destf"
        return
    fi
    for f in "$source"/*; do
        if [ -f "$f" ]; then
            local destf="$dest/$(basename "$f")"
            echo "Writing $prefixSrc/$pref$(basename "$f") -> $prefixD/$pref$(basename "$f")"
            local destp="$(dirname "$destf")"
            if [ ! -d "$destp" ]; then mkdir -p "$destp" ; fi
            cp "$f" "$destf"
        fi
    done
    for d in "$source"/*/; do
        if [ -d "$d" ]; then
            if [ ! -d "$dest/$(basename "$d")" ] ; then mkdir -p "$dest/$(basename "$d")" ; fi
            copyContentsInternal "$d" "$dest/$(basename "$d")" "$prefixSrc" "$prefixD" "$pref$(basename "$d")/"
        fi
    done
}

# Sources from assets folder
function from-assets() {
    # Args
    local source="$1"
    local dest="$2"
    if [ "$source" == "" ]; then
        1>&2 echo "Error: missing argument for from-assets: source: source files to copy"
        1>&2 echo "Usage: from-assets <source> [<target>]"
        printStackTrace
        exit 1
    fi
    local destN="$dest"
    local sourceN="$source"
    if [ "$destN" != "" ]; then
        destN="/$destN"
    fi
    dest="$BUILD_TARGET/$dest"
    source="$ASSETS_DIR/$source"
    if [ ! -d "$source" ] && [ ! -f "$source" ]; then
        1>&2 echo "Error: source path does not exist: $sourceN"
        printStackTrace
        exit 1
    fi
    if [ ! -d "$(dirname "$dest")" ]; then mkdir -p "$(dirname "$dest")" ; fi
    dest="$(readlink -f "$dest")"
    source="$(readlink -f "$source")"

    # Copy
    copyContentsInternal "$(readlink -f "$source")" "$dest" "<assets>/$sourceN" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME$destN" || exit 1
}

# Sources from base folder
function from-cwd() {
    # Args
    local source="$1"
    local dest="$2"
    if [ "$source" == "" ]; then
        1>&2 echo "Error: missing argument for from-cwd: source: source files to copy"
        1>&2 echo "Usage: from-cwd <source> [<target>]"
        printStackTrace
        exit 1
    fi
    local destN="$dest"
    local sourceN="$source"
    if [ "$destN" != "" ]; then
        destN="/$destN"
    fi
    dest="$BUILD_TARGET/$dest"
    source="$BUILD_CWD/$source"
    if [ ! -d "$source" ] && [ ! -f "$source" ]; then
        1>&2 echo "Error: source path does not exist: $sourceN"
        printStackTrace
        exit 1
    fi
    if [ ! -d "$(dirname "$dest")" ]; then mkdir -p "$(dirname "$dest")" ; fi
    dest="$(readlink -f "$dest")"
    source="$(readlink -f "$source")"

    # Copy
    copyContentsInternal "$(readlink -f "$source")" "$dest" "<cwd>/$sourceN" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME$destN" || exit 1
}

# Sources from download
function from-url() {
    # Args
    local mode="$1"
    local emitmode="$2"
    local url="$3"
    local dest="$4"
    local destSearcher="$5"

    # Check args
    if [ "$mode" == "" ]; then
        1>&2 echo "Error: missing argument for from-url: mode: file store mode (raw/decompress-zip/decompress-targz)"
        1>&2 echo "Usage: from-url <store mode> <selection mode> <source url> [<target path>] [<subpath in downloaded data to use>]"
        printStackTrace
        exit 1
    fi
    if [ "$emitmode" == "" ]; then
        1>&2 echo "Error: missing argument for from-url: selection mode: file selection mode (auto/normal/firstdir)"
        1>&2 echo "Usage: from-url <store mode> <selection mode> <source url> [<target path>] [<subpath in downloaded data to use>]"
        printStackTrace
        exit 1
    fi
    if [ "$url" == "" ]; then
        1>&2 echo "Error: missing argument for from-url: url: source url"
        1>&2 echo "Usage: from-url <store mode> <selection mode> <source url> [<target path>] [<subpath in downloaded data to use>]"
        printStackTrace
        exit 1
    fi

    # Check mode
    if [ "$mode" != "raw" ] && [ "$mode" != "decompress-zip" ] && [ "$mode" != "decompress-targz" ]; then
        1>&2 echo "Error: invalid argument for from-url: mode: expected file store mode (raw/decompress-zip/decompress-targz)"
        1>&2 echo "Usage: from-url <store mode> <selection mode> <source url> [<target path>] [<subpath in downloaded data to use>]"
        printStackTrace
        exit 1
    fi
    if [ "$emitmode" != "auto" ] && [ "$emitmode" != "normal" ] && [ "$emitmode" != "firstdir" ]; then
        1>&2 echo "Error: missing argument for from-url: selection mode: file selection mode (auto/normal/firstdir)"
        1>&2 echo "Usage: from-url <store mode> <selection mode> <source url> [<target path>] [<subpath in downloaded data to use>]"
        printStackTrace
        exit 1
    fi

    # Process
    local destN="$dest"
    local sourceN="$source"
    if [ "$destN" != "" ]; then
        destN="/$destN"
    fi
    dest="$BUILD_TARGET/$dest"
    if [ ! -d "$(dirname "$dest")" ]; then mkdir -p "$(dirname "$dest")" ; fi
    dest="$(readlink -f "$dest")"

    # Download
    local downloadStoreFile="$BUILD_SOURCETEMP_DOWNLOADS/$(echo "$url" | sha256sum | sed "s/ .*//g")"
    if [ ! -f "$downloadStoreFile" ]; then
        echo "Downloading: $url into <downloadtemp>/$(basename "$downloadStoreFile")..."
        mkdir -p "$(dirname "$downloadStoreFile")"
        curl -fL "$url" --output "$downloadStoreFile.tmp" || exit 1
        mv "$downloadStoreFile.tmp" "$downloadStoreFile"
    fi

    # Handle result
    if [ "$mode" != "raw" ]; then
        # Handle decompression
        if [ "$mode" == "decompress-targz" ]; then
            # Decompress
            if [ ! -d "$downloadStoreFile.decompressed" ]; then
                mkdir "$downloadStoreFile.decompressed.tmp"
                echo Decompressing...
                (cd "$downloadStoreFile.decompressed.tmp" ; tar -xvf "$downloadStoreFile") || exit 1
                mv "$downloadStoreFile.decompressed.tmp" "$downloadStoreFile.decompressed"
            fi
            downloadStoreFile="$downloadStoreFile.decompressed"
        elif [ "$mode" == "decompress-zip" ]; then
            # Decompress
            if [ ! -d "$downloadStoreFile.decompressed" ]; then
                mkdir "$downloadStoreFile.decompressed.tmp"
                echo Decompressing...
                (cd "$downloadStoreFile.decompressed.tmp" ; unzip "$downloadStoreFile") || exit 1
                mv "$downloadStoreFile.decompressed.tmp" "$downloadStoreFile.decompressed"
            fi
            downloadStoreFile="$downloadStoreFile.decompressed"
        else
            crash unimplemented mode $mode
        fi

        # Select files
        if [ "$emitmode" != "normal" ]; then
            if [ "$emitmode" != "firstdir" ] && [ "$emitmode" != "auto" ]; then
                crash unimplemented emitmode $emitmode
            fi

            # Try firstdir mode
            local foundfirst=false
            local selectedpath=""
            for dir in "$downloadStoreFile"/*/; do
                # Check file
                if [ ! -d "$dir" ] && [ ! -f "$dir" ]; then continue; fi

                # Check selected
                if [ "$foundfirst" == true ]; then
                    # Check mode
                    if [ "$emitmode" != "auto" ]; then
                        # Unsupported operation
                        1>&2 echo "Error: decompressed folder contains more than one subdirectory"
                        1>&2 echo "Downloaded data folder: $downloadStoreFile"
                        printStackTrace
                        exit 1
                    fi

                    # Unset
                    selectedpath="$downloadStoreFile"
                    break
                fi

                # Select
                selectedpath="$dir"
                foundfirst=true
            done

            # Handle path
            if [ "$selectedpath" == "" ] && [ "$emitmode" != "auto" ]; then
                # None found
                1>&2 echo "Error: decompressed folder does not have subdirectories to select from"
                1>&2 echo "Downloaded data folder: $downloadStoreFile"
                printStackTrace
                exit 1
            elif [ "$selectedpath" != "" ]; then
                downloadStoreFile="$selectedpath"
            fi 
        fi

        # Navigate to subfolder
        if [ "$destSearcher" != "" ]; then
            downloadStoreFile="$downloadStoreFile/$destSearcher"
            if [ ! -f "$downloadStoreFile" ] && [ ! -d "$downloadStoreFile" ]; then
                # Not found
                1>&2 echo "Error: could not locate file or directory $destSearcher in decompressed download"
                1>&2 echo "Downloaded data folder: $downloadStoreFile"
                printStackTrace
                exit 1
            fi
        fi
    fi

    # Copy files
    copyContentsInternal "$(readlink -f "$downloadStoreFile")" "$dest" "$url" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME$destN" || exit 1
}

# From output
function from-target() {
    # Args
    local type="$1"
    local target="$2"
    local dest="$3"
    if [ "$type" == "" ]; then
        1>&2 echo "Error: missing argument for from-target: target type: base folder for targets"
        1>&2 echo "Usage: from-target <target type> <target name> [<target path>]"
        printStackTrace
        exit 1
    fi
    if [ "$target" == "" ]; then
        1>&2 echo "Error: missing argument for from-target: target name: build target name"
        1>&2 echo "Usage: from-target <target type> <target name> [<target path>]"
        printStackTrace
        exit 1
    fi
    
    # Process
    local source="$BUILD_TARGET_BASE_OUT/$type/$target"
    if [ ! -d "$source" ]; then
        # Invalid target
        1>&2 echo "Error: target data not found for build target $type -> $target"
        printStackTrace
        exit 1
    fi

    # Process destination
    local destN="$dest"
    if [ "$destN" != "" ]; then
        destN="/$destN"
    fi
    dest="$BUILD_TARGET/$dest"
    if [ ! -d "$(dirname "$dest")" ]; then mkdir -p "$(dirname "$dest")" ; fi

    # Prepare
    dest="$(readlink -f "$dest")"
    source="$(readlink -f "$source")"

    # Copy
    copyContentsInternal "$(readlink -f "$source")" "$dest" "<target>/$type/$target" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME$destN" || exit 1
}

# Require artifact
function require-artifact() {
    # Args
    local args=("$@")
    local target="$1"
    
    # Try to run
    local targetfile="$ARTIFACT_SCRIPTS_DIR/$target"
    if [ -f "$targetfile" ]; then
        # Encapsulate environment
        local ARTIFACT_SCRIPT_BAK="$ARTIFACT_SCRIPT"
        local ARTIFACT_SCRIPT_NAME_BAK="$ARTIFACT_SCRIPT_NAME"
        local BUILD_TARGET_BAK="$BUILD_TARGET"
        local BUILD_TARGETTYPE_NAME_BAK="$BUILD_TARGETTYPE_NAME"
        local BUILD_TARGET_NAME_BAK="$BUILD_TARGET_NAME"
        local BUILD_TEMP_BAK="$BUILD_TEMP"
        local BUILD_TEMP_DOWNLOADS_BAK="$BUILD_TEMP_DOWNLOADS"

        # Remember state
        local hadTarget=false
        if arrayContains "$(readlink -f "$targetfile")" CALLED_TARGETS; then
            hadTarget=true
        fi
        
        # Run task
        runTarget "$targetfile" "$target"
        local exit=$?

        # Restore env
        export ARTIFACT_SCRIPT="$ARTIFACT_SCRIPT_BAK"
        export ARTIFACT_SCRIPT_NAME="$ARTIFACT_SCRIPT_NAME_BAK"
        export BUILD_TARGET="$BUILD_TARGET_BAK"
        export BUILD_TARGET_NAME="$BUILD_TARGET_NAME_BAK"
        export BUILD_TARGETTYPE_NAME="$BUILD_TARGETTYPE_NAME_BAK"
        export BUILD_TEMP="$BUILD_TEMP_BAK"
        export BUILD_TEMP_DOWNLOADS="$BUILD_TEMP_DOWNLOADS_BAK"
        unset ARTIFACT_SCRIPT_BAK
        unset ARTIFACT_SCRIPT_NAME_BAK
        unset BUILD_TARGET_BAK
        unset BUILD_TARGET_NAME_BAK
        unset BUILD_TARGETTYPE_NAME_BAK
        unset BUILD_TEMP_BAK
        unset BUILD_TEMP_DOWNLOADS_BAK

        # Exit if needed
        if [ "$exit" != 0 ]; then exit $exit ; fi

        # Check
        if [ "$hadTarget" == false ]; then
            # Its been invoked, log return
            echo "> $ARTIFACT_SCRIPT_NAME: Build"
        fi
    else
        # Invalid target
        1>&2 echo "Error: target file not found: $target, make sure to use the fully qualified name relative to the 'artifacts' folder"
        printStackTrace
        exit 1
    fi
}

# Sources from existing artifact
function from-artifact() {
    # Args
    local args=("$@")
    local target="$1"
    
    # Try to run
    local targetfile="$ARTIFACT_SCRIPTS_DIR/$target"
    if [ -f "$targetfile" ]; then
        # Encapsulate environment
        local ARTIFACT_SCRIPT_BAK="$ARTIFACT_SCRIPT"
        local ARTIFACT_SCRIPT_NAME_BAK="$ARTIFACT_SCRIPT_NAME"
        local BUILD_TARGET_BAK="$BUILD_TARGET"
        local BUILD_TARGETTYPE_NAME_BAK="$BUILD_TARGETTYPE_NAME"
        local BUILD_TARGET_NAME_BAK="$BUILD_TARGET_NAME"
        local BUILD_TEMP_BAK="$BUILD_TEMP"
        local BUILD_TEMP_DOWNLOADS_BAK="$BUILD_TEMP_DOWNLOADS"

        # Remember state
        local hadTarget=false
        if arrayContains "$(readlink -f "$targetfile")" CALLED_TARGETS; then
            hadTarget=true
        fi
        
        # Run task
        runTarget "$targetfile" "$target"
        local exit=$?

        # Restore env
        export ARTIFACT_SCRIPT="$ARTIFACT_SCRIPT_BAK"
        export ARTIFACT_SCRIPT_NAME="$ARTIFACT_SCRIPT_NAME_BAK"
        export BUILD_TARGET="$BUILD_TARGET_BAK"
        export BUILD_TARGET_NAME="$BUILD_TARGET_NAME_BAK"
        export BUILD_TARGETTYPE_NAME="$BUILD_TARGETTYPE_NAME_BAK"
        export BUILD_TEMP="$BUILD_TEMP_BAK"
        export BUILD_TEMP_DOWNLOADS="$BUILD_TEMP_DOWNLOADS_BAK"
        unset ARTIFACT_SCRIPT_BAK
        unset ARTIFACT_SCRIPT_NAME_BAK
        unset BUILD_TARGET_BAK
        unset BUILD_TARGET_NAME_BAK
        unset BUILD_TARGETTYPE_NAME_BAK
        unset BUILD_TEMP_BAK
        unset BUILD_TEMP_DOWNLOADS_BAK

        # Exit if needed
        if [ "$exit" != 0 ]; then exit $exit ; fi

        # Check
        if [ "$hadTarget" == false ]; then
            # Its been invoked, log return
            echo "> $ARTIFACT_SCRIPT_NAME: Build"
        fi

        # Copy result target payload
        local targets=()
        arrayCopyOfRange args targets 1 "${#args[@]}"
        if [ "${#targets[@]}" == 0 ]; then
            # All targets
            local setId="${TARGET_SET_IDS["$(readlink -f "$targetfile")"]}"
            local targetPaths=()
            eval 'targetPaths=("${targetpaths_'"$setId"'[@]}")'

            # Copy all targets
            for targetPath in "${targetPaths[@]}"; do
                # Copy target
                copyContentsInternal "$BUILD_TARGET_BASE_OUT/$targetPath" "$BUILD_TARGET" "<targets>/$targetPath" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME"
            done
        else
            # Selective
            local i=0
            local skip=skip
            for arg in "${targets[@]}"; do
                if [ "$skip" == true ]; then
                    skip=false
                    i=$((i+1))
                    continue
                fi

                # Skip
                if ((i + 1 < ${#targets[@]})); then
                    local type="$arg"
                    local targetf="${targets[$((i + 1))]}"
                    
                    # Copy target
                    copyContentsInternal "$BUILD_TARGET_BASE_OUT/$type/$targetf" "$BUILD_TARGET" "<targets>/$type/$targetf" "$BUILD_TARGETTYPE_NAME/$BUILD_TARGET_NAME"
                fi
                i=$((i+1))
            done
        fi
    else
        # Invalid target
        1>&2 echo "Error: target file not found: $target, make sure to use the fully qualified name relative to the 'artifacts' folder"
        printStackTrace
        exit 1
    fi
}
