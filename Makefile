export VERSION := 1.0.0
export GITHUB_REPO := Krught/jellyfin-plugin-auto-collections-random-collections
export FILE := auto-collections-random-collections-${VERSION}.zip

build:
	dotnet build -c Release

zip:
	@echo "Creating release package..."
	@powershell -Command "Compress-Archive -Path 'bin/Release/net8.0/RandomCollectionsHome.dll' -DestinationPath '${FILE}' -Force"

csum:
	@powershell -Command "Get-FileHash -Algorithm MD5 '${FILE}' | Select-Object -ExpandProperty Hash"

create-tag:
	git tag v${VERSION}
	git push origin v${VERSION}

create-gh-release:
	gh release create v${VERSION} "${FILE}" --generate-notes --verify-tag

update-version:
	node scripts/update-version.js

update-manifest:
	node scripts/validate-and-update-manifest.js

push-manifest:
	git add manifest.json
	git commit -m "Update manifest for release ${VERSION}"
	git push origin main

release: update-version build zip create-tag create-gh-release update-manifest push-manifest
	@echo "Release ${VERSION} complete!"

clean:
	dotnet clean
	@if exist "${FILE}" del "${FILE}"

.PHONY: build zip csum create-tag create-gh-release update-version update-manifest push-manifest release clean

