# Project Overview
- **Game Title**: Match-3 RPG Prototype
- **High-Level Concept**: 7x7のパズルグリッドでブロックを消してアクションを生成し、敵と戦うターン制RPGプロトタイプ。
- **Players**: シングルプレイヤー vs AI
- **Inspiration / Reference Games**: 一般的なマッチ3RPG
- **Tone / Art Direction**: スタイライズド2D、URP 2D Rendererを使用。
- **Target Platform**: PC / Mac (Standalone)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: URP (Universal Render Pipeline) with 2D Renderer

# Game Mechanics
## Core Gameplay Loop
1. プレイヤーがグリッドの最下段をクリックしてブロックを消す。
2. 重力でブロックが落下し、3つ以上揃うとマッチが発生。
3. マッチしたブロックの種類（剣、魔法、盾など）に応じてCombatManagerにアクションがキューイングされる。
4. CombatManagerがアクションを順次実行し、敵にダメージを与えたりプレイヤーを回復したりする。
5. 敵はタイマーに基づき自動で攻撃を行う。

## Controls and Input Methods
- マウスによる最下段ブロックのクリック。
- New Input System を使用。

# UI
- **UI Toolkit (UITK)** を使用。
- 画面下部のHUD（HP、シールド、経験値、鍵の状態）と、画面上の通知レイヤー（ダメージ数値などのフローティングテキスト）。

# Key Asset & Context
- **Assets/Shaders/SpriteOverlay.shader** (新規): `_OverlayColor` プロパティを持ち、テクスチャ色に対してアルファブレンディング（Lerp）で色を重ねるカスタムURP 2Dシェーダー。
- **Assets/Materials/EnemyOverlay.mat** (新規): 上記シェーダーを使用する共通マテリアル。
- **Assets/Scripts/EnemyUnit.cs** (既存変更): `MaterialPropertyBlock` を使用して、ダメージ時のフラッシュ演出を実装。

# Implementation Steps

1. **カスタムシェーダーの作成**
   - `Assets/Shaders/SpriteOverlay.shader` を作成。
   - `_OverlayColor` (Color, Default: 0,0,0,0) プロパティを定義。
   - フラグメントシェーダー内で、サンプリングしたテクスチャ色（頂点カラー乗算後）に対し、`_OverlayColor` をアルファ値に基づき `lerp` する処理を実装。
   - SRP Batcher に対応させるため、適切な CBUFFER 構造を維持する。

2. **共通マテリアルの作成**
   - `Assets/Materials/EnemyOverlay.mat` を作成。
   - `SpriteOverlay` シェーダーを割り当てる。

3. **敵プレハブの更新**
   - `Fighter`, `Mage`, `Tank` などの敵プレハブの `SpriteRenderer` に、`EnemyOverlay` マテリアルをセットする。

4. **EnemyUnit.cs のダメージ演出ロジック変更**
   - `MaterialPropertyBlock` のインスタンスを保持。
   - `_OverlayColor` のプロパティIDを取得。
   - `HitFeedback` コルーチン内で、`sr.color = Color.red` の代わりに、`MaterialPropertyBlock` を介して `_OverlayColor` を `(1, 0, 0, 1)`（赤色、アルファ1）に設定。
   - 待機後、`(0, 0, 0, 0)` に戻す。

# Verification & Testing
- **視覚確認**: 敵にダメージを与えた際、シルエットが真っ赤に塗りつぶされるようなフラッシュが発生することを確認。
- **バッチング確認**: `Frame Debugger` を開き、複数の敵が同時に描画される際に Draw Call が結合されていることを確認。
- **エッジケース**: 敵が消滅する際、演出が途切れてもエラーが発生しないか、マテリアルプロパティが適切にクリアされているかを確認。
