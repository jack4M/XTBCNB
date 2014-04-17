using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using ActiproSoftware.Windows.Controls.Ribbon;
using System.Threading;
using System.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Net.Security;
using System.Net;
using System.IO;

namespace XtbCnb2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public static MainWindow MainWin = null;

        public MainWindow()
        {
            InitializeComponent();
            //InitializeComponent();
            //this.WindowState = WindowState.Maximized;
            MainWin = this;

            Log.LogAsText(GeneralDataSource.GeneralDataSourceSingleton.WelcomeInfo);
        }

//        public GeneralDataSource GeneralDataSource = new GeneralDataSource();

        private volatile string submissionResult = null;

        public bool MyValidationCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors err)
        {
            return true;
        }

        private void wizard_SelectedPageChanged(object sender, ActiproSoftware.Windows.Controls.Wizard.WizardSelectedPageChangeEventArgs e)
        {
            if (e.NewSelectedPage != donePage)
                e.NewSelectedPage.FinishButtonEnabled = false;
            else
                e.NewSelectedPage.FinishButtonEnabled = true;

            if (e.NewSelectedPage == welcomePage)
            {
                welcomePage.BackButtonVisible = false;
            }
            else if (e.NewSelectedPage == processingPage)
            {
                // Clear the processing amount
                progressBar.Value = 0;
                SetProgressTextInMainThread("", false);
                processingPage.NextButtonEnabled = false;
            }
            else if (e.NewSelectedPage == dataPage)
            {
                // GeneralDataSource.GeneralDataSourceSingleton.win1.Activate();
            }
            else if (e.NewSelectedPage == confirmationPage)
            {
                //if ((bool)GeneralDataSource.GeneralDataSourceSingleton.CnbData.IsTest)
                //{
                //    wizard.SelectedPage = submissionPage;
                //    return;
                //}

                TotallySure.IsChecked = false;
                confirmationPage.NextButtonEnabled = false;
            }
            else if (e.NewSelectedPage == credentialsPage)
            {
                username.Text = Properties.Settings.Default.LastUsername;

                password.Password = Properties.Settings.Default.LastPswd;
                checkBox_rememberUserPswd.IsChecked = !String.IsNullOrEmpty(password.Password);

                if (String.IsNullOrEmpty(Properties.Settings.Default.LastCertificatePswd))
                {
                    certPassword.Password = "";
                    checkBox_rememberCertificatePswd.IsChecked = false;
                    certFilename.Text = "";
                }
                else
                {
                    certPassword.Password = Properties.Settings.Default.LastCertificatePswd;
                    certFilename.Text = Properties.Settings.Default.LastPathCertificate;
                    checkBox_rememberCertificatePswd.IsChecked = true;
                }

                credentialsNextButtonCheck();
                credentialsPage.NextPage = (GeneralDataSource.GeneralDataSourceSingleton.CnbData.IsTest == true) ? submissionPage : confirmationPage;
            }
            else if (e.NewSelectedPage == submissionPage)
            {
                Properties.Settings.Default.LastUsername = username.Text;
                Properties.Settings.Default.LastPswd = (checkBox_rememberUserPswd.IsChecked == true) ? password.Password : null;
                Properties.Settings.Default.LastCertificatePswd = (checkBox_rememberCertificatePswd.IsChecked == true) ? certPassword.Password : null;

                Properties.Settings.Default.Save();

                string certPass = certPassword.Password;
                if (certPass != null && certPass == "")
                    certPass = null;

                X509Certificate2 certificate = null;

                try
                {
                    certificate = new X509Certificate2(certFilename.Text, certPass);
                }
                catch (CryptographicException ce)
                {
                    MessageBox.Show("Error:\n" + ce.Message);
                    wizard.SelectedPage = credentialsPage;
                    return;
                }

                int totalFragments = (int)GeneralDataSource.GeneralDataSourceSingleton.CnbData.TotalFragments;

                ServicePointManager.ServerCertificateValidationCallback = MyValidationCallback;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

                submissionProgressBar.Minimum = 0;
                submissionProgressBar.Maximum = totalFragments - 1;
                submissionProgressBar.Value = 0;

                submissionPage.CancelButtonEnabled = false;
                submissionPage.BackButtonEnabled = false;
                submissionPage.NextButtonEnabled = false;

                string submissionUsername = username.Text;
                string submissionPassword = password.Password;

                // initialize background worker
                if (submissionWorker == null)
                {
                    submissionWorker = new BackgroundWorker();
                    submissionWorker.WorkerReportsProgress = true;
                    submissionWorker.DoWork += delegate(object sndr, DoWorkEventArgs eventArgs)
                    {
                        submissionResult = CnbConnection.SubmitData(GeneralDataSource.GeneralDataSourceSingleton.CnbData, certificate, submissionUsername, submissionPassword, delegate(int progress)
                        {
                            submissionWorker.ReportProgress(progress);
                        });



                    };
                    submissionWorker.ProgressChanged += delegate(object sndr, ProgressChangedEventArgs eventArgs)
                    {
                        SetSubmissionProgressTextInMainThread("Submitting fragment " + (eventArgs.ProgressPercentage+1) + " of " + GeneralDataSource.GeneralDataSourceSingleton.CnbData.TotalFragments);
                        submissionProgressBar.IsIndeterminate = false;
                        submissionProgressBar.Value = eventArgs.ProgressPercentage;
                    };
                    submissionWorker.RunWorkerCompleted += delegate(object sndr, RunWorkerCompletedEventArgs eventArgs)
                    {
                        SetSubmissionProgressTextInMainThread("Done");

                        // re-enable the buttons now that the processing is complete
                        submissionPage.CancelButtonEnabled = null;
                        submissionPage.BackButtonEnabled = null;
                        submissionPage.NextButtonEnabled = null;
                        simpleProcessingBackgroundWorker = null;
                        
                        // complain about error, if any
                        if (submissionResult != null)
                        {
                            MessageBox.Show(submissionResult, "CNB Submission Error");

                            wizard.SelectedPage = welcomePage;
                            return;
                        }
                    };
                }

                // Start the background work
                submissionWorker.RunWorkerAsync();
            }
            else if (e.NewSelectedPage == resultPage)
            {
                if (fileName != null && fileName != "")
                    resultFilename.Text = fileName;

                UpdateResultButtonState();
            }
        }

        private BackgroundWorker simpleProcessingBackgroundWorker;
        private BackgroundWorker submissionWorker;
        private int progressMin = 0;
        private int progressMax = 0;
        private int progressCur = 0;
        private string fileName = null;

        /// <summary>
        /// Processes input data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startProcessingButton_Click(object sender, RoutedEventArgs e)
        {
            // request a file name
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Data Files (*.xml)|*.xml|All Files|*.*";

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastDirectoryXmlFile))
                ofd.InitialDirectory = Properties.Settings.Default.LastDirectoryXmlFile;

            startProcessingButton.IsEnabled = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = ofd.FileName;
                FileInfo fi = new FileInfo(ofd.FileName);
                Properties.Settings.Default.LastDirectoryXmlFile = fi.Directory.ToString();
            }
            else
                return;

            // Disable the buttons while processing occurs
            startProcessingButton.IsEnabled = false;
            processingPage.CancelButtonEnabled = false;
            processingPage.BackButtonEnabled = false;
            processingPage.NextButtonEnabled = false;
            progressBar.Value = 0;


            if (!fileName.EndsWith(".xml"))
                fileName = @"C:\projekty\XtbCnb\XtbCnb\MOKAS42_31052010_377521.xml";

            // Initialize the background worker
            if (simpleProcessingBackgroundWorker == null)
            {
                simpleProcessingBackgroundWorker = new BackgroundWorker();
                simpleProcessingBackgroundWorker.WorkerReportsProgress = true;

                simpleProcessingBackgroundWorker.DoWork += delegate(object sndr, DoWorkEventArgs eventArgs)
                {
                    SetProgressTextInMainThread("Loading file", true);

                    try
                    {
                        GeneralDataSource.GeneralDataSourceSingleton.CnbData.xmlDataAreValid = true;        // default value - xml file is valid
                        GeneralDataSource.GeneralDataSourceSingleton.CnbData.LoadData(fileName, DelegateSetMinMax, DelegateSetCurrent);
                    }
                    catch (Exception e2)
                    {
                        // no data in data file!
                        if (e2.Message == "EXC_NO_DATA")
                        {
                            MessageBoxResult mbres = MessageBox.Show("This file doesn't contain any data.\n\nForce send?", "Suspicious Data Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (mbres == MessageBoxResult.Yes)
                            {
                                
                                GeneralDataSource.GeneralDataSourceSingleton.CnbData.xmlDataAreValid = true;        // xml file is valid

                                Application.Current.Dispatcher.Invoke(
                                  System.Windows.Threading.DispatcherPriority.Normal,
                                  new Action(
                                    delegate()
                                    {
                                        wizard.SelectedPage = credentialsPage;
                                    }
                                ));

                                return;
                            }

                        }

                        GeneralDataSource.GeneralDataSourceSingleton.CnbData.xmlDataAreValid = false;               // xml file is invalid

                        MessageBox.Show("Error loading XML:\n" + e2.Message);

                        Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                        {
                            startProcessingButton.IsEnabled = false;
                            processingPage.CancelButtonEnabled = true;
                            processingPage.BackButtonEnabled = true;
                            processingPage.NextButtonEnabled = false;
                            progressBar.Value = 0;
                            progressBar.IsIndeterminate = false;
                            SetProgressTextInMainThread("", true);
                            progressTextBlock.Text = "Try again";
                        }));
                    }
                };

                simpleProcessingBackgroundWorker.ProgressChanged += delegate(object sndr, ProgressChangedEventArgs eventArgs)
                {
                    SetProgressTextInMainThread("Processing...");
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = eventArgs.ProgressPercentage;
                };

                simpleProcessingBackgroundWorker.RunWorkerCompleted += delegate(object sndr, RunWorkerCompletedEventArgs eventArgs)
                {
                    // Re-enable the buttons now that the processing is complete
                    if (GeneralDataSource.GeneralDataSourceSingleton.CnbData.xmlDataAreValid)
                    {
                        progressBar.Value = progressBar.Maximum;
                        SetProgressTextInMainThread("Processing complete", false);

                        processingPage.CancelButtonEnabled = null;
                        processingPage.BackButtonEnabled = null;
                        processingPage.NextButtonEnabled = null;
                    }

                    progressBar.Value = progressBar.Maximum;
                    startProcessingButton.IsEnabled = true;
                    simpleProcessingBackgroundWorker = null;

                    resultDetail.Text = "";
                    GeneralDataSource.GeneralDataSourceSingleton.ResultHighlights = null;
                        //                    dp.XPath = "/VYDANI/*[not(self::DATA)]";
                };
            }

            // Start the background work
            simpleProcessingBackgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Method that will be passed to CnbData.Load as a delegate that is called when processing bounds are known.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        private void DelegateSetMinMax(int min, int max)
        {
            progressMin = min;
            progressMax = max;
        }

        /// <summary>
        /// Method that will be passed to CnbData.Load as a delegate that is called when progress is achieved
        /// </summary>
        /// <param name="cur"></param>
        private void DelegateSetCurrent(int cur)
        {
            progressCur = cur;
            int pct = ((cur - progressMin + 1) * 100) / (progressMax - progressMin);
            Debug.WriteLine("Reporting progress: " + pct);
            simpleProcessingBackgroundWorker.ReportProgress(pct);
        }

        /// <summary>
        /// Sets a loading progress bar's value, but uses the main thread (which is required).
        /// May also optionally set the progress bar to indeterminate state.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="setIndeterminate"></param>
        private void SetProgressTextInMainThread(string text, bool setIndeterminate=false)
        {
            Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            progressTextBlock.Text = text;
                            if (setIndeterminate)
                                progressBar.IsIndeterminate = true;
                        }
                    ));
        }

        private void SetSubmissionProgressTextInMainThread(string text, bool setIndeterminate = false)
        {
            Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            submissionTextBlock.Text = text;
                            if (setIndeterminate)
                                submissionProgressBar.IsIndeterminate = true;
                        }
                    ));
        }

        private void TotallySure_Checked(object sender, RoutedEventArgs e)
        {
            confirmationPage.NextButtonEnabled = true;
        }

        private void TotallySure_Unchecked(object sender, RoutedEventArgs e)
        {
            confirmationPage.NextButtonEnabled = false;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            GeneralDataSource.GeneralDataSourceSingleton.CnbData = new CnbData();
            wizard.SelectedPage = welcomePage;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // request a file name
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Certificates|*.pem;*.cer;*.crt;*.der;*.p7b;*.p7c;*.p12;*.pfx|All Files|*.*";

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastDirectoryCertificate))
                ofd.InitialDirectory = Properties.Settings.Default.LastDirectoryCertificate;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                certFilename.Text = ofd.FileName;
                FileInfo fi = new FileInfo(ofd.FileName);
                Properties.Settings.Default.LastDirectoryCertificate = fi.Directory.ToString();
                Properties.Settings.Default.LastPathCertificate = ofd.FileName;
            }


            credentialsNextButtonCheck();
        }

        /// <summary>
        /// Sets the credential input page's Next button enabled/disabled state based on the form fill-in status.
        /// </summary>
        private void credentialsNextButtonCheck()
        {
            credentialsPage.NextButtonEnabled = username.Text != null && username.Text.Length > 2 && password.Password != null && password.Password.Length > 2 && certFilename.Text != null && certFilename.Text.Length > 3;
        }

        private void username_TextChanged(object sender, TextChangedEventArgs e)
        {
            credentialsNextButtonCheck();
        }

        private void password_TextInput(object sender, TextCompositionEventArgs e)
        {
            credentialsNextButtonCheck();
        }

        private void password_KeyUp(object sender, KeyEventArgs e)
        {
            credentialsNextButtonCheck();
        }

        private void password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            credentialsNextButtonCheck();
        }

        private void Hyperlink_Click_1(object sender, RoutedEventArgs e)
        {
            wizard.SelectedPage = resultPage;
        }


        private BackgroundWorker checkBackgroundWorker = null;


        /// <summary>
        /// Sets a loading progress bar's value, but uses the main thread (which is required).
        /// May also optionally set the progress bar to indeterminate state.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="setIndeterminate"></param>
        private void SetProgressStateForCheck(string text, int progressValue, bool setIndeterminate = false, bool isVisible = true)
        {
            Application.Current.Dispatcher.Invoke(
                      System.Windows.Threading.DispatcherPriority.Normal,
                      new Action(
                        delegate()
                        {
                            progressBarCheckResult.Value = progressValue;

                            labelCheckResultProgress.Content = text;
                            if (setIndeterminate)
                                progressBarCheckResult.IsIndeterminate = true;

                            if (isVisible)
                            {
                                if (!progressBarCheckResult.IsVisible)
                                    progressBarCheckResult.Visibility = Visibility.Visible;
                                if (!labelCheckResultProgress.IsVisible)
                                    labelCheckResultProgress.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                if (progressBarCheckResult.IsVisible)
                                    progressBarCheckResult.Visibility = Visibility.Hidden;
                                if (!labelCheckResultProgress.IsVisible)
                                    labelCheckResultProgress.Visibility = Visibility.Hidden;
                            }
                        }
                    ));
        }

        private string tmpFilename;
        private string tmpUsername, tmpPassword;

        // find out submission processing result
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            XmlDocument doc;

            // invoke a login dialog if login/pass not entered in wizard already
            if (username.Text == null || password.Password == null || username.Text == "" || password.Password == "")
            {
                LoginPassWindow lpw = new LoginPassWindow();
                bool? res = lpw.ShowDialog();

                if (lpw.username.Text == null || lpw.username.Text=="" || lpw.password.Password == null || lpw.password.Password == "")
                    return;

                username.Text = lpw.username.Text;
                password.Password = lpw.password.Password;
            }

            SetProgressStateForCheck("Initialize ...", 0);

            tmpUsername = username.Text;
            tmpPassword = password.Password;
            tmpFilename = resultFilename.Text;

            resultPage.CancelButtonEnabled = false;
            resultPage.BackButtonEnabled = false;
            resultPage.NextButtonEnabled = false;

            resultDetail.Text = "";
            GeneralDataSource.GeneralDataSourceSingleton.ResultHighlights = null;

            // Initialize the background worker
            if (checkBackgroundWorker == null)
            {
                checkBackgroundWorker = new BackgroundWorker();
                checkBackgroundWorker.WorkerReportsProgress = true;

                checkBackgroundWorker.DoWork += delegate(object sndr, DoWorkEventArgs eventArgs)
                {

                    SetProgressStateForCheck("Loading XML file...", 1, false, true);

                    // take the already loaded file if it's the one chosen or load another

                    if (tmpFilename == fileName)
                        doc = GeneralDataSource.GeneralDataSourceSingleton.CnbData.Xml;
                    else
                    {
                        doc = new XmlDocument();
                        //                doc.XmlResolver = new EmbeddedResourceResolver();
                        Stream stream = File.OpenRead(tmpFilename);
                        doc.Load(stream);
                        stream.Close();
                    }

                    SetProgressStateForCheck("Connecting to CNB...", 50);

                    // call the ws manually
                    string resS = CnbConnection.GetProcessingResult(doc, this.GetType(), tmpUsername, tmpPassword);

                    SetProgressStateForCheck("Processing result...", 75);

                    Application.Current.Dispatcher.Invoke(
                          System.Windows.Threading.DispatcherPriority.Normal,
                          new Action(
                            delegate()
                            {
                                resultDetail.Text = (resS == null) ? "" : resS;
                            }
                        ));

//                    resultDetail.Text = (resS == null) ? "" : resS;

                    GeneralDataSource.GeneralDataSourceSingleton.ResultHighlights = null;

                    if (resS == null)
                        return;

                    
                    // response wasn't base64 encoded (only show detail info)
                    if (resS.StartsWith("RAW RESPONSE:"))
                        return;

                    // and parse the results
                    XmlDocument resDoc;
                    try
                    {
                        resDoc = new XmlDocument();
                        //                resDoc.XmlResolver = new EmbeddedResourceResolver();
                        resDoc.LoadXml(resS);
                    }
                    catch (Exception ee)
                    {
                        return;
                    }

                    XmlNode node;
                    List<KeyValuePair<string, string>> hili = new List<KeyValuePair<string, string>>();
                    XmlNamespaceManager nsMan = new XmlNamespaceManager(resDoc.NameTable);
                    nsMan.AddNamespace("a", "www.ewi.ws");

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:ErrorLog/a:Status", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Status", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:ErrorLog/a:ErrorCode", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Error Code", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:ErrorLog/a:ErrorText", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Error Text", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vyskyt/a:StavKod", nsMan);
                    if (node != null)
                    {
                        int t;
                        if (int.TryParse(node.InnerText, out t))
                        {
                            string s;
                            if (CnbConnection.VyskytStavKod.ContainsKey(t))
                                s = CnbConnection.VyskytStavKod[t];
                            else
                                s = t.ToString();

                            hili.Add(new KeyValuePair<string, string>("Vyskyt StavKod", s));
                        }
                    }

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vyskyt/a:Stav", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Vyskyt Status", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:StavKod", nsMan);
                    if (node != null)
                    {
                        int t;
                        if (int.TryParse(node.InnerText, out t))
                        {
                            string s;
                            if (CnbConnection.VydaniStavKod.ContainsKey(t))
                                s = CnbConnection.VydaniStavKod[t];
                            else
                                s = t.ToString();

                            hili.Add(new KeyValuePair<string, string>("Vydani StavKod", s));
                        }
                    }

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:Stav", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Vydani Status", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:StavOd", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Stav Od", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:DatumPrijmu", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Datum Prijmu", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:Druh", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Druh", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:ChybaZpracovani", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("Chyba Zpracovani", node.InnerText));

                    node = resDoc.SelectSingleNode("/a:EwiWSResult/a:VydaniSeznam/a:Vydani/a:ChybnyKrokKontroly", nsMan);
                    if (node != null)
                        hili.Add(new KeyValuePair<string, string>("ChybnyKrokKontroly", node.InnerText));

                    if (hili.Count > 0)
                        GeneralDataSource.GeneralDataSourceSingleton.ResultHighlights = hili.ToArray();

                };

                checkBackgroundWorker.ProgressChanged += delegate(object sndr, ProgressChangedEventArgs eventArgs)
                {
//                    SetProgressStateForCheck("Processing...", eventArgs.ProgressPercentage);
                };

                checkBackgroundWorker.RunWorkerCompleted += delegate(object sndr, RunWorkerCompletedEventArgs eventArgs)
                {

                    SetProgressStateForCheck("", 0, false, false);

                    checkBackgroundWorker = null;

                    resultPage.CancelButtonEnabled = null;
                    resultPage.BackButtonEnabled = null;
                    resultPage.NextButtonEnabled = null;
                };
            }

            // Start the background work
            checkBackgroundWorker.RunWorkerAsync();
        }

        private void UpdateResultButtonState()
        {
            resultButton.IsEnabled = resultFilename.Text != null && resultFilename.Text != "";
        }

        // load xml to return its processing status button
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // request a file name
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Data Files (*.xml)|*.xml|All Files|*.*";

            startProcessingButton.IsEnabled = true;

            string fName;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fName = ofd.FileName;
            else
                return;

            if (!fName.EndsWith(".xml"))
                fName = @"C:\projekty\XtbCnb\XtbCnb\MOKAS42_31052010_377521.xml";

            resultFilename.Text = fName;
            UpdateResultButtonState();

            LoginPassWindow lpw = new LoginPassWindow();
        }

        private void Hyperlink_Click_2(object sender, RoutedEventArgs e)
        {
            // test
            Log.LogAsText("Test logovani");
            Log.LogAsFile("Kviksi kabu kobalsi riremacha epofi", Log.CurrFilename()+"2.xml");
        }
    }
}
