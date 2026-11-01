<p align="center">
  <img src="Tataru_img.png" width="112" alt="Tataru Helper" />
</p>

<h1 align="center">Tataru Helper</h1>

<p align="center">Sobreposição de tradução em tempo real para Final Fantasy XIV: história, cenas, missões, falas de NPCs e chat.</p>
<p align="center">Mantido por [xDarkOne](https://github.com/xDarkOne). Criado originalmente por [NightlyRevenger](https://github.com/NightlyRevenger).</p>

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

<p align="center"><strong><a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe">Baixar o Setup.exe</a></strong> · <a href="Guide.MD">Guia (em inglês)</a> · <a href="Linux.md">Linux</a> · <a href="https://github.com/NightlyRevenger/TataruHelper/wiki">FAQ</a> · <a href="https://discord.gg/bSrpbd9">Discord</a></p>

<p align="center"><strong>Idiomas:</strong> <a href="../README.md">EN</a> | <a href="README_ru_RU.md">RU</a> | <a href="README_ko_KR.md">KO</a> | <a href="README_es_ES.md">ES</a> | <a href="README_ca_ES.md">CA</a> | <a href="README_pl_PL.md">PL</a> | <a href="README_pt_BR.md">PT-BR</a> | <a href="README_uk_UA.md">UK</a> | <a href="README_zh_ZH.md">ZH</a> | <a href="README_ja_JP.md">JA</a></p>

<p align="center">
  <img src="SettingsTour.gif" width="720" alt="Tour pelas configurações do Tataru Helper" />
</p>

## Recursos

- Traduz o texto do jogo conforme ele aparece: história principal, legendas de cenas, missões, falas de NPCs e chat.
- Lê o texto do próprio jogo, sem copiar e colar.
- Vários mecanismos de tradução, inclusive gratuitos, com troca automática para outro quando um falha. Veja [Mecanismos de tradução](#mecanismos-de-tradução).
- Várias janelas de chat independentes, cada uma com seu mecanismo, idiomas, fontes, cores e atalhos.
- Escolha quais canais de chat são traduzidos e a cor de cada um.
- Mostra a tradução manual [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate) no lugar da automática quando existe.
- A sobreposição pode deixar os cliques passarem, ficar sempre no topo, se esconder sozinha e ser controlada por atalhos.
- Interface em 12 idiomas. As atualizações se instalam sozinhas.

## Requisitos

- Final Fantasy XIV com DirectX 11 (cliente de 64 bits).
- **Windows 10 ou 11, de 64 bits.** Permissões de administrador são pedidas ao iniciar: o aplicativo lê o texto da memória do jogo. Não é preciso instalar o .NET: a versão já o inclui.
- **Ou Linux** com o jogo rodando no Wine. Veja [TataruHelper no Linux](Linux.md); o suporte é experimental (documento em inglês).

## Instalação

### Windows

1. Baixe o [`Setup.exe`](https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe) da [latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest).
2. Execute-o. Se o SmartScreen aparecer, clique em **Mais informações** e depois em **Executar assim mesmo**.
3. Inicie o jogo e depois o Tataru Helper pelo menu Iniciar ou pelo atalho da área de trabalho (não abra o `Setup.exe` de novo). As atualizações se instalam sozinhas.
4. Nas configurações, escolha o idioma do jogo e o idioma em que quer ler, e posicione a sobreposição onde preferir.
5. Nas configurações de chat do jogo, ative os tipos de mensagem da imagem abaixo, em qualquer aba:

![Configurações de chat do jogo](InGameSettings.png)

Cada versão também traz uma compilação portátil, `Portable.zip`.

### Linux

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

O script encontra o Wine em que o jogo roda e inicia o Tataru Helper ao lado dele. Os detalhes, o que funciona e o que ainda não, e a solução de problemas estão em [Documents/Linux.md](Linux.md) (em inglês).

## Mecanismos de tradução

| Tipo | Mecanismos |
| --- | --- |
| Gratuitos, sem chave nem conta | DeepL (por um relé local incluído), Google Translate, Yandex, Bing, Translate.com, ModernMT, Papago; Tencent TranSmart, Volcengine e Alibaba podem ser ativados |
| Com sua própria chave | DeepL API, Google Cloud, Azure Translator, Yandex Cloud, OpenAI, Claude, Gemini, DeepSeek, YandexGPT, OpenRouter |
| No seu computador ou servidor | Ollama, LM Studio, LibreTranslate |

Quando um mecanismo recusa um pedido, o próximo ativado assume. Como configurar cada um: [Guia](Guide.MD#translation) (em inglês).

## Documentação

- [Guia](Guide.MD) ([em russo](GuideRU.MD)): todas as páginas de configurações, em inglês.
- [TataruHelper no Linux](Linux.md) ([em russo](LinuxRU.md)), em inglês.
- [FAQ](https://github.com/NightlyRevenger/TataruHelper/wiki) e solução de problemas, em vários idiomas.
- [Como o jogo é lido](ReadingTheGame.md): como o diálogo é extraído do jogo, para quem for alterar essa parte (em inglês).
- Vídeo: [demonstração no YouTube](https://youtu.be/7HiQXzmkQuw).

![Tradução ao vivo dentro do jogo](LiveTranslation.jpg)

## Contribuir

- Perguntas e ideias: [Discord](https://discord.gg/bSrpbd9) ou [issues](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose).
- Código: pull requests são bem-vindos; leia antes o [CONTRIBUTING.md](../CONTRIBUTING.md) (em inglês).
- Tradução da interface: [Crowdin](https://crowdin.com/project/tataru-helper).
- Problemas de segurança: [SECURITY.md](../SECURITY.md).

## Créditos

Obrigado a todos os colaboradores e aos projetos que tornaram o Tataru Helper possível:

- [TataruHelper (NightlyRevenger)](https://github.com/NightlyRevenger/TataruHelper): o original, do qual este é um fork
- [TataruHelper (progneo)](https://github.com/progneo/TataruHelper): o fork que este projeto continua
- [Sharlayan](https://github.com/FFXIVAPP/sharlayan): lê o texto do jogo da memória
- [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate): a tradução manual para o russo
- [WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- [NHotKey.Wpf](https://github.com/thomaslevesque/NHotkey)
- [NotifyIcon WPF](https://bitbucket.org/hardcodet/notifyicon-wpf/)
- [Velopack](https://github.com/velopack/velopack): o instalador e o atualizador
- [Tataru Art (Nezusagi)](https://www.deviantart.com/nezusagi)

### O mesmo, de outro jeito

Se você joga com o [Dalamud](https://github.com/goatcorp/Dalamud), vale conhecer o [Echoglossian](https://github.com/lokinmodar/Echoglossian): um plugin que traduz o diálogo dentro do próprio jogo, em vez de numa sobreposição ao lado.

## Licença

[MIT](../LICENSE)
