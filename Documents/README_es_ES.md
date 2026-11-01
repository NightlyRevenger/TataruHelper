<p align="center">
  <img src="Tataru_img.png" width="112" alt="Tataru Helper" />
</p>

<h1 align="center">Tataru Helper</h1>

<p align="center">Superposición de traducción en tiempo real para Final Fantasy XIV: historia, escenas, misiones, diálogos de PNJ y chat.</p>
<p align="center">Mantenido por [xDarkOne](https://github.com/xDarkOne). Creado originalmente por [NightlyRevenger](https://github.com/NightlyRevenger).</p>

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

<p align="center"><strong><a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe">Descargar Setup.exe</a></strong> · <a href="Guide.MD">Guía (en inglés)</a> · <a href="Linux.md">Linux</a> · <a href="https://github.com/NightlyRevenger/TataruHelper/wiki">FAQ</a> · <a href="https://discord.gg/bSrpbd9">Discord</a></p>

<p align="center"><strong>Idiomas:</strong> <a href="../README.md">EN</a> | <a href="README_ru_RU.md">RU</a> | <a href="README_ko_KR.md">KO</a> | <a href="README_es_ES.md">ES</a> | <a href="README_ca_ES.md">CA</a> | <a href="README_pl_PL.md">PL</a> | <a href="README_pt_BR.md">PT-BR</a> | <a href="README_uk_UA.md">UK</a> | <a href="README_zh_ZH.md">ZH</a> | <a href="README_ja_JP.md">JA</a></p>

<p align="center">
  <img src="SettingsTour.gif" width="720" alt="Recorrido por los ajustes de Tataru Helper" />
</p>

## Características

- Traduce el texto del juego según aparece: historia principal, subtítulos de escenas, misiones, diálogos de PNJ y chat.
- Lee el texto del propio juego, sin copiar ni pegar.
- Muchos motores de traducción, también gratuitos, con cambio automático a otro si uno falla. Véase [Motores de traducción](#motores-de-traducción).
- Varias ventanas de chat independientes, cada una con su motor, idiomas, fuentes, colores y atajos.
- Elige qué canales de chat se traducen y el color de cada uno.
- Muestra la traducción manual [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate) en lugar de la automática cuando existe.
- La superposición puede ser transparente a los clics, estar siempre encima, ocultarse sola y controlarse con atajos de teclado.
- Interfaz en 12 idiomas. Las actualizaciones se instalan solas.

## Requisitos

- Final Fantasy XIV con DirectX 11 (cliente de 64 bits).
- **Windows 10 u 11, de 64 bits.** Se piden permisos de administrador al iniciar: la aplicación lee el texto de la memoria del juego. No hay que instalar .NET: la versión lo incluye.
- **O Linux** con el juego ejecutándose bajo Wine. Véase [TataruHelper en Linux](Linux.md); el soporte es experimental (documento en inglés).

## Instalación

### Windows

1. Descarga [`Setup.exe`](https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe) desde la [latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest).
2. Ejecútalo. Si aparece SmartScreen, pulsa **Más información** y después **Ejecutar de todas formas**.
3. Inicia el juego y luego Tataru Helper desde el menú Inicio o el acceso directo del escritorio (no vuelvas a abrir `Setup.exe`). Las actualizaciones se instalan solas.
4. En los ajustes, elige el idioma del juego y el idioma en el que quieres leer, y coloca la superposición donde prefieras.
5. En los ajustes de chat del juego, activa los tipos de mensaje de la imagen de abajo, en cualquier pestaña:

![Ajustes de chat del juego](InGameSettings.png)

Cada versión incluye también una compilación portátil, `Portable.zip`.

### Linux

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

El script encuentra el Wine con el que se ejecuta el juego e inicia Tataru Helper a su lado. Los detalles, qué funciona y qué no todavía, y la solución de problemas están en [Documents/Linux.md](Linux.md) (en inglés).

## Motores de traducción

| Tipo | Motores |
| --- | --- |
| Gratuitos, sin clave ni cuenta | DeepL (mediante un relé local incluido), Google Translate, Yandex, Bing, Translate.com, ModernMT, Papago; se pueden activar Tencent TranSmart, Volcengine y Alibaba |
| Con tu propia clave | DeepL API, Google Cloud, Azure Translator, Yandex Cloud, OpenAI, Claude, Gemini, DeepSeek, YandexGPT, OpenRouter |
| En tu equipo o servidor | Ollama, LM Studio, LibreTranslate |

Cuando un motor rechaza una petición, el siguiente activado toma el relevo. Cómo configurar cada uno: [Guía](Guide.MD#translation) (en inglés).

## Documentación

- [Guía](Guide.MD) ([en ruso](GuideRU.MD)): todas las páginas de ajustes, en inglés.
- [TataruHelper en Linux](Linux.md) ([en ruso](LinuxRU.md)), en inglés.
- [FAQ](https://github.com/NightlyRevenger/TataruHelper/wiki) y solución de problemas, en varios idiomas.
- [Cómo se lee el juego](ReadingTheGame.md): cómo se extrae el diálogo del juego, para quien modifique esa parte (en inglés).
- Vídeo: [demostración en YouTube](https://youtu.be/7HiQXzmkQuw).

![Traducción en vivo dentro del juego](LiveTranslation.jpg)

## Colaborar

- Preguntas e ideas: [Discord](https://discord.gg/bSrpbd9) o [issues](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose).
- Código: los pull requests son bienvenidos; lee antes [CONTRIBUTING.md](../CONTRIBUTING.md) (en inglés).
- Traducción de la interfaz: [Crowdin](https://crowdin.com/project/tataru-helper).
- Problemas de seguridad: [SECURITY.md](../SECURITY.md).

## Créditos

Gracias a todos los colaboradores y a los proyectos que hicieron posible Tataru Helper:

- [TataruHelper (NightlyRevenger)](https://github.com/NightlyRevenger/TataruHelper): el original, del que este es un fork
- [TataruHelper (progneo)](https://github.com/progneo/TataruHelper): el fork que este proyecto continúa
- [Sharlayan](https://github.com/FFXIVAPP/sharlayan): lee el texto del juego desde su memoria
- [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate): la traducción manual al ruso
- [WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- [NHotKey.Wpf](https://github.com/thomaslevesque/NHotkey)
- [NotifyIcon WPF](https://bitbucket.org/hardcodet/notifyicon-wpf/)
- [Velopack](https://github.com/velopack/velopack): el instalador y el actualizador
- [Tataru Art (Nezusagi)](https://www.deviantart.com/nezusagi)

### Lo mismo, de otra manera

Si juegas con [Dalamud](https://github.com/goatcorp/Dalamud), conviene conocer [Echoglossian](https://github.com/lokinmodar/Echoglossian): un plugin que traduce el diálogo dentro del propio juego en lugar de en una superposición a su lado.

## Licencia

[MIT](../LICENSE)
