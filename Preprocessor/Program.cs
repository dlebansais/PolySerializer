using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Preprocessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string Folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (Folder.EndsWith("Debug") || Folder.EndsWith("Release"))
                Folder = Path.GetDirectoryName(Folder);
            if (Folder.EndsWith("x64"))
                Folder = Path.GetDirectoryName(Folder);
            if (Folder.EndsWith("bin"))
                Folder = Path.GetDirectoryName(Folder);

            string FileName = "Serializer.cs";
            string SourceFileName = Path.Combine(Folder, FileName);

            string PolySerializerFolder = Path.Combine(Path.GetDirectoryName(Folder), "PolySerializer");
            string DestinationFileName = Path.Combine(PolySerializerFolder, "x" + FileName);

            Preprocess(SourceFileName, DestinationFileName);
        }

        private static void Preprocess(string sourceFileName, string destinationFileName)
        {
            using (FileStream SourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader Reader = new StreamReader(SourceStream, Encoding.UTF8))
                {
                    using (FileStream DestinationStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write))
                    {
                        using (StreamWriter Writer = new StreamWriter(DestinationStream, Encoding.UTF8))
                        {
                            Preprocess(Reader, Writer);
                        }
                    }
                }
            }
        }

        private static void Preprocess(StreamReader reader, StreamWriter writer)
        {
            string Line;
            List<string> ConfigurationNames = new List<string>();

            for (;;)
            {
                Line = reader.ReadLine();
                if (Line == null)
                    break;

                if (!PreprocessConfiguration(Line, ConfigurationNames))
                    break;
            }

            Dictionary<string, bool?> CurrentConfiguration = null;
            List<string[]> Configurations = new List<string[]>();
            List<string> ConditionalChain = new List<string>();

            while (Line != null)
            {
                PreprocessCode(Line, ConfigurationNames, Configurations, ConditionalChain, ref CurrentConfiguration, writer);
                Line = reader.ReadLine();
            }
        }

        private static bool GetTag(string pattern, string line, out string tag)
        {
            if (!line.StartsWith(pattern))
            {
                tag = null;
                return false;
            }

            int CommentIndex = line.IndexOf("//", pattern.Length);
            if (CommentIndex < 0)
                CommentIndex = line.Length;

            tag = line.Substring(pattern.Length, CommentIndex - pattern.Length).Trim();
            return true;
        }

        private static bool GetTags(string pattern, string line, out List<string> tags)
        {
            if (!line.StartsWith(pattern))
            {
                tags = null;
                return false;
            }

            int CommentIndex = line.IndexOf("//", pattern.Length);
            if (CommentIndex < 0)
                CommentIndex = line.Length;

            string TagList = line.Substring(pattern.Length, CommentIndex - pattern.Length).Trim();
            string[] Splitted = TagList.Split(',');

            tags = new List<string>();
            foreach (string Item in Splitted)
            {
                string Tag = Item.Trim();
                if (Tag.Length > 0 && !tags.Contains(Tag))
                    tags.Add(Tag);
            }

            return true;
        }

        private static bool PreprocessConfiguration(string line, List<string> configurationNames)
        {
            if (!GetTag("// Configuration:", line, out string ConfigurationName))
                return false;

            if (!IsIdentifier(ConfigurationName))
                return false;

            if (!configurationNames.Contains(ConfigurationName))
                configurationNames.Add(ConfigurationName);

            return true;
        }

        private static bool IsIdentifier(string s)
        {
            if (s.Length == 0)
                return false;

            char c = s[0];
            if (!(/*(c >= 'a' && c <= 'z') ||*/ (c >= 'A' && c <= 'Z')))
                return false;

            for (int i = 1; i < s.Length; i++)
                if (!(/*(c >= 'a' && c <= 'z') ||*/ (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_'))
                    return false;

            return true;
        }

        private static void PreprocessCode(string line, List<string> configurationNames, List<string[]> configurations, List<string> conditionalChain, ref Dictionary<string, bool?> currentConfiguration, StreamWriter writer)
        {
            if (line.StartsWith("//#replicate"))
            {
                currentConfiguration = new Dictionary<string, bool?>();
                foreach (string ConfigurationName in configurationNames)
                    currentConfiguration.Add(ConfigurationName, null);

                configurations.Clear();
            }

            else if (line.StartsWith("//#endreplicate"))
            {
                currentConfiguration = null;
            }

            else if (GetTag("//#if ", line, out string ConfigurationName))
            {
                if (conditionalChain.Contains(ConfigurationName))
                    Debug.WriteLine($"Warning: nested #if {ConfigurationName}");
                else
                {
                    conditionalChain.Add(ConfigurationName);
                    ChangeConfiguration(conditionalChain, currentConfiguration, true);
                }
            }

            else if (line.StartsWith("//#else"))
            {
                if (conditionalChain.Count == 0)
                    Debug.WriteLine("Warning: #else with no #if");
                else
                    ChangeConfiguration(conditionalChain, currentConfiguration, false);
            }

            else if (line.StartsWith("//#endif"))
            {
                if (conditionalChain.Count == 0)
                    Debug.WriteLine("Warning: #endif with no #if");
                else
                {
                    ChangeConfiguration(conditionalChain, currentConfiguration, null);
                    conditionalChain.RemoveAt(conditionalChain.Count - 1);
                }
            }

            else if (GetTags("//#issue ", line, out List<string> IssueNames))
            {
                Dictionary<string, bool?> SpecificConfiguration = new Dictionary<string, bool?>();
                foreach (string Item in configurationNames)
                    SpecificConfiguration.Add(Item, null);

                foreach (string Item in IssueNames)
                {
                    bool? Value;
                    string IssueName;

                    if (Item[0] == '!')
                    {
                        Value = false;
                        IssueName = Item.Substring(1);
                    }
                    else
                    {
                        Value = false;
                        IssueName = Item;
                    }

                    if (SpecificConfiguration.ContainsKey(IssueName))
                        SpecificConfiguration[IssueName] = Value;
                    else
                        Debug.WriteLine($"Warning: unknown configuration {Item} issued");
                }

                bool AllSet = true;
                int ConfigurationIndex = 0;
                foreach (KeyValuePair<string, bool?> Entry in currentConfiguration)
                {
                    bool? Configuration = Entry.Value;

                    if (!Configuration.HasValue)
                        AllSet = false;
                    else
                    {
                        ConfigurationIndex <<= 1;
                        ConfigurationIndex |= (Configuration.Value ? 1 : 0);
                    }
                }

                if (!AllSet)
                    Debug.WriteLine($"Warning: attempt to issue with not all configurations specified");
                else
                {
                    foreach (string[] Lines in configurations)
                    {
                        string IssuedLine = Lines[ConfigurationIndex];
                        if (IssuedLine != null)
                            writer.WriteLine(IssuedLine);
                    }
                }
            }

            else if (currentConfiguration == null)
                writer.WriteLine(line);

            else
            {
                int ConfigurationCombinationCount = 1 << currentConfiguration.Count;
                string[] Lines = new string[ConfigurationCombinationCount];

                for (int i = 0; i < ConfigurationCombinationCount; i++)
                {
                    bool IsIssued = true;

                    int j = 0;
                    foreach (KeyValuePair<string, bool?> Entry in currentConfiguration)
                    {
                        bool? Configuration = Entry.Value;
                        bool ConfigurationSelected = (((i >> j) & 1) != 0);

                        if (Configuration.HasValue && Configuration.Value != ConfigurationSelected)
                        {
                            IsIssued = false;
                            break;
                        }

                        j++;
                    }

                    Lines[i] = IsIssued ? line : null;
                }

                configurations.Add(Lines);
            }
        }

        private static void ChangeConfiguration(List<string> conditionalChain, Dictionary<string, bool?> currentConfiguration, bool? condition)
        {
            if (currentConfiguration == null)
            {
                Debug.WriteLine("Warning: #if, #else or #endif found outside a #replicate block");
                return;
            }

            string ConfigurationName = conditionalChain[conditionalChain.Count - 1];
            if (!currentConfiguration.ContainsKey(ConfigurationName))
            {
                Debug.WriteLine($"Warning: unknown configuration {ConfigurationName }");
                return;
            }

            currentConfiguration[ConfigurationName] = condition;
        }
    }
}
