#!/bin/bash

# Requirement
if [ "${#OOPBASE_sourcelist_base[@]}" == 0 ]; then
    declare -g OOPBASE_sourcelist_base=()
fi
function arrayContains() {
    local key="$1"
    local array="$2"
    eval 'local arrIn=("${'"$array"'[@]}")'

    # Check
    for entry in "${arrIn[@]}"; do
        if [ "$entry" == "$key" ]; then
            return 0
        fi
    done
    return 1
}

# Import
function sourceAll() {
    local dir="$1"
    for subdir in "$dir"/*/; do
        if [ -d "$subdir" ]; then
            sourceAll "$subdir"
        fi
    done
    for file in "$dir"/*; do
        if [ -f "$file" ]; then
            file="$(readlink -f "$file")"
            if ! arrayContains "$file" OOPBASE_sourcelist_base; then
                OOPBASE_sourcelist_base+=("$file")
                source "$file"
            fi
        fi
    done
}
