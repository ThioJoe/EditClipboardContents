# 📋 Edit Clipboard Contents
#### Windows application for inspection and editing of raw clipboard contents

## ✔️ Key Features
- **View Details of Every Clipboard Object**: View all clipboard format objects currently in use and their contents
- **Raw Content Editing**: Modify clipboard data at the hexadecimal level, then save the changes back to the clipboard
- **Individual Object Removal**: Ability to delete individual objects from the clipboard altogether
- **Add Custom Formats**: Add a new format to the clipboard with a specific name or ID, as well as the data contents
- **Data Structure Analysis**: Examine the object structure of many supported formats
- **Save & Restore**: Export and import clipboard contents
- **Format Reordering**: Rearrange clipboard format priority order
- **Binary Data Filetype Detection**: Automatic detection of 300+ file types using byte signatures

## Screenshot:
<p align="center">
<img alt="Main application Window Screenshot" width=750 src="https://github.com/user-attachments/assets/0b7bb7b7-d9db-4be0-818f-f9bf2a61264f">
</p>

## 🔍 Main Features

### 1. All-Format Clipboard Inspection
- View every clipboard format currently in use, including:
  - Standard formats (text, images, files)
  - Application-specific formats
  - System and "synthesized" formats
- Display format details such as:
  - Format name and ID
  - Data size
  - Identify likely "synthesized" formats, meaning the system added them automatically
  - Parsed metadata details for certain supported formats like BMP image dimensions
  - Visual indicators for known exportable types
  - Extra information about underlying data structure
- View the object struct info for certain standard formats such as Bitmap formats (CF_BITMAP, CF_DIB, CF_DIBV5)
- View raw data alongside interpreted text in either UTF-8 or UTF-16

### 2. Raw Content Editing
- Edit multiple clipboard objects, then apply pending changes when ready
- Edit clipboard contents at the hexadecimal level
- Real-time plaintext preview of hex edits
   - Support for both UTF-8 and UTF-16 encodings
- Or just remove individual objects/formats
- Load raw data directly from files
- Clear clipboard contents entirely
- Rearrange clipboard format priority order

### 3. Backup and Restore
- Export entire clipboard contents as:
  - ZIP file
  - Folder structure
- Export specific format ranges
- Re-import saved formats from backup

### 4. Add Custom Formats
- Add one or multiple data formats to the clipboard
- Specify a custom name or ID number to use for each
- Edit the raw contents of custom formats just like any other, or even use null data

### 5. Advanced Features
- Timed refresh capability
  - Set custom delay for automatic clipboard refresh
  - This might be useful to see if an app sets different clipboard data while certain actions are occurring, during which you wouldn't be able to normally click the refresh button
- Progress tracking for format loading
- Debug console available (launch with `-console` argument)
- Presentation of underlying data structures for supported formats
- Manually search for specific formats by format ID or name
  - If the format was already in the list, it will be updated, or otherwise added
  - Good for updating specific formats while preserving the rest

### 6. Export Capabilities
- Text file containing raw data converted to hexadecimal
- Raw data as binary file
  - Some non-standard formats such as PNG just contain the entire file as the raw data, so in such cases exporting it as a file would result in the working PNG image
- Native file export for certain formats
  - Example: The three native bitmap formats (CF_BITMAP, CF_DIB, CF_DIBV5) don't directly contain the same data that would be found by opening a `.bmp` file, but rather the image data as well as other struct metadata. But the app will use the Native Windows API to output each format to a BMP file, and each may be slightly different despite supposedly containing the same image data.
- Export a list of all registered clipboard format names on the system

-----

## How To Compile:

### Requirements:
 - Only requires Visual Studio 2022
   - At the moment there are no external dependencies beyond the .NET Framework 4.8 built into windows (no need to download anything extra, install any Nuget packages, etc)

### Instructions:
1. Open the "Solution" file (`EditClipboardContents.sln`) with Visual Studio 2022
   - The entire solution/project is included with the repo, so after opening it should be ready to compile and run immediately after opening it
2. Optional: Choose the build "configuration" mode (Either `Release` or `Debug`)
    - The Debug configuration is for during development, and you'll notice the app will have an additional menu called `[Debugging]` with some various options and modes that do not appear in release mode, and an additional label in the UI with some layout info at the top. Debugging menu examples:
       - An option to have the tooltip show the dimensions and location coordinates of Windows Forms controls when hovering over them, like when debugging scaling or layout issues
       - A "Test" button you can use to run miscelaneous code you can put in that button's event handler
       - A button used to parse the table from a wikipedia page containing file signatures and convert it to JSON, so it can be used as an embedded reference by the program
3. Compile by going to `Build` (top menu) > `Build Solution`, or if in Debug configuration, `Debug` > `Start Debugging` (Or just click the toolbar button that says "Start" with the green triangle)
