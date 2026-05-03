

# ApplyRandomBuildingMaterial

※このスクリプトと README は、ChatGPT を活用して作成・整理しました。

PLATEAU SDK for Unity で作成・インポートした FBX ファイルの建物モデルに対して、建物側面用のテクスチャを一括で適用するための Unity Editor 拡張スクリプトです。

PLATEAU の LOD1 建物モデルは、Unity に取り込んだ直後だと建物の見た目が単調になりやすいため、このスクリプトでは `bldg` で始まる建物オブジェクト／FBXを対象に、指定フォルダ内の画像をランダムに割り当て、街並みに簡易的な変化を付けます。

なお、屋根部分のテクスチャ適用についても検討しましたが、今回の方法では期待どおりに反映できなかったため、本スクリプトでは屋根へのテクスチャ設定は行っていません。  
対象は主に建物の側面テクスチャです。

## 概要

このスクリプトは、PLATEAU SDK for Unity で作成・インポートした FBX 建物モデルに対して、建物側面用テクスチャを一括適用する Unity Editor 上の処理ツールです。

## 動作確認 Unityバージョン

Unity 6000.2.6f2
Unity 6000.4.3f1 


### イメージ図

実行前
<img width="841" height="386" src="https://github.com/user-attachments/assets/f76c07b6-8996-450c-a5d4-e58f33735f02" />

実行後
<img width="841" height="386" src="https://github.com/user-attachments/assets/6a6ee31b-4145-43a3-b9b1-4870593ec8ad" />



対象となる建物オブジェクトの Material に対して、以下の処理を行います。

- Shader を `PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)` に変更
- `Assets/BldgTexture` 内の画像からランダムに1枚選択
- 選択した画像を `Side-MainTexture` に設定
- テクスチャの最大解像度を `1024` に変更
- 既存の `PlateauTriplanerShader` を上書きするかどうかを開始時に確認
- 同じ Material アセットの重複作成を防止
- Console に開始時刻・途中経過・終了時刻・処理時間を出力

屋根部分のテクスチャ適用は、今回の方法ではうまく反映できなかったため含めていません。  
本スクリプトでは、建物側面の見た目を簡易的に変えることを目的としています。

## 対象オブジェクトの条件

以下の条件をすべて満たす GameObject が対象です。

1. 指定した Tag が設定されている
2. オブジェクト名、または元FBXファイル名が `bldg` から始まる
3. `Renderer` コンポーネントを持っている

※サイズはX,Y,Z ともに100倍にしてください（重要）


## 事前準備

### 1. Editor フォルダを作成

`Assets` の直下に `Editor` フォルダを作成します。

```text
Assets/Editor
```

### 2. スクリプトを配置

`ApplyRandomBuildingMaterial.cs` を `Assets/Editor` に配置します。

```text
Assets/Editor/ApplyRandomBuildingMaterial.cs
```

### 3. テクスチャフォルダを作成

建物壁面用の画像を以下に配置します。

```text
Assets/BldgTexture
```

対応画像例：

```text
.png
.jpg
.jpeg
.tga
```

Unity上で `Texture2D` として認識される画像であれば使用できます。

（参考）
PLATEAU SDK　デフォルトのテクスチャは以下のフォルダにあります。

```text
Assets\PLATEAU-SDK-for-Unity\Materials\Fallback\MaterialTexture\TexDefaultBuilding
```


### 4. 対象タグを作成

Unity の `Tags and Layers` で、対象にしたいタグを作成してください。

例：

```text
bldg
```

### 5. スクリプト内のタグ名を変更

スクリプト内の以下の部分を、実際に使用するタグ名に変更します。

```csharp
private const string TargetTag = "○○";
```

例：

```csharp
private const string TargetTag = "bldg";
```

## 実行方法

Unity Editor 上部メニューから以下を選択します。

```text
Tools > PLATEAU > Apply Random Building Texture By Tag
```

Unity の再生ボタンは押す必要はありません。

このスクリプトは Play Mode ではなく、Editor 上で実行するツールです。

## 実行時の確認ダイアログ

実行すると、最初に以下の確認ダイアログが表示されます。

```text
すでに PlateauTriplanerShader が設定されているマテリアルがある場合、どうしますか？
```

選択肢は以下の3つです。

### 上書きする

すでに `PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)` が設定されている Material も再処理します。

既存の Material に対して、新しいランダムテクスチャが再設定されます。

### 上書きしない

すでに `PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)` が設定されている Material はスキップします。

一度処理済みの建物を変更したくない場合はこちらを選択します。

### キャンセル

処理を中止します。

## Material アセットの重複防止について

このスクリプトでは、Material アセットを以下のような固定パスで作成します。

```text
Assets/GeneratedBuildingMaterials/オブジェクト名_番号.mat
```

すでに同じ名前の Material アセットが存在する場合は、新規作成せずに既存の Material を再利用します。

そのため、何度実行しても以下のようなファイルが増え続けることを防ぎます。

```text
bldg_xxx_0.mat
bldg_xxx_0 1.mat
bldg_xxx_0 2.mat
bldg_xxx_0 3.mat
```

## テクスチャ設定

`Assets/BldgTexture` 内の画像は、実行時に以下の Import 設定へ変更されます。

```text
Max Size: 1024
Texture Type: Default
```

建物の側面用テクスチャとして使用するため、Normal Map には設定しません。

## Shader

使用する Shader は以下です。

```text
PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)
```

スクリプト内では以下の定数で指定しています。

```csharp
private const string ShaderName =
    "PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)";
```

Unity上で Shader が見つからない場合は、Shader名が異なっている可能性があります。

その場合は、対象 Material の Shader 名を確認し、上記の `ShaderName` を修正してください。

## Side-MainTexture の設定先

スクリプトでは、以下の順番で Texture プロパティを探します。

```csharp
"_SideMainTex"
"_Side_MainTexture"
"_SideMainTexture"
"_MainTex"
```

最初に見つかったプロパティにランダムテクスチャを設定します。

もし Console に以下のような警告が出る場合、

```text
Side-MainTexture のプロパティが見つかりませんでした。
```

Shader の内部プロパティ名が異なっている可能性があります。

## Console ログ

実行中は Console に以下のようなログが出力されます。

```text
処理開始: 2026/05/02 19:10:00
対象タグ: bldg
対象条件: Tag一致 かつ オブジェクト名または元FBXファイル名が bldg から始まるもの
既存 PlateauTriplanerShader の上書き: しない
LoadTextures開始: Assets/BldgTexture
検出したTexture2D数: 12
対象オブジェクト数: 1250
Side-MainTexture用テクスチャ数: 12
途中経過: 100/1250 件完了 / Material処理済み 100 個 / スキップ 0 個 / 新規作成 100 個 / 再利用 0 個 / 経過 3.2 秒 / 現在: bldg_xxxx
処理終了: 2026/05/02 19:11:20
処理時間: 80.25 秒
完了: タグ 'bldg' の対象オブジェクト 1250 個 / Material処理済み 1250 個 / スキップ 0 個 / 新規作成 1250 個 / 再利用 0 個
```

## 処理時間

マテリアル1546個（広島県呉市中心街1tin）で102秒

## よくあるエラー

### Shader が見つかりません

```text
Shader が見つかりません: PlateauTriplanerShader/PlateauTriplanarShader(DualTextures)
```

原因：

* Shader名が違う
* PLATEAU SDK の Shader がプロジェクトに入っていない
* Shader が別名で登録されている

対処：

対象 Material を選択し、Inspector で実際の Shader 名を確認してください。

---

### テクスチャが見つかりません

```text
Side-MainTexture用テクスチャが見つかりません: Assets/BldgTexture
```

原因：

* `Assets/BldgTexture` フォルダが存在しない
* フォルダ名が違う
* 画像が入っていない
* Unityがまだ画像を Import していない

対処：

以下を確認してください。

```text
Assets/BldgTexture
```

フォルダ名が完全一致しているか確認します。

また、Unity上部メニューから以下を実行してください。

```text
Assets > Refresh
```

または、`BldgTexture` フォルダを右クリックして `Reimport` を実行してください。

---

### タグが登録されていません

```text
タグ 'bldg' がUnityに登録されていません。
```

原因：

スクリプトで指定したタグが Unity の Tags and Layers に登録されていません。

対処：

Unity の `Tags and Layers` から対象タグを追加してください。

例：

```text
bldg
```

## カスタマイズ

### 対象タグを変更する

```csharp
private const string TargetTag = "bldg";
```

### テクスチャフォルダを変更する

```csharp
private const string SideTextureFolder =
    "Assets/BldgTexture";
```

### 生成Materialフォルダを変更する

```csharp
private const string GeneratedMaterialFolder =
    "Assets/GeneratedBuildingMaterials";
```

### 途中経過ログの頻度を変更する

以下の値を変更します。

```csharp
int logInterval = 100;
```

例：500件ごとにログを出す場合

```csharp
int logInterval = 500;
```

## 注意点

* Unity の Play ボタンを押す必要はありません。
* このスクリプトは Editor 拡張です。
* 処理対象が多い場合、実行に時間がかかります。
* 初回実行時はテクスチャの Import 設定変更が入るため、少し時間がかかる場合があります。
* 生成された Material は `Assets/GeneratedBuildingMaterials` に保存されます。
* 既存の Material アセットを直接上書きせず、生成用フォルダ内の Material を使用します。
* 屋根部分へのテクスチャ適用は、今回の方法では期待どおりに反映できなかったため対象外です。
* `Top-NormalMap` には設定しません。
* `Assets/CeilingTexture` は使用しません。

## 想定用途

* PLATEAU の LOD1 建物モデルに、簡易的なランダム壁面テクスチャを適用したい場合
* 大量の建物オブジェクトに対して、手作業で Material を設定する手間を減らしたい場合
* cluster や Unity 上で、PLATEAU 建物の見た目を少し変化させたい場合

## メニュー名

Unity Editor 上では、以下のメニューから実行します。

```text
Tools > PLATEAU > Apply Random Building Texture By Tag
```

```
```
