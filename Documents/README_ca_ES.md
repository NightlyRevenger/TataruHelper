<p align="center">
  <img src="Tataru_img.png" width="112" alt="Tataru Helper" />
</p>

<h1 align="center">Tataru Helper</h1>

<p align="center">Superposició de traducció en temps real per a Final Fantasy XIV: història, escenes, missions, diàlegs de PNJ i xat.</p>
<p align="center">Mantingut per [xDarkOne](https://github.com/xDarkOne). Creat originalment per [NightlyRevenger](https://github.com/NightlyRevenger).</p>

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

<p align="center"><strong><a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe">Descarrega Setup.exe</a></strong> · <a href="Guide.MD">Guia (en anglès)</a> · <a href="Linux.md">Linux</a> · <a href="https://github.com/NightlyRevenger/TataruHelper/wiki">FAQ</a> · <a href="https://discord.gg/bSrpbd9">Discord</a></p>

<p align="center"><strong>Idiomes:</strong> <a href="../README.md">EN</a> | <a href="README_ru_RU.md">RU</a> | <a href="README_ko_KR.md">KO</a> | <a href="README_es_ES.md">ES</a> | <a href="README_ca_ES.md">CA</a> | <a href="README_pl_PL.md">PL</a> | <a href="README_pt_BR.md">PT-BR</a> | <a href="README_uk_UA.md">UK</a> | <a href="README_zh_ZH.md">ZH</a> | <a href="README_ja_JP.md">JA</a></p>

<p align="center">
  <img src="SettingsTour.gif" width="720" alt="Recorregut pels ajustos de Tataru Helper" />
</p>

## Característiques

- Tradueix el text del joc a mesura que apareix: història principal, subtítols de les escenes, missions, diàlegs de PNJ i xat.
- Llegeix el text del mateix joc, sense copiar ni enganxar.
- Molts motors de traducció, també gratuïts, amb canvi automàtic a un altre si un falla. Vegeu [Motors de traducció](#motors-de-traducció).
- Diverses finestres de xat independents, cadascuna amb el seu motor, idiomes, tipus de lletra, colors i dreceres.
- Tria quins canals de xat es tradueixen i el color de cadascun.
- Mostra la traducció manual [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate) en lloc de l'automàtica quan n'hi ha.
- La superposició pot deixar passar els clics, estar sempre al davant, amagar-se sola i controlar-se amb dreceres de teclat.
- Interfície en 12 idiomes. Les actualitzacions s'instal·len soles.

## Requisits

- Final Fantasy XIV amb DirectX 11 (client de 64 bits).
- **Windows 10 o 11, de 64 bits.** En iniciar-se es demanen permisos d'administrador: l'aplicació llegeix el text de la memòria del joc. No cal instal·lar .NET: la versió l'inclou.
- **O Linux** amb el joc executant-se sota Wine. Vegeu [TataruHelper a Linux](Linux.md); el suport és experimental (document en anglès).

## Instal·lació

### Windows

1. Descarrega [`Setup.exe`](https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe) de l'[latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest).
2. Executa'l. Si apareix SmartScreen, prem **Més informació** i després **Executa igualment**.
3. Inicia el joc i després Tataru Helper des del menú Inici o la drecera de l'escriptori (no tornis a obrir `Setup.exe`). Les actualitzacions s'instal·len soles.
4. Als ajustos, tria l'idioma del joc i l'idioma en què vols llegir, i col·loca la superposició on vulguis.
5. Als ajustos de xat del joc, activa els tipus de missatge de la imatge de sota, en qualsevol pestanya:

![Ajustos de xat del joc](InGameSettings.png)

Cada versió inclou també una compilació portàtil, `Portable.zip`.

### Linux

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

L'script troba el Wine amb què s'executa el joc i inicia Tataru Helper al seu costat. Els detalls, què funciona i què encara no, i la resolució de problemes són a [Documents/Linux.md](Linux.md) (en anglès).

## Motors de traducció

| Tipus | Motors |
| --- | --- |
| Gratuïts, sense clau ni compte | DeepL (mitjançant un relé local inclòs), Google Translate, Yandex, Bing, Translate.com, ModernMT, Papago; es poden activar Tencent TranSmart, Volcengine i Alibaba |
| Amb la teva pròpia clau | DeepL API, Google Cloud, Azure Translator, Yandex Cloud, OpenAI, Claude, Gemini, DeepSeek, YandexGPT, OpenRouter |
| Al teu equip o servidor | Ollama, LM Studio, LibreTranslate |

Quan un motor rebutja una petició, pren el relleu el següent d'activat. Com configurar cadascun: [Guia](Guide.MD#translation) (en anglès).

## Documentació

- [Guia](Guide.MD) ([en rus](GuideRU.MD)): totes les pàgines d'ajustos, en anglès.
- [TataruHelper a Linux](Linux.md) ([en rus](LinuxRU.md)), en anglès.
- [FAQ](https://github.com/NightlyRevenger/TataruHelper/wiki) i resolució de problemes, en diversos idiomes.
- [Com es llegeix el joc](ReadingTheGame.md): com s'extreu el diàleg del joc, per a qui modifiqui aquesta part (en anglès).
- Vídeo: [demostració a YouTube](https://youtu.be/7HiQXzmkQuw).

![Traducció en directe dins del joc](2020-08-18_21-12-52.gif)

## Col·laborar

- Preguntes i idees: [Discord](https://discord.gg/bSrpbd9) o [issues](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose).
- Codi: els pull requests són benvinguts; llegeix abans [CONTRIBUTING.md](../CONTRIBUTING.md) (en anglès).
- Traducció de la interfície: [Crowdin](https://crowdin.com/project/tataru-helper).
- Problemes de seguretat: [SECURITY.md](../SECURITY.md).

## Crèdits

Gràcies a tots els col·laboradors i als projectes que han fet possible Tataru Helper:

- [TataruHelper (NightlyRevenger)](https://github.com/NightlyRevenger/TataruHelper): l'original, del qual aquest és un fork
- [TataruHelper (progneo)](https://github.com/progneo/TataruHelper): el fork que aquest projecte continua
- [Sharlayan](https://github.com/FFXIVAPP/sharlayan): llegeix el text del joc des de la seva memòria
- [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate): la traducció manual al rus
- [WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- [NHotKey.Wpf](https://github.com/thomaslevesque/NHotkey)
- [NotifyIcon WPF](https://bitbucket.org/hardcodet/notifyicon-wpf/)
- [Velopack](https://github.com/velopack/velopack): l'instal·lador i l'actualitzador
- [Tataru Art (Nezusagi)](https://www.deviantart.com/nezusagi)

### El mateix, d'una altra manera

Si jugues amb [Dalamud](https://github.com/goatcorp/Dalamud), val la pena conèixer [Echoglossian](https://github.com/lokinmodar/Echoglossian): un connector que tradueix el diàleg dins del mateix joc en lloc d'una superposició al costat.

## Llicència

[MIT](../LICENSE)
