# After a game patch: what to do when the reading breaks

A checklist for the day after FFXIV updates and the translation misbehaves.
[Reading the game](ReadingTheGame.md) explains why the reader is built the way
it is; this is only what to do, in the order that costs least. Dates and client
builds are kept so a claim can be checked against a client of that vintage.

## First: is it the patch?

Ask before anything else. A game that updated overnight and an application that
did not are the usual pair, but the same symptoms come from the game still
starting up, from the wrong process being attached, and from the game not
running at all - the application reads the game's memory and has nothing to
read without it.

The work can be done from either system. On Windows the application runs
natively beside the game. On Linux it runs under the game's own Wine prefix, so
the game has to be running there too (`scripts/linux/tataru-linux.sh`, see
[Linux](Linux.md)). The offsets are the same on both; only the place to run
the check and to find the logs differs.

If the game has been in the world for a minute and the symptom stays, carry on.

## Read the symptom

The failures do not look alike, and the first one does not look like a failure.

| what you see | what is down | where to look |
|---|---|---|
| Translation arrives one line behind, or every other line, and only once the player clicks through. Chat is fine | the screen cannot be read; the chat log is carrying the conversation alone | `RaptureAtkModule` offset, `TryFindWindowList` |
| Nothing at all, chat included | the `CHATLOG` signature - everything else is found relative to it | Sharlayan's signatures |
| Dialogue works, cutscene subtitles do not | `TalkSubtitleTextOffset` (`0x238`, checked against 2026.07.16) | `TalkAddonRealtimeReader.cs` |
| Lines work, but bubbles and subtitles carry no speaker | the `LastTalk` pair in `UIModule` | `ResolveUiDirectDialogOffsets` |
| Garbled or cut-off text | `AtkTextNode.NodeText` or the `Utf8String` layout | same |

The first row is the one to expect. It is quiet by design: nothing announces
that the screen has gone unreadable, because the chat log goes on working.

## Collect before touching the code

1. **The diagnostics button** on the General page ("Collect diagnostics") puts a
   report on the clipboard and writes a copy beside the log. Ask a player who
   reports a problem for exactly this.
2. **`Log.txt`**, in `%APPDATA%/TataruHelper` on Windows; on Linux the same
   folder inside the game's prefix
   (`drive_c/users/<you>/AppData/Roaming/TataruHelper`). Two lines matter:
   - `Dialogue cannot be read off the screen: <why>. The chat log will still be
     read, a line behind.` - a field this reader asks for by name is gone from
     the FFXIVClientStructs types, and `<why>` names it. That is the whole
     diagnosis; the rest is finding what it was renamed to.
   - `The game's window list is not where the bundled offsets say.
     RaptureAtkModule found at UIModule+0x...` - not a fault. The reader has
     searched and found its footing on its own; the line is there to be noted.
3. **`--log-raw-dialog`**, as a start-up argument, writes
   `%APPDATA%/TataruHelper/RealtimeRawLog.txt`. Look first at `LoadedAddons=`:
   if `Talk` and `TalkSubtitle` are in it, the window list is reached and the
   fault is further in. If the list is empty or the line never appears, the road
   to the windows is down. `WindowList settled at UIModule+0x...` is the same
   news as the line in `Log.txt`.

## Fix, in this order

Everything here is tied to a build of the client, and none of it is fetched at
run time: the offsets are exactly as old as the Sharlayan package pinned in
`Directory.Packages.props`.

1. **Test on a live client before anything else.** FFXIVClientStructs does not
   update the day a patch lands, so for a while it describes the previous build.
   Waiting for it is waiting for the wrong thing. After patch 7.56 (client
   2026.09.01) the reader was fixed here on 2026-09-08; the Sharlayan release
   carrying it came on 2026-09-10.
2. **Check whether a newer Sharlayan is out.** Look on NuGet, raise the version in
   `Directory.Packages.props`, run the tests, and check against a live client.
   This is the cheapest cure when it exists. It does not always exist yet.
3. **Compare with FFXIVClientStructs** - the commits and pull requests made for
   the patch, once they appear. This is for hints, to see which structures moved
   and by how much, not a source to wait for. A shift in `AtkUnitBase` moves every
   window at once; a shift in `UIModule` moves the road to them.
4. **Fix it by hand.** Two precedents, for two different stakes (both in
   `TalkAddonRealtimeReader.cs`, written down in ReadingTheGame.md under
   "When a patch moves the ground"):
   - *What nothing works without gets a search.* `RaptureAtkModule` is tried at
     the described offset and then at nearby ones, nearest first, up to 0x2000
     bytes either way; a candidate is believed only when a good share of the
     windows it claims carry plausible names.
   - *What costs little gets a correction.* The `LastTalk` pair was moved by hand,
     guarded by the exact stale layout, so a Sharlayan that already carries the
     new offsets steps past the correction. A guard of this kind is what keeps a
     later package update from being broken by the fix for this one.

   To find an offset on a live client when neither exists: take a line the game
   has on screen, find the `Utf8String` holding it, and work back from there to
   the node and the window that own it, checking each step against the fields in
   FFXIVClientStructs. As the reader sees a `Utf8String`: a pointer at `+0x00`, the
   length at `+0x18`, an inline-buffer flag at `+0x21` and the inline buffer itself
   at `+0x22` (`Utf8StringInlineBufferOffset` and its neighbours).

## After it works

- Build and run the tests (`dotnet build TataruHelper.sln`, `dotnet test` on the
  test projects; see [CONTRIBUTING](../CONTRIBUTING.md)). It works the same on
  Windows and Linux; on Linux `AnEnvironmentVariable_IsExpanded` is a known
  failure. Then start the application against a live game and look.
- Write the finding into "When a patch moves the ground" in
  [Reading the game](ReadingTheGame.md): the patch, the client build, what moved
  and by how much. The next person will start from there.
- If the fix is a value picked by hand, put the build it was checked against
  beside it in a comment, as `TalkSubtitleTextOffset` does.
