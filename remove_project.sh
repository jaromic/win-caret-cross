#!/bin/bash
# Tear down a test project created via new-project.sh: stops/removes its
# Docker container and image, deletes its GitHub repo, and removes its
# local directory.
#
# Usage: ./remove_project.sh <project-name>
# Run from the parent directory (e.g. ~/Desktop), not from inside the
# project directory itself.
set -euo pipefail

[ $# -eq 1 ] || {
	echo "Usage: $0 <project-name>" >&2
	exit 1
}
NAME="$1"

[ "$(basename "$PWD")" = "$NAME" ] && {
	echo "Refusing to run: this looks like the project directory itself. cd .. and run it from there instead." >&2
	exit 1
}

OWNER="$(gh api user --jq .login)"

docker rm -f "$NAME" 2>/dev/null || true
docker rmi -f "$NAME" 2>/dev/null || true
gh repo delete "$OWNER/$NAME" --yes
rm -rf --preserve-root=all --one-file-system "$NAME"
