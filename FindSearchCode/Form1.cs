using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FindSearchCode
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        int firstFindIndex = 0;
        Stack<int> indexTrack = new Stack<int>();
        public void SetFindTextColor(Color color)
        {
            var text = this.textBox1.Text.Trim().ToUpper();
            
            var length = richTextBox1.Text.Length;
            var content = richTextBox1.Text;

            var loopLine = new List<string>();
            while (firstFindIndex <= length)
            {
                var index = content.IndexOf(text, firstFindIndex, StringComparison.OrdinalIgnoreCase);
                var currFindIndex = index >= 0 ? index : -1;
                if (currFindIndex == -1) return;
                richTextBox1.Select(currFindIndex, text.Length);
                richTextBox1.SelectionBackColor = Color.Yellow;
                firstFindIndex = currFindIndex + text.Length;
                indexTrack.Push(firstFindIndex);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (firstFindIndex >= richTextBox1.Text.Length)
            {
                return;
            }
            this.richTextBox1.Focus();
            this.richTextBox1.SelectionStart = firstFindIndex;
            SetFindTextColor(Color.Yellow);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var keyWord = this.textBox1.Text.Trim();
            var fileList = GetFileList.GetFiles();
            this.button2.Enabled = false;
            
            listView1.Items.Clear();
            foreach (var item in fileList)
            {
                using (var read=new StreamReader(item))
                {
                    var content = read.ReadToEnd();
                    if (content.IndexOf(keyWord,StringComparison.OrdinalIgnoreCase)>0)
                    {
                        var itemShow = new ListViewItem(item);
                        var listviewSubItem = new ListViewItem.ListViewSubItem(itemShow, content);
                        itemShow.SubItems.Add(listviewSubItem);
                        listView1.Items.Add(itemShow);
                    }
                }
                   
            }
            this.button2.Enabled = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            firstFindIndex = 0;
            indexTrack = new Stack<int>();
            if (this.listView1.SelectedItems.Count>0)
            {
                var path = this.listView1.SelectedItems[0].Text;
                var text = this.textBox1.Text.Trim().ToUpper();
                using (var reader=new StreamReader(path))
                {
                    this.richTextBox1.Text= reader.ReadToEnd();
                }
                this.listView1.SelectedItems[0].BackColor = Color.Blue;
                button1_Click(null, null);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button2_Click(null, null);
            }
            switch (e.KeyCode)
            {
                case Keys.F3:
                    if (indexTrack.Count > 0)
                    {
                        var start = indexTrack.Pop();
                        this.richTextBox1.Focus();
                        this.richTextBox1.SelectionStart = start;
                    }else
                    {
                        MessageBox.Show("arrive the begin");
                    }
                   
                    break;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
           
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
          
        }
    }

    public class GetFileList
    {
        public static List<string> GetFiles()
        {
            var path = ConfigurationManager.AppSettings["DIRPath"];
            var fileExtension = ConfigurationManager.AppSettings["fileExtension"];
            var arrExtension = fileExtension.Split('|');
            var fileList = new List<string>();
            foreach (var extension in arrExtension)
            {
                var files = System.IO.Directory.GetFiles(path, "*." + extension, SearchOption.AllDirectories);
                foreach (var file in files)
                    fileList.Add(file);
            }
            return fileList;
        }
    }

    public class SearchEngine
    {
        private static Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_30;
        private static string _indexPath = AppDomain.CurrentDomain.BaseDirectory + "IndexVB";

        public static void CrateIndex(string title, string content)
        {
            var analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(_version);
            using (var iew = new Lucene.Net.Index.IndexWriter(FSDirectory.Open(new DirectoryInfo(_indexPath)), analyzer,false, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var document = new Lucene.Net.Documents.Document();
                document.Add(new Lucene.Net.Documents.Field("title", title, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.NO));
                document.Add(new Lucene.Net.Documents.Field("content", content, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED));
                iew.AddDocument(document);
                iew.Optimize();
            }
        }

        public static List<Item> search(string keyWord)
        {
            List<Item> results = new List<Item>();
            Lucene.Net.Analysis.Analyzer analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(_version);
            Lucene.Net.Search.IndexSearcher searcher = new Lucene.Net.Search.IndexSearcher(FSDirectory.Open(new DirectoryInfo(_indexPath)),true);
            Lucene.Net.QueryParsers.MultiFieldQueryParser parser = new Lucene.Net.QueryParsers.MultiFieldQueryParser(_version, new string[] { "content" }, analyzer);
            Lucene.Net.Search.Query query = parser.Parse(keyWord);
            Lucene.Net.Search.TopScoreDocCollector collector = Lucene.Net.Search.TopScoreDocCollector.Create(1000, true);
            var topDocs=searcher.Search(query,(Lucene.Net.Search.Filter)null,1000);
            var totalHits = topDocs.TotalHits;
            //Console.WriteLine("Total Find: " + totalHits);
            var hits = topDocs.ScoreDocs;// collector.TopDocs().ScoreDocs;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                Lucene.Net.Documents.Document doc = searcher.Doc(hit.Doc);
                results.Add(new Item() {  Title=doc.Get("title"),Content = doc.Get("content") });
            }
            searcher.Dispose();
            return results;
        }

        public class Item
        {
            public string Title { get; set; }
            public string Content { get; set; }
        }
    }

}
