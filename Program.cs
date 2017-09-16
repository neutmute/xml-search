using Kraken.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XmlSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var stopwatch = Stopwatch.StartNew();

            var files = GetFiles(@"D:\Downloads\powershell-scripts\aboutus");
            var searchText = "parsys";
            var attributeNameFilter = "resourceType";
            var parsers = files
                            .Select(f => new FileParser(f, searchText, attributeNameFilter))
                            .ToList();

            var parserTasks = parsers.ConvertAll(p => p.GetMatchTasks());

            Task.WaitAll(parserTasks.ToArray());

            var matchCount = parsers.Sum(p => p.Matches.Count);
            var timeTaken = stopwatch.Elapsed.ToHumanReadable();
            Console.WriteLine($"Found {matchCount} matches");
            Console.WriteLine($"Finished in {timeTaken}");
        }
               
        private static List<string> GetFiles(string rootPath)
        {
            var files = Directory.GetFiles(rootPath, "*.content.xml", SearchOption.AllDirectories);
            return files.ToList();
        }
    }

    class FileParser
    {
        public string Filename { get; set; }
        public List<XAttribute> Matches {get;private set;}
        string _attributeNameFilter;
        string _searchText;

        public FileParser(string filename, string searchText, string attributeNameFilter = null)
        {
            Filename = filename;
            Matches = new List<XAttribute>();
            _searchText = searchText;
            _attributeNameFilter = attributeNameFilter;
        }

        public Task GetMatchTasks()
        {
            return Task.Factory.StartNew(BuildMatches);
        }

        public void BuildMatches()
        {
            //Console.WriteLine($"Scanning {Filename}");
            var content = File.ReadAllText(Filename);
            XmlReader reader = XmlReader.Create(new StringReader(content));
            XElement root = XElement.Load(reader);
            XmlNameTable nameTable = reader.NameTable;
            var namespaceManager = new XmlNamespaceManager(nameTable);
            //namespaceManager.AddNamespace("aw", "http://www.adventure-works.com");
            namespaceManager.AddNamespace("cq", "http://www.day.com/jcr/cq/1.0");
            var elements = root.XPathSelectElements("//*[name()='jcr:content']", namespaceManager);
            foreach (XElement el in elements)
            {
                RecurseNodes(el, string.Empty);
            }
        }

        private void RecurseNodes(XElement element, string name)
        {
            name += element.Name + "\\";
            BuildMatches(element, name);
            foreach ( var child in element.Descendants())
            {
                RecurseNodes(child, name);
            }
        }

        private void BuildMatches(XElement element, string name)
        {
            var attributesToScan = string.IsNullOrEmpty(_attributeNameFilter) ? element.Attributes() : element.Attributes().Where(a => a.Name.LocalName == _attributeNameFilter);
            var attributeMatches = attributesToScan.Where(a => a.Value.Contains(_searchText)).ToList();
            Matches.AddRange(attributeMatches);
        }
    }

    //public class AttributeMatchCollection:List<AttributeMatch>
    //{

    //}

    //public class AttributeMatch
    //{
    //    public string Name { get; set; }

    //    public string Value { get; set; }
    //}
}
