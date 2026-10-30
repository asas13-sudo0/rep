# REPO Photon Cloud (BepInEx 5 / Unity Mono) - ソース

このプロジェクトは、R.E.P.O. の PUN 接続時に Photon Realtime App ID / Fixed Region を上書きするBepInExプラグインのソースです。Steamテスト用AppID 480の設定も含みます。

## 現在の状態と注意

- これは**ソースコード一式**です。こちらの実行環境にC#コンパイラがないため、DLLはまだ生成していません。
- 提供された `Managed.rar` と `core.rar` は展開でき、主要なPhoton/BepInEx DLLを確認しています。ただしゲーム起動での動作テストは未実施です。
- Photon CloudのApp IDは利用者自身のRealtime App IDを設定してください。空欄の場合はゲーム既定値を変更しません。
- Steam AppID 480はSteamworksの開発・テスト用IDです。Steamの所有権や認証を回避する機能ではありません。Steamworks初期化より前にIDを読ませる必要があるため、最も確実なのはゲーム実行ファイルと同じフォルダで `setup_steam_appid_480.bat` を一度実行し、`steam_appid.txt` を作ってからゲームを起動する方法です。プラグイン側でも起動時に同ファイルと環境変数を設定しますが、既にSteamworksが初期化済みの場合は次回起動から反映されます。

## ビルド方法（Windows）

1. Visual Studio 2022 または .NET SDK を用意します。
2. このZIPを展開し、次のように配置します（RARを展開したフォルダを利用）。
   ```
   BuildRoot/
     Managed/   <- Managed.rar の中身
     core/      <- core.rar の中身
     RepoPhotonCloudMod/
       RepoPhotonCloudMod.csproj
       src/Plugin.cs
   ```
3. `RepoPhotonCloudMod.csproj` のあるフォルダで実行:
   ```
   dotnet build -c Release
   ```
   `Managed` / `core` の場所が違う場合:
   ```
   dotnet build -c Release -p:ManagedDir="D:\Game\REPO_Data\Managed" -p:BepInExCoreDir="D:\Game\BepInEx\core"
   ```
4. 出力された `bin\Release\net472\RepoPhotonCloudMod.dll` をゲームの `BepInEx\plugins\` にコピーします。

## 設定

初回起動後に `BepInEx\config\jp.user.repophotoncloud.cfg` が作成されます。

```ini
[Photon]
AppIdRealtime = ここにPhoton Realtime App ID
FixedRegion = jp
EnableConnectionPatch = true

[Steam]
SetAppId480 = true
```

`FixedRegion` は任意です。空欄ならPhotonのBest Region選択に任せます。App IDを変更した後はゲームを再起動してください。

## 技術メモ

- `Photon.Pun.PhotonNetwork.ConnectUsingSettings` の全ての静的オーバーロードにHarmony Prefixを適用します。
- `PhotonNetwork.PhotonServerSettings.AppSettings` と、引数で渡された `Photon.Realtime.AppSettings` の両方を反射で処理します。
- Photonの別バージョンで接続経路が異なる場合、ログにパッチ数が出ない可能性があります。その場合は `BepInEx\LogOutput.log` を確認してください。


## GitHub Actionsでコンパイルする方法

このZIPをGitHubリポジトリのルートに展開してコミットします。次の構成にしてください。

```
(repository root)/
  core/
    BepInEx.dll
    0Harmony.dll
  RepoPhotonCloudMod/
    RepoPhotonCloudMod.csproj
    src/Plugin.cs
    .github/workflows/build.yml ではなく、下記の配置に注意
  .github/
    workflows/
      build.yml
```

**注意:** `.github` フォルダは `RepoPhotonCloudMod` の中ではなく、リポジトリの一番上に置きます。このZIP内の `.github` はプロジェクトフォルダ内に入っているため、GitHubへアップロードする際に `.github` をリポジトリのルートへ移動してください。

1. GitHubで `New repository` を選び、Privateでリポジトリを作成します。
2. ZIP内の `RepoPhotonCloudMod` フォルダの中身をリポジトリ直下に置くのではなく、`RepoPhotonCloudMod/` ディレクトリごと配置します。
3. `core.rar` をPC上で展開し、`core/BepInEx.dll` と `core/0Harmony.dll` をリポジトリ直下の `core/` に入れます。ほかのcore DLLやManaged全体はこのコードのコンパイルには不要です。
4. `.github/workflows/build.yml` をリポジトリ直下の `.github/workflows/build.yml` に置きます。
5. GitHubの `Actions` タブで `Build REPO Photon Cloud Mod` を開きます。最初のpushで自動実行されます。手動なら `Run workflow` を押します。
6. 緑の成功表示になったら実行結果を開き、`Artifacts` の `RepoPhotonCloudMod-DLL` をダウンロードします。中に `RepoPhotonCloudMod.dll` が入っています。

GitHub ActionsでWindowsランナーと.NET SDKを用いてビルドし、成果物をArtifactとして保存する構成です。公式手順: https://docs.github.com/ja/actions/tutorials/build-and-test-code/net

ゲームやBepInEx由来のDLLをGitHubに置く場合、公開リポジトリへのアップロードは避け、必ずPrivateリポジトリを使用してください。
