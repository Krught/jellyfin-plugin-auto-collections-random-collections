# Home Sections Random Collections


<p align="center">
  <img src="logo.png" alt="Home Sections Random Collections Logo" width="100%"/>
</p>


A Jellyfin plugin that randomly displays collections on your home screen, keeping your content fresh and interesting every time you visit!

## What Does This Plugin Do?

This plugin automatically selects random collections from your library and displays them on your Jellyfin home screen. An admin configurable setting allows you to change how often categories are updated, you'll see different collections, helping you rediscover content in your library.

## Features

- 🎲 Randomly displays collections accessible via API
- ⚙️ Configurable - choose how many collections to show, how long to show collections, and what display type they appear in
- 🔄 Auto-refreshes with smart caching
- 🎨 Beautiful, easy-to-use configuration interface
- 📊 REST API endpoints for integration
- 🔍 Comprehensive logging for troubleshooting

## Requirements

- Jellyfin Server **10.9.0 or higher**
- **HomeScreenSections Plugin** (required for displaying sections on home screen) - [GitHub Repository](https://github.com/IAmParadox27/jellyfin-plugin-home-sections/tree/main)

---

## Installation

### Installing via Jellyfin Admin Panel (Recommended)

This is the easiest way to install the plugin!

#### Step 1: Install HomeScreenSections Plugin

This plugin requires HomeScreenSections to display collections on your home screen.

1. Open your Jellyfin web interface and log in as an administrator
2. Go to **Dashboard** → **Plugins** → **Catalog**
3. Search for **"HomeScreenSections"**
4. Click **Install** next to it
5. Wait for the installation to complete

#### Step 2: Add Custom Repository

Add this plugin's repository to your Jellyfin server:

1. Go to **Dashboard** → **Plugins** → **Catalog**
2. Click the **⚙️** button to modify repositories
2. Click the **"+"** button to add a new repository
3. Enter the following details:
   - **Repository Name**: `@Krught (Home Sections Random Collections)`
   - **Repository URL**: `https://raw.githubusercontent.com/Krught/jellyfin-plugin-home-sections-random-collections/refs/heads/main/manifest.json`
4. Click **Save**

#### Step 3: Install Home Sections Random Collections

Now install this plugin:

1. Go to **Dashboard** → **Plugins** → **Catalog**
2. Search for **"Home Sections Random Collections"**
3. Click the **Install** button
4. Wait for installation to complete

#### Step 4: Restart Your Jellyfin Server

1. Go to **Dashboard**
2. Click **Restart** in the top-right corner
3. Confirm the restart and wait for your server to come back online
4. Log back in

#### Step 5: Verify Plugins Are Enabled

1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Verify both plugins are listed and "Active":
   - ✅ **HomeScreenSections**
   - ✅ **Home Sections Random Collections**
3. If either shows as disabled, click on it, enable it, and restart again

#### Step 6: Configure the Plugin (Optional)

1. In **Dashboard** → **Plugins** → **My Plugins**
2. Click on **Home Sections Random Collections**
3. Set **Number of Random Collections** (default is 3, you can choose any number)
4. Click **Save Settings**

#### Step 7: Check Your Home Screen!

1. Navigate to your **Home Screen**
2. You should now see random collections appearing as sections!
3. Check the **Jellyfin logs** (Dashboard → Logs) for detailed information:
   - Look for "Home Sections Random Collections plugin initialized"
   - See which collections were found: "Available collection: '{Name}'"
   - Verify sections were registered: "Successfully registered section for collection '{Name}'"

**Troubleshooting**: If you don't see sections, check the logs for:
- "HomeScreenSections plugin not found" → Install HomeScreenSections first
- "No collections found in library" → Create at least one collection
- Detailed registration logs will show exactly what happened

---

## Configuration

### How to Change Settings

1. Log in to Jellyfin as an administrator
2. Go to **Dashboard** → **Plugins** → **My Plugins**
3. Click on **Home Sections Random Collections**
4. Adjust the settings (see below)
5. Click **Save**
6. **Restart your server** for changes to take effect

### Available Settings

**Number of Random Collections**
- Controls how many random collections are returned by the API
- Default: 3
- Example: If set to 5, the API will return 5 different random collections for each user

### When Do Collections Update?

Collections use smart caching per user:
- **70% of the time**: Cached collections are returned (prevents excessive randomization)
- **30% of the time**: New random collections are selected
- **On configuration change**: Cache is cleared and new collections are selected
- **Manual refresh**: Use the `/RandomCollections/Refresh` endpoint

---

## Troubleshooting

### Plugin Settings Page Not Showing

**If clicking the settings button shows "no settings" error:**
- Make sure you've restarted Jellyfin after installation
- Check that the plugin is marked as "Active" in My Plugins
- Check Jellyfin logs for any errors during plugin initialization

### Can't Access the API

**Check 1: Do you have collections?**
- This plugin only works if you have collections (BoxSets) in your Jellyfin library
- To create a collection: Select some movies/shows → Click **"+"** → **"Add to Collection"** → **"New Collection"**

**Check 2: Is the plugin installed and enabled?**
1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Verify **Home Sections Random Collections** is listed and "Active"
3. If disabled, click on it, enable it, and restart your server

**Check 3: Did you restart after installation?**
- Jellyfin requires a full server restart after installing plugins
- Go to **Dashboard** → **Restart**

**Check 4: Check if HomeScreenSections is installed**
1. Go to **Dashboard** → **Plugins** → **My Plugins**
2. Look for **HomeScreenSections** plugin
3. If not installed, go to Catalog and install it
4. Restart Jellyfin after installing

**Check 5: Look at the logs for detailed information**
- Go to **Dashboard** → **Logs**
- Look for messages from this plugin:
  - `"Found X collections in library"` - Shows detected collections
  - `"Available collection: 'Name'"` - Lists each collection found
  - `"Attempting to register home screen sections"` - Shows registration started
  - `"Successfully registered section for collection 'Name'"` - Confirms registration
  - `"HomeScreenSections plugin not found"` - Means you need to install it
- These logs will tell you exactly what the plugin is doing

### Collections Aren't Changing

This is normal behavior! The plugin uses smart caching:
- ✅ 70% of the time: Returns cached collections (consistent experience)
- ✅ 30% of the time: Selects new random collections
- ✅ On configuration change: Cache is cleared

**To force new collections:**
- Use the API endpoint: `POST /RandomCollections/Refresh`
- Or change the plugin configuration and save

### The Plugin Isn't in the Catalog

If you can't find the plugin in the Jellyfin catalog after adding the repository:

**Option 1: Check Your Jellyfin Version**
- This plugin requires Jellyfin **10.9.0 or higher**
- Check your version: **Dashboard** → Look at the top for version number
- If needed, update Jellyfin to the latest version

**Option 2: Verify Repository Was Added**
1. Go to **Dashboard** → **Plugins** → **Repositories**
2. Check that you see the repository: `@Krught (Home Sections Random Collections)`
3. Verify the URL is correct: `https://raw.githubusercontent.com/Krught/jellyfin-plugin-home-sections-random-collections/refs/heads/main/manifest.json`
4. If missing or incorrect, add it again following Step 2 of the installation

**Option 3: Manual Installation**
1. Download the latest `RandomCollectionsHome.dll` file from the releases page
2. Find your Jellyfin plugins folder:
   - **Windows**: `C:\ProgramData\Jellyfin\Server\plugins\`
   - **Linux**: `/var/lib/jellyfin/plugins/`
   - **Docker**: `/config/plugins/` (inside the container)
3. Create a folder called `Home-Sections-Random-Collections` if it doesn't exist
4. Copy the `.dll` file into that folder
5. Restart your Jellyfin server
6. Continue with Step 1 above to install HomeScreenSections

### Still Having Issues?

**Check the Logs:**
1. Go to **Dashboard** → **Logs**
2. Look for entries containing `Jellyfin.Plugin.RandomCollectionsHome`
3. The plugin now uses proper ILogger for comprehensive logging
4. Look for error messages that might explain the issue

**Common Log Messages:**
- **"Found {Count} collections in library"** → Shows how many collections were detected
- **"No collections found in library"** → Create at least one collection
- **"Plugin instance is null"** → The plugin failed to initialize properly
- **"Getting random collections for user {UserId}"** → Normal operation

---

## API Usage

### Endpoints

**Get Random Collections**
```
GET /RandomCollections/Get
```
Returns a list of random collections for the authenticated user.

Response:
```json
[
  {
    "id": "guid",
    "name": "Collection Name",
    "itemCount": 10
  }
]
```

**Get Collection Items**
```
GET /RandomCollections/Items/{collectionId}
```
Returns all items within a specific collection.

**Refresh Collections**
```
POST /RandomCollections/Refresh
```
Clears the cache and forces new random collection selection on next request.

## Frequently Asked Questions

**Q: How do I display collections on my home screen?**
A: This plugin provides the API endpoints. You'll need to integrate them with your custom home screen implementation or use a compatible home screen plugin.

**Q: How many collections should I configure?**
A: Start with 3-5. Adjust based on your needs and integration.

**Q: Can I choose which collections to show?**
A: Not currently - the plugin randomly selects from all your collections. This is by design to help you rediscover content.

**Q: Will this work with movies and TV shows?**
A: Yes! It works with any type of collection (BoxSet) you create in Jellyfin.

**Q: Does this slow down my server?**
A: No, it's very lightweight. The plugin uses smart caching to minimize performance impact.


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
   cd jellyfin-plugin-home-sections-random-collections
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


## License

MIT License
