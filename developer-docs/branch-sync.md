# Keeping develop up to date with main

`.github/workflows/sync_develop.yml` merges `main` into `develop` after every
push to `main`. It preserves commits unique to `develop`, does nothing when
`develop` already contains `main`, and never resets or force-pushes either branch.
The GitHub merge API performs the merge without downloading the Unity project
or its LFS assets.

## Activation and manual runs

The workflow must be committed and merged into `main` to enable the push trigger
and hourly schedule. No additional secret is required: it uses the repository's
`GITHUB_TOKEN` with `contents: write` permission. Existing branch protection and
organization policies still apply. If those policies require pull requests or
checks before updating `develop`, this direct-merge workflow will fail instead
of bypassing them.

In GitHub Actions, select **Sync main into develop**, then **Run workflow** with
the `main` branch selected to sync immediately or retry a failed run. Runs from
other branches and forks are skipped. Only one sync runs at a time; each run
merges the current branch tips rather than an older triggering commit.

## Automated commits and timing

GitHub does not start push workflows for commits made with `GITHUB_TOKEN`.
The existing release workflow writes release notes this way, so the sync also
runs hourly at minute 17 to catch those updates. Scheduled runs can be delayed
by GitHub; this is eventual synchronization, not a guarantee that the branches
are always synchronized immediately.

The sync itself also uses `GITHUB_TOKEN`, so its changes to `develop` do not
automatically start the existing push-triggered Unity test/build workflow.
Run **Test, Build, and Deploy with GameCI** manually on `develop` when validation
of a synced change is needed. That workflow can also build and deploy according
to its existing settings; branch synchronization does not dispatch it.

See GitHub's [workflow trigger documentation](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/trigger-a-workflow)
and [merge API documentation](https://docs.github.com/en/rest/branches/branches#merge-a-branch).

## Failed syncs

A merge conflict leaves `develop` unchanged and fails the Actions run. Resolve
the conflict in a normal merge of `main` into `develop`, review and push the
resolution, then rerun the workflow. Permission or protection failures also
fail visibly. Configure GitHub Actions failure notifications for the repository
if you want alerts; the workflow does not open issues or send messages.
