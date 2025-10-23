const fs = require('fs');
const path = require('path');

// Get version from command line or prompt
const newVersion = process.argv[2] || process.env.VERSION;

if (!newVersion) {
    console.error('❌ Error: No version specified');
    console.log('Usage: node update-version.js <version>');
    console.log('   or: set VERSION=x.x.x && node update-version.js');
    process.exit(1);
}

console.log(`📝 Updating version to ${newVersion}...`);

// Update .csproj file
const csprojPath = path.join(__dirname, '..', 'RandomCollectionsHome.csproj');
try {
    let csproj = fs.readFileSync(csprojPath, 'utf8');
    csproj = csproj.replace(
        /<Version>[\d.]+<\/Version>/,
        `<Version>${newVersion}</Version>`
    );
    fs.writeFileSync(csprojPath, csproj);
    console.log('✅ Updated RandomCollectionsHome.csproj');
} catch (err) {
    console.error('❌ Error updating .csproj:', err.message);
    process.exit(1);
}

// Update Makefile
const makefilePath = path.join(__dirname, '..', 'Makefile');
try {
    let makefile = fs.readFileSync(makefilePath, 'utf8');
    makefile = makefile.replace(
        /export VERSION := [\d.]+/,
        `export VERSION := ${newVersion}`
    );
    fs.writeFileSync(makefilePath, makefile);
    console.log('✅ Updated Makefile');
} catch (err) {
    console.error('❌ Error updating Makefile:', err.message);
    process.exit(1);
}

console.log(`✅ Version updated to ${newVersion}`);
console.log('📌 Next steps:');
console.log('   1. Review changes');
console.log('   2. Commit: git add . && git commit -m "Bump version to ' + newVersion + '"');
console.log('   3. Run: make release');

