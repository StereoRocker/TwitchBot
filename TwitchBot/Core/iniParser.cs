using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace TwitchBot
{
    public class iniParser
    {
        private string currentNode;
        private Dictionary<string, Dictionary<string, string>> iniStructure;

        /* Reset the contents of this iniParser instance
         * Does not throw errors
         */
        public void Reset()
        {
            currentNode = "";
            iniStructure = new Dictionary<string, Dictionary<string, string>>();
        }

        /* Add the contents of <filename> to this iniParser instance
         * Does not throw errors
         */
        public void Parse(string filename)
        {
            StreamReader reader = File.OpenText(filename);

            string line;
            while (!reader.EndOfStream)
            {
                // Read the line & remove leading/trailing whitespace
                line = reader.ReadLine();
                line = line.Trim();

                // Ensure we're not dealing with a blank line
                if (String.IsNullOrEmpty(line))
                    continue;

                // Ensure we're not dealing with a comment line - begins with '#' or ';'
                if (line.StartsWith("#"))
                    continue;
                if (line.StartsWith(";"))
                    continue;

                // Check if we're dealing with a section header
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // Get the name of the section header
                    line = line.Substring(1, line.Length - 2);
                    currentNode = line;

                    // Ensure the strucure has the required elements
                    if (!iniStructure.ContainsKey(currentNode))
                        iniStructure.Add(currentNode, new Dictionary<string, string>());

                    // Parse the next line
                    continue;
                }

                // Ensure we're dealing with a valid line
                if (!line.Contains('='))
                    continue;

                // Ensure we have a valid section header
                //if (currentNode == "")
                //continue;

                // Now all of our sanity checks are in place, parse the line!
                string[] elements;
                elements = line.Split(new char[] { '=' }, 2);
                if (elements.Length < 2)
                    continue;   // We should never really reach this line, it's here on the off-chance

                // Trim the leading/trailing whitespace of the key & value
                elements[0] = elements[0].Trim();
                elements[1] = elements[1].Trim();

                // Add the key & value to the structure
                iniStructure[currentNode].Add(elements[0], elements[1]);
            }

            reader.Close();
        }

        /* Checks if the specified key in a specified section
         * exists.
         * Returns true if the key is present, false otherwise
         */
        public bool hasValue(string section, string key)
        {
            if (!iniStructure.ContainsKey(section))
                return false;
            return iniStructure[section].ContainsKey(key);
        }

        /* Get the value of a specified key in a specified section
         * Returns null if the value has not been specified
         */
        public string getValue(string section, string key)
        {
            // Ensure the specified section exists
            if (!iniStructure.ContainsKey(section))
                return null;

            // Ensure the specified key exists within the section
            if (!iniStructure[section].ContainsKey(key))
                return null;

            // Return the requested value
            return iniStructure[section][key];
        }

        public Dictionary<string, string> getSection(string section)
        {
            if (!iniStructure.ContainsKey(section))
                return null;

            return iniStructure[section];
        }

        /* Adds a section to the structure
         * Will never throw an exception - even if the section already exists.
         */
        public void addSection(string section)
        {
            if (iniStructure.ContainsKey(section))
                return;
            iniStructure.Add(section, new Dictionary<string, string>());
        }

        /* Adds or sets the value of a specified key in a specified
         * section. Will automatically add the section if it does not
         * exist. However, this should be avoided to keep your code
         * clean and to ensure it makes sense.
         */
        public void setValue(string section, string key, string value)
        {
            addSection(section);
            if (!iniStructure[section].ContainsKey(key))
                iniStructure[section].Add(key, value);
            else
                iniStructure[section][key] = value;
        }

        public void Save(string path)
        {
            // Save the contents of this iniParse to a file
            StreamWriter writer = File.CreateText(path);
            foreach (KeyValuePair<string, Dictionary<string, string>> sectionPair in iniStructure)
            {
                writer.WriteLine("[" + sectionPair.Key + "]");
                foreach (KeyValuePair<string, string> kvp in iniStructure[sectionPair.Key])
                    writer.WriteLine(kvp.Key + "=" + kvp.Value);
            }
            writer.Close();
        }

        public List<string> getSectionList()
        {
            List<string> sections = new List<string>();

            foreach (String s in iniStructure.Keys)
            {
                sections.Add(s);
            }

            return sections;
        }

        public iniParser()
        {
            Reset();
        }

        public iniParser(string filename)
        {
            Reset();
            Parse(filename);
        }
    }
}
