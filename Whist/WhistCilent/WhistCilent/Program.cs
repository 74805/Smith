using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WhistCilent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            WhistClient whistclient = null;
            try
            {
                whistclient = new WhistClient();
                Application.Run(whistclient);
            }
            catch
            {
                MessageBox.Show("Whist server does not currently work");
                if (whistclient != null)
                {
                    whistclient.Close();
                }
            }
        }
    }
}
