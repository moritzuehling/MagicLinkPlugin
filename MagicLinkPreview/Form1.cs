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

            textBox1.Text = "https://twitter.com/imgur/status/898891550138159104";
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


            var res = await MagicLinkPlugin.LinkHandler.GetContent(textBox1.Text);

            if (res != null)
                Clipboard.SetText(res);

            if (res == null)
                res = "(no result given)";

            webBrowser1.DocumentText =
                "<html><head><style>img { border-style:none; } </style></head><body style='font-family: Arial;'>" +
                "Server: " +
                res +
                "</body></html>";
        }
    }
}
