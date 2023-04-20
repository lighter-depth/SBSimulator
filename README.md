SBSimulator
====
SBSimulator, the one and the only simulator for SB.

## Install
[Releases](https://github.com/lighter-depth/SBSimulator/releases)にアクセスし、最新版のSource Codeファイル(zip形式)をダウンロードしてください。

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

wikiに記載のない頭文字の単語についてはタイプ推論機能をサポートしていませんので、ご了承ください。

## Author
<ul>
  <li>作成者：ゟいたー</li>
  <li>Twitter：<a href="https://twitter.com/lighter_depth">@lighter_depth</a></li>
</ul>

## License
This software includes the work that is distributed in the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).

このソフトウェアは、 [Apache 2.0ライセンス](https://www.apache.org/licenses/LICENSE-2.0)で配布されている製作物が含まれています。
