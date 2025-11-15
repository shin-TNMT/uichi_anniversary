## story_settings.md

## 概要
このドキュメントは、ゲームのストーリー設計と会話（NPC）を軸にした収集システムの仕様を整理するためのフォーマット（テンプレート）です。

ゲームジャンル: PRG（探索要素含む） + ノベル（会話重視）のハイブリッド
コア概念: 戦闘は無く、NPCとの会話を通じて情報やアイテムを得て進行する。会話選択により物語が分岐し、イベント／アイテム取得に影響を与える。

---

## ドキュメントの目的（Contract）
- 入力: ストーリー要件（シナリオ案、登場人物、世界観）、設計方針（会話でアイテムを与えるルール）。
- 出力: 開発で使える `story_settings.md`（本ファイル）、会話データの JSON スキーマ例、主要 NPC とシナリオの要約。
- エラーモード: 不整合な会話データ（start/next が未定義）、UI 表示崩れ。
- 成功基準: 各 NPC の会話が JSON で定義され、DialogManager が正しく読んで表示し、会話結果でアイテムやフラグが更新されること。

---

## 目次（テンプレート）
1. プロジェクトサマリ（短い一文）
2. コアプレイとゲームループ
3. ストーリー要約（全体）
4. 主要登場人物（NPC）一覧
5. 世界観・ロケーション概要
6. 会話システム設計（フォーマット・JSON スキーマ）
7. アイテム付与ルール / フラグ設計
8. シーンと進行フロー（起点・分岐の例）
9. UI / UX ガイドライン
10. ローカライズ（i18n）とテキスト管理
11. テスト観点と品質ゲート
12. 作業 TODO（短期・中期）
13. 参考・追記

---

## 1) プロジェクトサマリ
(例)
プレイヤーは町やフィールドを歩き回り、NPC と会話して情報やアイテムを入手し、依頼を達成して物語を進める。戦闘は無く、会話の選択肢と取得アイテムがゲーム内の進行を作る。

---

## 2) コアプレイとゲームループ
- プレイヤーは探索 → NPC 接触 → 会話による情報収集／アイテム獲得 → 新しい探索先解放
- 会話はノード式（line / choice / end）で記述。選択肢によって次のノードIDが変わり、特定のノードでアイテム付与やフラグ設定が行われる。
- グローバルな "seen" フラグで NPC ごとの既読判定を行い、初回挨拶と再訪問挨拶を切り替える。

---

## 3) ストーリー要約（高レベル）
- 冒頭: ポーション屋さんの「ういち」が倉庫を確認すると、開店に必要な材料が無くなっていることに気づきます。
  このままでは店を開けないため、プレイヤー（店主または店主の手伝い）は仲間（ダチ）たちに会い、会話を通じて素材を集めていきます。
- 中盤: 各ダチが持っている素材は会話選択や小さな依頼で入手可能。選択肢やフラグにより、入手方法や順序が変化します。
- 終盤: 必要な素材をすべて集めきると、無事にお店を開店でき、エンディングに繋がります。

(具体的なサブプロットやサイドイベントは別ファイルで管理します。ここでは会話→素材収集→開店という主要フローを設計対象とします)

---

## 4) 主要登場人物（NPC）一覧（テンプレート）
以下は本ストーリーに合わせた初期の主要 NPC 例です。各 NPC ごとに会話ファイルを作成し、会話で素材やフラグを与えるようにします。

- ID: uichi
  - 表示名: ういち
  - 役割: プレイヤー (Player)
  - 初期位置: Town.tscn の店（倉庫）
  - 会話ファイル: res://dialogues/uichi.json
  - 関連フラグ: uichi_needs_materials (bool)
  - 取得可能アイテム: （プレイヤー：このキャラクター自身は素材を渡さない／サポート役）

(必要に応じて NPC を追加し、会話ファイルとアイテム定義を作成してください)

-- 以下は現状の NPC 名簿（ゲームに登場するダチ一覧） --

- ID: npc_kua
  - 表示名: くあ
  - 会話ファイル: res://dialogues/kua.json
  - 関連フラグ: kua_helped
  - 取得可能アイテム: material_kua

- ID: npc_oshikatsu_purin
  - 表示名: 推し活プリン
  - 会話ファイル: res://dialogues/oshikatsu_purin.json
  - 関連フラグ: oshikatsu_helped
  - 取得可能アイテム: material_purin

- ID: npc_futami
  - 表示名: 二見
  - 会話ファイル: res://dialogues/futami.json
  - 関連フラグ: futami_helped
  - 取得可能アイテム: material_futami

- ID: npc_feniki
  - 表示名: フェニキ
  - 会話ファイル: res://dialogues/feniki.json
  - 関連フラグ: feniki_helped
  - 取得可能アイテム: material_feniki

- ID: npc_kiraccho
  - 表示名: キラっちょ
  - 会話ファイル: res://dialogues/kiraccho.json
  - 関連フラグ: kiraccho_helped
  - 取得可能アイテム: material_kiraccho

- ID: npc_toramoto_taiga
  - 表示名: 虎本タイガ
  - 会話ファイル: res://dialogues/toramoto_taiga.json
  - 関連フラグ: toramoto_helped
  - 取得可能アイテム: material_taiga

- ID: npc_feikaraiya
  - 表示名: フェイカライア
  - 会話ファイル: res://dialogues/feikaraiya.json
  - 関連フラグ: feikaraiya_helped
  - 取得可能アイテム: material_feikaraiya

- ID: npc_konpeito
  - 表示名: 金平糖
  - 会話ファイル: res://dialogues/konpeito.json
  - 関連フラグ: konpeito_helped
  - 取得可能アイテム: material_konpeito

- ID: npc_menchop
  - 表示名: めんちょP
  - 会話ファイル: res://dialogues/menchop.json
  - 関連フラグ: menchop_helped
  - 取得可能アイテム: material_menchop

- ID: npc_white_stew
  - 表示名: ほわいとしちゅー
  - 会話ファイル: res://dialogues/white_stew.json
  - 関連フラグ: white_stew_helped
  - 取得可能アイテム: material_stew

- ID: npc_nemu
  - 表示名: ねむ
  - 会話ファイル: res://dialogues/nemu.json
  - 関連フラグ: nemu_helped
  - 取得可能アイテム: material_nemu

- ID: npc_morino_kuma
  - 表示名: 森野クマ
  - 会話ファイル: res://dialogues/morino_kuma.json
  - 関連フラグ: morino_kuma_helped
  - 取得可能アイテム: material_kuma

- ID: npc_namizou
  - 表示名: ナミゾウ
  - 会話ファイル: res://dialogues/namizou.json
  - 関連フラグ: namizou_helped
  - 取得可能アイテム: material_nami

- ID: npc_glutamic_acid
  - 表示名: グルタミン酸
  - 会話ファイル: res://dialogues/glutamic_acid.json
  - 関連フラグ: glutamic_helped
  - 取得可能アイテム: material_glutamic

- ID: npc_shunu
  - 表示名: しゅぬ
  - 会話ファイル: res://dialogues/shunu.json
  - 関連フラグ: shunu_helped
  - 取得可能アイテム: material_shunu

- ID: npc_zaurus
  - 表示名: ざうるす
  - 会話ファイル: res://dialogues/zaurus.json
  - 関連フラグ: zaurus_helped
  - 取得可能アイテム: material_zaurus

- ID: npc_ringo_yaya
  - 表示名: りんごヤヤ
  - 会話ファイル: res://dialogues/ringo_yaya.json
  - 関連フラグ: ringo_yaya_helped
  - 取得可能アイテム: material_apple

- ID: npc_taro_chan
  - 表示名: たろちゃん
  - 会話ファイル: res://dialogues/taro_chan.json
  - 関連フラグ: taro_chan_helped
  - 取得可能アイテム: material_taro

- ID: npc_mahirun
  - 表示名: まひるん
  - 会話ファイル: res://dialogues/mahirun.json
  - 関連フラグ: mahirun_helped
  - 取得可能アイテム: material_mahirun

- ID: npc_chitose
  - 表示名: ちとせ
  - 会話ファイル: res://dialogues/chitose.json
  - 関連フラグ: chitose_helped
  - 取得可能アイテム: material_chitose

- ID: npc_sample
  - 表示名: サンプル
  - 会話ファイル: res://dialogues/npc_sample.json
  - 関連フラグ: npc_sample_helped
  - 取得可能アイテム: material_sample


勝利条件（現行実装）
- 方式（デフォルト）: プレイヤーが合計で一定数のアイテム（個数合計）を集めるとゲームクリアになります。現行実装では `scripts/GameState.cs` の `winThreshold` がデフォルト 14 に設定されています（合計 14 個で勝利）。

- 変更方法:
  - 合計数基準を変える場合は `scripts/GameState.cs` の `winThreshold` の値を変更してください。
  - 「異なるアイテム種類数で判定」など別の判定ルールにする場合は `GameState` を拡張してください（例: distinct item count を使う、特定の必須アイテムリストを設定する等）。

実装メモ / JSON の書き方
- 会話ノードでアイテムを付与するにはノードに `actions` を追加します。例:

  { "id": "give_item", "type": "line", "text": "草の葉を渡した。", "actions": [ { "give_item": "herb_leaf" } ], "next": "end" }

- 複数アイテムを同時に与えたい場合は `actions` に複数の `give_item` オブジェクトを並べます。
- `set_flag` や `require_flag` のような動作は今後実装可能です（DialogManager 側でフラグ管理を追加する必要あり）。

---

## 5) 世界観・ロケーション概要（テンプレート）
- Town: 商人や住民がいる中心地。複数 NPC が固有の会話を持つ。
 

---

## 6) 会話システム設計（フォーマット）
以下は既存実装に合わせた JSON フォーマットの例です。DialogManager はこの形式を読み込み、UI を更新します。

JSON 例（既存フォーマットに合わせる）:

{
  "start": "greet_new",
  "start_seen": "greet_seen",
  "nodes": [
    { "id": "greet_new", "type": "line", "speaker": "商人", "text": "いらっしゃいませ！", "next": "ask_help" },
    { "id": "ask_help", "type": "choice", "choices": [ { "text": "助ける", "next": "give_item" }, { "text": "忙しい", "next": "end" } ] },
    { "id": "give_item", "type": "line", "speaker": "商人", "text": "これをあげよう", "next": "end", "actions": [ { "give_item": "amulet_fragment" }, { "set_flag": "merchant_helped" } ] }
  ]
}

拡張フィールド（提案）:
- actions: 会話ノード到達時に実行する副作用のリスト（例: give_item, set_flag, require_flag, remove_item）
- require_flags / require_items: ノードに入るための前提条件
- once: true の場合、到達後 seen フラグや同等の処理を行う

---

## 7) アイテム付与ルール / フラグ設計
- give_item: プレイヤーのインベントリにアイテムを追加する。重複を許可するかどうかはアイテム定義で決定。
- set_flag: シナリオ進行用のブールフラグ。例: merchant_helped = true
- require_flag / require_item: ノードや選択肢が現れるための条件。

例: 会話内で選択肢を表示する条件
- ノードで choices を描く際に、各 choice に require_flags を追加できる:
  { "text": "もっと聞く", "next": "detail", "require_flags": ["merchant_helped"] }

---

## 8) シーンと進行フロー（例）
- Town 起動 → DialogManager ノードを探索 → NPC に接触 → NPC が StartDialogue を呼ぶ
- プレイヤーは選択肢を選ぶ → DialogManager が次ノードに遷移 → actions を実行
- アイテム入手後は Inventory システム（別モジュール）に通知

---

## 9) UI / UX ガイドライン
- 会話ウィンドウは画面下部固定（最大幅を決める）。選択肢はボタンリストで表示。
- "次へ" は行モードのみ表示。選択肢モードでは隠す。重複表示を避けるため、DialogManager は複製ボタンを検出して非表示にする。
- テキストは日本語のみを想定（ローカライズ対応は不要）。
 - テキストは日本語のみを想定（ローカライズ対応は不要）。

### 会話をモーダルとして展開する（仕様）

- 目的: 会話を他の操作から切り離して集中して操作できるように、"モーダル" なダイアログとして表示する。
- 表示方式: Godot の `WindowDialog` または `PopupPanel`（Control 系）を用い、専用の `CanvasLayer`（UI 層）上に PackedScene を Instantiate して表示する。
- 見た目: 小ウィンドウ風（タイトルバー、閉じるボタン、背景の半透明オーバーレイ）で、画面中央に表示するのが基本。
- モーダル挙動:
  - 会話表示中はプレイヤー入力（移動・攻撃等）を無効化する（推奨）。実装は `GetTree().Paused = true` や、プレイヤー制御スクリプトでフラグを切る方法を使用。
  - 背景クリックで閉じるかは会話デザイン次第（重要な会話は明示的な閉じるボタンのみを許可する）。
- 実装コントラクト（簡潔）:
  - 入力: DialogManager が表示したい会話データ（JSON）と、オプション（モーダル/非モーダル、中央配置/右下など）。
  - 出力: 画面上に表示されたダイアログノード。閉じたときは QueueFree され、必要ならコールバックで終了イベントを返す。
  - エラー: PackedScene のロード失敗や Instantiate 例外はログ出力してフォールバック UI を表示する。

#### 実装メモ（C# / Godot）

1. ダイアログ PackedScene を `res://scenes/ui/DialogWindow.tscn` として作成（Root: `WindowDialog` または `Panel`）。`PopupCentered()` をサポートする設計推奨。
2. `DialogManager` の ShowDialog メソッドを差し替え、既存の埋め込み UI を使わず PackedScene を Instantiate して `GetTree().Root.AddChild(dialog)`（または `GetTree().Root.GetNode("UIRoot")`）で追加する。
3. 表示開始時に以下を行う:
   - (オプション) `GetTree().Paused = true;`（ゲームを一時停止）またはプレイヤーコントローラの入力無効化フラグを設定。
   - `dialog.PopupCentered();` もしくは `dialog.Visible = true;` を呼ぶ。
4. 閉じる時:
   - `dialog.QueueFree();`
   - (オプション) `GetTree().Paused = false;` または入力無効化フラグの解除。

短い擬似コード:

```csharp
var packed = GD.Load<PackedScene>("res://scenes/ui/DialogWindow.tscn");
var dlg = (WindowDialog)packed.Instantiate();
GetTree().Root.AddChild(dlg);
// モーダル開始
GetTree().Paused = true;
dlg.PopupCentered();
// 閉じるハンドラで
// dlg.QueueFree(); GetTree().Paused = false;
```

#### 考慮事項 / エッジケース
- モバイル対応: 画面サイズが小さいためフルスクリーンのモーダルや縦向け最適化を検討する。
- フォーカス管理: モーダル中にキーボード入力を扱う場合、フォーカスが正しくダイアログ内にあることを保証する。
- 複数ダイアログ: 連続で開く可能性がある場合は Z-order と Pause の取り扱いを定義する（例: スタック式の Pause 管理）。
- アニメーション: 開閉時のアニメーションを追加すると UX が向上するが、ポップアップのタイミングで Pause を入れるかは検討。

#### テストチェックリスト
- ダイアログが中央に表示されること（各解像度）。
- モーダル中にプレイヤー入力が無効化されること。
- ダイアログを閉じるとゲームが正常に再開すること（Pause が解除される）。
- PackedScene のロード失敗時、フォールバック UI（簡易 Panel）が表示されること。
- 複数連続で会話を実行したときに UI が重複して残らないこと。

---

## 10) ローカライズについて

- 本プロジェクトは日本語のみ対応とします。ローカライズ（i18n）対応は不要です。
- 会話テキストや UI 表示はすべて日本語で JSON 内に直接記述してください。
- 将来的に多言語対応が必要になった場合は、その時点で設計（キー化・翻訳ファイルの導入・翻訳ワークフロー）を検討しますが、現段階では対応しません。

---

## 11) テスト観点と品質ゲート
- JSON スキーマバリデーション（start が存在するか、全 next が定義されているか）
- DialogManager 単体テスト: 読み込み、ノード遷移、actions 実行を確認
- UI テスト: 選択肢表示／非表示、Next ボタンの挙動

---

## 12) 作業 TODO（短期）
- [ ] story_settings.md をチームでレビュー
- [ ] 会話 JSON のスキーマを作成（JSON Schema）
- [ ] 既存の `dialogues/*.json` を上記スキーマに合わせて整備
- [ ] DialogManager の actions 実行フックを実装（give_item / set_flag / require_item）

---

## 13) 参考・追記
- 既存の DialogManager 実装 (`scripts/DialogManager.cs`) を参照し、actions 実行とノード条件チェックを組み込む。

---

## 付録: 会話ノードの仕様（簡潔スキーマ）
- Node:
  - id: string (必須)
  - type: "line" | "choice" | "end"
  - speaker: string (任意)
  - text: string (line の場合)
  - choices: array (choice の場合) [{ text: string, next: string, require_flags?: [string], require_items?: [string] }]
  - next: string (line の場合)
  - actions: array (任意) 例: [{ "give_item": "amulet_fragment" }, { "set_flag": "merchant_helped" }]
  - once: boolean (任意)



---

このテンプレートを基に、次は実際のストーリー案（短いあらすじを 1 ページ程度）と主要 NPC の一覧を追加してください。
