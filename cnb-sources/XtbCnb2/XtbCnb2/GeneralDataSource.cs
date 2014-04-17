using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.DataGrid;
using System.Diagnostics;
using Microsoft.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Deployment;
using System.Deployment.Application;

namespace XtbCnb2
{
    public class GeneralDataSource : INotifyPropertyChanged
    {
        public GeneralDataSource()
        {
            // init version to clickonce ver or the regular version if not clickonce deployed
            if (ApplicationDeployment.IsNetworkDeployed)
                _version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            else
                _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private String _version;

        // we're presented as a singleton (devised by the oh great wpf guru)
        public static GeneralDataSource _GeneralDataSource = new GeneralDataSource();
        /// <summary>
        /// Use this singleton to access all data using DataContext
        /// </summary>
        public static GeneralDataSource GeneralDataSourceSingleton
        {
            get
            {
                return _GeneralDataSource;
            }
        }

        /// <summary>
        /// Welcome title text w/ version
        /// </summary>
        public string WelcomeInfo
        {
            get
            {
                return "Welcome to the XTB CNB submission wizard v" + _version;
            }
        }

        public KeyValuePair<string, string>[] DataHeaders
        {
            get
            {
                return CnbData.DataHeaders;
            }
        }

        // error overview
        private KeyValuePair<string, string>[] _resultHighlights = null;

        /// <summary>
        /// Error highlights access just to make data binding easier.
        /// </summary>
        public KeyValuePair<string, string>[] ResultHighlights
        {
            get
            {
                return _resultHighlights;
            }
            set
            {
                _resultHighlights = value;
                OnPropertyChanged("ResultHighlights");
            }
        }

        // CnbData
        public CnbData CnbData = new CnbData();

        public CnbData Data
        {
            get
            {
                return CnbData;
            }
        }

        public DocumentWindow win1 = null;

        public DocumentWindowCollection DataWindows
        {
            get
            {
                Debug.WriteLine("DataWindows invoked");

                if (CnbData == null)
                    return null;

                DocumentWindowCollection res = new DocumentWindowCollection();

                for (int i = 0; i < CnbData.JmenaOblasti.Count; i++)
                {
                    Debug.WriteLine("Adding data doc " + CnbData.JmenaOblasti[i]);

                    ThemedDataGrid grid = new ThemedDataGrid();
                    grid.DataContext = CnbData.Data[i];
                    grid.AutoGenerateColumns = false;
                    grid.Initialized += DataGrid_Initialized;

//                    System.Windows.Controls.Grid grid = new System.Windows.Controls.Grid();

                    DocumentWindow win = new DocumentWindow(null, "doc" + i, CnbData.JmenaOblasti[i], new BitmapImage(new Uri("/Resources/Images/TextDocument16.png", UriKind.Relative)), grid);

                    if (win1 == null)
                        win1 = win;

                    res.Add(win);
                }

                return res;
            }
        }

        public void DataGrid_Initialized(object sender, EventArgs e)
        {
            Debug.WriteLine("DataGrid_Initialized");

            ThemedDataGrid dataGrid1 = (ThemedDataGrid)sender;
            for (int i = 0; i < CnbData.COL_NAMES.Length; i++)
            {
                dataGrid1.Columns.Add(new DataGridTextColumn
                {
                    Header = CnbData.COL_NAMES[i],
                    Width = DataGridLength.Auto,
                    Binding = new Binding("[" + i.ToString() + "]"),
                    IsReadOnly = true
                });

            }

            // get index
            string s = (dataGrid1.Parent as DocumentWindow).Title;

            int j=0;
            for (; j<CnbData.JmenaOblasti.Count; j++)
                if (CnbData.JmenaOblasti[j] == s)
                    break;

            Debug.WriteLine("Setting data for grid #" + j);

            dataGrid1.ItemsSource = GeneralDataSource.GeneralDataSourceSingleton.Data.Data[j];
        }

    }
}
