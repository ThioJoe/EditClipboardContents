# 📋 Edit Clipboard Contents

#### Windows application for inspection and editing of raw clipboard contents

## ✔️ Key Features

- **View Details of Every Clipboard Object**: View all clipboard format objects currently in use and their contents
- **Raw Content Editing**: Modify clipboard data at the hexadecimal level, then save the changes back to the clipboard
- **Individual Object Removal**: Ability to delete individual objects from the clipboard altogether
- **Add custom formats**: Add a new format to the clipboard with a specific name or ID, as well as the data contents
- **Object Analysis (For Certain Formats)**: Examine the object structure of certain standard formats like BMP

## Screenshot:
<p align="center">
<img alt="Main application Window Screenshot" width=750 src="https://github.com/user-attachments/assets/0dafd3ef-15be-4576-b764-b8c3a722a967">
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
- View the object struct info for certain standard formats such as Bitmap formats (CF_BITMAP, CF_DIB, CF_DIBV5)
- View raw data alongside interpreted text in either UTF-8 or UTF-16

### 2. Raw Content Editing

- Edit multiple clipboard objects, then apply pending changes when ready
- Edit clipboard contents at the hexadecimal level
- Real-time plaintext preview of hex edits
   - Support for both UTF-8 and UTF-16 encodings
- Or just remove individual objects/formats

### 3. Add Custom Formats
- Add one or multiple data formats to the clipboard
- Specify a custon name or ID number to use for each
- Edit the raw contents of custom formats just like any other, or even use null data

### 4. Manually search specific formats
- Ability to specify a format name or ID to fetch
- If the format was already in the list, it will be updated, or otherwise added
- Good for updating specific formats while preserving the rest

### 5. Export Capabilities

- Text file containing raw data converted to hexadecimal
- Raw data as binary file
  - Some non-standard formats such as PNG just contain the entire file as the raw data, so in such cases exporting it as a file would result in the working PNG image
- Native file export for certain formats
  -  Example: The three native bitmap formats (CF_BITMAP, CF_DIB, CF_DIBV5) don't directly contain the same data that would be found by opening a `.bmp` file, but rather the image data as well as other struct metadata. But the app will use the Native Windows API to output each format to a BMP file, and each may be slightly different despite supposedly containing the same image data.
-  Export a list of all registered clipboard format names on the system


