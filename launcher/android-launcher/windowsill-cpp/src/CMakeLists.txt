cmake_minimum_required(VERSION 3.4.1)

add_library (
    windowsill

    SHARED

    wmni.cpp
)

include_directories (
    includes
    includes/mono
    includes/mono/jit
    includes/mono/metadata
    includes/mono/utils
)
