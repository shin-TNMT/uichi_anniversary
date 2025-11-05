# 調査レポート — uichi_anniversary

作成日: 2025-11-01

## 概要
この調査は、Godot（Mono/.NET）プロジェクトとして初めて作成する前提で、リポジトリ内の主要ファイルを確認し、必須環境との整合性、欠落している構成要素、および次の実施手順をまとめたものです。

ユーザ報告: 環境セットアップは完了している（ユーザ発言）。

## 読み取ったファイル
- `project.godot` (プロジェクトファイル)
  - config/features に `4.5` が含まれている
   - `dotnet/project/assembly_name = "uichi_anniversary"`
- `Document/requirement.md`
  - 推奨環境: Godot v4.5.1.stable.mono、.NET SDK 9.0.306
- `.github/copilot-instructions.md`
  - 本プロジェクトの開発方針、命名規約、必須環境、ドキュメント運用ルール等を記載
  - 「調査」は `Document/reports.md` に記載すること（本ファイル）

（ワークスペース上の現時点のファイル一覧は最小構成に見える。`scenes/` や `scripts/`、`assets/` といった標準的なディレクトリが存在しない可能性あり。）

## 現状で確認できること
1. 必須環境（copilot-instructions と requirement）は明確に定義されている。
   - Godot: v4.5.1.stable.mono
   - .NET SDK: 9.0.306
2. `project.godot` は Godot 4 系で構成されており、Mono 用の設定（assembly_name）が存在する。
3. まだシーンやスクリプトのソースがリポジトリに追加されていない（少なくともトップレベルには見当たらない）。

## 想定される問題・注意点
- Godot エディタ（Monoビルド）とローカルの .NET SDK バージョンが合致していないと、C# スクリプトのビルドやソリューション生成で問題が出る可能性が高い。
- プロジェクトに C# スクリプトが追加されている場合、IDE（Visual Studio / Rider / VSCode + C# 拡張）でソリューションの復元とビルドが必要。
- アセットやシーンが不足しているため、プロジェクトをエディタで開いても「メインシーンが実行できない」状態の可能性がある。

## 推奨の次ステップ（優先順）
1. Godot エディタ（v4.5.1.stable.mono）でプロジェクトフォルダを開く
   - エディタ起動後、メニューから本リポジトリを開き、Mono の設定で問題がないか確認する。
2. ローカルで .NET SDK バージョンを確認する
   - `dotnet --version` で SDK バージョンを確認。推奨は `9.0.306`。
3. 最低限のプロジェクト構成を作成
   - `scenes/` にメインシーン（例: `Main.tscn`）を作成
   - `scripts/` に C# スクリプト（例: `Main.cs`）を追加し、エディタ上でソリューションを生成してビルドを確認
4. CI/ローカルでのビルド確認（任意）
   - Windows の場合、Visual Studio または `dotnet` CLI でビルドできることを確認する。
5. ドキュメント整備
   - `Document/tasks.md` に今後の実装計画を記載する（要件に従う）

## 簡易「やってみる」手順（Windows / cmd.exe）
1. .NET SDK バージョン確認
```
# Windows (cmd.exe)
dotnet --version
```
2. Godot（Mono）でプロジェクトを開く
   - Godot エディタを起動し、`c:\Users\owner\Documents\uichi_anniversary` を開く
3. エディタ内で C# ソリューション生成 → IDE でビルド
   - Godot の出力に表示されるソリューションファイルを IDE で開いてビルドする

## 提案（短期・低リスクの追加実装）
- リポジトリに以下の最小ファイルを追加する（私が作業してよければ対応します）:
  - `scenes/Main.tscn`（空の Node2D ルート + 最小設定）
  - `scripts/Main.cs`（Godot 用のエントリ的なC#スクリプト）
  - `README.md` に「開発セットアップと起動手順」を追記
- これにより、エディタで「シーンを開いて実行」できる最小環境が整うため、以降の実装・デバッグがスムーズになります。

## 要約
- 必須環境は Document/requirement.md と `.github/copilot-instructions.md` に明記されており、`project.godot` も Mono 用の設定を含んでいるため基本方針は整っている。
- 現時点でシーンやスクリプトが見当たらないため、まずは Godot エディタでプロジェクトを開いて Mono の設定とソリューション生成を確認することを推奨する。

---

## スタート画面実装のための調査と整理（2025-11-04）

目的: ゲーム起動時に表示する「スタート画面（タイトル画面）」を実装するにあたり、まず何を準備し、どのファイルを編集・追加すべきかを整理する。

1) 現状の該当ファイル（ワークスペースに存在）
   - `project.godot` (プロジェクト設定)
   - `scenes/Main.tscn` (メインシーン候補)
   - `scripts/Main.cs` (既存の C# スクリプト)
   - `icon.svg.import` (インポート済みアセットの痕跡)

2) 目的（Contract）
   - 入力: ユーザの操作（Start ボタンのクリック、キー入力）
   - 出力: ゲーム本編のシーンへスムーズに遷移すること。また、オプション/終了が利用できること。
   - 成功基準: スタート画面から "Start" を押すと `Main.tscn`（または意図したゲームシーン）に遷移し、戻る/終了が正しく動作する。

3) 必要な UI 要素（最小）
   - タイトル表示（ロゴ/テキスト）
   - Start ボタン（エンターキー / クリックで起動）
   - Options ボタン（音量等、任意）
   - Quit ボタン（デスクトップ実行時にアプリケーションを終了）
   - 背景画像またはパーティクル、BGM（任意だが推奨）

4) 実装方針（技術設計）
   - 新規シーン `scenes/StartScreen.tscn` を作成し、ルートは `CanvasLayer` または `Control` にする（UI 表示用）。
   - `scripts/StartScreen.cs` を作成して、ボタン押下時に `GetTree().ChangeSceneToFile("res://scenes/Main.tscn")` 相当の遷移を行う。
   - `project.godot` の `run/main_scene`（またはプロジェクト設定のメインシーン）を `scenes/StartScreen.tscn` に設定する（エディタ上での設定が必要）。
   - 音声は `AudioStreamPlayer` を用意。音量設定は `Options` 経由で保存（`ConfigFile` 等）できるようにする。

5) テストと受け入れ基準
   - ボタンが有効（クリック/キーで反応）であること
   - Start で `Main.tscn` に遷移し、遷移先の初期化が正常に走ること
   - Quit がデスクトップ環境でアプリを終了すること（エディタ実行時は停止）

6) 想定されるエッジケース
   - `Main.tscn` に依存するアセットが欠落している場合の遷移失敗
   - 解像度やアスペクトによる UI はみ出し（レスポンシブ対応要検討）
   - キーボード/ゲームパッド両対応の入力設計

7) 変更/追加予定ファイル（短期での最小セット）
   - 追加: `scenes/StartScreen.tscn` (UI シーン)
   - 追加: `scripts/StartScreen.cs` (シーン制御スクリプト)
   - 編集: `project.godot` の main scene 設定（またはエディタ上で設定）
   - 必要に応じて: `assets/` に背景画像・BGM を追加

8) 見積時間（概算）
   - 最小実装（タイトル + Start 遷移）: 1〜2 時間
   - Options/設定保存・BGM・レスポンシブ対応含む: 追加で 1〜3 時間

次の推奨アクション（優先順）
 1. `project.godot` を開いて現在の main scene を確認（`scenes/Main.tscn` かどうか）
 2. `scenes/Main.tscn` と `scripts/Main.cs` をエディタで開き、Main シーンがゲーム本編の開始地点か確認する
 3. `scenes/StartScreen.tscn` の雛形を作り、`scripts/StartScreen.cs` を追加して Start 遷移を実装する
 4. 動作確認: Godot エディタでプロジェクトを実行し、タイトル画面から遷移することを確認する

備考: ここまでの調査・整理を基に、私が最小実装（`StartScreen.tscn` と `StartScreen.cs` の追加）を行うことも可能です。進めてよければ続けて実装します。

作業完了: この調査レポート（`Document/reports.md`）を作成しました。
次に何を行いますか？（例: 最小シーンとスクリプトの追加、CI ビルドの追加、詳細な要件定義の作成 など）

---

## 調査: `Main` シーンに「街マップ（Town）」を作成するための手順（2025-11-04）

目的: 2D RPG のメインシーン (`res://scenes/Main.tscn`) に街（Town）マップを作成するための手順、必要なアセット、Godot 固有の実装ポイント、テスト項目を整理する。

前提
- 本プロジェクトは Godot (Mono/.NET) を使用する 2D RPG。タイルベースの街マップを想定する。
- タイルサイズの基準を決める（例: 32x32 または 48x48）。プロジェクト内で統一すること。

高レベル手順（要約）
1. アセット準備
   - 必要なタイル画像（地面、道、建物、装飾、オブジェクト）を用意する。PNG、透過あり。ファイル名/フォルダ構成を決める（例: `assets/tiles/town/`）。
   - タイルサイズを決める（例: 48x48）。既製のスプライトシートを使用する場合は、各セルが同一サイズであることを確認する。
2. TileSet の作成
   - Godot エディタで `TileSet` リソースを作成（例: `res://scenes/tiles/TownTiles.tres`）。
   - テクスチャをインポートし、タイルごとに `Tile` を作成する。
   - Collision (CollisionShape2D) をタイルに割り当てる（建物や壁など歩けない領域）。
   - Autotile / Atlas を設定して、地形を簡単に敷き詰められるようにする（`Autotile` ルールで接続/角処理を自動化）。
   - 必要なら NavigationPolygon（NavigationRegion2D 相当）用の領域をタイル毎に用意する／または後述の方法でナビメッシュを作成。
3. TileMap の作成とレイヤー設計
   - `scenes/Town.tscn` を作成。ルートは `Node2D`（ゲーム座標系）、子に複数の `TileMap` を追加。
   - 推奨レイヤー例（下から上へ）:
     1. `Ground` (TileMap) — 地面タイル
     2. `Roads` (TileMap) — 道や舗装
     3. `Decor` (TileMap) — 木・生け垣などの地形デコレーション（衝突あり）
     4. `BuildingsBase` (TileMap) — 建物の下半分（歩行判定の下）
     5. `Objects` (Node2D / PackedScene インスタンス) — 前景オブジェクト（テーブル、椅子など）
     6. `BuildingsTop` (TileMap) — 建物の上半分（プレイヤーの前に描画）
   - `YSort` ノードを必要に応じて使い、オブジェクトの描画順（Y 座標ベース）を自動化する。
4. 衝突とナビゲーション
   - TileSet のタイルに `CollisionShape2D` を設定して歩行不可領域を定義。
   - ナビゲーション (経路探索) は `NavigationRegion2D` / `NavigationServer2D` を利用するか、タイルベースなら通行可能タイルをグリッドとして扱うロジック（A*）を実装する。
   - NPC 用のスポーンポイント（`Position2D`）やトリガー（`Area2D`）を配置。
5. マップの分割とストリーミング
   - 大きいマップは複数シーンに分割して必要時にロード（`Node2D` を `PackedScene` としてロード/アンロード）する。Godot の `Viewport` や `VisibilityNotifier2D` を使い、不要オブジェクトを非表示にしてパフォーマンスを確保する。
6. 照明とエフェクト
   - `Light2D`／`CanvasModulate` を使い雰囲気を調整。昼夜切り替えや影を追加する場合、Light2D と NormalMap を利用する。
7. インタラクティブ要素
   - ドア、NPC、ショップなどは個別に `PackedScene` を作成して `Objects` レイヤー上にインスタンス化する。
   - インタラクトは `Area2D` とシグナルで実装（プレイヤが近づいたら `body_entered`）
8. テストと調整
   - プレイヤをスポーンさせ、歩行可能領域・当たり判定・遷移地点を検証。
   - ナビメッシュ（または A*）で NPC が正しく経路探索できるか確認。

詳細ステップ（Godot エディタでの作業順）
1. アセットを `assets/tiles/town/` に置く（例: `magecity.png`）。
2. Godot で `TileSet` を新規作成し、テクスチャを登録する。
3. 各タイルに Collision を追加。Autotile 用のビットマスクとルールを作成。
4. `scenes/Town.tscn` を作成し、複数の `TileMap` ノードを用意。
5. TileMap の `Tileset` に先ほど作った `TownTiles.tres` を割り当て、地形を塗りつぶす。
6. 建物やオブジェクトは `scenes/props/` に個別シーンで作成し、TileMap の上にインスタンス化する。
7. 衝突・ナビゲーション用の領域（`CollisionShape2D` / `NavigationRegion2D`）を配置。
8. マップのパフォーマンスを測定し、必要に応じて分割や Visibility ノードを導入する。

推奨ファイル構成（例）
- `assets/tiles/town/` — タイル画像
- `scenes/tiles/TownTiles.tres` — TileSet リソース
- `scenes/Town.tscn` — 街マップのメインシーン（TileMap を含む）
- `scenes/props/` — 建物や家具、NPC などの PackedScenes
- `scripts/MapManager.cs` — マップのロード・アンロード / スポーン制御（任意）

注意点 / エッジケース
- タイルサイズが混在すると配置ズレの原因になる。最初にサイズを固定する。
- Autotile 設定は初回で時間がかかるが、後の作業を大幅に楽にする。
- 衝突形状はタイル単位で正確に作成する。複雑な当たり判定は `CollisionPolygon2D` を使う。
- 大規模マップでは描画負荷・メモリを監視し、分割・ストリーミングを検討する。

参考（所要時間見積）
- 基本マップ（小規模、1 地区）: 2〜4 時間
- 中規模（NPC、複数のゾーン）: 1〜2 日
- 大規模ワールド（分割・ストリーミング含む）: 数日〜週

次の推奨アクション（優先順）
1. タイルサイズ（32/48/64 など）とアセット命名規約を決める。
2. `assets/tiles/town/` にサンプルタイルを準備して `TileSet` を作る（PoC）。
3. `scenes/Town.tscn` を作り、最初の TileMap レイヤー（Ground）を敷設する。
4. 衝突とナビゲーションの最小実装（通行不可タイルと NPC の簡易経路探索）を追加して動作確認する。

必要であれば、私が 1) `TownTiles.tres` の雛形 2) `scenes/Town.tscn` の雛形 3) `scripts/MapManager.cs` の最小実装 をリポジトリに追加します。どれを作りましょうか？


## 追加調査: VSCodeで使用できる MCP サーバ作成の調査（2025-11-02）

目的: VSCode から利用できる Model Context Protocol（MCP）サーバを作成するための手順・実装アプローチを調べ、次の実装に必要な情報と最小構成案をまとめる。

1) 公的リポジトリ／仕様の確認結果
- 今回、想定される公式リポジトリ（例: `microsoft/model-context-protocol` の README 等）にアクセスを試みたが、一部の URL が 404 になり、公開ドキュメントを取得できなかった。
- そのため、本調査では「公式仕様が手元にある」ことを前提にした厳密な手順ではなく、実務で使える実装パターンと VSCode への統合方法（起動・通信方法）を提案する形でまとめる。

2) 前提（仮定）と契約（contract）
- 入力: クライアント（VSCode 拡張）からのコンテキスト要求（ファイルパス、バッファ、範囲、言語等）
- 出力: モデルに供給するための文脈データ（テキスト断片、シンボル一覧、依存情報、ファイルメタ情報など）を JSON で返す API
- エラーモード: 対象ワークスペースへのファイルアクセス失敗、タイムアウト、大規模リポジトリでの部分失敗
- 成功基準: VSCode 拡張が起動後にサーバへ接続し、/context のようなエンドポイントで JSON レスポンスが得られること

3) 通信方式の選択肢（利点・欠点）
- HTTP REST (例: /context)
   - 利点: 実装が容易、デバッグしやすい、既存のHTTPクライアントが利用可
   - 欠点: 双方向リアルタイム通知は工夫が必要（WebSocket 等を併用）
- WebSocket
   - 利点: サーバからクライアントへイベント通知がしやすい（インクリメンタル更新）
   - 欠点: 実装がやや複雑、プロキシ経由や認証の取り扱いに注意
- stdio (JSON-RPC over stdio)
   - 利点: VSCode の Language Server のように拡張から直接プロセスを起動して接続できる
   - 欠点: サーバをエディタの外で共有したい場合には向かない

4) 実装言語の候補
- Node.js/TypeScript
   - 豊富なパッケージ（Express, Fastify, ws 等）、VSCode 拡張も TypeScript で作りやすい
- Python (FastAPI)
   - データ処理が得意、既存の ML 前処理コードを流用しやすい
- .NET (C#)
   - 既存プロジェクトが Godot（Mono/.NET）であれば相互運用しやすい。Kestrel を使った WebAPI 実装が可能

5) 最小実装案（Node.js + HTTP） — 仕様サマリ
- エンドポイント例:
   - GET /health => {status: "ok"}
   - POST /context => request: {paths?: string[], openBuffers?: [{path, text}] ,options?: {...}} => response: {contexts: [{path, snippet, language,metadata}], warnings?: []}
- 実装の流れ:
   1. VSCode 拡張がワークスペースパスを渡してサーバに接続
   2. 拡張から `/context` にリクエスト（例: 現在編集中のファイルと範囲）を送信
   3. サーバは必要ならワークスペースのファイルを読み取り、解析（シンボル抽出、依存追跡、トークンカウント）して JSON を返す

6) VSCode との統合方法（2 パターン）
- パターン A: 拡張がサーバを「プロセスとして起動」し stdio や HTTP で通信
   - 拡張の activation 時に child_process.spawn でサーバを起動
   - 長所: サーバのライフサイクルを拡張で管理できる
   - 短所: ユーザの環境に依存するバイナリやランタイムが必要
- パターン B: サーバは外部サービスとして常駐し、拡張は URL を設定から読み接続
   - 長所: サーバを複数ユーザで共有できる、デバッグが容易
   - 短所: セキュリティ（認証/認可）やネットワーク設定が必要

7) セキュリティ・運用上の注意点
- ワークスペースのファイルを外部へ送信する場合は必ずユーザの許可を得る（設定と明示的同意）
- サーバが任意のファイルパスへアクセスする機能は危険。サンドボックスや許可済みパス制限を導入する
- 認証: サーバが外部公開される場合はトークン・TLS を必須にする

8) 必要ファイルと初期作業（Node.js 例）
- files to create:
   - `mcp-server/package.json`
   - `mcp-server/src/index.ts` (Express/Fastify + /context)
   - `vscode-extension/` (必要なら拡張の雛形)
   - `Document/tasks.md` に実装タスクを記載
- 初期コマンド例（Windows / cmd.exe）:
```
# サーバ雛形の作成
cd mcp-server
npm init -y
npm install express body-parser
tsc --init
```

9) サンプル最小 API の動作確認方法
- サーバ起動後、curl 等で POST /context にデータを送って期待する JSON が返るか確認

10) 次の推奨アクション（選択肢）
- A: 公式 MCP 仕様やリポジトリへのアクセスを入手してから厳密実装に進む（推奨）
- B: まずは「MCP っぽい」最小サーバ（HTTP API）と VSCode 拡張のプロトタイプを作り、後で公式仕様に沿った調整を行う（迅速な PoC）

まとめ: 今回の検索では公開されている公式 README/spec の完全取得に失敗したため、まずは上記の実装パターンと最小 API を雛形として進めることを提案します。公式ドキュメントが入手できれば、すぐに仕様に合わせてサーバ API と拡張側を調整できます。

---
作業状態: この追記は完了しました。次に進める場合は、どの実装スタック（Node/Python/.NET）でのサンプルを作るか指示をください。私は選択後、最小動作するサーバと VSCode 側の接続サンプルを作成します。

## 追加調査: `github-mcp-server` の概要と導入手順（2025-11-02）

概要:
- `github-mcp-server` は GitHub が公開している MCP（Model Context Protocol）サーバ実装で、AI エージェントや VS Code 等の MCP ホストから GitHub のリポジトリ、Issue、PR、Actions、セキュリティ情報などにアクセスするためのツール群を提供します。リモート（GitHub ホスト提供）版とローカル版の両方があり、ローカルは Docker コンテナやビルド済バイナリで動かせます。

主要な特徴:
- リポジトリ参照、コード検索、コミット/ファイル取得などの豊富な "tools" を持つ。
- ユーザが有効にした toolset（例: repos, issues, actions, code_security 等）を制御可能。
- Docker イメージ（ghcr.io/github/github-mcp-server）で簡単にローカル起動できる。
- `stdio` モードでローカルバイナリを標準入出力でホストと繋ぐ設定が可能（VSCode の MCP 設定で command/args に指定）。

導入・実行の概略手順:

1) 前提
- Docker が使える環境（ローカル起動）または Go 環境（ソースからビルド）
- GitHub Personal Access Token（PAT）を用意。最小権限のみ付与する（repo、read:packages、read:org など必要に応じて）

2) ローカル（Docker）での起動（推奨：試用用／開発用）
- Windows(cmd) の簡単な例:
```cmd
REM 環境変数に PAT をセット（セッション内）
set GITHUB_PERSONAL_ACCESS_TOKEN=your_token_here

REM コンテナ起動（read-only モードやツールセットを ENV で指定可能）
docker run -i --rm -e GITHUB_PERSONAL_ACCESS_TOKEN=%GITHUB_PERSONAL_ACCESS_TOKEN% ghcr.io/github/github-mcp-server
```
- 追加オプション例（ツールセット限定）:
```cmd
docker run -i --rm -e GITHUB_PERSONAL_ACCESS_TOKEN=%GITHUB_PERSONAL_ACCESS_TOKEN% -e GITHUB_TOOLSETS="repos,issues,actions" ghcr.io/github/github-mcp-server
```

3) VS Code（または他の MCP ホスト）への設定例
- VS Code のホスト設定（MCP サーバ定義）に以下の JSON ブロックを追加します（Remote GitHub MCP を使う場合の例）:

```json
{
   "servers": {
      "github": {
         "type": "http",
         "url": "https://api.githubcopilot.com/mcp/"
      }
   }
}
```

- ローカル Docker を command 指定で使う場合の例（`.vscode/mcp.json` やホストの MCP 設定に追加）:

```json
{
   "inputs": [{ "type": "promptString", "id": "github_token", "description": "GitHub Personal Access Token", "password": true }],
   "servers": {
      "github": {
         "command": "docker",
         "args": ["run", "-i", "--rm", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
         "env": { "GITHUB_PERSONAL_ACCESS_TOKEN": "${input:github_token}" }
      }
   }
}
```

4) ビルド（ソースから）
- Go 環境がある場合:
```cmd
cd <repo_root>\cmd\github-mcp-server
go build -o github-mcp-server
set GITHUB_PERSONAL_ACCESS_TOKEN=your_token_here
github-mcp-server stdio
```
- ビルド済バイナリを MCP ホストの `command` に指定して stdio モードで接続することで、拡張からプロセスを起動して使えます。

5) 設定と運用上のポイント
- トークン管理: PAT は最小スコープで発行し、.env か OS のセキュアなストアで管理する。`.env` をリポジトリにコミットしないこと。
- ネットワーク: リモート `https://api.githubcopilot.com/mcp/` を使う場合は OAuth/ホスト側の認可フローに従う。
- ツールセット制御: `--toolsets` / `GITHUB_TOOLSETS` で有効化する機能を限定し、不要なアクセスを避ける。
- Read-only モード: 書き込み権限が不要なら `--read-only` を使う。開発や調査での安全対策になる。

6) 参考となるコマンド（Windows / cmd の例）
```cmd
REM Docker で最小起動
set GITHUB_PERSONAL_ACCESS_TOKEN=your_token_here
docker run -i --rm -e GITHUB_PERSONAL_ACCESS_TOKEN=%GITHUB_PERSONAL_ACCESS_TOKEN% ghcr.io/github/github-mcp-server

REM Docker 起動（ツールセット限定）
docker run -i --rm -e GITHUB_PERSONAL_ACCESS_TOKEN=%GITHUB_PERSONAL_ACCESS_TOKEN% -e GITHUB_TOOLSETS="default,code_security" ghcr.io/github/github-mcp-server

REM ソースからビルドして stdio モードで起動 (Go がインストール済の場合)
cd path\to\github-mcp-server\cmd\github-mcp-server
go build -o github-mcp-server
set GITHUB_PERSONAL_ACCESS_TOKEN=your_token_here
.\github-mcp-server stdio
```

まとめと推奨:
- まずは Docker でローカル起動して VS Code 側で MCP 設定を行い、動作を確認するのが早く安全です。トークンの権限は最小化し、読み取り専用モードで試験することを推奨します。
- 必要なら私の方で `Document/tasks.md` に「ローカル Docker での起動手順のスクリプト作成」「`.vscode/mcp.json` の雛形追加」「簡易検証手順」のタスクを作成します。
