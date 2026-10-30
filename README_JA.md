# REPO Photon Cloud Mod - GitHub Actionsビルド用

このZIPはGitHubリポジトリのルートにそのままアップロードできる構成です。`core/BepInEx.dll` と `core/0Harmony.dll` も同梱しています。

## GitHubでのビルド

1. GitHubで **Private** リポジトリを作成します。
2. ZIPを展開し、中身（`src`、`core`、`.github`、csprojなど）を**フォルダごとではなく、リポジトリ直下**にアップロードしてコミットします。
3. `Actions` タブを開き、`Build REPO Photon Cloud Mod` が実行されるのを待ちます（初回pushで自動実行）。
4. 成功した実行を開き、下部の `Artifacts` から `RepoPhotonCloudMod-DLL` をダウンロードします。
5. ZIP内の `RepoPhotonCloudMod.dll` をゲームの `BepInEx/plugins/` に配置します。

正しいリポジトリ構成:

```
repository root/
  .github/workflows/build.yml
  core/BepInEx.dll
  core/0Harmony.dll
  src/Plugin.cs
  RepoPhotonCloudMod.csproj
```

**重要:** GitHubのファイル一覧に `core` フォルダが表示され、その中に上記2つのDLLが存在する必要があります。`core.rar` をそのまま置くだけでは認識されません。

## 注意

- 同梱したBepInEx/Harmony DLLは、ユーザー提供の `core.rar` から抽出したビルド参照用ファイルです。公開リポジトリにせずPrivateで管理してください。
- これはビルド用ソース一式であり、ゲーム上での動作確認済みを意味しません。
- Steam AppID 480は開発・テスト用IDであり、Steam所有権や認証を回避するものではありません。
