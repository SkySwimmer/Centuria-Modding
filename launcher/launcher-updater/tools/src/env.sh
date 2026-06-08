#!/bin/bash

# Root folder
SCRIPTROOT="$artifacttoolbasedir"
SOURCEBASEDIR="$artifacttoolbasedir/src"

# Statements
declare -A TARGET_SET_IDS=()

# Load OOP
source "$artifacttoolbasedir/src/util/oop/oopinit.sh"
sourceAll "$artifacttoolbasedir/src/util/oop"

# Load util sources
sourceAll "$artifacttoolbasedir/src/util"
