#!/bin/bash
set -euo pipefail

# The deploy key (see CLAUDE.md "SSH deploy key") is bind-mounted read-only
# from the host at /run/secrets/github_deploy_key. On hosts that can't
# represent real per-owner Unix permissions on the mounted file (e.g.
# Windows filesystems bind-mounted via Docker Desktop), ssh refuses to use
# it regardless of what the host chmod/icacls says. Copy it into the
# container's own filesystem, where chmod always works normally, and use
# that copy for git/ssh instead.
if [ -f /run/secrets/github_deploy_key ]; then
	mkdir -p /root/.ssh
	cp /run/secrets/github_deploy_key /root/.ssh/deploy_key
	chmod 600 /root/.ssh/deploy_key
fi

exec "$@"
