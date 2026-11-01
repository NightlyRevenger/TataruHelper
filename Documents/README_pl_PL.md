<p align="center">
  <img src="Tataru_img.png" width="112" alt="Tataru Helper" />
</p>

<h1 align="center">Tataru Helper</h1>

<p align="center">Nakładka tłumacząca na żywo Final Fantasy XIV: fabuła, przerywniki, zadania, kwestie NPC i czat.</p>
<p align="center">Rozwijany przez [xDarkOne](https://github.com/xDarkOne). Oryginalnie stworzony przez [NightlyRevenger](https://github.com/NightlyRevenger).</p>

<p align="center">
  <a href="https://github.com/NightlyRevenger/TataruHelper/actions/workflows/build.yml"><img src="https://github.com/NightlyRevenger/TataruHelper/actions/workflows/build.yml/badge.svg" alt="Build" /></a>
  <a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest"><img src="https://img.shields.io/github/v/release/NightlyRevenger/TataruHelper?label=Release" alt="Release" /></a>
  <img src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux-lightgrey" alt="Windows, Linux" />
  <img src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <a href="../LICENSE"><img src="https://img.shields.io/github/license/NightlyRevenger/TataruHelper" alt="License" /></a>
  <a href="https://github.com/NightlyRevenger/TataruHelper/releases"><img src="https://img.shields.io/github/downloads/NightlyRevenger/TataruHelper/total?label=Downloads" alt="Downloads" /></a>
  <a href="https://crowdin.com/project/tataru-helper"><img src="https://badges.crowdin.net/tataru-helper/localized.svg" alt="Crowdin" /></a>
  <a href="https://discord.gg/bSrpbd9"><img src="https://img.shields.io/discord/592039000538349569?label=Discord&logo=discord" alt="Discord" /></a>
</p>

<p align="center"><strong><a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe">Pobierz Setup.exe</a></strong> · <a href="Guide.MD">Przewodnik (po angielsku)</a> · <a href="Linux.md">Linux</a> · <a href="https://github.com/NightlyRevenger/TataruHelper/wiki">FAQ</a> · <a href="https://discord.gg/bSrpbd9">Discord</a></p>

<p align="center"><strong>Języki:</strong> <a href="../README.md">EN</a> | <a href="README_ru_RU.md">RU</a> | <a href="README_ko_KR.md">KO</a> | <a href="README_es_ES.md">ES</a> | <a href="README_ca_ES.md">CA</a> | <a href="README_pl_PL.md">PL</a> | <a href="README_pt_BR.md">PT-BR</a> | <a href="README_uk_UA.md">UK</a> | <a href="README_zh_ZH.md">ZH</a> | <a href="README_ja_JP.md">JA</a></p>

<p align="center">
  <img src="SettingsTour.gif" width="720" alt="Przegląd ustawień Tataru Helper" />
</p>

## Możliwości

- Tłumaczy tekst gry na bieżąco: główną fabułę, napisy w przerywnikach, zadania, kwestie NPC i czat.
- Czyta tekst prosto z gry — bez kopiowania i wklejania.
- Wiele silników tłumaczenia, także darmowych, z automatycznym przełączeniem na inny, gdy któryś zawiedzie. Zobacz [Silniki tłumaczenia](#silniki-tłumaczenia).
- Kilka niezależnych okien czatu, każde z własnym silnikiem, językami, czcionkami, kolorami i skrótami.
- Wybierasz, które kanały czatu są tłumaczone, i kolor każdego z nich.
- Pokazuje ręczne tłumaczenie [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate) zamiast maszynowego, gdy istnieje.
- Nakładka może przepuszczać kliknięcia, być zawsze na wierzchu, chować się sama i reagować na skróty klawiszowe.
- Interfejs w 12 językach. Aktualizacje instalują się same.

## Wymagania

- Final Fantasy XIV z DirectX 11 (klient 64-bitowy).
- **Windows 10 lub 11, 64-bitowy.** Przy uruchomieniu wymagane są uprawnienia administratora: aplikacja czyta tekst z pamięci gry. Nie trzeba instalować .NET — wydanie zawiera własny.
- **Albo Linux** z grą uruchomioną w Wine. Zobacz [TataruHelper na Linuksie](Linux.md); wsparcie jest eksperymentalne (dokument po angielsku).

## Instalacja

### Windows

1. Pobierz [`Setup.exe`](https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe) z [latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest).
2. Uruchom go. Jeśli pojawi się SmartScreen, wybierz **Więcej informacji**, a potem **Uruchom mimo to**.
3. Uruchom grę, a potem Tataru Helper z menu Start lub skrótu na pulpicie (nie ponownie `Setup.exe`). Aktualizacje instalują się same.
4. W ustawieniach wybierz język gry i język, w którym chcesz czytać, a nakładkę ustaw tam, gdzie ci wygodnie.
5. W ustawieniach czatu w grze włącz typy wiadomości widoczne na obrazku poniżej, na dowolnej karcie:

![Ustawienia czatu w grze](InGameSettings.png)

Do każdego wydania dołączona jest wersja przenośna, `Portable.zip`.

### Linux

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

Skrypt znajduje Wine, na którym działa gra, i uruchamia Tataru Helper obok niej. Szczegóły, co działa, a co jeszcze nie, oraz rozwiązywanie problemów są w [Documents/Linux.md](Linux.md) (po angielsku).

## Silniki tłumaczenia

| Rodzaj | Silniki |
| --- | --- |
| Darmowe, bez klucza i konta | DeepL (przez dołączony lokalny przekaźnik), Google Translate, Yandex, Bing, Translate.com, ModernMT, Papago; można włączyć Tencent TranSmart, Volcengine i Alibaba |
| Z własnym kluczem | DeepL API, Google Cloud, Azure Translator, Yandex Cloud, OpenAI, Claude, Gemini, DeepSeek, YandexGPT, OpenRouter |
| Na twoim komputerze lub serwerze | Ollama, LM Studio, LibreTranslate |

Gdy silnik odmówi, przejmuje następny włączony. Jak skonfigurować każdy z nich: [Przewodnik](Guide.MD#translation) (po angielsku).

## Dokumentacja

- [Przewodnik](Guide.MD) ([po rosyjsku](GuideRU.MD)): wszystkie strony ustawień, po angielsku.
- [TataruHelper na Linuksie](Linux.md) ([po rosyjsku](LinuxRU.md)), po angielsku.
- [FAQ](https://github.com/NightlyRevenger/TataruHelper/wiki) i rozwiązywanie problemów, w kilku językach.
- [Jak czytana jest gra](ReadingTheGame.md): jak dialogi są wydobywane z gry, dla osób zmieniających tę część (po angielsku).
- Wideo: [pokaz na YouTube](https://youtu.be/7HiQXzmkQuw).

![Tłumaczenie na żywo w grze](LiveTranslation.jpg)

## Współpraca

- Pytania i pomysły: [Discord](https://discord.gg/bSrpbd9) lub [issues](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose).
- Kod: pull requesty są mile widziane; najpierw przeczytaj [CONTRIBUTING.md](../CONTRIBUTING.md) (po angielsku).
- Tłumaczenie interfejsu: [Crowdin](https://crowdin.com/project/tataru-helper).
- Problemy z bezpieczeństwem: [SECURITY.md](../SECURITY.md).

## Podziękowania

Dziękujemy wszystkim współtwórcom i projektom, dzięki którym powstał Tataru Helper:

- [TataruHelper (NightlyRevenger)](https://github.com/NightlyRevenger/TataruHelper): oryginał, którego ten projekt jest forkiem
- [TataruHelper (progneo)](https://github.com/progneo/TataruHelper): fork, który ten projekt kontynuuje
- [Sharlayan](https://github.com/FFXIVAPP/sharlayan): czyta tekst gry z jej pamięci
- [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate): ręczne tłumaczenie na rosyjski
- [WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- [NHotKey.Wpf](https://github.com/thomaslevesque/NHotkey)
- [NotifyIcon WPF](https://bitbucket.org/hardcodet/notifyicon-wpf/)
- [Velopack](https://github.com/velopack/velopack): instalator i aktualizator
- [Tataru Art (Nezusagi)](https://www.deviantart.com/nezusagi)

### To samo, inaczej

Jeśli grasz z [Dalamud](https://github.com/goatcorp/Dalamud), warto znać [Echoglossian](https://github.com/lokinmodar/Echoglossian): wtyczkę, która tłumaczy dialogi wewnątrz gry, a nie w nakładce obok.

## Licencja

[MIT](../LICENSE)
