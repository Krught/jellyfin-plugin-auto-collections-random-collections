# Auto-Collections-Random-Collections

A Jellyfin plugin that randomly displays collections on your home screen, keeping your content fresh and interesting every time you visit!

## What Does This Plugin Do?

This plugin automatically selects random collections from your library and displays them on your Jellyfin home screen. Each time you reload the home screen or restart your server, you'll see different collections, helping you rediscover content in your library.

## Features

- 🎲 Randomly displays collections on your home screen
- ⚙️ Configurable - choose how many collections to show (1-20)
- 🔄 Auto-refreshes on server restart or page reload
- 👤 Smart caching prevents too-frequent changes
- 🎨 Beautiful, easy-to-use configuration interface

## Requirements

- Jellyfin Server **10.9.0 or higher**
- **HomeScreenSections Plugin** (you'll install this alongside this plugin)

---

## Installation

### Installing via Jellyfin Admin Panel (Recommended)

This is the easiest way to install the plugin!

#### Step 1: Install HomeScreenSections Plugin

This plugin requires HomeScreenSections to work. Install it first:

1. Open your Jellyfin web interface and log in as an administrator
2. Go to **Dashboard** → **Plugins** → **Catalog**
3. Search for **"HomeScreenSections"**
4. Click the **Install** button next to it
5. Wait for the installation to complete

#### Step 2: Add Custom Repository

Add this plugin's repository to your Jellyfin server:

1. Go to **Dashboard** → **Plugins** → **Repositories**
2. Click the **"+"** button to add a new repository
3. Enter the following details:
   - **Repository Name**: `@Krught (Auto Collections Random Collections)`
   - **Repository URL**: `https://raw.githubusercontent.com/Krught/jellyfin-plugin-auto-collections-random-collections/refs/heads/main/manifest.json`
4. Click **Save**

#### Step 3: Install Auto-Collections-Random-Collections

Now install this plugin:

1. Go to **Dashboard** → **Plugins** → **Catalog**
2. Search for **"Auto Collections"** or **"Random Collections"**
3. Click the **Install** button
4. Wait for installation to complete

#### Step 4: Restart Your Jellyfin Server

1. Go to **Dashboard**
2. Click **Restart** in the top-right corner
3. Confirm the restart and wait for your server to come back online
4. Log back in

#### Step 5: Verify Plugins Are Enabled

1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Look for both:
   - ✅ **HomeScreenSections** - should show as "Active"
   - ✅ **Auto-Collections-Random-Collections** - should show as "Active"
4. If either shows as disabled, click on it and enable it, then restart again

#### Step 6: Configure the Plugin (Optional)

1. In **Dashboard** → **Plugins** → **My Plugins**
2. Click on **Auto-Collections-Random-Collections**
3. Set **Number of Random Collections** (default is 3, you can choose 1-20)
4. Click **Save**
5. Restart your server if you changed the setting

#### Step 7: Enjoy Your Random Collections!

1. Navigate to your **Home Screen**
2. You should now see random collections appearing!
3. Collections will change on server restart and periodically when you reload the page

---

## Configuration

### How to Change Settings

1. Log in to Jellyfin as an administrator
2. Go to **Dashboard** → **Plugins** → **My Plugins**
3. Click on **Auto-Collections-Random-Collections**
4. Adjust the settings (see below)
5. Click **Save**
6. **Restart your server** for changes to take effect

### Available Settings

**Number of Random Collections**
- Controls how many random collections appear on your home screen
- Range: 1 to 20
- Default: 3
- Example: If set to 5, you'll see 5 different random collections each time

### When Do Collections Update?

Collections will automatically refresh in these situations:
- When you **restart your Jellyfin server**
- When you **change the plugin configuration**
- Periodically when you **reload the home screen** (30% chance each reload to keep things fresh without being too chaotic)

---

## Troubleshooting

### I Don't See Any Collections on My Home Screen

**Check 1: Do you have collections?**
- This plugin only works if you have collections in your Jellyfin library
- To create a collection: Select some movies/shows → Click **"+"** → **"Add to Collection"** → **"New Collection"**

**Check 2: Are both plugins installed and enabled?**
1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Verify you see both plugins listed:
   - **HomeScreenSections** - must be "Active"
   - **Auto-Collections-Random-Collections** - must be "Active"
3. If either is missing, return to the [Installation](#installation) section
4. If either is disabled, click on it, enable it, and restart your server

**Check 3: Did you restart after installation?**
- Jellyfin requires a full server restart after installing plugins
- Go to **Dashboard** → **Restart**

### Collections Aren't Updating

This is normal behavior! Collections update:
- ✅ On server restart
- ✅ When you change the configuration
- ✅ Occasionally on home screen reload (30% of the time)

**To force new collections to appear:**
1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Click **Auto-Collections-Random-Collections**
3. Change the number of collections (even by 1)
4. Click **Save**
5. Restart your server

### The Plugin Isn't in the Catalog

If you can't find the plugin in the Jellyfin catalog after adding the repository:

**Option 1: Check Your Jellyfin Version**
- This plugin requires Jellyfin **10.9.0 or higher**
- Check your version: **Dashboard** → Look at the top for version number
- If needed, update Jellyfin to the latest version

**Option 2: Verify Repository Was Added**
1. Go to **Dashboard** → **Plugins** → **Repositories**
2. Check that you see the repository: `@Krught (Auto Collections Random Collections)`
3. Verify the URL is correct: `https://raw.githubusercontent.com/Krught/jellyfin-plugin-auto-collections-random-collections/refs/heads/main/manifest.json`
4. If missing or incorrect, add it again following Step 2 of the installation

**Option 3: Manual Installation**
1. Download the latest `RandomCollectionsHome.dll` file from the releases page
2. Find your Jellyfin plugins folder:
   - **Windows**: `C:\ProgramData\Jellyfin\Server\plugins\`
   - **Linux**: `/var/lib/jellyfin/plugins/`
   - **Docker**: `/config/plugins/` (inside the container)
3. Create a folder called `Auto-Collections-Random-Collections` if it doesn't exist
4. Copy the `.dll` file into that folder
5. Restart your Jellyfin server
6. Continue with Step 1 above to install HomeScreenSections

### Still Having Issues?

**Check the Logs:**
1. Go to **Dashboard** → **Logs**
2. Look for entries containing `RandomCollectionsHome` or `Auto-Collections-Random-Collections`
3. Look for error messages that might explain the issue

**Common Error Messages:**
- **"HomeScreenSections not found"** → Install the HomeScreenSections plugin
- **"No collections available"** → Create at least one collection in your library
- **"Permission denied"** → Check file permissions on the plugins folder

---

## Frequently Asked Questions

**Q: How many collections should I configure?**
A: Start with 3-5. Too many can make your home screen cluttered, too few might not showcase enough content.

**Q: Can I choose which collections to show?**
A: Not currently - the plugin randomly selects from all your collections. This is by design to help you rediscover content.

**Q: Will this work with movies and TV shows?**
A: Yes! It works with any type of collection you create in Jellyfin.

**Q: Does this slow down my server?**
A: No, it's very lightweight. The plugin uses smart caching to minimize performance impact.

**Q: Can I make collections update more often?**
A: Currently, collections have a 30% chance to refresh on each home screen reload. To manually force a refresh, restart your server.

**Q: Will different users see different collections?**
A: Yes! The plugin caches collections per user, so each user can have a different set of random collections.

---

## Advanced: Building from Source

If you want to build this plugin yourself (for development or customization):

### Requirements
- .NET 8.0 SDK or higher
- Git

### Build Steps

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd jellyfin-plugin-auto-collections-random-collections
   ```

2. **Build the plugin:**
   ```bash
   dotnet build -c Release
   ```

3. **Find the built file:**
   - Location: `bin/Release/net8.0/RandomCollectionsHome.dll`

4. **Install manually:**
   - Copy the DLL to your Jellyfin plugins folder (see Manual Installation above)
   - Restart Jellyfin

For troubleshooting build issues, ensure you have .NET 8.0 SDK installed:
```bash
dotnet --version
```

---

## Support

If you encounter issues not covered in the troubleshooting section, please check the project repository for additional help or to report bugs.

## Credits

Based on the jellyfin-plugin-auto-collections project by KeksBombe.

## License

MIT License
