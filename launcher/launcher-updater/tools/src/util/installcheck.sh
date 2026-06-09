#!/bin/bash

function checkInstalled() {
    # Check git
    if ! command -v "$1" &> /dev/null; then
        return 1
    fi

    # Present
    return 0
}