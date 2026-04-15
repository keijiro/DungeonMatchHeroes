# 実装プラン - 戦闘ポップアップ数値の表示

このプランでは、戦闘中にキャラクターの上にダメージ量、回復量、シールド量、およびブロック量を表示する数値ポップアップ通知の実装について概説します。これは既存のフラッシュ演出と同期して表示されます。

## プロジェクト概要
- **ゲームタイトル**: Match-Combat Prototype
- **ハイレベルコンセプト**: マッチ3パズルとターン制RPGを組み合わせたハイブリッドゲーム。
- **プレイヤー**: シングルプレイヤー vs AIウェーブ。
- **レンダーパイプライン**: URP (2D Renderer)。
- **UI システム**: Unity UI Toolkit (UITK)。

## ゲームメカニクス
### 戦闘フィードバック
- **ポップアップ数値**: キャラクターがダメージを受ける、回復する、またはシールドを適用する際に、その数値がキャラクターの上に表示されます。
- **色分け**:
    - **ダメージ**: 赤 (Red)
    - **HP回復**: 緑 (Green)
    - **シールド回復**: 青 (Blue)
    - **シールドによるブロック**: 黄色 (Yellow)
- **ビジュアルスタイル**: 暗い背景でも読みやすいよう、白の縁取り（アウトライン）を施した大きく太い数値。
- **アニメーション**: 数値は一瞬で出現（スケール 0.0 から 1.0）、1秒間表示を維持し、その後0.5秒かけて透明になって消えます。

## UI
### USS スタイル
`Main.uss` に新しい `.combat-number` クラスを追加します。
- 自由な配置のための `position: absolute`。
- 座標の中心にラベルを配置するための `translate: -50% -50%`。
- `-unity-text-outline-color: white` および `-unity-text-outline-width: 2px` による白の縁取り。
- `.container` から継承したフォント定義（Pirata One）。

### 通知レイヤー
既存の `Main.uxml` 内にある `notification-layer` を、これらのポップアップの親として使用します。

## 主要アセットとコンテキスト
### 修正するスクリプト
- `Assets/Scripts/CombatManager.cs`: ポップアップの生成、アニメーション、トリガーロジック。
- `Assets/UI/Main.uss`: ポップアップラベルのスタイリング。

### ロジックのポイント
- **配置**: `RuntimePanelUtils.CameraTransformWorldToPanel` を使用して、キャラクターのワールド座標をUI空間にマッピングします。
- **重心**: パーティー全体の効果（回復など）には、Fighter、Mage、Tank の平均位置を使用します。

## 実装ステップ

### 1. UI スタイルの更新
- `Assets/UI/Main.uss` を修正します。
- 指定されたアウトラインとフォント設定を持つ `.combat-number` クラスを追加します。
- **依存関係**: なし。

### 2. `CombatManager` へのポップアップロジックの実装
- `ShowCombatNumber(int value, Color color, Vector3 worldPos)` メソッドを追加。
- `AnimateCombatNumber(Label label)` コルーチンを追加。
- ヘルパーメソッド `GetCharacterCenter(GameObject go)` と `GetPartyCentroid()` を追加。
- **依存関係**: ステップ 1。

### 3. プレイヤーアクションとの統合
- **プレイヤー攻撃**: `HandlePlayerAttack` 内で `ShowCombatNumber` を呼び出し（赤、ターゲットの上）。
- **メイジ魔法**: `HandlePlayerMagicAttack` 内でヒットした各敵に対して `ShowCombatNumber` を呼び出し（赤）。
- **回復**: `ExecuteAction` の `PlayerHeal` 内で `ShowCombatNumber` を呼び出し（緑、パーティーの重心）。
- **シールド**: `ExecuteAction` の `PlayerShield` 内で `ShowCombatNumber` を呼び出し（青、Tank の上）。
- **依存関係**: ステップ 2。

### 4. 敵アクションとの統合
- **フルブロック**: `HandleEnemyAttack` 内で `ShowCombatNumber` を呼び出し（黄色、Tank の上）。
- **ダメージ**: ダメージが完全にブロックされなかった場合、`HandleEnemyAttack` 内でパーティーメンバー3人全員に対して `ShowCombatNumber` を呼び出し（赤）。
- **依存関係**: ステップ 2。

## 検証とテスト
### 手動チェック
1.  **ダメージ**: 剣（Sword）をマッチさせ、手前の敵の上に赤い数値が表示されることを確認。
2.  **AOEダメージ**: 魔法（Magic）をマッチさせ、すべての敵の上に赤い数値が表示されることを確認。
3.  **回復**: ハート（Heal）をマッチさせ、パーティーの中央に緑の数値が表示されることを確認。
4.  **シールド**: シールド（Shield）をマッチさせ、Tank の上に青い数値が表示されることを確認。
5.  **ブロック**: シールドがある状態で敵の攻撃を受け、Tank の上に黄色の数値が表示されることを確認。
6.  **アニメーション**: 数値が一瞬でポップし、待機後、ジャンプすることなくスムーズに消えることを確認。
7.  **重なり**: グリッドのマッチ通知（"Attack! 10 pts"）と戦闘数値（"10"）が両方表示され、可読性に問題がないことを確認。
