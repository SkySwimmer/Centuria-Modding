#!/bin/bash

function createFolder() {
    # Check
    if [ "$1" == "" ]; then
        1>&2 echo "Error: missing argument 'folder' in createFolder"
        printStackTrace 1
        exit 1
    fi
    
    if [ ! -d "$1" ]; then
        mkdir -p "$1"
    fi
}
