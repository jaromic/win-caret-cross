#!/bin/bash
# Bootstrap a new project from this template's GitHub repo.
#
# Usage:
#   mkdir my-project && cd my-project
#   curl -fsSL https://raw.githubusercontent.com/jaromic/jarosoft-claude-code-tools/main/new-project.sh | bash
#
# Run from inside an empty, non-git directory. The new GitHub repo (and
# local directory name, via git) is taken from that directory's name.
set -euo pipefail

# Everything runs inside main(), which bash must fully parse into memory
# before calling. That means once main starts, bash never reads this file
# from disk again for the rest of execution, so it's safe for main() to
# delete this very script partway through (needed below, to clear the way
# for `gh repo clone .`) without risking a mid-read truncation/corruption.

# Checks everything this script (and the start-session.sh it hands off to)
# will need, and reports every gap at once, before anything is created.
# Deliberately does not auto-install: Docker Desktop/WSL2 need admin rights
# and a reboot, and `gh auth login` needs an interactive browser/device
# flow, so none of these are safe to run unattended from a piped script.
check_dependencies() {
	local is_windows=false
	case "$(uname -s)" in
	MINGW* | MSYS* | CYGWIN*) is_windows=true ;;
	esac

	local issues=()

	command -v git >/dev/null 2>&1 || issues+=(
		"git — not found in PATH. Install Git for Windows (also provides this Bash shell): https://git-scm.com/download/win  (or: winget install --id Git.Git -e)"
	)

	if command -v gh >/dev/null 2>&1; then
		gh auth status >/dev/null 2>&1 || issues+=(
			"gh is installed but not logged in. Run: gh auth login"
		)
	else
		issues+=(
			"gh (GitHub CLI) — not found in PATH. https://cli.github.com  (or: winget install --id GitHub.cli -e)"
		)
	fi

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

main() {
	TEMPLATE="jaromic/jarosoft-claude-code-tools"

	check_dependencies

	[ -d .git ] && {
		echo "Refusing to run: $(pwd) is already a git repository." >&2
		exit 1
	}

	SELF="$(basename "$0")"
	shopt -s nullglob dotglob
	entries=(*)
	shopt -u nullglob dotglob
	other_entries=()
	for entry in "${entries[@]}"; do
		[ "$entry" = "$SELF" ] || other_entries+=("$entry")
	done
	[ ${#other_entries[@]} -eq 0 ] || {
		echo "Refusing to run: $(pwd) is not empty." >&2
		exit 1
	}

	NAME="$(basename "$PWD")"
	OWNER="$(gh api user --jq .login)"

	echo "Creating $OWNER/$NAME from template $TEMPLATE..."
	gh repo create "$NAME" --template "$TEMPLATE" --private

	# GitHub copies the template's contents into the new repo asynchronously,
	# after repo creation already returned, so an immediate clone can land on
	# an empty repo. Poll until it has content; this has been observed to take
	# longer than 30s, so keep waiting rather than giving up.
	echo "Waiting for template contents to populate..."
	waited=0
	while :; do
		# repo.size is a cached/derived stat that can lag well behind the
		# actual content; the contents endpoint reflects reality directly
		# (404 while genuinely empty, 200 once files exist).
		gh api "repos/$OWNER/$NAME/contents" >/dev/null 2>&1 && break
		sleep 2
		waited=$((waited + 2))
		[ $((waited % 20)) -eq 0 ] && echo "...still waiting (${waited}s)"
	done

	echo "Cloning into $(pwd)..."
	# git clone requires an empty target dir; the template repo tracks its own
	# copy of this script, so it's safe to remove ours and let the clone restore it.
	[ -f "$SELF" ] && rm -f "$SELF"
	gh repo clone "$OWNER/$NAME" .

	# The container has no access to the host's global git config, so commits
	# made from inside it need identity set locally in this repo instead.
	# Read from /dev/tty rather than stdin, since stdin is the script source
	# itself when this is run via `curl | bash`.
	GIT_NAME=""
	while [ -z "$GIT_NAME" ]; do
		read -rp "git user.name: " GIT_NAME < /dev/tty
	done
	GIT_EMAIL=""
	while [ -z "$GIT_EMAIL" ]; do
		read -rp "git user.email: " GIT_EMAIL < /dev/tty
	done
	git config --local user.name "$GIT_NAME"
	git config --local user.email "$GIT_EMAIL"

	# Quiet down Claude Code's tips/recaps for a fresh project by default.
	# Written here rather than tracked in the template itself, so this repo's
	# own session is unaffected.
	[ -f .claude/settings.json ] || {
		mkdir -p .claude
		cat > .claude/settings.json <<'EOF'
{
  "spinnerTipsEnabled": false,
  "awaySummaryEnabled": false
}
EOF
	}

	exec ./start-session.sh
}

main "$@"
