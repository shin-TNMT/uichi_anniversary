# Copilot Instructions — uichi_anniversary

## 基本
- 日本語で応答すること
- 必要に応じて、ユーザに質問を行い、要求を明確にすること
- 作業後、作業内容とユーザが次に取れる行動を説明すること

## develop
各作業を以下のように定義する。
- 「調査」と指示された場合、都度 Document/reports.md に記載すること
- 「計画」と指示した場合、Document/tasks.md に計画を記載する
    - 前回の内容が残っている場合は、読まずに消して構わない
- コードベース / Document を読み込み、要件に関連性のあるファイルパスをすべて記載すること
    - 不明な点については、fetch mcp を使用して検索すること
    - 必要最小限の要件のみを記載すること
    - このフェーズで、コードを書いては絶対にいけない
- ユーザが「実装」と指示した場合、Document/tasks.md に記載された内容に基づいて実装を行う
    - 記載されている以上の実装を絶対に行わない
    - ここでデバッグしない
- 「デバッグ」と指示された場合、直前のタスクのデバッグ「手順」のみを示す

## 作業履歴の記録
- 各作業（調査・計画・実装・デバッグ）を完了したら、必ず `Document/process.md` に実施日時、実施者、実施内容の要約、変更したファイル一覧、実行した主要コマンド、次の推奨アクションを1エントリとして追記すること。
- `Document/process.md` のフォーマット例（必須）:
    - 日付: YYYY-MM-DD
    - 実施者: <名前/自動化エージェント>
    - 要約: 1行でのサマリ
    - 変更ファイル: 箇条書きでファイルパス
    - 実行コマンド: 実行した主要コマンド（必要な場合）
    - 次のアクション: 箇条書きで短く
  
    この追記ルールは作業の可視化と CI/レビュープロセスを容易にするために必須です。

## documents
- Document/reports/*.md : 調査レポート
- Document/tasks.md : 計画
- Document/requirements.md : 要件定義

-## プロジェクト概要
- プロジェクト: uichi_anniversary
- ジャンル: 2D RPG
- エンジン: Godot Engine (Mono/.NET)
- 言語: C# (Godot の C# API を使用)

## 目的（Contract）
- 入力: このリポジトリのソース、アセット、Godotプロジェクトファイル
- 出力: 動作するGodotエディタプロジェクト、エクスポート可能なビルド、テストが通る状態
- エラーモード: 環境不一致（Godot/.NETバージョン違い）、依存未解決、アセット破損
- 成功基準: エディタでプロジェクトを開けること、主要シーンが実行可能であること

## 必須環境（明示）
- Godot: v4.5.1.stable.mono
- .NET SDK: 9.0.306
- OS: 開発者は Windows / macOS / Linux のいずれか（CIは Windows を想定する場合は明記）

（注）インストール手順・ダウンロードリンクは `Document/requirement.md` を参照してください。

## クイックセットアップ（開発者向け）
1. 上記の Godot Mono および .NET SDK をインストール
2. Godot エディタを起動し、プロジェクトフォルダを開く
3. C# のビルドが必要な場合は、IDE（Visual Studio / Rider / VSCode + C# 拡張）を使ってソリューションを復元してビルド

## プロジェクト構成（推奨）
- `project.godot` — Godot プロジェクトファイル
- `scenes/` — シーン（.tscn / .scn）を配置
- `scripts/` — C# スクリプト（名前空間を揃える）
- `assets/` — 画像、音声、タイルセット等
- `Document/` — 仕様・要求・設計メモ

## 命名規約とコーディング規約（C# / Godot）
- C# — PascalCase クラス名、camelCase メソッド/フィールド（public は PascalCase）
- ファイル名はクラス名と一致させる（例: `PlayerController.cs` 内に `PlayerController`）
- Godot のシーンは PascalCase で命名（例: `BattleScene.tscn`）
- `Exported variables` — 明確なプレフィックスを使わず、説明コメントを付ける

## シーン・ノード設計のガイドライン
- ルートノードはシーンタイプを表す（例: `Player`、`Enemy`、`BattleManager`）
- Node の命名は機能ベース（例: `HPBar`, `AttackButton`）
- 再利用可能な小パーツは `addons/` や `scenes/common/` にまとめる

## シグナル（Signals）とコールバック
- シグナル名は動詞ベース（例: `OnDamageTaken`, `DialogFinished`）
- 可能な限りシグナルで疎結合にする。シーン間通信はシグナルか中央の Manager を使う

## アセット管理
- リサイズ・圧縮ルールを Document に記載
- ソース画像（.psd/.xcf）がある場合は `assets/source/` に保存
- バイナリ大きめファイル（音声等）は LFS の利用を検討

## ローカリゼーション
- テキストは直接ハードコーディングせず、`translations/` に辞書を配置して利用する

## テストと品質保証
- C# ロジックは可能なら単体テストを追加（NUnit など）
- シーンの統合テストはエディタ上で手動実行の手順を Document に記載
