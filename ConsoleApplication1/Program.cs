using Lucene.Net.Store;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ConsoleApplication1
{
    public class SearchEngine
    {
       private static Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_30;
        private static string _indexPath = AppDomain.CurrentDomain.BaseDirectory + "IndexVB";

        private static FSDirectory _directory= FSDirectory.Open(_indexPath);
        public static void CrateIndex(string title, string content)
        {
            var analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(_version);
            var directory = _directory;
            using (var iew = new Lucene.Net.Index.IndexWriter(directory, analyzer, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var document = new Lucene.Net.Documents.Document();
                document.Add(new Lucene.Net.Documents.Field("title", title, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                document.Add(new Lucene.Net.Documents.Field("content", content, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                iew.AddDocument(document);
                iew.Optimize();
            }
        }

       public static List<Item> search(string keyWord)
        {
            List<Item> results = new List<Item>();
            Lucene.Net.Analysis.Analyzer analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(_version);
            Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(_directory);
            Lucene.Net.QueryParsers.MultiFieldQueryParser parser = new Lucene.Net.QueryParsers.MultiFieldQueryParser(_version,new string[] { "title", "content" }, analyzer);
            Lucene.Net.Search.Query query = parser.Parse(keyWord);
            Lucene.Net.Search.TopScoreDocCollector collector = Lucene.Net.Search.TopScoreDocCollector.Create(1, true);;
            searcher.Search(query, collector);
            var totalHits = collector.TotalHits;
            Console.WriteLine("Total Find: " + totalHits);
            var hits = collector.TopDocs().ScoreDocs;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                Lucene.Net.Documents.Document doc =searcher.Doc(hit.Doc);
                results.Add(new Item() { Title = doc.Get("title"), Content = doc.Get("content") });
            }
            searcher.Dispose();
            return results;
        }

        public class Item{
            public string Title { get; set; }
            public string Content { get; set; }
            }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var path = ConfigurationManager.AppSettings["DIRPath"];
            var fileExtension = ConfigurationManager.AppSettings["fileExtension"];
            var arrExtension = fileExtension.Split('|');
            var fileList = new List<string>();
            foreach(var extension in arrExtension)
            {
                var files = System.IO.Directory.GetFiles(path, "*."+extension, SearchOption.AllDirectories);
                foreach (var file in files)
                    fileList.Add(file);
            }
           

            foreach(var file in fileList)
            {
                Console.WriteLine(file);
                using (var writer=new StreamReader(file))
                {
                    SearchEngine.CrateIndex(file, writer.ReadToEnd());
                }
                   
                //Thread.Sleep(200);
            }
            Console.WriteLine("==========");
            while (true)
            {
                var line = Console.ReadLine();
                var result = SearchEngine.search(line);
                foreach (var item in result)
                {
                    Console.WriteLine(item.Title);
                    //Console.WriteLine("             " + item.Content);
                    Console.ReadLine();
                }
            }
           
        }
    }
}
