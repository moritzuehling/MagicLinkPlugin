using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagicLinkPreview
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            webBrowser1.Navigate("about:blank");

            textBox1.Text = "https://gist.github.com/moritzuehling/823e257f5be86f570b163bb7d4d2fac9";
        }
        

        private async void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            e.Handled = true;

            webBrowser1.DocumentText =
                "<html><head><style>img { border-style:none; } </style></head><body style='font-family: Arial;'>" +
                "Loading..." +
                "</body></html>";


            var resArr = await MagicLinkPlugin.LinkHandler.GetContent(textBox1.Text);
            
            if (resArr != null && resArr.Length > 0)
                Clipboard.SetText(resArr[0]);

            if (resArr == null || resArr.Length == 0)
            {
                webBrowser1.DocumentText =
                    "<html><head><style>img { border-style:none; } </style></head><body style='font-family: Arial;'>" +
                    "(nothing to see)" +
                    "</body></html>";
                return;
            }
            

            string txt = "<html><head><style>img { border-style:none; } </style></head><body style='font-family: Arial;'>";
            foreach (var res in resArr)
            {
                txt += "[12:00] Server: ";
                txt += res;
                txt += "<hr>";
            }
                
            txt += "</body></html>";

            webBrowser1.DocumentText = txt;
        }
    }
}
