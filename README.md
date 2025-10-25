# Home Sections Random Collections


<p align="center">
  <img src="logo.png" alt="Home Sections Random Collections Logo" width="100%"/>
</p>


A Jellyfin plugin that randomly displays collections on your home screen, keeping your content fresh and interesting every time you visit!

## What Does This Plugin Do?

This plugin automatically selects random collections from your library and displays them on your Jellyfin home screen. Collections are cached and automatically refreshed at a configurable interval (default: 24 hours), helping you rediscover content in your library.

## Features

- 🎲 Randomly displays collections on your home screen
- ⚙️ Configurable - choose how many collections to show, update interval, item limits, and display types
- 🔄 Automatic refresh based on configurable interval
- 🎨 Beautiful, easy-to-use configuration interface
- 📊 REST API endpoints for integration
- 🔍 Comprehensive logging for troubleshooting
- 🌐 Instance-level (all users see the same collections)

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

#### Step 6: Configure the Plugin

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
5. Click **Save** to apply changes
6. **Note:** Changes to settings will clear the cache and immediately select new random collections

**Quick Actions:**
- **Rerandomize Sections** button: Instantly selects new random collections without changing settings
- **Developer Mode** checkbox: Toggle debug information display on/off

### Available Settings

**Number of Random Collections**
- Controls how many random collections are displayed on the home screen
- Default: 3
- Range: 1-100 collections

**View Mode Options**
- Choose which display types are available: Portrait, Square, Landscape
- If only one is selected, all collections will use that mode
- If multiple are selected, each collection will randomly use one of the enabled modes
- All three enabled by default
- **Note:** At least one view mode must be selected

**Collection Items**
- Controls how many items from each collection are displayed on the home screen
- This is the number of movies/shows visible in each collection section
- Default: 20 items per collection
- Set to 0 to display all items from each collection

**Collection Update Interval**
- Controls how often collections are automatically refreshed (in minutes)
- Default: 1440 minutes (24 hours)
- Set to 0 to disable automatic updates
- When this interval expires, new random collections are automatically selected

**Developer Mode** (Advanced)
- When enabled, shows debug information about currently registered sections
- Displays Section ID, Collection Name, Collection ID, View Mode, and Item Count for each section
- Auto-refreshes debug info at a configurable interval (5-300 seconds, default: 60 seconds)
- Useful for troubleshooting and verifying which collections are currently displayed
- Does not affect plugin operation, only displays debug information

### How Caching Works

Collections are cached at the Jellyfin instance level:
- **Automatic refresh**: New random collections are selected after the configured interval (default: 24 hours)
- **On configuration change**: Cache is cleared and new collections are selected immediately
- **Manual refresh**: Use the `/RandomCollections/Refresh` API endpoint to force an update

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

This is normal behavior! Collections are cached for the configured update interval:
- ✅ Collections update automatically based on the **Collection Update Interval** setting (default: 24 hours)
- ✅ This provides a consistent experience and reduces server load
- ✅ All users see the same collections (instance-level, not per-user)

**To force new collections immediately:**
- Click the **Rerandomize Sections** button in the plugin settings page
- Or use the API endpoint: `POST /RandomCollections/Refresh`
- Or change any plugin setting and save (this clears the cache)
- Or adjust the **Collection Update Interval** to refresh more frequently

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
- **"Successfully registered section for collection '{CollectionName}'"** → Collection added to home screen
- **"Auto-updating collections for user {UserId}"** → Collections being refreshed after interval expires

---

## API Usage

### Endpoints

**Get Random Collections**
```
GET /RandomCollections/Get
```
Returns the list of currently displayed random collections (same for all users on the Jellyfin instance).

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
Clears the cache and forces new random collection selection immediately.

**Get Debug Information**
```
GET /RandomCollections/Debug/Sections
```
Returns detailed debug information about currently registered sections (used by Developer Mode).

Response:
```json
[
  {
    "SectionId": "RANDOMONE",
    "CollectionName": "Collection Name",
    "CollectionId": "guid",
    "ViewMode": "Portrait",
    "ItemCount": 15
  }
]
```

## Frequently Asked Questions

**Q: How do I display collections on my home screen?**
A: You need to install the **HomeScreenSections** plugin. This plugin automatically registers sections with HomeScreenSections to display random collections on your home screen.

**Q: How many collections should I configure?**
A: Start with 3-5. You can adjust the "Number of Random Collections" setting based on your preference.

**Q: Can I choose which collections to show?**
A: Not currently - the plugin randomly selects from all your collections. This is by design to help you rediscover content.

**Q: Will this work with movies and TV shows?**
A: Yes! It works with any type of collection (BoxSet) you create in Jellyfin.

**Q: Does this slow down my server?**
A: No, it's very lightweight. The plugin uses caching to minimize performance impact, only refreshing at the configured interval.

**Q: Are collections different for each user?**
A: No, collections are selected at the Jellyfin instance level. All users see the same random collections that update at the configured interval.

**Q: How many items from each collection are shown?**
A: This is controlled by the **Collection Items** setting (default: 20). This determines how many movies/shows are displayed in each collection section on your home screen. Set it to 0 to show all items from each collection.

**Q: How do I see which collections are currently displayed?**
A: Enable **Developer Mode** in the plugin settings. This will show debug information including Section ID, Collection Name, View Mode, and Item Count for each currently displayed collection.


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
