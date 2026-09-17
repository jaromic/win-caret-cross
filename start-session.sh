#!/bin/bash
set -euo pipefail

# Reports every missing/unready dependency at once, before doing anything
# else, rather than letting `docker` fail with a bare "command not found"
# or a daemon-connection error partway through.
check_dependencies() {
	local is_windows=false
	case "$(uname -s)" in
	MINGW* | MSYS* | CYGWIN*) is_windows=true ;;
	esac

	local issues=()

	if $is_windows; then
		if command -v wsl.exe >/dev/null 2>&1; then
			wsl.exe --status >/dev/null 2>&1 || issues+=(
				"WSL2 doesn't appear to be installed/enabled yet (Docker Desktop requires it as its backend). Run as Administrator: wsl --install, then reboot."
			)
		else
			issues+=(
				"WSL2 — the 'wsl' launcher isn't on PATH. Run as Administrator: wsl --install, then reboot."
			)
		fi
	fi

	if command -v docker >/dev/null 2>&1; then
		docker info >/dev/null 2>&1 || issues+=(
			"docker is installed but its daemon isn't reachable. Start Docker Desktop (check the tray icon — it may still be starting up)."
		)
	else
		issues+=(
			"docker — not found in PATH. Install Docker Desktop: https://www.docker.com/products/docker-desktop  (or: winget install --id Docker.DockerDesktop -e)."
		)
	fi

	[ ${#issues[@]} -eq 0 ] && return 0

	echo "Missing or unready dependencies:" >&2
	local issue
	for issue in "${issues[@]}"; do
		echo "  - $issue" >&2
	done
	exit 1
}

check_dependencies

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

[ -f "$SCRIPT_DIR/project.env" ] || cp "$SCRIPT_DIR/project.template.env" "$SCRIPT_DIR/project.env"

# shellcheck source=project.env
source "$SCRIPT_DIR/project.env"

BASEDIRNAME=$(basename "$SCRIPT_DIR")
if [ -z "${PROJECT_NAME:-}" ]; then
	PROJECT_NAME="$BASEDIRNAME"
	sed -i "s/^PROJECT_NAME=.*/PROJECT_NAME=\"$PROJECT_NAME\"/" "$SCRIPT_DIR/project.env"
fi
PROJECT_NAME=$(echo "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]')

echo "$SCRIPT_DIR"
echo "$PROJECT_NAME"

if docker container inspect "$PROJECT_NAME" >/dev/null 2>&1; then
	RUNNING="$(docker container inspect -f '{{.State.Running}}' "$PROJECT_NAME")"
	if [ "$RUNNING" = "true" ]; then
		echo "Already running - attaching..."
		docker container attach "$PROJECT_NAME"
	else
		echo "Container exists but is stopped - starting..."
		docker container start -ai "$PROJECT_NAME"
	fi
	exit 0
fi

docker image inspect "$PROJECT_NAME" >/dev/null 2>&1 || {
	echo "Image not found - building..."
	"$SCRIPT_DIR/build-image.sh"
}

# Optional per-project SSH deploy key: if present on the host at
# ~/.ssh-claude-docker/<project-name>, mount it read-only into the
# container so git push/fetch over SSH works without exposing the
# host's main ~/.ssh. See CLAUDE.md "SSH deploy key" section.
DEPLOY_KEY_MOUNT=()
DEPLOY_KEY_HOST_PATH="$HOME/.ssh-claude-docker/$PROJECT_NAME"
[ -f "$DEPLOY_KEY_HOST_PATH" ] && DEPLOY_KEY_MOUNT=(-v "$DEPLOY_KEY_HOST_PATH:/run/secrets/github_deploy_key:ro")

(cd "$SCRIPT_DIR" && MSYS_NO_PATHCONV=1 docker run -it -v "$(pwd -W)":/workspace "${DEPLOY_KEY_MOUNT[@]}" -w /workspace --name "$PROJECT_NAME" "$PROJECT_NAME")
