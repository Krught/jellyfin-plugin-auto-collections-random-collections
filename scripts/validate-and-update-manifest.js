const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { execSync } = require('child_process');

// Get version and file path
const version = process.env.VERSION;
const fileName = process.env.FILE;
const githubRepo = process.env.GITHUB_REPO;

if (!version || !fileName || !githubRepo) {
    console.error('❌ Error: Missing required environment variables');
    console.log('Required: VERSION, FILE, GITHUB_REPO');
    process.exit(1);
}

console.log(`📦 Updating manifest for version ${version}...`);

// Check if zip file exists
const zipPath = path.join(__dirname, '..', fileName);
if (!fs.existsSync(zipPath)) {
    console.error(`❌ Error: File not found: ${fileName}`);
    console.log('Make sure to run "make zip" first');
    process.exit(1);
}

// Calculate MD5 checksum
console.log('🔐 Calculating checksum...');
const fileBuffer = fs.readFileSync(zipPath);
const md5Hash = crypto.createHash('md5').update(fileBuffer).digest('hex');
console.log(`   Checksum: ${md5Hash}`);

// Get current timestamp in ISO format
const timestamp = new Date().toISOString();

// Read and update manifest
const manifestPath = path.join(__dirname, '..', 'manifest.json');
let manifest;

try {
    manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
} catch (err) {
    console.error('❌ Error reading manifest.json:', err.message);
    process.exit(1);
}

// Create new version entry
const newVersionEntry = {
    version: version,
    changelog: `- See the full changelog at [GitHub](https://github.com/${githubRepo}/releases/tag/v${version})\n`,
    targetAbi: "10.9.0.0",
    sourceUrl: `https://github.com/${githubRepo}/releases/download/v${version}/${fileName}`,
    checksum: md5Hash,
    timestamp: timestamp
};

// Check if version already exists
const existingVersionIndex = manifest[0].versions.findIndex(v => v.version === version);

if (existingVersionIndex !== -1) {
    console.log(`⚠️  Version ${version} already exists in manifest, updating...`);
    manifest[0].versions[existingVersionIndex] = newVersionEntry;
} else {
    console.log(`✅ Adding new version ${version} to manifest`);
    // Add to beginning of versions array (newest first)
    manifest[0].versions.unshift(newVersionEntry);
}

// Write updated manifest
try {
    fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 4) + '\n');
    console.log('✅ Manifest updated successfully');
} catch (err) {
    console.error('❌ Error writing manifest.json:', err.message);
    process.exit(1);
}

// Display summary
console.log('\n📋 Release Summary:');
console.log(`   Version: ${version}`);
console.log(`   File: ${fileName}`);
console.log(`   Checksum: ${md5Hash}`);
console.log(`   Timestamp: ${timestamp}`);
console.log(`   Download URL: ${newVersionEntry.sourceUrl}`);
console.log('\n✅ Manifest is ready to commit!');

