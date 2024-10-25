using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

// Disable IDE warnings that showed up after going from C# 7 to C# 9
#pragma warning disable IDE0079 // Disable message about unnecessary suppression
#pragma warning disable IDE1006 // Disable messages about capitalization of control names
#pragma warning disable IDE0063 // Disable messages about Using expression simplification
#pragma warning disable IDE0090 // Disable messages about New expression simplification
#pragma warning disable IDE0028,IDE0300,IDE0305 // Disable message about inputArray initialization
#pragma warning disable IDE0074 // Disable message about compound assignment for checking if null
#pragma warning disable IDE0066 // Disable message about switch case expression
// Nullable reference types
#nullable enable


namespace EditClipboardContents
{
    public class FileSignature
    {
        public List<string> Extensions { get; set; }
        public string Description { get; set; }
        public List<string> _Offsets { get; set; }
        public List<Signature> Signatures { get; set; }

        public FileSignature()
        {
            Extensions = new List<string>();
            _Offsets = new List<string>();
            Signatures = new List<Signature>();
        }
    }

    public class Signature
    {
        public SignatureType SignatureType { get; set; }
        public string SignatureValue { get; set; }
    }

    public enum SignatureType
    {
        Generic,
        GenericWildCard,
        BigEndian,
        BigEndianWildcard,
        LittleEndian,
        LittleEndianWildcard,
        zipEmpty,
        zipSpanned,

    }

    public class Cell
    {
        public string Content { get; set; }
        public int Rowspan { get; set; } = 1; // default is 1
    }

    public class RowspanCell
    {
        public string Content { get; set; }
        public int RemainingRowspan { get; set; }
    }

    public class FileSignatureParser
    {
        private const int COLUMN_COUNT = 5; // Assuming the table has 5 columns

        public List<FileSignature> ParseFileSignatures(string tableData)
        {
            List<FileSignature> fileSignatures = new List<FileSignature>();
            string[] lines = tableData.Split('\n');

            // First clean up lines by removing consecutive lines with "|-" and replace with one "|-"
            List<string> cleanedLines = new List<string>();
            for (int j = 0; j < lines.Length; j++)
            {
                string line = lines[j].TrimEnd();
                if (line.StartsWith("|-") && cleanedLines.Count > 0 && cleanedLines[cleanedLines.Count - 1].TrimStart().StartsWith("|-"))
                {
                    // Skip this line
                }
                else
                {
                    cleanedLines.Add(line);
                }
            }
            lines = cleanedLines.ToArray();

            int i = 0;

            // Initialize currentRowspanCells for each column
            List<RowspanCell> currentRowspanCells = new List<RowspanCell>();
            for (int col = 0; col < COLUMN_COUNT; col++)
            {
                currentRowspanCells.Add(null);
            }

            while (i < lines.Length)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("|-"))
                {
                    // Start of a new row
                    List<string> rowLines = new List<string>();
                    i++;
                    while (i < lines.Length && !lines[i].TrimStart().StartsWith("|-"))
                    {
                        rowLines.Add(lines[i]);
                        i++;
                    }
                    // Process the row
                    ProcessRow(rowLines, fileSignatures, currentRowspanCells);
                }
                else
                {
                    i++;
                }
            }

            // Remove any fileSignatures where everything is empty
            fileSignatures.RemoveAll(fs => string.IsNullOrEmpty(fs.Description) && fs.Extensions.Count == 0 && fs._Offsets.Count == 0 && fs.Signatures.Count == 0);
            // Remove any where the signatures consist only of zeroes and spaces
            fileSignatures.RemoveAll(fs => fs.Signatures.TrueForAll(s => string.IsNullOrEmpty(s.SignatureValue) || s.SignatureValue.Replace("0", "").Replace(" ", "" ) == ""));

            return fileSignatures;
        }

        private void ProcessRow(List<string> rowLines, List<FileSignature> fileSignatures, List<RowspanCell> currentRowspanCells)
        {
            List<Cell> parsedCells = new List<Cell>();

            int lineIndex = 0;
            while (lineIndex < rowLines.Count)
            {
                string line = rowLines[lineIndex];

                if (line.TrimStart().StartsWith("|"))
                {
                    // Start of a new cell
                    Cell cell = new Cell();
                    // Remove the leading "|"
                    string cellLine = line.TrimStart('|').TrimEnd();
                    // Initialize cell content
                    StringBuilder cellContent = new StringBuilder();
                    cellContent.AppendLine(cellLine);

                    // Check for attributes like rowspan
                    string remainingLine = cellLine;

                    // Check for attributes
                    if (Regex.IsMatch(remainingLine, @"\w+\s*=\s*""[^""]*""\s*\|"))
                    {
                        int pipeIndex = remainingLine.IndexOf("|");
                        string attrText = remainingLine.Substring(0, pipeIndex).Trim();
                        cell = ParseCell("|" + attrText + "|");
                        cellLine = remainingLine.Substring(pipeIndex + 1).Trim();
                        cellContent.Clear();
                        cellContent.AppendLine(cellLine);
                    }

                    lineIndex++;

                    // Read the content lines
                    while (lineIndex < rowLines.Count)
                    {
                        string nextLine = rowLines[lineIndex];
                        if (nextLine.TrimStart().StartsWith("|"))
                        {
                            // Start of a new cell
                            break;
                        }
                        else
                        {
                            cellContent.AppendLine(nextLine);
                            lineIndex++;
                        }
                    }

                    cell.Content = cellContent.ToString().Trim();

                    // Add cell to parsedCells
                    parsedCells.Add(cell);
                }
                else
                {
                    // Continuation of current cell content
                    if (parsedCells.Count > 0)
                    {
                        Cell currentCell = parsedCells[parsedCells.Count - 1];
                        currentCell.Content += "\n" + line;
                    }
                    lineIndex++;
                }
            }

            // Now map parsedCells to columns, taking into account rowspans
            List<Cell> rowCells = new List<Cell>();
            int colIndex = 0;
            int parsedCellIndex = 0;

            while (colIndex < COLUMN_COUNT)
            {
                if (currentRowspanCells[colIndex] != null && currentRowspanCells[colIndex].RemainingRowspan > 0)
                {
                    // Use the cell from currentRowspanCells
                    rowCells.Add(new Cell
                    {
                        Content = currentRowspanCells[colIndex].Content,
                        Rowspan = currentRowspanCells[colIndex].RemainingRowspan + 1 // Original rowspan
                    });
                    // Decrement the remaining rowspan
                    currentRowspanCells[colIndex].RemainingRowspan--;
                    if (currentRowspanCells[colIndex].RemainingRowspan == 0)
                    {
                        currentRowspanCells[colIndex] = null;
                    }
                }
                else
                {
                    if (parsedCellIndex < parsedCells.Count)
                    {
                        Cell cell = parsedCells[parsedCellIndex];

                        // Handle rowspan
                        if (cell.Rowspan > 1)
                        {
                            currentRowspanCells[colIndex] = new RowspanCell
                            {
                                Content = cell.Content,
                                RemainingRowspan = cell.Rowspan - 1
                            };
                        }

                        rowCells.Add(cell);
                        parsedCellIndex++;
                    }
                    else
                    {
                        // No cell for this column
                        rowCells.Add(new Cell { Content = "", Rowspan = 1 });
                    }
                }
                colIndex++;
            }

            // Now, we should have COLUMN_COUNT cells in rowCells
            // Process the cells
            if (rowCells.Count >= COLUMN_COUNT)
            {
                string hexSignatureCell = rowCells[0].Content;
                // string isoCell = rowCells[1].Content; // Ignored
                string offsetCell = rowCells[2].Content;
                string extensionCell = rowCells[3].Content;
                string descriptionCell = rowCells[4].Content;

                FileSignature fileSignature = new FileSignature();
                fileSignature.Description = CleanUpText(descriptionCell);
                fileSignature.Extensions = ParseExtensions(extensionCell);
                fileSignature._Offsets = ParseOffsets(offsetCell);
                fileSignature.Signatures = ParseSignatures(hexSignatureCell);

                fileSignatures.Add(fileSignature);
            }
        }

        private Cell ParseCell(string cellText)
        {
            Cell cell = new Cell();

            // Remove the leading "|"
            cellText = cellText.TrimStart('|').Trim();

            // Use regex to extract attributes and content
            // The pattern is: (optional attributes) | content
            Regex cellRegex = new Regex(@"^(?<attrs>(?:\w+\s*=\s*""[^""]*""\s*)+)?\s*(?<content>.*)", RegexOptions.Singleline);
            Match match = cellRegex.Match(cellText);

            if (match.Success)
            {
                string attrsText = match.Groups["attrs"].Value;
                string contentText = match.Groups["content"].Value.Trim();

                // Parse attributes
                if (!string.IsNullOrEmpty(attrsText))
                {
                    Regex attrRegex = new Regex(@"(\w+)\s*=\s*""([^""]*)""");
                    MatchCollection attrMatches = attrRegex.Matches(attrsText);
                    foreach (Match attrMatch in attrMatches)
                    {
                        string attrName = attrMatch.Groups[1].Value.ToLower();
                        string attrValue = attrMatch.Groups[2].Value;
                        if (attrName == "rowspan")
                        {
                            cell.Rowspan = int.Parse(attrValue);
                        }
                        // Handle other attributes if needed
                    }
                }

                cell.Content = contentText;
            }
            else
            {
                // No match, use the whole text as content
                cell.Content = cellText;
            }

            return cell;
        }

        private readonly string wikiPattern = @"{{[^}]+}}|\[\[(?:[^|\]]*\|)?([^\]]+)\]\]|={2,}.*?={2,}|Category:.*?(?=\||}}|\n|$)";

        private string CleanUpText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";


            string nobreak = input.Replace("<br />", "; ").Replace("<br/>", "; "); // Replace <br /> tags with semicolons
            string noref = Regex.Replace(nobreak, "<ref[^>]*?>.*?</ref>", ""); //Remove anything between <ref> tags
            string noHtml = Regex.Replace(noref, "<.*?>", ""); // Remove HTML tags
            string noWiki = Regex.Replace(noHtml, wikiPattern, "$1"); // Remove wiki formatting
            string cleaned = Regex.Replace(noWiki, "''[^']*''", ""); // Remove text that is surrounded by two single quotes
            // Trim whitespace
            cleaned = cleaned.Trim();
            return cleaned;
        }

        private List<string> ParseExtensions(string extensionCell)
        {
            List<string> extensions = new List<string>();

            if (string.IsNullOrEmpty(extensionCell))
                return extensions;

            // Replace <br /> tags with line breaks
            string noBreaks = extensionCell.Replace("<br />", "\n").Replace("<br/>", "\n");
            string noWiki = Regex.Replace(noBreaks, wikiPattern, "$1");
            string content = Regex.Replace(noWiki, "''[^']*''", "");
            // Remove HTML tags
            content = Regex.Replace(content, "<.*?>", "");
            // Split by line breaks
            string[] extArray = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string ext in extArray)
            {
                extensions.Add(ext.Trim());
            }
            return extensions;
        }

        private List<string> ParseOffsets(string offsetCell)
        {
            List<string> offsets = new List<string>();

            if (string.IsNullOrEmpty(offsetCell))
                return offsets;

            string nobreak = offsetCell.Replace("<br />", "\n").Replace("<br/>", "\n"); // Replace <br /> tags with line breaks
            string noHtml = Regex.Replace(nobreak, "<.*?>", ""); // Remove HTML tags
            string content = Regex.Replace(noHtml, wikiPattern, "$1");
            // Split by line breaks
            string[] offsetArray = content.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string offset in offsetArray)
            {
                offsets.Add(offset.Trim());
            }
            return offsets;
        }

        private List<Signature> ParseSignatures(string hexSignatureCell)
        {
            List<Signature> signatures = new List<Signature>();

            if (string.IsNullOrEmpty(hexSignatureCell))
                return signatures;

            // Use Regex to find all code blocks with possible labels before or after
            // Now handling both <code>...</code> and {{code|...}} formats
            Regex codeBlockRegex = new Regex(
                @"(?<labelBefore>\([^\)]*\)\s*)?" +
                @"(?<codeBlock>(<code>(?<code1>.*?)</code>)|(\{\{code\|(?<code2>.*?)\}\}))" +
                @"\s*(?<labelAfter>\([^\)]*\))?",
                RegexOptions.Singleline);

            MatchCollection matches = codeBlockRegex.Matches(hexSignatureCell);

            foreach (Match match in matches)
            {
                string labelBefore = match.Groups["labelBefore"].Value.Trim();
                string labelAfter = match.Groups["labelAfter"].Value.Trim();

                string codeContent = match.Groups["code1"].Success
                    ? match.Groups["code1"].Value.Trim()
                    : match.Groups["code2"].Value.Trim();
                string cleanedCodeContent = codeContent.Replace("<br />", "").Replace("<br/>", "");

                string label = "";
                if (!string.IsNullOrEmpty(labelBefore))
                {
                    label = labelBefore;
                }
                else if (!string.IsNullOrEmpty(labelAfter))
                {
                    label = labelAfter;
                }

                label = label.Trim('(', ')').Trim().ToLower();
                SignatureType sigType = DetermineSignatureType(label, cleanedCodeContent);

                

                signatures.Add(new Signature
                {
                    SignatureType = sigType,
                    SignatureValue = cleanedCodeContent
                });
            }

            return signatures;
        }


        private SignatureType DetermineSignatureType(string label, string signature)
        {
            switch (label)
            {
                case "big-endian":
                    if (signature.Contains("??"))
                        return SignatureType.BigEndianWildcard;
                    else
                        return SignatureType.BigEndian;

                case "little-endian":
                    if (signature.Contains("??"))
                        return SignatureType.LittleEndianWildcard;
                    else
                        return SignatureType.LittleEndian;

                case "empty archive":
                    return SignatureType.zipEmpty;

                case "spanned archive":
                    return SignatureType.zipSpanned;

                default:
                    if (signature.Contains("??"))
                        return SignatureType.GenericWildCard;
                    else
                        return SignatureType.Generic;
            }
        }

        private static List<FileSignature>? _cachedSignatures = null;

        public List<FileSignature> LoadFileSignatures()
        {
            if (_cachedSignatures != null)
            {
                return _cachedSignatures;
            }
            else
            {
                _cachedSignatures = new List<FileSignature>();
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "EditClipboardContents.FileSignatures.json";
                StreamReader streamReader = new StreamReader(assembly.GetManifestResourceStream(resourceName));

                string json = streamReader.ReadToEnd();
                List<FileSignature>? fileSignatures = JsonSerializer.Deserialize<List<FileSignature>>(json);

                if (fileSignatures == null)
                {
                    fileSignatures = new List<FileSignature>();
                }

                _cachedSignatures = fileSignatures;
                return fileSignatures;
            }
        }


    } // -------------------------------------------------------- End of FileSignatureParser class --------------------------------------------------------

    public partial class MainForm : Form
    {
        private void buttonMakeSignatureJson_Click(object sender, EventArgs e)
        {
            Console.WriteLine(e.ToString());

            FileSignatureParser parser = new FileSignatureParser();
            //string tableData = File.ReadAllText("Signatures.txt");
            string tableData = File.ReadAllText("Signatures.txt");

            var fileSignatures = parser.ParseFileSignatures(tableData);

            StringBuilder PrintSignatures(List<FileSignature> fileSignatures)
            {
                StringBuilder output = new StringBuilder();
                int index = 0;
                foreach (var fs in fileSignatures)
                {
                    output.AppendLine($"{new string('-', 20)} {index} {new string('-', 20)}");
                    output.AppendLine("Description: " + fs.Description);
                    output.AppendLine("Extensions: " + string.Join(", ", fs.Extensions));
                    output.AppendLine("Offsets: " + string.Join(", ", fs._Offsets));
                    output.AppendLine("Signatures:");
                    foreach (var sig in fs.Signatures)
                    {
                        output.AppendLine($"  Type: {sig.SignatureType}, Value: {sig.SignatureValue}");
                    }
                    //output.AppendLine(new string('-', 40));
                    index++;
                }
                return output;
            }

            StringBuilder output1 = PrintSignatures(fileSignatures);
            File.WriteAllText("ParsedSignatures.txt", output1.ToString());

            // Serialize the file signatures
            string json = JsonSerializer.Serialize(fileSignatures, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("ParsedSignatures.json", json);

            //// Test loading and deserializing the file signatures
            string jsonFromFile = File.ReadAllText("ParsedSignatures.json");
            List<FileSignature>? fileSignaturesFromJson = JsonSerializer.Deserialize<List<FileSignature>>(jsonFromFile);

            //if (fileSignaturesFromJson == null)
            //{
            //    throw new InvalidOperationException("Deserialization returned null.");
            //}

            //string json2 = JsonSerializer.Serialize(fileSignaturesFromJson, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText("Re-Serialized-ParsedSignatures.json", json2);

            //StringBuilder output2 = PrintSignatures(fileSignaturesFromJson);
            //File.WriteAllText("Re-Serialized-ParsedSignatures.txt", output2.ToString());

            Console.WriteLine("");
        }
    }
}
