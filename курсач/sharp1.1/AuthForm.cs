﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace networks
{
    public partial class AuthForm : Form
    {

        //string login = "user";
        string pass = "qwerty";
        bool status = false;


        public AuthForm()
        {
            InitializeComponent();
        }

        public void name_of_user(ref string name)
        {
            name = textBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (/*textBox1.Text == login && */ textBox2.Text == pass)
            {
                status = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Ошибка авторизации!");
            }
        }

        private void auth_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!status)
                Application.Exit();
        }
        
    }
}
