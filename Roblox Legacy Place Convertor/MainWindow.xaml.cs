using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Roblox_Legacy_Place_Convertor
{
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
private string fileToConvertPath;
private string newFilePath;
private bool isConverting;
private static readonly Dictionary<string, string> color3uint8ToBrickColor = new Dictionary<string, string> // Yes, i wrote this all manually. Took me about 2 hours.
{
{"4292120765", "343"},
{"4290668642", "338"},
{"4288891723", "1007"},
{"4283835428", "339"},
{"4294939796", "337"},
{"4288042325", "344"},
{"4287581226", "345"},
{"4293572754", "125"},
{"4292511354", "101"},
{"4287118910", "350"},
{"4291595881", "18"},
{"4286340166", "217"},
{"4288046950", "360"},
{"4288700213", "38"},
{"4284107842", "364"},
{"4285151497", "365"},
{"4288654784", "314"},
{"4278497260", "1013"},
{"4290019583", "1006"},
{"4289158811", "219"},
{"4286263163", "322"},
{"4285215356", "104"},
{"4289832959", "1026"},
{"4286626779", "11"},
{"4294928076", "1016"},
{"4294901951", "1032"},
{"4289331370", "1015"},
{"4288086016", "327"},
{"4290040548", "45"},
{"4288201435", "329"},
{"4294940892", "330"},
{"4294924633", "331"},
{"4294901760", "1004"},
{"4291045404", "21"},
{"4285857792", "332"},
{"4291286244", "336"},
{"4292915920", "342"},
{"4294112243", "1"},
{"4293442248", "9"},
{"4294953417", "1025"},
{"4293515994", "349"},
{"4294954137", "1030"},
{"4290491314", "354"},
{"4291677645", "1002"},
{"4292330906", "5"},
{"4289439902", "358"},
{"4289696899", "359"},
{"4288914085", "194"},
{"4285097564", "363"},
{"4284193356", "310"},
{"4278255615", "1019"},
{"4285826717", "135"},
{"4285438410", "102"},
{"4279069100", "23"},
{"4278190335", "1010"},
{"4284031577", "312"},
{"4278255360", "1020"},
{"4288672745", "1027"},
{"4288651692", "311"},
{"4278815183", "315"},
{"4287388575", "1023"},
{"4284622289", "1031"},
{"4286251131", "316"},
{"4289715711", "1024"},
{"4292861918", "325"},
{"4291480529", "320"},
{"4293256415", "208"},
{"4293388268", "335"},
{"4294967244", "1029"},
{"4294506744", "1001"},
{"4293057724", "347"},
{"4293782250", "348"},
{"4291477411", "353"},
{"4287986039", "153"},
{"4287990152", "357"},
{"4284702562", "199"},
{"4280763949", "141"},
{"4283460948", "301"},
{"4278226844", "107"},
{"4279970357", "26"},
{"4280374457", "1012"},
{"4278194352", "303"},
{"4278198368", "1011"},
{"4281099549", "304"},
{"4280844103", "28"},
{"4279430868", "1018"},
{"4284177769", "302"},
{"4283595950", "305"},
{"4281555074", "306"},
{"4279249628", "307"},
{"4282193285", "308"},
{"4282023189", "1021"},
{"4281634368", "309"},
{"4283144011", "37"},
{"4286549604", "1022"},
{"4287277957", "318"},
{"4290364593", "319"},
{"4287938177", "323"},
{"4289248665", "324"},
{"4289848742", "328"},
{"4291624908", "1025"},
{"4294498669", "334"},
{"4294830733", "226"},
{"4294043591", "340"},
{"4294898619", "341"},
{"4292066966", "346"},
{"4290550621", "351"},
{"4291275896", "352"},
{"4288709711", "356"},
{"4283843126", "361"},
{"4286474303", "362"},
{"4286356587", "317"},
{"4288791692", "29"},
{"4293040960", "105"},
{"4285290571", "355"},
{"4280254493", "313"},
{"4286091394", "151"},
{"4290887234", "1008"},
{"4288986439", "119"},
{"4294946816", "1005"},
{"4294967040", "1009"},
{"4293900344", "333"},
{"4294298928", "24"},
{"4292178749", "133"},
{"4292511041", "106"},
{"4289352960", "1014"},
{"4285087784", "192"}
};
public MainWindow()
{
InitializeComponent();
}

code
Code
download
content_copy
expand_less
private void GithubHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        // You can remove this if you want, this is just me self promoting lol
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        e.Handled = true;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (isConverting == true) // So you can't browse for a place while it's converting
        {
            return;
        }
        // Opens a file dialog asking you which file do you want to convert
        OpenFileDialog fileDialog = new OpenFileDialog();
        fileDialog.Filter = "Roblox XML Place Files (*.rbxlx)|*.rbxlx|Roblox XML Model Files (*.rbxmx)|*.rbxmx|Roblox XML Place Files (*.rbxl)|*.rbxl|Roblox XML Model Files (*.rbxm)|*.rbxm";
        if (fileDialog.ShowDialog() == true)
        {
            fileToConvertPath = fileDialog.FileName;
            PlaceSelectedLabel.Content = "File selected: " + fileToConvertPath;
        }
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        if (isConverting == true) // If you somehow managed to click the Convert button twice
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(fileToConvertPath)) // When you haven't selected a place file
        {
            MessageBox.Show("Please select a model or place you'd like to convert by clicking on 'Browse'", "Cannot convert place", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string[] file = File.ReadAllLines(fileToConvertPath);

        foreach (var line in file)
        {
            if (line.Contains("<roblox!"))
            {
                MessageBox.Show("Please select a model or place in Roblox XML format.", "Cannot convert place", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        isConverting = true;
        ConvertButton.IsEnabled = false;
        ProgressBar.Value = 0;
        ProgressLabel.Content = "";

        // Ask user where to save the copy of the file
        SaveFileDialog fileDialog = new SaveFileDialog();
        fileDialog.Filter = "Roblox XML Place Files (*.rbxl)|*.rbxl|Roblox XML Model Files (*.rbxm)|*.rbxm";
        if (fileDialog.ShowDialog() != true)
        {
            isConverting = false;
            ConvertButton.IsEnabled = true;
            return;
        }
        newFilePath = fileDialog.FileName;
        File.Copy(fileToConvertPath, newFilePath, true);
        // Read file contents
        string fileContents = File.ReadAllText(newFilePath);

        // Search for terrain in place file, necessary for place to even open on old Roblox versions. When it finds its start and end, it removes the terrain part completely.

        int terrainIndex = fileContents.IndexOf("<Item class=\"Terrain\"", StringComparison.Ordinal);
        if (terrainIndex != -1)
        {
            int terrainEndIndex = fileContents.IndexOf("</Item>", terrainIndex, StringComparison.Ordinal);
            if (terrainEndIndex != -1)
            {
                fileContents = fileContents.Remove(terrainIndex, terrainEndIndex - terrainIndex + 7);

            }
        }

        //Convert TextSize to Legacy FontSize
        if (ChangeTextSizeToFontSizeCheckbox.IsChecked == true)
        {
            int[] FontSizes = new int[10] { 8, 9, 10, 11, 12, 14, 18, 24, 36, 48 };
            Dictionary<int, int> converted = new Dictionary<int, int>();
            Regex rg = new Regex(@"<float name=""TextSize"">(\d{1,3})</float>");
            MatchCollection matchedfloats = rg.Matches(fileContents);

            foreach (Match ItemMatch in matchedfloats)
            {
                var nearest = FontSizes.OrderBy(x => Math.Abs((long)x - Int32.Parse(ItemMatch.Groups[1].Value))).First();
                converted[Int32.Parse(ItemMatch.Groups[1].Value)] = nearest;
            }

            foreach (KeyValuePair<int, int> entry in converted)
            {
                // do something with entry.Value or entry.Key
                fileContents = fileContents.Replace("<float name=\"TextSize\">" + entry.Key.ToString() + "</float>", "<token name=\"FontSize\">" + Array.IndexOf(FontSizes, entry.Value).ToString() + "</token>");
            }
        }

        // Convert colors from Color3uint8 to BrickColor
        if (ColorCheckbox.IsChecked == true)
        {
            foreach (KeyValuePair<string, string> entry in color3uint8ToBrickColor)
            {
                fileContents = fileContents.Replace("<Color3uint8 name=\"Color3uint8\">" + entry.Key + "</Color3uint8>", "<int name=\"BrickColor\">" + entry.Value + "</int>");
            }
        }

        //If Union data is turned off, removes union data
        if (UnionCheckbox.IsChecked == false)
        {
            int unionIndex = fileContents.IndexOf("<Item class=\"NonReplicatedCSGDictionaryService\"", StringComparison.Ordinal);
            if (unionIndex != -1)
            {
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

        //Script conversion, removes the weird CDATA stuff if your script is multiline.
        if (ScriptConvertCheckbox.IsChecked == true)
        {
            fileContents = fileContents.Replace("<ProtectedString name=\"Source\"><![CDATA[", "<ProtectedString name=\"Source\">");
            fileContents = fileContents.Replace("]]></ProtectedString>", "</ProtectedString>");
            //Change stuff like quotes to a format old roblox places support
            int scriptStartIndex = fileContents.IndexOf("<ProtectedString name=\"Source\">", StringComparison.Ordinal);
            while (scriptStartIndex != -1)
            {
                int scriptEndIndex = fileContents.IndexOf("</ProtectedString>", scriptStartIndex, StringComparison.Ordinal);
                if (scriptEndIndex != -1)
                {
                    string scriptBeforeContents = fileContents.Substring(scriptStartIndex + 31, scriptEndIndex - scriptStartIndex - 31);
                    if (scriptBeforeContents.Length > 0)
                    {
                        string scriptAfterContents = scriptBeforeContents;
                        scriptAfterContents = scriptAfterContents.Replace("\"", "&quot;");
                        scriptAfterContents = scriptAfterContents.Replace("\'", "&apos;");
                        scriptAfterContents = scriptAfterContents.Replace("<", "&lt;");
                        scriptAfterContents = scriptAfterContents.Replace(">", "&gt;");
                        fileContents = fileContents.Replace(scriptBeforeContents, scriptAfterContents);
                    }
                }
                scriptStartIndex = fileContents.IndexOf("<ProtectedString name=\"Source\">", scriptEndIndex, StringComparison.Ordinal);
            }
        }

        //Changes rbxassetid to longer link variant, fixes assets not loading in older clients
        if (ChangeRbxassetidCheckbox.IsChecked == true)
        {
            fileContents = fileContents.Replace("rbxassetid://", "http://www.roblox.com/asset/?id=");
        }

        //Convert folders to models, since old Roblox clients don't support folders and therefore everything inside of folders isn't shown
        if (ConvertFoldersCheckbox.IsChecked == true)
        {
            fileContents = fileContents.Replace("<Item class=\"Folder\"", "<Item class=\"Model\"");
        }

        

        // Write fileContents string to the copy file
        File.WriteAllText(newFilePath, fileContents);
        // Done :D
        ProgressBar.Value = 100;
        ProgressLabel.Content = "Done!";
        MessageBox.Show("Conversion done!", "Conversion status", MessageBoxButton.OK, MessageBoxImage.Information);
        isConverting = false;
        ConvertButton.IsEnabled = true;
    }
}

}
