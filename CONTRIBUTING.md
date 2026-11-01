# Contributing to Tataru Helper

Thank you for wanting to help. Small fixes and big features are equally welcome. For a larger change, describe the idea in an [issue](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose) or in [Discord](https://discord.gg/bSrpbd9) first, so the work is not wasted.

By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Other ways to help

- **Translating the interface:** use [Crowdin](https://crowdin.com/project/tataru-helper). Do not edit the `.po` files in `TataruHelper/Locale` by hand: they are overwritten by Crowdin.
- **Reporting a problem:** [open an issue](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose) with the version, the system and the diagnostics archive from **General → Collect diagnostics**.
- **Documentation:** the [Guide](Documents/Guide.MD) and the [README](README.md) are code too; corrections are welcome. The READMEs in other languages are translated from the English one.

## Building

You need Windows or Linux with the **.NET 10 SDK** (the exact version is in [global.json](global.json); newer feature bands are accepted). The application is WPF, so it runs on Windows, or on Linux under Wine; building and testing work on both.

```sh
dotnet build TataruHelper.sln
dotnet test Translation.Tests
```

The other test projects are `TataruHelper.Tests` and `Updater.Tests`. A few tests depend on the operating system; on Linux `AnEnvironmentVariable_IsExpanded` is known to fail.

The DeepL relay is a separate small Go program in [tools/deepl-relay](tools/deepl-relay). You need **Go 1.27 or newer** only to change it; the application itself does not.

For reading the game's memory see [Reading the game](Documents/ReadingTheGame.md).

## Making a change

1. Fork the repository and make a branch from `master`.
2. Keep to the style of the code around your change: naming, comments and how much of them.
3. Add or update a test when there is a place for one. Translation engines have parsing tests in `Translation.Tests` that run against saved responses, without the network.
4. Run the tests and, for anything visible, start the application and look.
5. Open a pull request. Describe what was wrong or missing and how you checked it.

### Commits

- One thought per commit; a commit should build and pass the tests.
- A short first line saying what changed, with the reason in the body if it is not obvious.
- No generated attribution lines.

### What makes a pull request easy to accept

- It does one thing.
- It does not reformat or rename code it has no reason to touch.
- It does not add a dependency without a reason that is written down.
- Text in the interface goes through the translation dictionary (`TataruHelper/TextDictionary.xaml`), not into the code.

## Releases

Maintainers publish releases. A version is a tag `vX.Y.Z`; the release carries `Setup.exe`, the Velopack packages and `Portable.zip`, and installed copies update themselves.
