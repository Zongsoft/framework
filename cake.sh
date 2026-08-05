#!/bin/sh

set -u

SCRIPT_DIRECTORY=$(CDPATH= cd "$(dirname "$0")" && pwd)
PROJECT_MANIFEST="$SCRIPT_DIRECTORY/cake.projects"
PROJECT_ADMINISTRATIVES="$SCRIPT_DIRECTORY/../Administratives/build.cake"
FAILED_PROJECTS=""

if ! command -v dotnet >/dev/null 2>&1; then
	printf '%s\n' "The dotnet command was not found." >&2
	exit 127
fi

if [ ! -f "$PROJECT_MANIFEST" ]; then
	printf 'The Cake project manifest was not found: %s\n' "$PROJECT_MANIFEST" >&2
	exit 1
fi

cd "$SCRIPT_DIRECTORY"

run_project()
{
	project="$1"
	shift

	printf '\n==> dotnet cake %s\n' "$project"

	if dotnet cake "$project" --verbosity=normal "$@"; then
		return 0
	else
		status=$?
		FAILED_PROJECTS="${FAILED_PROJECTS}${FAILED_PROJECTS:+ }${project}"
		printf '<== Failed (%s): %s\n' "$status" "$project" >&2
		return "$status"
	fi
}

while IFS= read -r project || [ -n "$project" ]; do
	project=$(printf '%s' "$project" | tr -d '\r')

	case "$project" in
		''|'#'*)
			continue
			;;
	esac

	if run_project "$project" "$@"; then
		:
	fi
done < "$PROJECT_MANIFEST"

if [ -f "$PROJECT_ADMINISTRATIVES" ]; then
	if run_project "$PROJECT_ADMINISTRATIVES" "$@"; then
		:
	fi
fi

if [ -n "$FAILED_PROJECTS" ]; then
	printf '\nThe following Cake projects failed:\n' >&2

	for project in $FAILED_PROJECTS; do
		printf ' - %s\n' "$project" >&2
	done

	exit 1
fi

printf '\nAll Cake projects completed successfully.\n'
