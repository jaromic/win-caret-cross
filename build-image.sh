#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

[ -f "$SCRIPT_DIR/project.env" ] || cp "$SCRIPT_DIR/project.template.env" "$SCRIPT_DIR/project.env"

# shellcheck source=project.env
source "$SCRIPT_DIR/project.env"

if [ -z "${PROJECT_NAME:-}" ]; then
	PROJECT_NAME="$(basename "$SCRIPT_DIR")"
	sed -i "s/^PROJECT_NAME=.*/PROJECT_NAME=\"$PROJECT_NAME\"/" "$SCRIPT_DIR/project.env"
fi
PROJECT_NAME=$(printf "%s" "$PROJECT_NAME" | tr '[:upper:]' '[:lower:]' | tr -c 'a-z0-9_.-' '-')

echo "project name: ""$PROJECT_NAME"
echo "script dir:   ""$SCRIPT_DIR"

docker build -t "$PROJECT_NAME" "$SCRIPT_DIR"
