# uichi_anniversary (最小プロジェクト)

このリポジトリに最小の Godot (Mono) プロジェクトの雛形を追加しました。

含まれるもの:
- `scenes/Main.tscn` — Node2D の最小シーン（`scripts/Main.cs` を参照）
- `scripts/Main.cs` — C# スクリプト（_Ready でログ出力）

動作確認手順（Windows / Godot Mono）:
1. Godot エディタ (v4.5.1.stable.mono 推奨) を起動し、プロジェクトフォルダ `c:\Users\owner\Documents\uichi_anniversary` を開く。
2. エディタで `scenes/Main.tscn` を開き、メニューから「シーンを実行」または「Play Scene」を実行する。
3. C# ソリューションの生成が求められた場合は生成し、IDE（Visual Studio / Rider / VSCode + C# 拡張）でビルドしても良い。

備考:
- .NET SDK のバージョンが Godot の Mono 設定と合っていることを確認してください（推奨: 9.0.306）。

## 改行ポリシー (Line endings)

このリポジトリでは改行を一貫化するために `.gitattributes` を利用しています。主な方針:

- リポジトリはテキストファイルを自動検出し、プラットフォームに合わせてチェックアウトします (`* text=auto`)。
- Windows 開発者向けに `.cs`, `.sln`, `.csproj` 等はチェックアウト時に CRLF に変換されます（`text eol=crlf`）。
- ドキュメント（`.md`）やシェルスクリプト（`.sh`）、YAML は LF を使います（`text eol=lf`）。
- Godot のシーン／プロジェクトファイル（`project.godot`, `*.tscn`）は LF を使用します。
- バイナリファイルは `binary` として扱い、改行正規化を行いません。

開発者向け簡単手順:

1. リポジトリを初めてクローンしたら、そのまま作業を開始できます。既にチェックアウト済みのファイルの改行をリポジトリ定義に合わせて再正規化するには:

```sh
git add --renormalize .
git commit -m "chore: normalize line endings"
```

2. `.gitattributes` を更新する場合は、更新後に同様に `git add --renormalize .` を実行してください。
3. CI では特別な設定は不要です（リポジトリがチェックアウトされる際に `.gitattributes` が適用されます）。

このセクションは `.gitattributes` の内容を簡潔にまとめたものです。詳細はリポジトリルートの `.gitattributes` を参照してください。
