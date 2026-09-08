# How a line of dialogue reaches the screen

Notes for whoever has to change this next. Everything here was arrived at by
watching the game, usually after something came out wrong; the dates are kept
so a claim can be re-checked against a client of that vintage.

## Two roads

A line reaches the application twice, by two independent roads.

**Off the screen.** `TalkAddonRealtimeReader` walks the game's list of loaded
UI windows every 50 ms and reads the text out of the ones that hold dialogue:

| addon | what it holds | names the speaker |
|---|---|---|
| `Talk` | the ordinary dialogue box | yes, in its first text node |
| `TalkSubtitle` | cutscene subtitles, drawn bare over the picture | no |
| `MiniTalk`, `_MiniTalk` | speech bubbles over characters' heads | no |

**From the chat log.** The game writes most dialogue into its own log, which
Sharlayan reads. This is slower for a line the player clicks through, and
*faster* in a duty - measured at some 40 ms ahead of the screen on
2026-08-24.

Which road wins is not fixed. Anything that assumes one of them arrives first
will be wrong half the time; that assumption is the single most common cause
of a line appearing twice.

**And a third road that is not one.** `UIModule` carries a `LastTalk` pair -
the name and text of the last line said - and it is tempting, because it is a
single pair of reads and always has something in it. That is exactly what is
wrong with it: it is not a record of what is on screen, and the game keeps it
long after the conversation is over. A line has been read out of it twenty
minutes late.

It was the reader's fallback until 2026-09-08 and it cost more than the stale
lines it produced. Because it always answered, the screen never once looked
empty, so everything waiting for the screen to clear waited forever: an NPC
greeted twice running was taken for the first greeting still standing there
and swallowed, and a line the pair had not caught up to was announced again
when the window closed. It survived as long as it did because it only becomes
load-bearing when the addon road fails - which is what patch 7.56 did to it.

`LastTalk` is now read for one purpose only: naming the speaker of a bubble or
a subtitle, neither of which names one itself, and then only when its text is
the very line the addon is showing.

## Choosing what is being said

`Talk` keeps the speaker in the first text node and the line in the second.
Do not pick "the longest node": a name and a line are often the same length -
"Short-tempered Thaumaturge" and "Is this our dark stranger?" are both 26
characters.

The bubble addons are different again: they hold **every bubble on screen at
once** - five, in a duty - and go on holding them long after the character has
stopped speaking. Length says nothing about which is being said. `SpeechBubbles`
answers with the bubble that **was not there on the previous sweep**, and with
nothing when none of them is new.

Two consequences worth knowing:

- A bubble that never leaves the addon's buffer can never be announced again.
  If the game shows the same words again without the old copy going away, the
  two are indistinguishable.
- The first sighting of a bubble after attaching may be something said before
  the application started. It is announced anyway: losing a real line is worse
  than one stale line once.

`TalkSubtitle` has no type in FFXIVClientStructs, so its text offset is the one
value in the reader derived by hand from a running client (`0x238`, checked
against 2026.07.16). If cutscene subtitles stop appearing after a game patch,
check that first.

## When a patch moves the ground

If **nothing at all** is read off the screen after a game patch, the offsets
into `UIModule` are the place to look, not the addons. `ResolveUiDirectDialogOffsets`
takes them from the FFXIVClientStructs metadata bundled with Sharlayan, which
lags the live client by however long it takes that project to publish.

Patch 7.56 (client 2026.09.01) moved `RaptureAtkModule` on 0x20 and the
`LastTalk` pair on 0x60. Read at the stale offsets, `AtkUnitManager` comes back
a null pointer, the list of loaded windows is never reached, and every road but
the chat log goes quiet. The correction in the reader is pinned to the exact
stale layout, so a Sharlayan carrying 7.56 offsets steps past it rather than
having correct values shifted out from under it.

The symptom to recognise, because it does not look like "nothing works": the
translation appears one line behind, or every other line. That is the chat log
carrying the conversation on its own.

## Not saying it twice

`RecentUtterance` is the memory both roads report to and both consult. A second
arrival is an echo when all three hold:

1. the words match, compared with the speaker stripped off and case and spacing
   collapsed (`BuildDuplicateKey`);
2. **one of the two names nobody** - the subtitle strip and the bubbles do not
   name a speaker, the chat log does. Two *different* names are two characters:
   Cid and Yda can both say "Understood." in the same breath and both must be
   shown;
3. they are less than **two seconds** apart. Measured: the two roads were 0.04
   to 1 second apart, and a line genuinely said again came 33 seconds later.

The memory holds the last sixteen lines, not one - a duty can put five on
screen inside a second.

Do **not** clear this memory when the screen goes empty. That was tried; the
chat log records its copy while nothing is on screen yet and the screen's copy
follows 40 ms later, so the clearing landed exactly between them, every time.
The two seconds are what handles an NPC repeating a bubble as you walk past.

The two seconds have a price, and it is the right one to pay. Clicking through
the same NPC faster than that - the same words, the same name, twice inside a
breath - is counted once and translated once. It cannot be told apart from the
window blinking mid-line, which is a thing that happens on its own and would
otherwise double every line. The game's own log does know the difference,
because it writes a record per utterance; building the guard on that was tried
on 2026-09-08 and made the common case fragile to fix the rare one.

## Three guards, not one

Saying a line once is enforced in three places, and they are not
interchangeable. Anyone tempted to fold them together should know what each
covers before removing any of it.

| where | keyed on | window | covers |
|---|---|---|---|
| `RecentUtterance` | the words, speaker dropped, plus the "one side names nobody" rule | 2 s, last 16 | both roads, dialogue only |
| `IsDuplicateOfRealtimeLine` | the words, speaker dropped | last 64, no clock | the chat log's copy of what the screen showed |
| `ShouldSuppressAsDuplicate` | the code and the **whole line, speaker included** | 2 s | every channel, the last gate before translation |

The third is keyed most strictly and therefore catches least: "Cassard:Well
met" and "Well met" are two different keys to it, which is why it never caught
the duty duplicates. What it does cover is everything the other two do not -
ordinary chat, party, linkshells - where a line has no second road to arrive
by and an exact repeat is the only duplicate there is.

The second has no clock at all: sixty-four lines deep, however long ago. That
is deliberate. It answers "did the screen already show this?", and the chat log
can lag a long way behind on a line the player leaves on screen.

So: a duplicate across the two roads is the first's business, a late chat-log
copy is the second's, and an exact repeat on any channel is the third's. Three
questions that look alike and are not.

## Where each decision can be watched

Run with `--log-raw-dialog` and the reader writes to
`%APPDATA%/TataruHelper/RealtimeRawLog.txt`, one line per change of state
rather than one per sweep:

```
LoadedAddons=...                     every UI window the game has open
Addon=[Talk] code=[003D] nodes={...} what an addon's text nodes hold
Bubbles on screen={...} spoken=[...] every bubble, and which was chosen
Emit code=[...] echo=[...] line=[...] a line going out, and whether it was an echo
ChatLog code=[...] seenLive=[...]     the chat log's copy, and whether it was dropped
NodeGeometry addon=[...] ...          where each node is drawn, for the dialogue copy
DialogueOverlay ...                   why the copy of the dialogue box is or is not shown
```

`Bubbles on screen` is logged **before** the choice is narrowed down. Logging
only the chosen one hides what there was to choose from, which is the whole of
what has to be seen to judge the choosing.

## Translating it

A line is looked up in the index of hand-made translations first
(`ReferenceTranslations.db`, built from XIV Rus Translation) and handed to an
engine only when it is not there. A line the index does not have is not a
fault: not everything upstream is translated. Before assuming a lookup failure,
check for the exact line and for anything close - `pattern` and `item_pattern`
as well as `line`.

The application reads whichever index was **built later**, the one shipped with
the release or the one the user fetched, so a fresh install does not read an
older copy than it ships with.
