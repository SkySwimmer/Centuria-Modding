# Application
enabled="1"
executable_name="Fer.al.app"
target_assembly="FeralTweaks/FeralTweaksBootstrap.dll"
ignore_disable_switch="0"
coreclr_path="CoreCLR/libcoreclr"
corlib_dir="CoreCLR"


################################################################################
# Everything past this point is the actual script

# Special case: program is launched via Steam
# In that case rerun the script via their bootstrapper to ensure Steam overlay works
if [ "$2" = "SteamLaunch" ]; then
    steam="$1 $2 $3 $4 $0 $5"
    shift 5
    $steam "$@"
    exit
fi

# Handle first param being executable name
if [ -x "$1" ] ; then
    executable_name="$1"
    echo "Target executable: $1"
    shift
fi

if [ -z "${executable_name}" ] || [ ! -x "${executable_name}" ]; then
    echo "Please set executable_name to a valid name in a text editor or as the first command line parameter"
    exit 1
fi

# Use POSIX-compatible way to get the directory of the executable
a="/$0"; a=${a%/*}; a=${a#/}; a=${a:-.}; BASEDIR=$(cd "$a" || exit; pwd -P)

arch=""
executable_path=""
lib_extension=""

# Set executable path and the extension to use for the libdoorstop shared object
os_type="$(uname -s)"
case ${os_type} in
    Linux*)
        executable_path="${executable_name}"
        # Handle relative paths
        if ! echo "$executable_path" | grep "^/.*$"; then
            executable_path="${BASEDIR}/${executable_path}"
        fi
        lib_extension="so"
    ;;
    Darwin*)
        # Make sure that the game can run and wont be blocked by quarantine
        xattr -cr "${BASEDIR}"

        real_executable_name="${executable_name}"

        # Handle relative directories
        if ! echo "$real_executable_name" | grep "^/.*$"; then
            real_executable_name="${BASEDIR}/${real_executable_name}"
        fi

        # If were not even an actual executable, check .app Info for actual executable
        if ! echo "$real_executable_name" | grep "^.*\.app/Contents/MacOS/.*"; then
            # Add .app to the end if not given
            if ! echo "$real_executable_name" | grep "^.*\.app$"; then
                real_executable_name="${real_executable_name}.app"
            fi
            inner_executable_name=$(defaults read "${real_executable_name}/Contents/Info" CFBundleExecutable)
            executable_path="${real_executable_name}/Contents/MacOS/${inner_executable_name}"
        else
            executable_path="${executable_name}"
        fi
        lib_extension="dylib"
    ;;
    *)
        # alright whos running games on freebsd
        echo "Unknown operating system ($(uname -s))"
        echo "Make an issue at https://github.com/NeighTools/UnityDoorstop"
        exit 1
    ;;
esac

abs_path() {
    echo "$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
}

_readlink() {
    # relative links with readlink (without -f) do not preserve the path info
    ab_path="$(abs_path "$1")"
    link="$(readlink "${ab_path}")"
    case $link in
        /*);;
        *) link="$(dirname "$ab_path")/$link";;
    esac
    echo "$link"
}


resolve_executable_path () {
    e_path="$(abs_path "$1")"

    while [ -L "${e_path}" ]; do
        e_path=$(_readlink "${e_path}");
    done
    echo "${e_path}"
}

# Get absolute path of executable and show to user
executable_path=$(resolve_executable_path "${executable_path}")
echo "${executable_path}"

# Figure out the arch of the executable with file
file_out="$(LD_PRELOAD="" file -b "${executable_path}")"
case "${file_out}" in
    *64-bit*)
        arch="x64"
    ;;
    *32-bit*)
        arch="x86"
    ;;
    *)
        echo "The executable \"${executable_path}\" is not compiled for x86 or x64 (might be ARM?)"
        echo "If you think this is a mistake (or would like to encourage support for other architectures)"
        echo "Please make an issue at https://github.com/NeighTools/UnityDoorstop"
        echo "Got: ${file_out}"
        exit 1
    ;;
esac

# Helper to convert common boolean strings into just 0 and 1
doorstop_bool() {
    case "$1" in
        TRUE|true|t|T|1|Y|y|yes)
            echo "1"
        ;;
        FALSE|false|f|F|0|N|n|no)
            echo "0"
        ;;
    esac
}

# Read from command line
while :; do
    case "$1" in
        --doorstop_enabled)
            enabled="$(doorstop_bool "$2")"
            shift
        ;;
        --doorstop_target_assembly)
            target_assembly="$2"
            shift
        ;;
        --doorstop-mono-dll-search-path-override)
            dll_search_path_override="$2"
            shift
        ;;
        --doorstop-mono-debug-enabled)
            debug_enable="$(doorstop_bool "$2")"
            shift
        ;;
        --doorstop-mono-debug-suspend)
            debug_suspend="$(doorstop_bool "$2")"
            shift
        ;;
        --doorstop-mono-debug-address)
            debug_address="$2"
            shift
        ;;
        --doorstop-clr-runtime-coreclr-path)
            coreclr_path="$2"
            shift
        ;;
        --doorstop-clr-corlib-dir)
            corlib_dir="$2"
            shift
        ;;
        *)
            if [ -z "$1" ]; then
                break
            fi
            rest_args="$rest_args $1"
        ;;
    esac
    shift
done

# Move variables to environment
export DOORSTOP_ENABLED="$enabled"
export DOORSTOP_TARGET_ASSEMBLY="$target_assembly"
export DOORSTOP_IGNORE_DISABLED_ENV="$ignore_disable_switch"
export DOORSTOP_MONO_DLL_SEARCH_PATH_OVERRIDE="$dll_search_path_override"
export DOORSTOP_MONO_DEBUG_ENABLED="$debug_enable"
export DOORSTOP_MONO_DEBUG_ADDRESS="$debug_address"
export DOORSTOP_MONO_DEBUG_SUSPEND="$debug_suspend"
export DOORSTOP_CLR_RUNTIME_CORECLR_PATH="$coreclr_path.$lib_extension"
export DOORSTOP_CLR_CORLIB_DIR="$corlib_dir"

# Final setup
doorstop_directory="${BASEDIR}/"
doorstop_name="libdoorstop.${lib_extension}"

export LD_LIBRARY_PATH="${doorstop_directory}:${corlib_dir}:${LD_LIBRARY_PATH}"
if [ -z "$LD_PRELOAD" ]; then
    export LD_PRELOAD="${doorstop_name}"
else
    export LD_PRELOAD="${doorstop_name}:${LD_PRELOAD}"
fi

export DYLD_LIBRARY_PATH="${doorstop_directory}:${DYLD_LIBRARY_PATH}"
if [ -z "$DYLD_INSERT_LIBRARIES" ]; then
    export DYLD_INSERT_LIBRARIES="${doorstop_name}"
else
    export DYLD_INSERT_LIBRARIES="${doorstop_name}:${DYLD_INSERT_LIBRARIES}"
fi

# shellcheck disable=SC2086
chmod +x "$executable_path"
export DOORSTOP_EXECUTABLE_ARGS="$rest_args"
exec "$executable_path" $rest_args
