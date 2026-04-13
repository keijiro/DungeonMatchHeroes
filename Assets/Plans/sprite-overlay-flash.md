# Project Overview
- **Game Title**: Match-3 RPG Prototype
- **High-Level Concept**: 7x7のパズルグリッドでブロックを消してアクションを生成し、敵と戦うターン制RPGプロトタイプ。
- **Players**: シングルプレイヤー vs AI
- **Tone / Art Direction**: スタイライズド2D、URP 2D Rendererを使用。
- **Render Pipeline**: URP (Universal Render Pipeline) with 2D Renderer

# Game Mechanics
## Core Gameplay Loop
1. プレイヤーがグリッドの最下段をクリックしてブロックを消す。
2. 重力でブロックが落下し、3つ以上揃うとマッチが発生。
3. マッチしたブロックの種類に応じてCombatManagerにアクションがキューイングされる。
4. CombatManagerがアクションを順次実行し、敵にダメージを与えたりプレイヤーを回復したりする。

# Key Asset & Context
- **Assets/Shaders/SpriteOverlay.shader** (新規): `_OverlayColor` プロパティを持ち、テクスチャ色に対してアルファブレンディング（Lerp）で色を重ねるカスタムURP 2Dシェーダー。GPU Instancing に対応。
- **Assets/Resources/EnemyOverlay.mat** (新規): 上記シェーダーを使用する共通マテリアル。動的な読み込みのため `Resources` フォルダに配置。
- **Assets/Scripts/EnemyUnit.cs** (既存変更): 通常時はデフォルトマテリアルを使用し、ダメージ演出時のみマテリアルを差し替えつつ `MaterialPropertyBlock` で色を制御するハイブリッド・アプローチを実装。

# Implementation Steps (Final Approach)

1. **カスタムシェーダーの作成**
   - `Assets/Shaders/SpriteOverlay.shader` を作成。
   - `_OverlayColor` (Color, Default: 0,0,0,0) プロパティを定義。
   - **GPU Instancing 対応**: `UNITY_INSTANCING_BUFFER` を使用してプロパティを宣言。
   - **アーティファクト対策**: フラグメントシェーダーの最終出力直前に `color.rgb *= color.a` (Premultiply Alpha) を行い、スプライト周囲のゴミを解消。

2. **共通マテリアルの作成と配置**
   - `Assets/Resources/EnemyOverlay.mat` を作成。
   - スクリプトからの `Resources.Load<Material>` を可能にするため、必ず `Resources` フォルダ配下に置く。

3. **敵プレハブの設定**
   - 敵キャラクターの `SpriteRenderer` には、通常時（非演出時）の描画効率を最大化するため、標準の `Sprite-Unlit-Default` マテリアルを設定したままにする。

4. **EnemyUnit.cs の演出ロジック実装**
   - **初期化**: `Start` 時に元のマテリアル（デフォルト）を保持し、演出用マテリアルをロード。
   - **演出（HitFeedback）**: 
     1. 攻撃を受けた瞬間、`sr.sharedMaterial` を演出用マテリアルに差し替える。
     2. `MaterialPropertyBlock` を介して `_OverlayColor` を `(1, 0, 0, 1)`（赤色、アルファ1）に設定。
     3. 一定時間（0.1秒）待機。
     4. `SetPropertyBlock(null)` でプロパティをクリアし、マテリアルを元のデフォルトに戻す。

# Verification & Testing
- **視覚確認**: 敵にダメージを与えた際、シルエットが真っ赤に塗りつぶされるようなフラッシュが発生し、終了後に元の色に戻ることを確認。
- **パフォーマンス**: 通常時は標準シェーダーでバッチングされ、演出時も GPU Instancing により Draw Call が抑制されていることを確認。
- **エッジケース**: 透明度を持つスプライトでも、周囲に黒い枠やゴミが出ないことを確認。
