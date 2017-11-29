using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace analysis
{
    class Program
    {
        static void Main(string[] args)
        {
            //open connection to MySQL
            const string server = "localhost";
            const string database = "reactome";
            const string uid = "yu";
            const string psd = "ch3ch2oh";
            string connectionStr = "SERVER=" + server + "; DATABASE=" + database + "; UID=" + uid + "; PASSWORD=" + psd + ";";
            var con = new MySqlConnection(connectionStr);
            con.Open();
            Dictionary<UInt32, string> geneid = new Dictionary<UInt32, string>();
            MySqlCommand createProcGene = con.CreateCommand();
            createProcGene.CommandText = funcinsGene;
            createProcGene.ExecuteNonQuery();
            MySqlCommand createProcID = con.CreateCommand();
            createProcID.CommandText = funcinsID;
            createProcID.ExecuteNonQuery();

            //open file
            using (var hs = new StreamReader(Path.GetFullPath( "HS.txt")))
            {
                string line;
                string[] tab = new string[] { "\t" };
                while ((line = hs.ReadLine()) != null)
                {
                    if (Char.IsDigit(line[0]))
                    {
                        string[] words = line.Split(tab, StringSplitOptions.None);
                        if (words[1].Length > 0 && words[2].Length > 0)
                        {
                            UInt32 id = UInt32.Parse(words[1]);
                            geneid[id] = words[2];
                            MySqlCommand cmd = con.CreateCommand();
                            cmd.CommandText = "call insertID(@id, @symbol)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@symbol", words[2]);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            using (var pathways = new StreamReader(Path.GetFullPath("pathways.txt")))
            {
                string line;
                string[] tab = new string[] { "\t" };
                while ((line = pathways.ReadLine()) != null)
                {
                    if (line[0] != '#')
                    {
                        string[] words = line.Split(tab, StringSplitOptions.None);
                        StringBuilder gene1 = new StringBuilder();
                        StringBuilder gene2 = new StringBuilder();
                        if (words[2].Length > 10)
                            for (int i = 11; i < words[2].Length && Char.IsDigit(words[2][i]); i++)
                                gene1.Append(words[2][i]);
                        if (words[5].Length > 10)
                            for (int i = 11; i < words[5].Length && Char.IsDigit(words[5][i]); i++)
                                gene2.Append(words[5][i]);
                        if (gene1.Length > 0 && gene2.Length > 0 && !gene1.Equals(gene2))
                        {
                            UInt32 geneid1 = UInt32.Parse(gene1.ToString());
                            UInt32 geneid2 = UInt32.Parse(gene2.ToString());
                            if (!geneid.ContainsKey(geneid1))
                            {
                                string symbol1 = FindGeneSymbol(geneid1);
                                geneid[geneid1] = symbol1;
                                MySqlCommand insCmd = con.CreateCommand();
                                insCmd.CommandText = "call insertID(@id, @symbol)";
                                insCmd.Prepare();
                                insCmd.Parameters.AddWithValue("@id", geneid1);
                                insCmd.Parameters.AddWithValue("@symbol", symbol1);
                                insCmd.ExecuteNonQuery();
                            }
                            if (!geneid.ContainsKey(geneid2))
                            {
                                string symbol2 = FindGeneSymbol(geneid2);
                                geneid[geneid2] = symbol2;
                                MySqlCommand insCmd = con.CreateCommand();
                                insCmd.CommandText = "call insertID(@id, @symbol)";
                                insCmd.Prepare();
                                insCmd.Parameters.AddWithValue("@id", geneid2);
                                insCmd.Parameters.AddWithValue("@symbol", symbol2);
                                insCmd.ExecuteNonQuery();
                            }
                            string pattern = words[6];
                            MySqlCommand cmd = con.CreateCommand();
                            if (geneid.ContainsKey(geneid1) && geneid.ContainsKey(geneid2))
                                Console.WriteLine(String.Format("{0} {1} {2}", geneid[geneid1], geneid[geneid2], pattern));
                            cmd.CommandText = @"call insertGenes(@id1, @id2, @pattern)";
                            cmd.Prepare();
                            cmd.Parameters.AddWithValue("@id1", geneid1);
                            cmd.Parameters.AddWithValue("@id2", geneid2);
                            cmd.Parameters.AddWithValue("@pattern", pattern);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            con.Close();
        }

        static string FindGeneSymbol(uint id)
        {
            WebRequest webRq;
            string url = @"https://www.ncbi.nlm.nih.gov/gene/" + id.ToString();
            webRq = WebRequest.Create(url);
            var webRes = webRq.GetResponse();
            var streamRes = webRes.GetResponseStream();
            var streamrd = new StreamReader(streamRes);
            string ReadStr = streamrd.ReadToEnd();
            string pattern = @"(?<=<!-- title -->\s{0,8}?<title>)(\w+?)(?:\s+)";
            Match mch = Regex.Match(ReadStr, pattern);
            string symbol = mch.Value.ToUpper();
            Console.WriteLine(String.Format("Fetch from web: {0}, {1}", id, symbol));
            return symbol;
        }

        const string funcinsGene = @"
            drop procedure if exists insertGenes;
            create procedure insertGenes(id1 int, id2 int, pattern varchar(10))
            begin
	            DECLARE symbol1 varchar(40);
	            DECLARE symbol2 varchar(40);
	            select symbol into symbol1 from geneid where id = id1 limit 1;
	            select symbol into symbol2 from geneid where id = id2 limit 1;		
	            if NOT exists (
		            select * from interaction
		            where gene1 = symbol1 
		            and gene2 = symbol2 
		            limit 1
		            )
	            then			
		            insert into interaction values(symbol1, symbol2, pattern);
	            end if;
            end";
        const string funcinsID = @"
            drop procedure if exists insertID;
            create procedure insertID(nid int, symbol varchar(40))
            begin		
	            if NOT exists (
		            select * from geneid
		            where id = nid 
		            limit 1
		            )
	            then			
		            insert into geneid values(nid, symbol);
	            end if;
            end";
    }
}



