using PDFToTiffConverter.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// This project is licensed under the terms of the AGPL (GNU Affero General Public License)
/// </summary>
namespace PDFToTiffConverter.WindowsServiceHost
{
    public partial class Service1 : ServiceBase
    {
         
        public Service1()
        {
            InitializeComponent();
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
        }

        protected override void OnStart(string[] args)
        {
            Host.Start();
        }

        protected override void OnStop()
        {
            Host.Stop();
        }
    }
}
