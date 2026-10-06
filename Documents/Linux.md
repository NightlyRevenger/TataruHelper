# TataruHelper on Linux

TataruHelper is a Windows program, and on Linux it runs under Wine, beside the game. It reads the game's memory, and Wine allows that only between programs that share one wineserver, so it has to run with the game's own Wine and in the game's own prefix. A script in this repository does that for you.

**Status: experimental.** Tested with XIVLauncher-RB (the native build, not Flatpak) on CachyOS with KDE Plasma on Wayland. If it works for you on another setup, or doesn't, tell us in [Discord](https://discord.gg/bSrpbd9) — that is how this list grows.

## What works

* Reading the game: dialogue, cutscenes, chat, your character's name.
* Translation into the chat window, including the hand-made Russian translations.
* The chat window stays above the game, and you can move it and resize it by its edges.
* Settings, translation engines and API keys.

## Not yet

* **Selecting and copying text in the chat window** doesn't work under Wine. The window must not take focus from the game. Otherwise it would sink under the game, and the game would get stray clicks.
* **Translation over the game's dialogue box** has not been tried under Wine.
* **Hotkeys and the tray icon** have not been tried under Wine.
* **XIVLauncher from Flatpak** is not supported by the script yet. The game runs inside the Flatpak sandbox, where the script can't reach its Wine.
* **Steam or other launchers** may work, since the script uses whatever Wine the running game uses, but they have not been tested.

## Install

You need `curl`, and one of `unzip`, `bsdtar` or `python3`; most distributions have them.

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

This downloads the latest release into `~/.local/share/tataruhelper-linux` and adds **TataruHelper** to your application menu. The downloaded `tataru-linux.sh` can be deleted afterwards; a copy is kept beside the program.

## Run

1. Start the game through your launcher as usual.
2. Start **TataruHelper** from the application menu.

You can also start TataruHelper first: it waits up to five minutes for the game. From a terminal, the same is:

```sh
~/.local/share/tataruhelper-linux/tataru-linux.sh
```

The first start looks like a first start on Windows: pick the languages and the translation engine in the main window, and in the game turn on the chat channels you want translated (see the [Guide](Guide.MD)).

## Update

Close TataruHelper, then:

```sh
~/.local/share/tataruhelper-linux/tataru-linux.sh update
```

Your settings are kept: they live in the game's prefix, not in the program folder.

TataruHelper can also update itself, as on Windows: when a new version is out, **Update available** appears in the main window, and clicking it installs the update and restarts the program beside the game.

## Remove

```sh
~/.local/share/tataruhelper-linux/tataru-linux.sh uninstall
```

This removes the program and the menu entry. Settings stay in the game's prefix, in `drive_c/users/<you>/AppData/Roaming/TataruHelper`; delete that folder too if you want them gone.

## When something goes wrong

* **"Final Fantasy XIV is not running"** — the script looks for `ffxiv_dx11.exe`. Make sure you are in the game, past the launcher.
* **"Could not find the Wine the game is running on"** — tell us your launcher and distribution; the script needs to learn where that launcher keeps its Wine.
* **Nothing happens** — Wine's own output is in `~/.local/share/tataruhelper-linux/wine.log`.
* **Translation problems** — TataruHelper's own log is in the game's prefix: `drive_c/users/<you>/AppData/Roaming/TataruHelper/Log.txt`. The first line after a start says `Running under Wine`. The diagnostics button in the settings works as on Windows.

Where the prefix is depends on the launcher. For XIVLauncher it is `~/.local/share/dev.goats.xivlauncher/wineprefix` (or `~/.xlcore/wineprefix` in older versions).
