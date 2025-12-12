private async void ConvertButton_Click(object sender, RoutedEventArgs e)
{
    if (isConverting) return;

    if (string.IsNullOrWhiteSpace(fileToConvertPath))
    {
        MessageBox.Show("Please select a model or place you'd like to convert by clicking on 'Browse'", "Cannot convert place", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
    }

    // Optimization: Don't read all lines just to check the header. 
    // This saves memory on massive files that might be on a single line.
    using (StreamReader reader = new StreamReader(fileToConvertPath))
    {
        char[] buffer = new char[50];
        await reader.ReadAsync(buffer, 0, buffer.Length);
        string header = new string(buffer);
        if (header.Contains("<roblox!"))
        {
            MessageBox.Show("Please select a model or place in Roblox XML format.", "Cannot convert place", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
    }

    // Ask user where to save
    SaveFileDialog fileDialog = new SaveFileDialog();
    fileDialog.Filter = "Roblox XML Place Files (*.rbxl)|*.rbxl|Roblox XML Model Files (*.rbxm)|*.rbxm";
    if (fileDialog.ShowDialog() != true) return;

    newFilePath = fileDialog.FileName;

    // UI Updates
    isConverting = true;
    ConvertButton.IsEnabled = false;
    ProgressBar.Value = 0;
    ProgressLabel.Content = "Starting...";
    
    // Capture the settings from UI checkboxes before going to background thread
    // (UI elements cannot be accessed safely from a background thread)
    bool convertColors = ColorCheckbox.IsChecked == true;
    bool removeUnions = UnionCheckbox.IsChecked == false;
    bool convertScripts = ScriptConvertCheckbox.IsChecked == true;
    bool convertFolders = ConvertFoldersCheckbox.IsChecked == true;
    bool fixAssetIds = ChangeRbxassetidCheckbox.IsChecked == true;
    bool convertTextSize = ChangeTextSizeToFontSizeCheckbox.IsChecked == true;

    try
    {
        // Run the heavy lifting on a background thread
        await Task.Run(() => 
        {
            ProcessFile(fileToConvertPath, newFilePath, 
                convertColors, removeUnions, convertScripts, 
                convertFolders, fixAssetIds, convertTextSize);
        });

        ProgressBar.Value = 100;
        ProgressLabel.Content = "Done!";
        MessageBox.Show("Conversion done!", "Conversion status", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error during conversion: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        ProgressLabel.Content = "Error";
    }
    finally
    {
        isConverting = false;
        ConvertButton.IsEnabled = true;
    }
}

// The heavy logic method - optimized for memory
private void ProcessFile(string sourcePath, string destPath, 
    bool convertColors, bool removeUnions, bool convertScripts, 
    bool convertFolders, bool fixAssetIds, bool convertTextSize)
{
    // 1. Copy the file first
    File.Copy(sourcePath, destPath, true);
    string fileContents = File.ReadAllText(destPath);

    // 2. Remove Terrain
    int terrainIndex = fileContents.IndexOf("<Item class=\"Terrain\"", StringComparison.Ordinal);
    if (terrainIndex != -1)
    {
        int terrainEndIndex = fileContents.IndexOf("</Item>", terrainIndex, StringComparison.Ordinal);
        if (terrainEndIndex != -1)
        {
            fileContents = fileContents.Remove(terrainIndex, terrainEndIndex - terrainIndex + 7);
        }
    }

    // 3. Convert TextSize (Optimized to Single Pass)
    if (convertTextSize)
    {
        int[] FontSizes = new int[] { 8, 9, 10, 11, 12, 14, 18, 24, 36, 48 };
        Regex rg = new Regex(@"<float name=""TextSize"">(\d{1,3})</float>");
        
        fileContents = rg.Replace(fileContents, (match) =>
        {
            if (int.TryParse(match.Groups[1].Value, out int currentSize))
            {
                var nearest = FontSizes.OrderBy(x => Math.Abs((long)x - currentSize)).First();
                int fontSizeIndex = Array.IndexOf(FontSizes, nearest);
                return $"<token name=\"FontSize\">{fontSizeIndex}</token>";
            }
            return match.Value;
        });
    }

    // 4. Convert Colors (CRITICAL OPTIMIZATION: Single Pass)
    if (convertColors)
    {
        // Instead of looping the dictionary 100 times, we regex find ALL Color3uint8 tags
        // and look them up in the dictionary on the fly.
        Regex colorRegex = new Regex(@"<Color3uint8 name=""Color3uint8"">(\d+)</Color3uint8>");
        
        fileContents = colorRegex.Replace(fileContents, (match) =>
        {
            string colorKey = match.Groups[1].Value;
            if (color3uint8ToBrickColor.TryGetValue(colorKey, out string brickColorId))
            {
                return $"<int name=\"BrickColor\">{brickColorId}</int>";
            }
            return match.Value; // Keep original if not found
        });
    }

    // 5. Remove Unions
    if (removeUnions)
    {
        int unionIndex = fileContents.IndexOf("<Item class=\"NonReplicatedCSGDictionaryService\"", StringComparison.Ordinal);
        if (unionIndex != -1)
        {
            // Note: This while-loop approach is still risky for memory on massive files, 
            // but acceptable for now since we saved memory on the colors.
            int binaryStringIndex = fileContents.IndexOf("<Item class=\"BinaryStringValue\"", unionIndex, StringComparison.Ordinal);
            while (binaryStringIndex != -1)
            {
                int binaryStringEndIndex = fileContents.IndexOf("</Item>", binaryStringIndex, StringComparison.Ordinal);
                if (binaryStringEndIndex != -1)
                {
                    fileContents = fileContents.Remove(binaryStringIndex, binaryStringEndIndex - binaryStringIndex + 7);
                }
                binaryStringIndex = fileContents.IndexOf("<Item class=\"BinaryStringValue\"", binaryStringIndex, StringComparison.Ordinal);
            }
        }
    }

    // 6. Script Conversion
    if (convertScripts)
    {
        // Simple replacements are fast
        fileContents = fileContents.Replace("<ProtectedString name=\"Source\"><![CDATA[", "<ProtectedString name=\"Source\">")
                                   .Replace("]]></ProtectedString>", "</ProtectedString>");

        // For the content escaping, we can use a Regex to find the Source tags 
        // to avoid manual index math, but sticking to your logic for stability:
        int scriptStartIndex = fileContents.IndexOf("<ProtectedString name=\"Source\">", StringComparison.Ordinal);
        while (scriptStartIndex != -1)
        {
            int scriptEndIndex = fileContents.IndexOf("</ProtectedString>", scriptStartIndex, StringComparison.Ordinal);
            if (scriptEndIndex != -1)
            {
                // Extract content
                int contentStart = scriptStartIndex + 31;
                int length = scriptEndIndex - contentStart;
                if (length > 0)
                {
                    string originalContent = fileContents.Substring(contentStart, length);
                    
                    // Only replace if necessary to save allocations
                    if (originalContent.IndexOfAny(new[] { '"', '\'', '<', '>' }) != -1)
                    {
                        string newContent = originalContent
                            .Replace("\"", "&quot;")
                            .Replace("\'", "&apos;")
                            .Replace("<", "&lt;")
                            .Replace(">", "&gt;");
                        
                        // We must reconstruct the string
                        fileContents = fileContents.Remove(contentStart, length)
                                                   .Insert(contentStart, newContent);
                        
                        // Adjust index because string length changed
                        scriptEndIndex = contentStart + newContent.Length; 
                    }
                }
            }
            scriptStartIndex = fileContents.IndexOf("<ProtectedString name=\"Source\">", scriptEndIndex, StringComparison.Ordinal);
        }
    }

    // 7. Asset IDs
    if (fixAssetIds)
    {
        fileContents = fileContents.Replace("rbxassetid://", "http://www.roblox.com/asset/?id=");
    }

    // 8. Convert Folders
    if (convertFolders)
    {
        fileContents = fileContents.Replace("<Item class=\"Folder\"", "<Item class=\"Model\"");
    }

    // Write final result
    File.WriteAllText(destPath, fileContents);
}
