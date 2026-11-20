# Unity Upgrades and Editor Warnings

How to upgrade the Unity Editor or packages without leaving warnings behind, and how to check that the Editor log
stays clean.

## Scan an Editor log

`tools/unity-log-scan/scan_unity_log.py` reads a Unity Editor log and reports warnings and errors: compiler and
analyzer diagnostics, shaders that cannot compile, GUID conflicts, outdated serialized files, `Debug.LogWarning`/
`LogError` calls, and Editor validation warnings. It exits with status 1 if it finds anything that is not on
`tools/unity-log-scan/allowlist.txt`.

```bash
python3 tools/unity-log-scan/scan_unity_log.py Logs/Editor.log
python3 tools/unity-log-scan/scan_unity_log.py Logs/AssetImportWorkerHW0.log   # import-time warnings
python3 -m unittest discover -s tools/unity-log-scan -p 'test_*.py'            # scanner tests
```

An interactive Editor writes `Logs/Editor.log` (and one `Logs/AssetImportWorker*.log` per import worker). The
Console is not saved, but every Console message is also in these logs. Many warnings appear only when an asset is
imported, so scan a log from a clean import (below) when you want to see everything.

CI runs the scanner on the EditMode and PlayMode test logs (`artifacts/editmode.log` and `artifacts/playmode.log`) in
the Unit Tests job of `.github/workflows/main.yml`. New warnings appear as annotations on the run and fail the job.
CI restores a cached `Library/`, so it only sees import warnings for assets that changed; the clean open below is
still needed after upgrades.

Only add an allowlist entry for a message we cannot fix in this repository, such as a shader inside a Unity package,
and write down why. Remove entries that no longer match.

## Compiler warnings that fail the build

`Assets/csc.rsp` passes `-warnaserror:CS0618,CS0612,UAC1001` to the C# compiler for every script under `Assets/`
(packages are not affected). Using an API marked `[Obsolete]`, or a field Unity cannot serialize, is a compile error
instead of a warning. Code inside platform blocks such as `#if UNITY_IOS` or `#if ENABLE_WINMD_SUPPORT` only compiles
when building for that platform, so CI's platform builds are what catch those. Replace the obsolete API instead of
suppressing the error; if a suppression is unavoidable, use `#pragma warning disable` around the single line with a
comment explaining why.

## Clean open

To see every import-time warning, open the project with an empty `Library/` in batchmode and scan the logs:

```bash
mv Library Library.backup   # delete Library.backup once the result looks right
"/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity" -batchmode -quit \
  -projectPath "$PWD" -logFile "$PWD/Logs/clean-open.log"
python3 tools/unity-log-scan/scan_unity_log.py Logs/clean-open.log
for log in Logs/AssetImportWorker*.log; do python3 tools/unity-log-scan/scan_unity_log.py "$log"; done
```

Close any Editor that has the project open first. A full import takes a few minutes.

## Upgrade checklist

1. Upgrade the Editor (`ProjectVersion.txt`) and packages in their own commit.
2. Do a clean open (above) and scan the logs. Fix everything the scan reports, or add a commented allowlist entry
   when the message belongs to a Unity package.
3. Re-save outdated files. When the log says a file is `below the supported minimum ... Open and re-save the file to
   upgrade`, re-save it with `AssetDatabase.ForceReserializeAssets(paths,
   ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata)` from a temporary Editor script (run it with
   `-executeMethod`), then delete the script. Do not re-save the whole project; it rewrites thousands of files.
4. Open **Project Settings > Player** for each platform. Some upgrade warnings (for example, Android icon changes)
   need a setting reviewed rather than just re-saved.
5. Review re-saved files before committing. Unity writes LF line endings, so files stored with CRLF show every line
   as changed; use `git diff --ignore-cr-at-eol` to see the real changes. Check that platform overrides and import
   settings stay the same.
6. Commit the files Unity re-saved separately from code changes, so reviewers can tell migrations apart from
   behavior changes.
7. Run the PlayMode tests and let CI build every platform before merging.

## Package samples

Do not import a whole package sample into `Assets/`. Copy only the files the project uses into a project folder
(for example, `Assets/Shaders and Materials/Subgraphs/`). Imported samples keep their package GUIDs, and a later
package version can ship the same files with the same GUIDs. Unity then reports `GUID [...] conflicts with` on every
fresh import and may point references at the package's copy instead of ours.

Likewise, delete optional resources the project does not use, such as the TextMesh Pro HDRP shaders in a URP project.
