<p align="center">
  <img src="Tataru_img.png" width="112" alt="Tataru Helper" />
</p>

<h1 align="center">Tataru Helper</h1>

<p align="center">ファイナルファンタジーXIV向けのリアルタイム翻訳オーバーレイ。ストーリー、カットシーン、クエスト、NPCのセリフ、チャットに対応。</p>
<p align="center">[xDarkOne](https://github.com/xDarkOne)が保守しています。原作者は[NightlyRevenger](https://github.com/NightlyRevenger)です。</p>

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

<p align="center"><strong><a href="https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe">Setup.exe をダウンロード</a></strong> · <a href="Guide.MD">ガイド（英語）</a> · <a href="Linux.md">Linux</a> · <a href="https://github.com/NightlyRevenger/TataruHelper/wiki">FAQ</a> · <a href="https://discord.gg/bSrpbd9">Discord</a></p>

<p align="center"><strong>言語:</strong> <a href="../README.md">EN</a> | <a href="README_ru_RU.md">RU</a> | <a href="README_ko_KR.md">KO</a> | <a href="README_es_ES.md">ES</a> | <a href="README_ca_ES.md">CA</a> | <a href="README_pl_PL.md">PL</a> | <a href="README_pt_BR.md">PT-BR</a> | <a href="README_uk_UA.md">UK</a> | <a href="README_zh_ZH.md">ZH</a> | <a href="README_ja_JP.md">JA</a></p>

<p align="center">
  <img src="SettingsTour.gif" width="720" alt="Tataru Helper の設定ツアー" />
</p>

## 機能

- ゲーム内のテキストを表示されたそばから翻訳します。メインストーリー、カットシーンの字幕、クエスト、NPCのセリフ、チャットに対応。
- テキストをゲームから直接読み取るので、コピー＆ペーストは不要です。
- 無料のものを含む多数の翻訳エンジンに対応し、1つが失敗すると自動的に別のエンジンに切り替えます。[翻訳エンジン](#翻訳エンジン)を参照してください。
- エンジン、言語、フォント、色、ホットキーをそれぞれ設定できる、独立した複数のチャットウィンドウ。
- 翻訳するチャットチャンネルと、チャンネルごとの色を選べます。
- 手動翻訳がある場合は、機械翻訳の代わりに [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate) を表示します。
- オーバーレイはクリックの透過、常に手前に表示、自動非表示に対応し、ホットキーで操作できます。
- インターフェースは12言語。アップデートは自動でインストールされます。

## 動作環境

- DirectX 11（64ビットクライアント）で動作するファイナルファンタジーXIV。
- **64ビット版 Windows 10 または 11。** 起動時に管理者権限を求められます。アプリがゲームのメモリからテキストを読み取るためです。.NET のインストールは不要で、リリースに同梱されています。
- **または Linux**（ゲームを Wine で実行）。[Linux での TataruHelper](Linux.md)を参照してください。サポートは実験的です（英語のドキュメント）。

## インストール

### Windows

1. [latest release](https://github.com/NightlyRevenger/TataruHelper/releases/latest)から [`Setup.exe`](https://github.com/NightlyRevenger/TataruHelper/releases/latest/download/Setup.exe) をダウンロードします。
2. 実行します。SmartScreen が表示されたら、**詳細情報**、続けて**実行**を選びます。
3. ゲームを起動してから、スタートメニューまたはデスクトップのショートカットから Tataru Helper を起動します（`Setup.exe` をもう一度実行しないでください）。アップデートは自動でインストールされます。
4. 設定でゲームの言語と読みたい言語を選び、オーバーレイを好きな位置に置きます。
5. ゲームのチャット設定で、どのタブでもよいので、下の画像にあるメッセージの種類を有効にします。

![ゲーム内のチャット設定](InGameSettings.png)

各リリースにはポータブル版の `Portable.zip` も添付されています。

### Linux

```sh
curl -LO https://raw.githubusercontent.com/NightlyRevenger/TataruHelper/master/scripts/linux/tataru-linux.sh
chmod +x tataru-linux.sh
./tataru-linux.sh install
```

スクリプトがゲームを動かしている Wine を見つけ、その隣で Tataru Helper を起動します。詳細、対応している機能としていない機能、トラブルシューティングは [Documents/Linux.md](Linux.md)（英語）にあります。

## 翻訳エンジン

| 種類 | エンジン |
| --- | --- |
| 無料（キー・アカウント不要） | DeepL（同梱のローカルリレー経由）、Google Translate、Yandex、Bing、Translate.com、ModernMT、Papago。Tencent TranSmart、Volcengine、Alibaba は有効にできます |
| 自分のキーを使用 | DeepL API, Google Cloud, Azure Translator, Yandex Cloud, OpenAI, Claude, Gemini, DeepSeek, YandexGPT, OpenRouter |
| 自分のPCまたはサーバー | Ollama, LM Studio, LibreTranslate |

エンジンがリクエストを拒否すると、次に有効なエンジンが引き継ぎます。各エンジンの設定方法は[ガイド](Guide.MD#translation)（英語）にあります。

## ドキュメント

- [ガイド](Guide.MD)（[ロシア語版](GuideRU.MD)）：すべての設定ページの説明（英語）。
- [Linux での TataruHelper](Linux.md)（[ロシア語版](LinuxRU.md)）、英語。
- [FAQ](https://github.com/NightlyRevenger/TataruHelper/wiki)とトラブルシューティング（複数言語）。
- [ゲームの読み取り](ReadingTheGame.md)：ゲームからセリフを読み取る仕組み。この部分を変更する人向け（英語）。
- 動画：[YouTube でのデモ](https://youtu.be/7HiQXzmkQuw)。

![ゲーム内でのライブ翻訳](LiveTranslation.jpg)

## コントリビュート

- 質問やアイデア：[Discord](https://discord.gg/bSrpbd9)または[issues](https://github.com/NightlyRevenger/TataruHelper/issues/new/choose)。
- コード：プルリクエストを歓迎します。まず [CONTRIBUTING.md](../CONTRIBUTING.md)（英語）をお読みください。
- インターフェースの翻訳：[Crowdin](https://crowdin.com/project/tataru-helper)。
- セキュリティ上の問題：[SECURITY.md](../SECURITY.md)。

## 謝辞

Tataru Helper を実現してくれたすべての貢献者とプロジェクトに感謝します。

- [TataruHelper (NightlyRevenger)](https://github.com/NightlyRevenger/TataruHelper): オリジナル。このプロジェクトはそのフォークです
- [TataruHelper (progneo)](https://github.com/progneo/TataruHelper): このプロジェクトが引き継いでいるフォーク
- [Sharlayan](https://github.com/FFXIVAPP/sharlayan): ゲームのメモリからテキストを読み取る
- [XIV Rus Translation](https://github.com/xivrus/xiv_ru_weblate): 手動のロシア語翻訳
- [WPF Toolkit](https://github.com/xceedsoftware/wpftoolkit)
- [NHotKey.Wpf](https://github.com/thomaslevesque/NHotkey)
- [NotifyIcon WPF](https://bitbucket.org/hardcodet/notifyicon-wpf/)
- [Velopack](https://github.com/velopack/velopack): インストーラーとアップデーター
- [Tataru Art (Nezusagi)](https://www.deviantart.com/nezusagi)

### 同じことを別の方法で

[Dalamud](https://github.com/goatcorp/Dalamud) を使っているなら、[Echoglossian](https://github.com/lokinmodar/Echoglossian) も知っておく価値があります。横のオーバーレイではなく、ゲーム内でセリフを翻訳するプラグインです。

## ライセンス

[MIT](../LICENSE)
