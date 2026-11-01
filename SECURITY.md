# Security policy

## Supported versions

Only the [latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest) receives fixes. Installed copies update themselves, so this is usually the version you have.

## Reporting a vulnerability

Please do not report a vulnerability in a public issue.

Use GitHub's private reporting: open the [Security tab](https://github.com/NightlyRevenger/TataruHelper/security/advisories/new) of the repository and choose **Report a vulnerability**. If that is not possible, send a private message to a maintainer in [Discord](https://discord.gg/bSrpbd9) without the details and ask for a way to pass them on.

Please say which version you tested, what you did and what happened. You can expect an answer within a week and a fix, or a reason why there is none, as soon as it can be done.

## What the application does

So you know what to expect from it:

- It asks for administrator rights because it reads the game's text out of the game's memory. It does not change the game's memory or files.
- It sends the lines it translates to the translation service you chose, and to nothing else. With the engines that need no key this includes services reached through the bundled local DeepL relay, which listens on `127.0.0.1` only.
- API keys are kept in the settings file on your computer.
- Updates are downloaded from this repository's releases.
