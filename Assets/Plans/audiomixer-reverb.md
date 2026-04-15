# Audio Mixer を用いた残響エフェクト（Reverb）の実装プラン

## プロジェクト概要
- ゲームタイトル: Dungeon Match-3 Combat Prototype
- 実装目標: Audio Mixer を導入し、特定の種類（例: 魔法、ダメージなど）の効果音に残響（Reverb）を付与できるようにする。

## 実装の方針
1. **Audio Mixer の作成**:
    - `Assets/Audio/MainMixer.mixer` を作成。
    - 階層構造: `Master` -> `Bypass`（通常） / `Reverb`（残響あり） の 2 つのグループを用意。
    - `Reverb` グループに `SFX Reverb` エフェクトを追加し、残響を設定。
2. **AudioManager の拡張**:
    - `SEClip` 構造体に「リバーブを使用するかどうか」のフラグを追加。
    - `AudioManager` に `AudioMixerGroup` の参照（Bypass, Reverb）を保持。
    - 再生時に `AudioSource` の `outputAudioMixerGroup` を動的に切り替えて再生。
3. **アセットの設定**:
    - 特定の SE（例: `SE_Magic`, `SE_Heal`, `SE_Title`）にリバーブフラグを立てる。

## Key Assets & Context
- `Assets/Scripts/AudioManager.cs`: 再生ロジックの変更。
- `Assets/Audio/MainMixer.mixer`: 新規作成するミキサーアセット。
- `Assets/Prefabs/AudioManager.prefab`: ミキサーグループの割り当て。

## Implementation Steps

### 1. Audio Mixer アセットの作成と設定
- **作業内容**: `Assets/Audio/MainMixer.mixer` を新規作成。
- **作業内容**: グループを `Master`, `Bypass`, `Reverb` に分割。
- **作業内容**: `Reverb` グループに `SFX Reverb` コンポーネントを追加し、プリセット（Cave や Hall など）を調整。
- **依存関係**: なし。

### 2. AudioManager.cs のスクリプト改修
- **作業内容**: `SEClip` 構造体に `bool UseReverb` を追加。
- **作業内容**: `AudioManager` クラスに `[SerializeField] private AudioMixerGroup bypassGroup;` と `[SerializeField] private AudioMixerGroup reverbGroup;` を追加。
- **作業内容**: `PlaySE` メソッド内で、対象の SE がリバーブを使用するか判定し、`source.outputAudioMixerGroup` を設定してから `PlayOneShot` を呼ぶように変更。
- **作業内容**: インスペクターでの設定を保持するための内部辞書（`reverbDictionary`）を更新。
- **依存関係**: なし。

### 3. プレハブの更新とグループの割り当て
- **作業内容**: `AudioManager.prefab` を開き、新設された `Bypass Group` と `Reverb Group` スロットにミキサーの各グループを割り当て。
- **作業内容**: 各 SE リストを確認し、残響をかけたいもの（Magic, Title, Heal 等）の `UseReverb` チェックボックスをオンにする。
- **依存関係**: ステップ 1, 2。

### 4. 動作検証
- **作業内容**: タイトル画面やゲーム内での魔法発動時に、残響が聞こえるか確認。
- **作業内容**: 残響設定が不要な音（Click 等）はクリアに聞こえるか確認。
- **作業内容**: 同時に複数の音が鳴った際に、ミキシングが正常に行われているか確認。

## Verification & Testing
1. **比較テスト**: `SE_Click`（リバーブなし）と `SE_Magic`（リバーブあり）を順に鳴らし、明らかな音響差があることを確認。
2. **ミキサーの可視化**: 再生中に Unity の Audio Mixer ウィンドウを開き、各グループの音量メーターが正しく振れているか視覚的に確認。
3. **エフェクト調整**: `Reverb` グループのパラメータを調整し、ゲームの雰囲気に適した残響になっているか確認。
