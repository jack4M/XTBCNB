using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Windows;
using System.Text.RegularExpressions;

namespace XtbCnb2
{
    public class CnbConnection
    {
        public delegate void DelegateReportProgress(int step);

        /// <summary>
        /// Submits all data cut into fragments to CNB as partial submissions.
        /// WARNING: Requests a unique document ID from a server. There's only that many doc IDs, so beware.
        /// Stops immdiately when a fragment's submission return code isn't Success.
        /// </summary>
        /// <param name="data">This one should be initialized</param>
        /// <param name="certificate">Loaded cert with a private key in it</param>
        /// <param name="submissionUsername">CNB username</param>
        /// <param name="submissionPassword">CNB password</param>
        /// <param name="ReportProgress">Method to call when a fragment is about to be transferred</param>
        /// <returns>Error message of null on success of all submissions</returns>
        public static string SubmitData(CnbData data, X509Certificate2 certificate, string submissionUsername, string submissionPassword, DelegateReportProgress ReportProgress)
        {
            int totalFragments = (int)data.TotalFragments;

            CnbData cnbData = GeneralDataSource.GeneralDataSourceSingleton.CnbData;                 // generate <CISLO-VYDANI> if it's neccessary
            if (cnbData != null && cnbData.Data != null && totalFragments > 1)
                cnbData.CisloVydani = DownloadDocNumAndIncrement(1).ToString();

            long cisloDokumentu = DownloadDocNumAndIncrement(totalFragments);                       // generate <CISLO-ZPRAVY>

            
            for (int i = 0; i < totalFragments; i++)
            {
                ReportProgress(i);

                try
                {
                    if (cisloDokumentu < 0)
                    {
                        Log.LogAsText("Couldn't download document serial number from 4M server");
                        throw new Exception("Couldn't download document serial number from 4M server");
                    }

                    XmlDocument doc = data.GetFragment(i, cisloDokumentu);
                    string s = CnbData.XmlDocumentToString(doc);

//                    File.WriteAllText(@"C:\projekty\XtbCnb2\XtbCnb2\Tomas Drabek\part" + i + ".xml", s, Encoding.UTF8);
                    
                    byte[] bytes = CnbData.SignAndGZip(s, certificate);
                    ZaslaniDat.ZaslaniDatPortTypeClient zaslani = new ZaslaniDat.ZaslaniDatPortTypeClient();
                    

                    // filename, zipmethod, signaturemethod, username, password, inputdata, language, country
                    //                    string result = zaslani.loadData(GeneralDataSource.GeneralDataSourceSingleton.CnbData.WsFilename, "GZIP", "PKCS7", username.Text, password.Password, bytes, "cs", "CZ");
                    string wsFname = CnbData.GetWsFilename(doc);

                    Log.LogAsText("User " + submissionUsername + " submitting fragment " + i + "/" + totalFragments + " of doc #" + cisloDokumentu + "; wsFname: " + wsFname);
                    Log.LogAsFile(s, string.Format("{0}-{1:0000}.xml", cisloDokumentu, i));

                    string result = zaslani.loadData(wsFname, submissionUsername, submissionPassword, "GZIP", "PKCS7", bytes, "cs", "CZ");

                    string tfname = "R" + CnbData.ConstructCisloVydani().ToString() + ".xml";
                    Log.LogAsText("Result: logged as file " + tfname);
                    Log.LogAsFile(result, tfname);

                    XmlDocument resDoc = new XmlDocument();
//                    resDoc.XmlResolver = new EmbeddedResourceResolver();
                    resDoc.LoadXml(result);

                    XmlNode resNode = resDoc.SelectSingleNode("/LoadDataResponse/status");
                    string resClass = resNode.Attributes["category"].Value;
                    string resCode = resNode.Attributes["code"].Value;

                    // handle errors
                    if (resClass != "Success")
                    {
                        // concat messages
                        XmlNodeList msgs = resDoc.SelectNodes("/LoadDataResponse/status/messages/message");

                        StringBuilder msgsSb = new StringBuilder();

                        foreach (XmlNode msg in msgs)
                            msgsSb.AppendFormat("Message type: {0}, value: {1}\n", msg.Attributes["type"].Value, msg.Attributes["value"].Value);

                        if (msgs.Count == 0)
                            msgsSb.Append("(none)");

                        string resMsg = string.Format("Error submitting fragment {0} of {1} of document {2}:\nResult class: {3}\nResult code: {4}\n\nMessages:", i+1, totalFragments, wsFname, resClass, resCode, msgsSb.ToString());
                        return resMsg;
                    }
                }
                catch (Exception e)
                {
                    return "Exception " + e.GetType().FullName + " occured:\n" + e.Message;
                }

                if (cisloDokumentu > 0)
                    cisloDokumentu++;
            }

            return null;
        }

        public const string machineName = "apl.cnb.cz";

        /// <summary>
        /// Performs performQuery() on Ews, asking how a doc processed.
        /// </summary>
        /// <param name="submittedFile">File to read parameters from</param>
        /// <param name="callerType">this.GetType() - since we're static, we need a way to load embedded resources</param>
        /// <returns>The XML as string as the webservice returns it</returns>
        public static string GetProcessingResult(XmlDocument submittedFile, Type callerType, string username, string password)
        {
            if (submittedFile == null)
                return null;
            // fill in query template
            Stream stream = callerType.Assembly.GetManifestResourceStream("XtbCnb2.performQueryTemplate.txt");
            StreamReader sr = new StreamReader(stream);
            string query = sr.ReadToEnd();
            sr.Close();

            query = query.Replace("***USERNAME***", username);
            query = query.Replace("***PASSWORD***", password);
            XmlNode node = submittedFile.SelectSingleNode("/VYDANI/IDENTIFIKACE-VYKAZU/VYSKYT/STAV-KE-DNI");
            // yyyymmdd -> dd.mm.yyyy
            string s = node.InnerText;
            query = query.Replace("***OBDOBIV***", s.Substring(6,2) + "." + s.Substring(4,2) + "." + s.Substring(0,4));
            node = submittedFile.SelectSingleNode("/VYDANI/IDENTIFIKACE-VYKAZU/VYSKYT/SUBJEKT");
            query = query.Replace("***SUBJEKTV***", node.InnerText);
            node = submittedFile.SelectSingleNode("/VYDANI/IDENTIFIKACE-VYKAZU/DATOVY-SOUBOR");
            s = node.InnerText;
            int pos = s.IndexOf('.');
            if (pos > 0)
                s = s.Substring(0, pos);
            query = query.Replace("***DATOVY-SOUBOR***", s);

            string t = query;
            t = t.Replace(password, "[[[CENSORED]]]");
            string fnameForLog = "Q" + CnbData.ConstructCisloVydani().ToString() + ".xml";
            Log.LogAsText("User " + username + " querying document status via document " + fnameForLog);
            Log.LogAsFile(t, fnameForLog);

            query = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(query), Base64FormattingOptions.InsertLineBreaks);

            // fill in SOAP template
            stream = callerType.Assembly.GetManifestResourceStream("XtbCnb2.QuerySOAPTemplate.txt");
            sr = new StreamReader(stream);
            string soap = sr.ReadToEnd();
            sr.Close();

            soap = soap.Replace("***DATA-LENGTH***", (query.Length + 1).ToString());                    // +1 for \n
            soap = soap.Replace("***DATA***", query);

            // fill in total length
            byte[] tData = System.Text.Encoding.UTF8.GetBytes(soap);

            byte lastByte = 0;
            for (pos = 0; pos < tData.Length; pos++)
            {
                if (lastByte == 10 && tData[pos] == 10)
                    break;

                lastByte = tData[pos];
            }

            int totalLength = tData.Length - pos - 1;
            soap = soap.Replace("***TOTAL-LENGTH***", totalLength.ToString());

            // set up connection to webservice (we can't do this the comfy way as MS doesn't support SwA)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

            try
            {
                TcpClient client = new TcpClient(machineName, 443);
                client.SendTimeout = 2 * 60 * 1000;
                SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                sslStream.AuthenticateAsClient(machineName, null, SslProtocols.Ssl2 | SslProtocols.Ssl3, false);
                byte[] messsage = System.Text.Encoding.UTF8.GetBytes(soap);

//                File.WriteAllBytes(@"C:\projekty\XtbCnb2\XtbCnb2\Tomas Drabek\soap.bin", messsage);

                // send request to ws
                sslStream.Write(messsage);
                sslStream.Flush();

                // read ws response
                byte[] buffer = new byte[2048];
                StringBuilder messageData = new StringBuilder();
                int bytes = -1;
                do
                {
                    bytes = sslStream.Read(buffer, 0, buffer.Length);

                    if (bytes == 0)
                        break;

                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);
                } while (bytes != 0);

                client.Close();
                
                // get the response part we care about
                string rawMessage = messageData.ToString();
                rawMessage = rawMessage.Replace("\r\n", "\n");

                string resfname = "S" + CnbData.ConstructCisloVydani().ToString() + ".txt";
                Log.LogAsText("Response recorded as file " + resfname);
                Log.LogAsFile(rawMessage, resfname);

                // check HTTP result code
                Regex checkHeader = new Regex("HTTP/1.. 200.*");
                if (!checkHeader.IsMatch(rawMessage))
                    throw new Exception("Webservice returned the following raw error:\n" + rawMessage);


                Regex regex = new Regex(".*?Content-Type: .*? boundary.?=.?\"(.*?)\".*");
                Match match = regex.Match(rawMessage);
                string boundary = match.Groups[1].Value;

                pos = rawMessage.IndexOf(boundary);
                pos = rawMessage.IndexOf(boundary, pos + boundary.Length);
                pos = rawMessage.IndexOf(boundary, pos + boundary.Length);
                pos += boundary.Length;
                int partStart = pos;
                partStart = rawMessage.IndexOf("\n\n", partStart) + 2;
                int partEnd = rawMessage.IndexOf(boundary, pos) - 2;                                    // mime prepends 2 minuses, we don't want them
                string b64Message = rawMessage.Substring(partStart, partEnd - partStart);

                // decode the part
                byte[] decoded = null;

                try
                {
                    decoded = Convert.FromBase64String(b64Message);
                    return System.Text.Encoding.UTF8.GetString(decoded);
                }
                catch (Exception)
                {
                    return b64Message;
                }
            }
            catch (Exception e)
            {
                string msg = string.Format("Exception type: {0}, exception info: {1}", e.ToString(), e.Message);
                if (e.InnerException != null)
                    msg += string.Format("\n\nInner exception type: {0}, exception info: {1}", e.InnerException.ToString(), e.InnerException.Message);

                MessageBox.Show(msg, "Exception Encountered");
                return null;
            }
        }

        /// <summary>
        /// Trivial "always OK" certificate validator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Status codes for performQuery() result XML
        /// </summary>
        public static Dictionary<int, string> VyskytStavKod = new Dictionary<int,string>()
        {
            {-1, "Termín uplynul, k dispozici jsou jen data replikována "},
            {10, "Nejsou žádná data, termín dosud neuplynul"},
            {15, "Nejsou žádná data, je urgováno jejich dodání"},
            {18, "Nejsou žádná data, všechny stupně upomínek vyčerpány"},
            {20, "Právě je zpracováváno vydání k výskytu"},
            {30, "Je požadováno zaslání potvrzení dat vykazujícím subjektem"},
            {40, "Je požadováno zaslání opravy dat vykazujícím subjektem "},
            {50, "K výskytu jsou platná data"},
            {55, "Výskyt je uzavřen, jsou platná data, nelze je již změnit"},
            {65, "Výskyt je promlčen, nejsou platná data"}
        };

        /// <summary>
        /// Status codes for performQuery() result XML
        /// </summary>
        public static Dictionary<int, string> VydaniStavKod = new Dictionary<int, string>()
        {
            {-1, "Replikované vydání vzniklé z posledních platných hodnot   daného datového souboru"},
            {0, "Fiktivní vydání pro došlé zprávy typu „storno“, „potvrzení“ aj.  "},
            {5, "Data vydání připravena uživatelem SDNS, data nejsou v db"},
            {10, "Vydání založeno"},
            {15, "Fatální chyba v JVK (např. dělení nulou)"},
            {16, "Zjištěny chyby v JVK, požadavek na opravu dat"},
            {17, "Zjištěny chyby v JVK, požadavek na potvrzení"},
            {18, "t.č. nepoužíváno"},
            {19, "V rámci JVK nebyla zjištěna chyba"},
            {31, "Data jsou uložena v db a je požadováno jejich potvrzení z důvodu chyby v JVK "},
            {32, "Data jsou uložena v db a je požadováno jejich potvrzení z důvodu chyby v KČŘ"},
            {51, "Data jsou uložena v db a jsou platná"},
            {52, "Data jsou uložena v db a jsou platná (byla potvrzena)"},
            {59, "Data byla stornována"},
            {61, "Chybné vydání, jehož data nebyla uložena do db"},
            {62, "Stornované vydání před potvrzením dat"},
            {99, "Interní chyba, vydání není možné zpracovat"}
        };

        /// <summary>
        /// Gets the next document number and increments it by one.
        /// DON'T USE THIS if you're not sure what it's good for.
        /// </summary>
        /// <returns>Document number you can safely use as it's unique. Or -1 if error.</returns>
        public static long DownloadDocNumAndIncrement()
        {
            HttpWebRequest WebRequestObject = (HttpWebRequest)HttpWebRequest.Create(@"http://www.4m.to/xtbcnb/getDocId.aspx?count=1");
            WebRequestObject.UserAgent = "XtbCnb";
            WebResponse Response = WebRequestObject.GetResponse();
            Stream WebStream = Response.GetResponseStream();
            StreamReader Reader = new StreamReader(WebStream);
            string PageContent = Reader.ReadToEnd();
            Reader.Close();
            WebStream.Close();
            Response.Close();

            long res = -1;
            long.TryParse(PageContent.Trim(), out res);

            return res;
        }

        /// <summary>
        /// Gets the next document number and increments it by "count" (reservation next ids).
        /// DON'T USE THIS if you're not sure what it's good for.
        /// </summary>
        /// <param name="count"></param>
        /// <returns>Document number you can safely use as it's unique. Or -1 if error.</returns>
        public static long DownloadDocNumAndIncrement(int count)
        {
            HttpWebRequest WebRequestObject = (HttpWebRequest)HttpWebRequest.Create(@"http://www.4m.to/xtbcnb/getDocId.aspx?count=" + count);
            WebRequestObject.UserAgent = "XtbCnb";
            WebResponse Response = WebRequestObject.GetResponse();
            Stream WebStream = Response.GetResponseStream();
            StreamReader Reader = new StreamReader(WebStream);
            string PageContent = Reader.ReadToEnd();
            Reader.Close();
            WebStream.Close();
            Response.Close();

            long res = -1;
            long.TryParse(PageContent.Trim(), out res);

            return res;
        }

    }
}
