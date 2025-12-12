using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Roblox_Legacy_Place_Convertor
{
    public partial class MainWindow : Window
    {
        private string fileToConvertPath;
        private string newFilePath;
        private bool isConverting;
        
        // The massive color dictionary
        private static readonly Dictionary<string, string> color3uint8ToBrickColor = new Dictionary<string, string>
        {
            {"4292120765", "343"}, {"4290668642", "338"}, {"4288891723", "1007"}, {"4283835428", "339"},
            {"4294939796", "337"}, {"4288042325", "344"}, {"4287581226", "345"}, {"4293572754", "125"},
            {"4292511354", "101"}, {"4287118910", "350"}, {"4291595881", "18"}, {"4286340166", "217"},
            {"4288046950", "360"}, {"4288700213", "38"}, {"4284107842", "364"}, {"4285151497", "365"},
            {"4288654784", "314"}, {"4278497260", "1013"}, {"4290019583", "1006"}, {"4289158811", "219"},
            {"4286263163", "322"}, {"4285215356", "104"}, {"4289832959", "1026"}, {"4286626779", "11"},
            {"4294928076", "1016"}, {"4294901951", "1032"}, {"4289331370", "1015"}, {"4288086016", "327"},
            {"4290040548", "45"}, {"4288201435", "329"}, {"4294940892", "330"}, {"4294924633", "331"},
            {"4294901760", "1004"}, {"4291045404", "21"}, {"4285857792", "332"}, {"4291286244", "336"},
            {"4292915920", "342"}, {"4294112243", "1"}, {"4293442248", "9"}, {"4294953417", "1025"},
            {"4293515994", "349"}, {"4294954137", "1030"}, {"4290491314", "354"}, {"4291677645", "1002"},
            {"4292330906", "5"}, {"4289439902", "358"}, {"4289696899", "359"}, {"4288914085", "194"},
            {"4285097564", "363"}, {"4284193356", "310"}, {"4278255615", "1019"}, {"4285826717", "135"},
            {"4285438410", "102"}, {"4279069100", "23"}, {"4278190335", "1010"}, {"4284031577", "312"},
            {"4278255360", "1020"}, {"4288672745", "1027"}, {"4288651692", "311"}, {"4278815183", "315"},
            {"4287388575", "1023"}, {"4284622289", "1031"}, {"4286251131", "316"}, {"4289715711", "1024"},
            {"4292861918", "325"}, {"4291480529", "320"}, {"4293256415", "208"}, {"4293388268", "335"},
            {"4294967244", "1029"}, {"4294506744", "1001"}, {"4293057724", "347"}, {"4293782250", "348"},
            {"4291477411", "353"}, {"4287986039", "153"}, {"4287990152", "357"}, {"4284702562", "199"},
            {"4280763949", "141"}, {"4283460948", "301"}, {"4278226844", "107"}, {"4279970357", "26"},
            {"4280374457", "1012"}, {"4278194352", "303"}, {"4278198368", "1011"}, {"4281099549", "304"},
            {"4280844103", "28"}, {"4279430868", "1018"}, {"4284177769", "302"}, {"4283595950", "305"},
            {"4281555074", "306"}, {"4279249628", "307"}, {"4282193285", "308"}, {"4282023189", "1021"},
            {"4281634368", "309"}, {"4283144011", "37"}, {"4286549604", "1022"}, {"4287277957", "318"},
            {"4290364593", "319"}, {"4287938177", "323"}, {"4289248665", "324"}, {"4289848742", "328"},
            {"4291624908", "1025"}, {"4294498669", "334"}, {"4294830733", "226"}, {"4294043591", "340"},
            {"4294898619", "341"}, {"4292066966", "346"}, {"4290550621", "351"}, {"4291275896", "352"},
            {"4288709711", "356"}, {"4283843126", "361"}, {"4286474303", "362"}, {"4286356587", "317"},
            {"4288791692", "29"}, {"4293040960", "105"}, {"4285290571", "355"}, {"4280254493", "313"},
            {"4286091394", "151"}, {"4290887234", "1008"}, {"4288986439", "119"}, {"4294946816", "1005"},
            {"4294967040", "1009"}, {"4293900344", "333"}, {"4294298928", "24"}, {"4292178749", "133"},
            {"4292511041", "106"}, {"4289352960", "1014"}, {"4285087784", "192"}
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GithubHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isConverting) return;
            
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Roblox XML Place Files (*.rbxlx)|*.rbxlx|Roblox XML Model Files (*.rbxmx)|*.rbxmx|Roblox XML Place Files (*.rbxl)|*.rbxl|Roblox XML Model Files (*.rbxm)|*.rbxm";
            if (fileDialog.ShowDialog() == true)
            {
                fileToConvertPath = fileDialog.FileName;
                PlaceSelectedLabel.Content = "File selected: " + fileToConvertPath;
            }
        }

        // --- NEW ASYNC METHOD ---
        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (isConverting) return;

            if (string.IsNullOrWhiteSpace(fileToConvertPath))
            {
                MessageBox.Show("Please select a model or place you'd like to convert by clicking on 'Browse'", "Cannot convert place", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Quick check for Roblox XML format
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

            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "Roblox XML Place Files (*.rbxl)|*.rbxl|Roblox XML Model Files (*.rbxm)|*.rbxm";
            if (fileDialog.ShowDialog() != true) return;

            newFilePath = fileDialog.FileName;
            isConverting = true;
            ConvertButton.IsEnabled = false;
            ProgressBar.Value = 0;
            ProgressLabel.Content = "Starting...";

            // Gather settings
            bool convertColors = ColorCheckbox.IsChecked == true;
            bool removeUnions = UnionCheckbox.IsChecked == false;
            bool convertScripts = ScriptConvertCheckbox.IsChecked == true;
            bool convertFolders = ConvertFoldersCheckbox.IsChecked == true;
            bool fixAssetIds = ChangeRbxassetidCheckbox.IsChecked == true;
            bool convertTextSize = ChangeTextSizeToFontSizeCheckbox.IsChecked == true;

            try
            {
                // Run background task
                await Task.Run(() =>
                {
                    ProcessFile(fileToConvertPath, newFilePath, convertColors, removeUnions, convertScripts, convertFolders, fixAssetIds, convertTextSize);
                });

                ProgressBar.Value = 100;
                ProgressLabel.Content = "Done!";
                MessageBox.Show("Conversion done!", "Conversion status", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ProgressLabel.Content = "Failed";
            }
            finally
            {
                isConverting = false;
                ConvertButton.IsEnabled = true;
            }
        }

        // --- BACKGROUND WORKER METHOD ---
        private void ProcessFile(string source, string dest, bool doColors, bool noUnions, bool doScripts, bool doFolders, bool fixAssets, bool doText)
        {
            File.Copy(source, dest, true);
            string content = File.ReadAllText(dest);

            // 1. Remove Terrain
            int terrainIndex = content.IndexOf("<Item class=\"Terrain\"", StringComparison.Ordinal);
            if (terrainIndex != -1)
            {
                int terrainEnd = content.IndexOf("</Item>", terrainIndex, StringComparison.Ordinal);
                if (terrainEnd != -1) content = content.Remove(terrainIndex, terrainEnd - terrainIndex + 7);
            }

            // 2. TextSize to FontSize (Regex)
            if (doText)
            {
                int[] sizes = { 8, 9, 10, 11, 12, 14, 18, 24, 36, 48 };
                Regex rg = new Regex(@"<float name=""TextSize"">(\d{1,3})</float>");
                content = rg.Replace(content, m =>
                {
                    if (int.TryParse(m.Groups[1].Value, out int s))
                    {
                        var nearest = sizes.OrderBy(x => Math.Abs((long)x - s)).First();
                        return $"<token name=\"FontSize\">{Array.IndexOf(sizes, nearest)}</token>";
                    }
                    return m.Value;
                });
            }

            // 3. Colors (Regex)
            if (doColors)
            {
                Regex colorRg = new Regex(@"<Color3uint8 name=""Color3uint8"">(\d+)</Color3uint8>");
                content = colorRg.Replace(content, m =>
                {
                    return color3uint8ToBrickColor.TryGetValue(m.Groups[1].Value, out string bc) 
                        ? $"<int name=\"BrickColor\">{bc}</int>" 
                        : m.Value;
                });
            }

            // 4. Unions
            if (noUnions)
            {
                int uIdx = content.IndexOf("<Item class=\"NonReplicatedCSGDictionaryService\"", StringComparison.Ordinal);
                if (uIdx != -1)
                {
                    int binIdx = content.IndexOf("<Item class=\"BinaryStringValue\"", uIdx, StringComparison.Ordinal);
                    while (binIdx != -1)
                    {
                        int binEnd = content.IndexOf("</Item>", binIdx, StringComparison.Ordinal);
                        if (binEnd != -1) content = content.Remove(binIdx, binEnd - binIdx + 7);
                        binIdx = content.IndexOf("<Item class=\"BinaryStringValue\"", binIdx, StringComparison.Ordinal);
                    }
                }
            }

            // 5. Scripts
            if (doScripts)
            {
                content = content.Replace("<ProtectedString name=\"Source\"><![CDATA[", "<ProtectedString name=\"Source\">")
                                 .Replace("]]></ProtectedString>", "</ProtectedString>");
                
                int sIdx = content.IndexOf("<ProtectedString name=\"Source\">", StringComparison.Ordinal);
                while (sIdx != -1)
                {
                    int sEnd = content.IndexOf("</ProtectedString>", sIdx, StringComparison.Ordinal);
                    if (sEnd != -1)
                    {
                        int start = sIdx + 31;
                        int len = sEnd - start;
                        if (len > 0)
                        {
                            string raw = content.Substring(start, len);
                            if (raw.IndexOfAny(new[] { '"', '\'', '<', '>' }) != -1)
                            {
                                string safe = raw.Replace("\"", "&quot;").Replace("\'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;");
                                content = content.Remove(start, len).Insert(start, safe);
                                sEnd = start + safe.Length; 
                            }
                        }
                    }
                    sIdx = content.IndexOf("<ProtectedString name=\"Source\">", sEnd, StringComparison.Ordinal);
                }
            }

            // 6. Asset IDs
            if (fixAssets) content = content.Replace("rbxassetid://", "http://www.roblox.com/asset/?id=");

            // 7. Folders
            if (doFolders) content = content.Replace("<Item class=\"Folder\"", "<Item class=\"Model\"");

            File.WriteAllText(dest, content);
        }
    }
}
