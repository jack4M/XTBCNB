using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.GZip;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Xml.Linq;

namespace XtbCnb2
{
    public delegate void DelegateSetMinMax(int min, int max);
    public delegate void DelegateSetCurrent(int cur);

    public class CnbData : INotifyPropertyChanged
    {
        /* PUBLIC STUFF */
        public static readonly string[] COL_NAMES_41 = { "Identifikace\nzáznamu", 
                                                        "Cenová notace", 
                                                        "Typ vztahu", 
                                                        "Typ pokynu", 
                                                        "Přijetí pokynu\nzpůsob", 
                                                        "Přijetí pokynu\ndatum", 
                                                        "Přijetí pokynu\nčas", 
                                                        "Číslo pokynu", 
                                                        "Číslo pokynu\npropojovací", 
                                                        "Identifikace\nzadavatele", 
                                                        "Identifikace\nzákazníka", 
                                                        "Platnost pokynu\ndatum", 
                                                        "Identifikace\nnástroje (ISIN)", 
                                                        "Identifikace nástroje\n(AII/ticker/interní)", 
                                                        "Ukazatel\nnákupu/prodeje", 
                                                        "Limitní\ncena 1", 
                                                        "Limitní\ncena 2", 
                                                        "Limitní\nobjem", 
                                                        "Požadované množství\n/nominální hodnota", 
                                                        "Notace\nmnožství", 
                                                        "Požadováné datum\nvypořádání", 
                                                        "Požadováná\nprotistrana", 
                                                        "Další\ndispozice", 
                                                        "Úplata\nOCP", 
                                                        "Úplata OCP\nměna", 
                                                        "Identifikace\ntřetí osoby", 
                                                        "Úplata\ntřetí osobě", 
                                                        "Úplata třetí\nosobě - měna", 
                                                        "Přijetí zrušení\npokynu - datum", 
                                                        "Přijetí zrušení\npokynu - čas", 
                                                        "Předání zrušení\npokynu - datum", 
                                                        "Předání zrušení\npokynu - čas", 
                                                        "Důvod\nzrušení", 
                                                        "Požadavek na nezveřejnění\nlimitního pokynu", 
                                                        "Nestandardní požadavek\nna provedení pokynu", 
                                                        "Zveřejnění pokynu\ndatum", 
                                                        "Zveřejnění\npokynu - čas", 
                                                        "Předání pokynu\ndatum", 
                                                        "Předání\npokynu - čas", 
                                                        "Makléř\n1", 
                                                        "Makléř\n2", 
                                                        };

        public static readonly string[] COL_NAMES_42 = { "Identifikace\nzáznamu", 
                                                        "Cenová\nnotace", 
                                                        "Typ\ntransakce", 
                                                        "Kategorie\nzákazníka - MIFID", 
                                                        "Číslo\npokynu", 
                                                        "Referenční\nčíslo obchodu", 
                                                        "Autor čísla\nobchodu", 
                                                        "Den uskutečnění\nobchodu", 
                                                        "Čas uskutečnění\nobchodu", 
                                                        "Ukazatel\nnákupu/prodeje", 
                                                        "Postavení", 
                                                        "Identifikace\nnástroje (ISIN)", 
                                                        "Identifikace nástroje\n(AII/ticker/interní)", 
                                                        "Identifikace\nzákazníka", 
                                                        "Jednotková\ncena v měně", 
                                                        "Jednotková cena\nv procentech", 
                                                        "Množství/nominální\nhodnota", 
                                                        "Protistrana", 
                                                        "Identifikace\nmísta", 
                                                        "Předpokládaný\nden vypořádání", 
                                                        "Skutečný\nden vypořádání", 
                                                        "Objem\nobchodu", 
                                                        "Úplata\nOCP", 
                                                        "Úplata\nOCP - měna", 
                                                        "Úplata\ntřetí osobě", 
                                                        "Úplata třetí\nosobě - měna", 
                                                        "Datum zrušení\nobchodu", 
                                                        "Čas zrušení\nobchodu", 
                                                        "Makléř\n3", 
                                                        "Makléř\n4", 
                                                        };


        public static readonly string[] COL_NAMES_43 = { "Identifikace\nzáznamu", 
                                                        "Měna nominální hodnoty\n/realizační ceny", 
                                                        "Druh nástroje\n(ČNB)", 
                                                        "Typ podkladového\naktiva", 
                                                        "Rozlišení dle\nsídla emitenta", 
                                                        "Identifikace\nnástroje (ISIN)", 
                                                        "Identifikace nástroje\n(AII/ticker/interní)", 
                                                        "Název\nnástroje", 
                                                        "Identifikace podkladového\nnástroje (ISIN)", 
                                                        "Identifikace podkladového\nnástroje (ticker/interní ident.)", 
                                                        "Název podkladového\nnástroje", 
                                                        "Druh nástroje\n(CFI)", 
                                                        "Datum splatnosti\n/realizace", 
                                                        "Realizační\ncena", 
                                                        "Cenový multiplikátor\n/hodnota bodu", 
                                                        "Aktuální nominální\nhodnota", 
                                                        "Vysvětlivka", 
                                                        };


        public static readonly string[] COL_NAMES_44 = { "Identifikace\nzáznamu", 
                                                        "Identifikace\nosoby", 
                                                        "Ekonomický\nsektor osoby", 
                                                        "Autor identifikace\nosoby", 
                                                        "Datum narození", 
                                                        "Identifikace\nosoby (IČ)", 
                                                        "Název\n/Přijmení", 
                                                        "Jméno", 
                                                        "Adresa sídla\n- ulice, číslo", 
                                                        "Adresa sídla\n- obec", 
                                                        "Adresa sídla\n- PSČ", 
                                                        "Adresa sídla\n- stát", 
                                                        };


        
        public static string[] COL_NAMES = COL_NAMES_42;

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(
                    this, new PropertyChangedEventArgs(propName));
        }

        /* PRIVATE STUFF */
        public XmlDocument Xml = null;
        private ObservableCollection<string> jmenaOblasti = new ObservableCollection<string>();
        public string[][][] Data = null;                               // addressing: datovaOblast(const int)->row(var int)->col(const int)
        DelegateSetMinMax SetMinMax;
        DelegateSetCurrent SetCurrent;

        public string CisloVydani = null;

        public bool xmlDataAreValid = false;        // user value 

        public CnbData()
        {
            CisloVydani = ConstructCisloVydani().ToString();        // NEPOUZITO - PREPSANO PRI ODESILANI DAT!!!
        }

        /// <summary>
        /// Catch any exceptions - XML loading and parsing occurs here
        /// </summary>
        /// <param name="XmlFilename">Where the XML file lives</param>
        public void LoadData(string XmlFilename, DelegateSetMinMax SetMinMax = null, DelegateSetCurrent SetCurrent = null)
        {
            Xml = new XmlDocument();
//            Xml.XmlResolver = new EmbeddedResourceResolver();
            Stream stream = File.OpenRead(XmlFilename);

            // TODO: vyhazovat error, pokud exception
            Xml.Load(stream);
            stream.Close();
            OnPropertyChanged("IsTest");
            OnPropertyChanged("GetHeaderInfo");

            this.SetMinMax = SetMinMax;
            this.SetCurrent = SetCurrent;

            ParseDataTable();

            // this is for sure a bad thing. but I have to test it first...
            GeneralDataSource.GeneralDataSourceSingleton.OnPropertyChanged("DataHeaders");
            GeneralDataSource.GeneralDataSourceSingleton.OnPropertyChanged("DataWindows");
        }

        /* THIS IS THE PUBLIC STUFF YOU CAN USE */
        /// <summary>
        /// Getter for an entire row
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public string[] this[int row]
        {
            get
            {
                return Data[0][row];
            }
        }

        /// <summary>
        /// Getter for column names
        /// </summary>
        public ObservableCollection<string> JmenaOblasti
        {
            get
            {
                return jmenaOblasti;
            }
        }

        /// <summary>
        /// Is the data for testing purposes or for real?
        /// </summary>
        public bool? IsTest
        {
            get
            {
                if (Xml == null)
                    return null;
                XmlNode node = Xml.SelectSingleNode("/VYDANI/IDENTIFIKACE-ZPRAVY/FUNKCE-ZPRAVY");
                return node.Attributes["KOD"].Value == "Testovací";
            }
        }

        public KeyValuePair<string, string>[] DataHeaders
        {
            get
            {
                if (Xml == null)
                    return null;

                List<KeyValuePair<string, string>> res = new List<KeyValuePair<string, string>>();

//                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-ZPRAVY/NAZEV-DOKUMENTU"));
//                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-ZPRAVY/CISLO-ZPRAVY"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/DATOVY-SOUBOR"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/VYSKYT/STAV-KE-DNI"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-ZPRAVY/FUNKCE-ZPRAVY"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/STATUS"));
                if (GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/REFERENCNI-ZPRAVA").Value != "*NOT IN XML*")
                    res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/REFERENCNI-ZPRAVA"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/DUVOD"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/AUDIT"));
                res.Add(GetHeaderInfo("/VYDANI/IDENTIFIKACE-VYKAZU/VYSKYT/SUBJEKT"));
                res.Add(GetHeaderInfo("/VYDANI/ADRESA/KONTAKT/JMENO-OSOBY"));

                return res.ToArray();
            }
        }

        private KeyValuePair<string, string> GetHeaderInfo(string xpath)
        {
            XmlNode node = Xml.SelectSingleNode(xpath);
            
            if (node == null)
                return new KeyValuePair<string, string>(xpath, "*NOT IN XML*");

            string val = node.InnerText;

            if (val == null || val == "")
                val = node.Attributes[0].Value;

            return new KeyValuePair<string, string>(node.Name, val);
        }

        /* THIS IS WHERE THE PRIVATE STUFF HAS A PARTY */
        /// <summary>
        /// Constructs the internal data table from XML.
        /// Throws an exception if no data is found.
        /// 
        /// The reason for a lot of optimizations (NextSibling etc.) here is that it's 100x faster
        /// than with string indexers.
        /// </summary>
        private void ParseDataTable()
        {
            XmlNodeList datoveOblasti = Xml.SelectNodes("/VYDANI/DATA/DATOVA-OBLAST");

            if (datoveOblasti == null || datoveOblasti.Count == 0)
                throw new Exception("EXC_NO_DATA");

                Application.Current.Dispatcher.Invoke(
              System.Windows.Threading.DispatcherPriority.Normal,
              new Action(
                delegate()
                {
                    jmenaOblasti.Clear();
                }
            ));

            Data = new string[datoveOblasti.Count][][];

            // count amount of lines in all data areas
            int totalRows = 0;
            for (int i = 0; i < datoveOblasti.Count; i++)
            {
                XmlNodeList rows = datoveOblasti[i].ChildNodes;
                totalRows += rows.Count;
            }

            // set min/max value for tracking progress
            if (SetMinMax != null)
                SetMinMax(0, totalRows);

            int progress = 0;
            int callNextProgress = totalRows / 100;

            int TEMP = 1;

            for (int i = 0; i < datoveOblasti.Count; i++)
            {
                XmlNodeList rows = datoveOblasti[i].ChildNodes;

                Application.Current.Dispatcher.Invoke(
              System.Windows.Threading.DispatcherPriority.Normal,
              new Action(
                delegate()
                {
                    string datovaOblast = datoveOblasti[i].Attributes["KOD"].Value;

                    jmenaOblasti.Add(datovaOblast);

                    switch (datovaOblast.ToUpper().Trim())
                    {
                        case "MOKA41_11":
                            COL_NAMES = COL_NAMES_41;
                            break;
                        case "MOKA42_11":
                            COL_NAMES = COL_NAMES_42;
                            break;
                        case "MOKA43_11":
                            COL_NAMES = COL_NAMES_43;
                            break;
                        case "MOKA44_11":
                            COL_NAMES = COL_NAMES_44;
                            break;
                        default:
                            throw new Exception("Invalid KOD in DATOVA-OBLAST");
                    }

                }
            ));

                // find max row id
                int rowsCnt = rows.Count;
                int maxRowIdx = -1;
                XmlNode row = null;

                for (int j = 0; j < rowsCnt; j++)
                {
                    if (row == null)
                        row = rows[j];
                    else
                        row = row.NextSibling;

                    //int rowIdx = int.Parse(row.Attributes["PORADI"].Value);
                    // delete these 3
                    row.Attributes["PORADI"].Value = TEMP.ToString();
                    int rowIdx = TEMP;
                    TEMP++;

                    if (rowIdx > maxRowIdx)
                        maxRowIdx = rowIdx;
                }

                row = null;

                Data[i] = new string[maxRowIdx][];

                for (int j = 0; j < rowsCnt; j++)
                {
                    progress++;

                    if (SetCurrent != null && (progress >= callNextProgress || progress == totalRows - 1))
                    {
                        callNextProgress = progress + totalRows / 100;
                        SetCurrent(progress);
                    }

                    if (row == null)
                        row = rows[j];
                    else
                        row = row.NextSibling;

                    int rowIdx = int.Parse(row.Attributes["PORADI"].Value) - 1;
                    XmlNodeList cols = row.ChildNodes;
                    if (rowIdx >= Data[i].Length)
                    {
                        Console.WriteLine("rowIdx=" + rowIdx + ", Length=" + Data[i].Length);
                    }
                    Data[i][rowIdx] = new string[COL_NAMES.Length];

                    XmlNode col = null;

                    for (int k = 0; k < COL_NAMES.Length; k++)
                    {
                        if (col == null)
                        {
                            if (k >= cols.Count) {
                                Console.WriteLine("k=" + k + ", Length=" + cols.Count);
                            }
                            col = cols[k];
                        }
                        else
                            col = col.NextSibling;

                        if (col == null)
                            break;

                        int colIdx = int.Parse(col.Attributes["PORADI"].Value) - 1;
                        if (colIdx == 0)
                            Console.WriteLine("rowIdx = " + rowIdx + ": " + " = " + col.InnerText);

                        if (colIdx >= Data[i][rowIdx].Length)
                        {
                            Console.WriteLine("rowIdx=" + rowIdx + ", coldIdx=" + colIdx + ", Length=" + Data[i][rowIdx].Length);
                        }
                        Data[i][rowIdx][colIdx] = col.InnerText;
                        Data[i][rowIdx] = new string[COL_NAMES.Length];
                    }

                    // null -> ""
                    for (int k = 0; k < COL_NAMES.Length; k++)
                    {
                        if (Data[i][rowIdx][k] == null)
                            Data[i][rowIdx][k] = "";
                    }
                }
            }
        }

        /* submission stuff here */
        // line ~ 500 bytes in XML -> gzip ~ 1:10 ratio -> base64 @ 4/3 = ~ 70 bytes per line in the resulting encoding
        // let's have some 100 kB of data per partial submission ~ 100000/70 = 1500 lines with a reserve
        public const int LINES_PER_FRAGMENT = 1500;

        /// <summary>
        /// How many lines of data are there in the file?
        /// </summary>
        public int? TotalLines
        {
            get
            {
                if (Xml == null)
                    return null;

                // for docs with no data
                if (Data == null)
                    return 0;

                int res = 0;

                for (int i = 0; i < Data.Length; i++)
                    res += Data[i].Length;

                return res;
            }
        }

        /// <summary>
        /// How many submission fragments are there going to be?
        /// </summary>
        public int? TotalFragments
        {
            get
            {
                if (Xml == null)
                    return null;

                // for docs with no data
                if (Data == null)
                    return 1;

                int totalLines = TotalLines.Value;
                int res = totalLines/LINES_PER_FRAGMENT;

                if (totalLines % LINES_PER_FRAGMENT != 0)
                    res++;

                return res;
            }
        }

        /// <summary>
        /// NEPOUZITO - ZISKANO STEJNE JAKO CISLO ZPRAVY
        /// Encodes current time into 32 bits as follows (from highest bit):
        /// 3 bits: year offset from 2010
        /// 4 bits: month
        /// 5 bits: day
        /// 5 bits: hours
        /// 6 bits: minutes
        /// 6 bits: seconds
        /// 3 bits: eights of a second
        /// 
        /// Will work until 2017. Beyond that, the timestamp will start repeating.
        /// </summary>
        /// <returns>Timestamp that contains eights of a second</returns>
        public static uint ConstructCisloVydani()
        {
            // DEBUG
 //           if (true)
//                return 475597536;

            uint res = 0;

            DateTime date = DateTime.UtcNow;
            uint t = (8 * (uint)date.Millisecond / 1000) & 7;
            res = t << 29;

            t = (uint)date.Second & 63;
            res |= t << 23;

            t = (uint)date.Minute & 63;
            res |= t << 17;

            t = (uint)date.Hour & 31;
            res |= t << 12;

            t = (uint)date.Day & 31;
            res |= t << 7;

            t = (uint)date.Month & 15;
            res |= t << 3;

            t = ((uint)date.Year - 2010) & 7;
            res |= t;

            return res;
        }

        /// <summary>
        /// Gets a partial document with only the appropriate data and the appropriate\
        /// partial submission headers.
        /// </summary>
        /// <param name="fragmentNum">Zero-based fragment index. Don't exceed the max.</param>
        /// <param name="cisloZpravy">Unique document number</param>
        /// <returns>A new xml doc with the content you need</returns>
        public XmlDocument GetFragment(int fragmentNum, long cisloZpravy)
        {
            if (Xml == null)
                return null;

            XmlDocument res = (XmlDocument)Xml.Clone();
//            res.XmlResolver = new EmbeddedResourceResolver();

            // add partial message tag and update message number
            XmlNode hangItHere = res.SelectSingleNode("/VYDANI/IDENTIFIKACE-ZPRAVY/CISLO-ZPRAVY");
            if (hangItHere == null)
            {
                XmlNode tempNode = res.CreateNode(XmlNodeType.Element, "CISLO-ZPRAVY", null);
                hangItHere = res.SelectSingleNode("/VYDANI/IDENTIFIKACE-ZPRAVY");
                hangItHere.AppendChild(tempNode);
                hangItHere = res.SelectSingleNode("/VYDANI/IDENTIFIKACE-ZPRAVY/CISLO-ZPRAVY");
            }

            hangItHere.InnerText = cisloZpravy.ToString();

            // for docs with no data or docs that fit within one fragment
            if (Data == null || TotalFragments <= 1)
            {
                return res;
            }

            XmlNode partialMessageNode = res.CreateNode(XmlNodeType.Element, "CASTECNA-ZPRAVA", null);
            XmlAttribute tmpNode = res.CreateAttribute("PORADI");
            tmpNode.Value = (fragmentNum + 1).ToString();
            partialMessageNode.Attributes.Append(tmpNode);

            // add cislo-vydani
            XmlNode cisloVydaniNode = res.CreateNode(XmlNodeType.Element, "CISLO-VYDANI", null);
            cisloVydaniNode.InnerText = CisloVydani.ToString();
            partialMessageNode.AppendChild(cisloVydaniNode);

            string s = null;

            // add first/last optional attribute
            if (fragmentNum == 0)
                s = "První";
            else if (fragmentNum == TotalFragments - 1)
                s = "Poslední";

            if (s != null)
            {
                tmpNode = res.CreateAttribute("TYP");
                tmpNode.Value = s;
                partialMessageNode.Attributes.Append(tmpNode);
            }

            hangItHere.ParentNode.InsertAfter(res.ImportNode(partialMessageNode, true), hangItHere);
//            hangItHere.AppendChild(res.ImportNode(partialMessageNode, true));

            // and now only retain nodes that belong in the part
            int firstIndex = fragmentNum * LINES_PER_FRAGMENT;
            int lastIndex = firstIndex + LINES_PER_FRAGMENT - 1;

            if (lastIndex > TotalLines - 1)
                lastIndex = (int)TotalLines - 1;

            XmlNodeList nodeList = res.SelectNodes("/VYDANI/DATA/DATOVA-OBLAST");

            int i = 0;
            List<XmlNode> nodesToDelete = new List<XmlNode>();

            // get a list of nodes to delete (can't do it now, foreach wouldn't iterate and indexing is slow)
            foreach (XmlNode node in nodeList)
            {
                foreach (XmlNode rowNode in node.ChildNodes)
                {
                    if (i < firstIndex || i > lastIndex)
                    {
                        // not in required range -> remove later
                        nodesToDelete.Add(rowNode);
                    }

                    i++;
                }
            }

            // delete the nodes now
            foreach (XmlNode node in nodesToDelete)
                node.ParentNode.RemoveChild(node);

            nodesToDelete.Clear();

            // and delete any empty parents now
            foreach (XmlNode node in nodeList)
                if (node.ChildNodes.Count == 0)
                    nodesToDelete.Add(node);

            foreach (XmlNode node in nodesToDelete)
                node.ParentNode.RemoveChild(node);

//            res.Save(@"c:\temp\xml" + fragmentNum + @".xml");

            return res;
        }

        /// <summary>
        /// Convert a string into bytes in utf8 encoding, gzip it and return result as base64 string.
        /// </summary>
        /// <param name="data">Input data</param>
        /// <param name="certificate">Optional PKCS#7 cert to sign the data with</param>
        /// <returns>Encoded data</returns>
        public static string SignGZipAndBase64(string data, X509Certificate2 certificate=null)
        {
            return Convert.ToBase64String(SignAndGZip(data, certificate));
        }

        /// <summary>
        /// Convert a string into bytes in utf8 encoding, gzip it and return result as byte[]
        /// </summary>
        /// <param name="data">Input data</param>
        /// <param name="certificate">Optional PKCS#7 cert to sign the data with</param>
        /// <returns>Encoded data</returns>
        public static byte[] SignAndGZip(string data, X509Certificate2 certificate = null)
        {
            // serialize the string as utf8
//            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            
            // TODO: udelat podle headeru XML
//            Encoding encoding = Encoding.GetEncoding("Windows-1250");
            Encoding encoding = Encoding.UTF8;
            byte[] bytes = encoding.GetBytes(data);

            // sign using the cert
            if (certificate != null)
            {
//                Debug.WriteLine("Private key: " + certificate.PrivateKey.ToString());

                ContentInfo content = new ContentInfo(bytes);
                SignedCms signedCms = new SignedCms(content, false);
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificate);
                signedCms.ComputeSignature(signer, false);
                bytes = signedCms.Encode();
            }

            // gzip it
            MemoryStream ms = new MemoryStream();
            GZipOutputStream gout = new GZipOutputStream(ms);
            gout.Write(bytes, 0, bytes.Length);
            gout.Close();

            return ms.ToArray();
        }

        /// <summary>
        /// Convert an XmlDocument to a string. It's more complicated than you think.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static string XmlDocumentToString(XmlDocument doc)
        {
//            MemoryStream ms = new MemoryStream();
//            doc.Save(ms);
//            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
//            return enc.GetString(ms.ToArray());

            //StringWriter sw = new StringWriter();
            //XmlTextWriter xw = new XmlTextWriter(sw);
            //doc.WriteTo(xw);
            //return sw.ToString();

            //XDocument xdoc = XDocument.Load();
            //return xdoc.ToString(SaveOptions.None);
            
            return doc.OuterXml;
        }

        public const string ORGANIZATION_NUMERIC_ID = "278";

        /// <summary>
        /// Filename parameter that CNB requires for submission. Format: ws[0-9]{3}[0-9]{7}.xml.
        /// First number = numeric ID of the organization, second number = zero-padded CISLO-ZPRAVY.
        /// </summary>
        /// <param name="doc">Doc to load CISLO-ZPRAVY from</param>
        /// <returns>The filename or null if doc is null</returns>
        public static string GetWsFilename(XmlDocument doc)
        {
            if (doc == null)
                return null;

            XmlNode node = doc.SelectSingleNode("/VYDANI/IDENTIFIKACE-ZPRAVY/CISLO-ZPRAVY");
            string num = node.InnerText.Trim();

            return string.Format("ws{0}{1}.xml", ORGANIZATION_NUMERIC_ID, num.PadLeft(7, '0'));
        }
    }
}
