#!/usr/bin/env bash
#
# Runs TataruHelper on Linux, beside Final Fantasy XIV.
#
# TataruHelper is a Windows program. It reads the game's memory, so it has to
# run under the same Wine as the game, in the game's own prefix: Wine lets one
# program read another's memory only when both belong to the same wineserver.
# This script finds the running game, takes its Wine and its Wine settings, and
# starts TataruHelper with them.
#
#   tataru-linux.sh install     download the latest release and add a menu entry
#   tataru-linux.sh             start TataruHelper beside the running game
#   tataru-linux.sh update      download the latest release again
#   tataru-linux.sh uninstall   remove what install added
#
# Tested with XIVLauncher-RB (native, not Flatpak) on CachyOS with KDE Plasma
# on Wayland. See Documents/Linux.md in the repository.

set -euo pipefail

REPO="NightlyRevenger/TataruHelper"
PORTABLE_URL="https://github.com/$REPO/releases/latest/download/Portable.zip"
ICON_URL="https://raw.githubusercontent.com/$REPO/master/Documents/Tataru_img.png"
SCRIPT_URL="https://raw.githubusercontent.com/$REPO/master/scripts/linux/tataru-linux.sh"

DIR="${TATARU_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/tataruhelper-linux}"
APP="$DIR/app"
EXE="$APP/current/TataruHelper.exe"
MENU_ENTRY="${XDG_DATA_HOME:-$HOME/.local/share}/applications/tataruhelper-linux.desktop"

# A download in progress, removed however the script ends.
DOWNLOADING=""
trap 'if [ -n "$DOWNLOADING" ]; then rm -rf "$DOWNLOADING"; fi' EXIT

# How long to wait for the game when it is not running yet - long enough to
# start TataruHelper first and then press Play in the launcher.
WAIT_SECONDS="${TATARU_WAIT:-300}"

say() {
    # From the application menu there is no terminal to print to, so the
    # desktop's notifications are used instead when they are there.
    if [ -t 1 ] || ! command -v notify-send >/dev/null 2>&1; then
        printf '%s\n' "$*"
    else
        notify-send --app-name=TataruHelper "TataruHelper" "$*" || printf '%s\n' "$*"
    fi
}

fail() {
    say "$*"
    exit 1
}

env_of() {
    # One variable from another process's environment, or nothing.
    tr '\0' '\n' < "/proc/$1/environ" 2>/dev/null | grep -m1 "^$2=" | cut -d= -f2- || true
}

find_game() {
    local pid
    for pid in $(pgrep -f 'ffxiv_dx11\.exe' || true); do
        if [ -n "$(env_of "$pid" WINEPREFIX)" ]; then
            echo "$pid"
            return 0
        fi
    done
    return 1
}

# The Wine the game runs on: the one whose wineserver serves the game's prefix.
# Any other Wine, even a newer one, would start a second wineserver that cannot
# see the game - or refuse to start at all over a version mismatch.
find_wine() {
    local game="$1" prefix pid exe dir candidate
    prefix="$(env_of "$game" WINEPREFIX)"

    for pid in $(pgrep -x wineserver || true); do
        [ "$(env_of "$pid" WINEPREFIX)" = "$prefix" ] || continue
        exe="$(readlink -f "/proc/$pid/exe" 2>/dev/null || true)"
        dir="$(dirname "$exe")"
        for candidate in wine wine64; do
            if [ -x "$dir/$candidate" ]; then
                echo "$dir/$candidate"
                return 0
            fi
        done
    done

    # No wineserver with the prefix in its environment: fall back to the game's
    # own loader, which lives beside the wine binary.
    exe="$(readlink -f "/proc/$game/exe" 2>/dev/null || true)"
    dir="$(dirname "$exe")"
    for candidate in wine wine64; do
        if [ -x "$dir/$candidate" ]; then
            echo "$dir/$candidate"
            return 0
        fi
    done
    return 1
}

# Wine's settings as the game has them. The sync mode in particular has to
# match, or the new process cannot talk to the running wineserver. Three are
# left out: they describe the game's own process, not the prefix.
game_environment() {
    tr '\0' '\n' < "/proc/$1/environ" \
        | grep -E '^(WINE[A-Z_]*|DXVK_[A-Z_]*|VKD3D_[A-Z_]*|LD_LIBRARY_PATH)=' \
        | grep -Ev '^(WINESERVERSOCKET|WINELOADERNOEXEC|WINEPRELOADRESERVE|WINELOADER)=' \
        || true
}

unpack() {
    local zip="$1" into="$2"
    if command -v unzip >/dev/null 2>&1; then
        unzip -q "$zip" -d "$into"
    elif command -v bsdtar >/dev/null 2>&1; then
        bsdtar -xf "$zip" -C "$into"
    elif command -v python3 >/dev/null 2>&1; then
        python3 -m zipfile -e "$zip" "$into"
    else
        fail "Need one of: unzip, bsdtar, python3 - to unpack the download."
    fi
}

download_release() {
    command -v curl >/dev/null 2>&1 || fail "Need curl to download TataruHelper."
    mkdir -p "$DIR"

    local tmp
    tmp="$(mktemp -d "$DIR/.download.XXXXXX")"
    DOWNLOADING="$tmp"

    echo "Downloading the latest TataruHelper..."
    curl -fL --progress-bar -o "$tmp/Portable.zip" "$PORTABLE_URL" \
        || fail "Download failed: $PORTABLE_URL"

    mkdir -p "$tmp/app"
    unpack "$tmp/Portable.zip" "$tmp/app"
    [ -f "$tmp/app/current/TataruHelper.exe" ] || fail "The download does not look like TataruHelper."

    # Settings do not live here - they are in the game's prefix - so the old
    # copy can simply be replaced.
    rm -rf "$APP"
    mv "$tmp/app" "$APP"
    rm -rf "$tmp"
    DOWNLOADING=""
}

add_menu_entry() {
    local self="$DIR/tataru-linux.sh"
    # The menu entry points at a copy kept beside the program. When this script
    # was not run from a file - piped into bash - there is nothing to copy, and
    # it is fetched instead.
    if [ -f "$0" ] && grep -q 'Runs TataruHelper on Linux' "$0" 2>/dev/null; then
        if [ "$(readlink -f "$0")" != "$(readlink -f "$self" 2>/dev/null || true)" ]; then
            cp "$0" "$self"
        fi
    else
        curl -fsL -o "$self" "$SCRIPT_URL" || fail "Could not fetch the script for the menu entry."
    fi
    chmod +x "$self"

    curl -fsL -o "$DIR/tataru.png" "$ICON_URL" || true

    mkdir -p "$(dirname "$MENU_ENTRY")"
    cat > "$MENU_ENTRY" <<EOF
[Desktop Entry]
Type=Application
Name=TataruHelper
Comment=Translate Final Fantasy XIV dialogue (start the game first, or right after)
Exec="$self" run
Icon=$DIR/tataru.png
Terminal=false
Categories=Game;Utility;
EOF
}

is_running() {
    pgrep -f 'current[\\/]TataruHelper\.exe' >/dev/null 2>&1
}

run() {
    [ -f "$EXE" ] || fail "TataruHelper is not installed yet. Run: $0 install"

    if is_running; then
        say "TataruHelper is already running."
        return 0
    fi

    local game="" waited=0
    if ! game="$(find_game)"; then
        say "Waiting for Final Fantasy XIV to start..."
        while ! game="$(find_game)"; do
            [ "$waited" -lt "$WAIT_SECONDS" ] || fail "Final Fantasy XIV is not running. Start the game, then TataruHelper."
            sleep 2
            waited=$((waited + 2))
        done
        # The game's process is up before its window and its own setup are;
        # starting beside it too early has the two race for the prefix.
        sleep 10
    fi

    if [ -e "/proc/$game/root/.flatpak-info" ]; then
        fail "The game runs in Flatpak, which this script cannot reach yet. See Documents/Linux.md."
    fi

    local wine
    wine="$(find_wine "$game")" || fail "Could not find the Wine the game is running on."

    local -a vars=()
    mapfile -t vars < <(game_environment "$game")

    # Detached before this script returns, not in the background of it: a
    # terminal that closes as soon as the script is done hangs up its whole
    # process group, and a backgrounded start had not left it yet.
    (
        cd "$APP/current"
        setsid -f env "${vars[@]}" "$wine" "$EXE" > "$DIR/wine.log" 2>&1 < /dev/null
    )

    say "TataruHelper started beside the game (Wine: $wine)."
}

uninstall() {
    if is_running; then
        fail "Close TataruHelper first, then uninstall."
    fi
    rm -f "$MENU_ENTRY"
    rm -rf "$DIR"
    echo "Removed $DIR and the menu entry."
    echo "Settings stay in the game's Wine prefix, under drive_c/users/$USER/AppData/Roaming/TataruHelper."
}

case "${1:-run}" in
    install)
        download_release
        add_menu_entry
        echo
        echo "Installed to $DIR."
        echo "Start the game, then start TataruHelper from the application menu or with:"
        echo "  $DIR/tataru-linux.sh"
        ;;
    update)
        if is_running; then
            fail "Close TataruHelper first, then update."
        fi
        download_release
        [ -f "$MENU_ENTRY" ] && add_menu_entry
        echo "Updated."
        ;;
    run)
        run
        ;;
    uninstall)
        uninstall
        ;;
    -h|--help|help)
        sed -n '3,17p' "$0" | sed 's/^# \{0,1\}//'
        ;;
    *)
        fail "Unknown command: $1 (try: install, update, uninstall, or nothing to start)"
        ;;
esac
