#!/bin/bash
chmod +x "$0"
cd "$(dirname "$0")"
export CENTURIA_LAUNCHER_PATH="$PWD/$(basename "$0")"
libs=$(find libs/ -name '*.jar' -exec echo -n :{} \;)
libs=$libs:$(find . -maxdepth 1 -name '*.jar' -exec echo -n :{} \;)
libs=${libs:1}

case "$(uname -s)" in
    Linux*)
    linux/java-17/bin/java -cp "$libs" org.asf.centuria.launcher.updater.LauncherUpdaterMain "$@" &
    ;;
    Darwin*)
    osx/java-17/bin/java -cp "$libs" org.asf.centuria.launcher.updater.LauncherUpdaterMain "$@" &
    ;;
esac
