#!/bin/bash
set -euo pipefail

# Publish UPM tarballs to npm. Run sign-upm.sh first to generate them.
# Usage:
#   ./Scripts/release-npm.sh           # publish
#   ./Scripts/release-npm.sh --dry-run # simulate without publishing

DRY_RUN=""
if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN="--dry-run"
    echo "Dry run mode enabled."
fi

# Publish a single tarball. Pre-release versions (e.g. 2.21.0-rc0) are tagged
# 'canary'; stable versions are tagged 'latest'.
# Usage:
# publish <package_tgz_file>
publish() {
    local tgz=$1
    local filename=$(basename "$tgz")
    local version=$(echo "$filename" | sed -E "s/.*-([0-9]+\.[0-9]+\.[0-9]+.*)\.tgz/\1/")
    local tag="latest"
    if [[ "$version" == *-* ]]; then
        tag="canary"
    fi

    echo "Publishing $tgz with --tag $tag"
    npm publish "$tgz" --tag "$tag" $DRY_RUN
}

publish upm/com.github.asus4.tflite/*.tgz
publish upm/com.github.asus4.mediapipe/*.tgz
publish upm/com.github.asus4.tflite.common/*.tgz

echo "Done."
exit 0
