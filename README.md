# jarosoft-claude-code-tools

This repository is a Docker-based **template** for running Claude Code on a new project. It is meant to be used via GitHub's "Use this template" (or `gh repo create --template`), producing one fresh repo per project that contains both this scaffold and the project's own code/history together.

The container is built on `python:3.12-slim` with Node.js, npm, git, and `@anthropic-ai/claude-code` installed globally. The `/workspace` directory inside the container is bind-mounted from the repo root on the host, so edits made by Claude Code are immediately visible on the host and vice versa. The container launches directly into `claude` (Claude Code CLI).

## Starting a new project from this template

```
mkdir my-project && cd my-project
curl -fsSL https://raw.githubusercontent.com/jaromic/jarosoft-claude-code-tools/main/new-project.sh | bash
```

This runs `new-project.sh` (tracked at the root of this repo) straight from GitHub: it must be run from inside an empty, non-git directory, creates a private GitHub repo named after that directory from this template, `gh repo clone`s it into the current directory, and hands off to `./start-session.sh`.

Equivalent manual steps, if you'd rather not pipe a script into `bash`:
```
gh repo create my-project --template jaromic/jarosoft-claude-code-tools --private --clone
cd my-project
./start-session.sh
```

No files need to be edited either way. The image/container name defaults to the directory name (`my-project` in this example).

## Naming

The image and container name come from `project.env`, which is gitignored (local/generated) and templated from the tracked `project.template.env`:

- On first run, `start-session.sh` (or `build-image.sh`, if run standalone) copies `project.template.env` to `project.env` if it doesn't exist yet.
- If `PROJECT_NAME` is left empty, the script fills it in with the current directory name and writes it back into `project.env`, so it's derived once and stays stable afterwards.
- To force a specific name (e.g. to avoid a clash, or because the directory name isn't Docker-safe), set `PROJECT_NAME` in `project.env` (after it's been generated) or in `project.template.env` before first run. This is the **only** place the name needs to be set — `build-image.sh` and `start-session.sh` both source it.

## Common Commands

### Start a session (build-if-needed, resume-if-stopped, attach-if-running)
```
./start-session.sh
```
This is the only command you normally need. It:
1. Attaches to the container if it's already running.
2. Starts the existing container if it's stopped (avoids Docker's "name already in use" error on a second run).
3. Builds the image via `build-image.sh` if it doesn't exist yet, then runs a new container.

### Build the image manually
```
./build-image.sh
```

## SSH deploy key (optional)

The container has no network credentials by default — `git push`/`fetch` inside it will fail with `could not read Username for 'https://github.com'` unless one is provided. The supported way is a repo-scoped SSH deploy key, kept outside the repo tree so it's never committed:

1. On the **host** (not inside the container): generate a dedicated key and add it as a deploy key (with write access) at `https://github.com/<owner>/<repo>/settings/keys`:
   ```
   mkdir -p ~/.ssh-claude-docker
   ssh-keygen -t ed25519 -C "<repo> deploy key" -N "" -f ~/.ssh-claude-docker/<project-name>
   chmod 600 ~/.ssh-claude-docker/<project-name>
   cat ~/.ssh-claude-docker/<project-name>.pub
   ```
   `<project-name>` must match `PROJECT_NAME` in `project.env` (lowercased) — that's the filename `start-session.sh` looks for.
2. `start-session.sh` mounts that file read-only into the container at `/run/secrets/github_deploy_key` if it exists, at `docker run` time only — so an existing stopped container needs to be removed (`docker stop`/`docker rm <project-name>`) once so the next `./start-session.sh` recreates it with the mount. Containers here are stateless (all state lives in the `/workspace` bind mount and the image), so this is safe.
   - On every container start, `entrypoint.sh` copies that mounted key to `/root/.ssh/deploy_key` (inside the container's own filesystem) and `chmod 600`s it there before launching `claude`. This sidesteps hosts that can't represent real per-owner Unix permissions on the bind-mounted file itself — notably Windows filesystems bind-mounted via Docker Desktop, where the mounted file's mode reflects only the binary DOS read-only attribute (showing as `777` or `444` for all of owner/group/other, never a usable `600`) no matter what `chmod`/`icacls` is run on the host copy. `ssh` refuses undersecured *and* overly-open key files, so use the container-local copy, not the raw mount.
3. Inside the container/repo, point git at it (one-time, stored in the untracked `.git/config` of this clone):
   ```
   git remote set-url origin git@github.com:<owner>/<repo>.git
   git config core.sshCommand "ssh -i /root/.ssh/deploy_key -o IdentitiesOnly=yes -o StrictHostKeyChecking=accept-new"
   ```

## Notes

- `Dockerfile` does `COPY . .`, but since `/workspace` is always bind-mounted at runtime, that copy is immediately overlaid and only matters if the image is ever run without the mount. `.dockerignore` keeps `.git`, `documents/`, and `screenshots/` out of the image layers regardless.
- To mount an additional path (e.g. a second, unrelated repo) alongside `/workspace`, add another `-v host_path:/container_path` to the `docker run` line in `start-session.sh`.

See `CLAUDE.md` for the spec-driven development workflow used on top of this scaffold.

## This project

The Windows tool itself lives in `src/CaretCrosshair/` — see
`src/CaretCrosshair/README.md` for build instructions and known
limitations, and `specs/win-caret-cross_spec.md` /
`specs/stories/win-caret-cross.md` for the spec and story.
