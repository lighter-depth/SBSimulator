SBSimulator
====
SBSimulator, the one and the only simulator for SB.
## Overview
SBSimulatorは、SBのダメージ計算や環境の構築、戦法のテストなどを目的として開発されたコンソール アプリケーションです。

通常のしりとり機能に加え、以下の機能を揃えています。
- SB準拠のシステム
- ワイルドカード機能
- タイプ指定機能
- パラメーター（急所倍率、医療回数制限など）の設定
- タイプ推論（一部頭文字のみ）

## Install
[Releases](https://github.com/lighter-depth/SBSimulator/releases)にアクセスし、最新版のSource codeファイル(zip形式)をダウンロードしてください。

実行ファイルは/bin/Release/net7.0 直下にあります。

## Usage
アプリケーション内で help コマンドを入力することにより、詳細な使用方法を表示することができます。
```antlr
help
```
### コマンドの入力
コマンドは共通して以下の構文で入力します。
```antlr
command_call
  : command_name ' ' command_parameters
  ;
command_parameters
  : command_parameter(' ' command_parameter)*
  ;
```
#### コマンドのサンプル
```antlr
change PlayerA RockRoll  // 名前が「PlayerA」であるプレイヤーを探し、とくせいをロックンロールに変更する
```
```antlr
option SetMaxSeedTurn 6  // やどりぎの継続ターンを６ターンに設定する
```
```antlr
show status       // プレイヤーのステータス（攻撃力の値など）を表示する
```
## Notice
タイプ付き単語の情報は[atwiki](https://w.atwiki.jp/1855528/)の「行別タイプ付きワード一覧」記事群を参考にしています。

wikiに記載のない頭文字の単語についてはタイプ推論機能をサポートしていませんので、ご理解ください。

## Author
  - 作成者：ゟいたー
  - Twitter：https://twitter.com/lighter_depth

## License
This software includes the work that is distributed in the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

このソフトウェアは、 [Apache 2.0ライセンス](https://www.apache.org/licenses/LICENSE-2.0)で配布されている製作物が含まれています。

SBSimulatorは、ささみ氏により作成されたブラウザゲーム「しりとりバトル」を参考に作成された、

ファンメイドのコンソール アプリケーションです。

本アプリケーションの二次配布・商用利用を固く禁止します。
- ささみ氏のTwitter: https://twitter.com/sasamijp
- しりとりバトル(ブラウザ版): http://siritoribattle.net
