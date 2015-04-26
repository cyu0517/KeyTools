﻿using System;
using System.Management;
using System.Net.NetworkInformation;
using System.Windows;

namespace MachineCodeGen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnCreate_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var codeString = string.Empty;

                try
                {
                    var mc = new ManagementClass("Win32_BIOS");
                    var moc = mc.GetInstances();

                    foreach (ManagementObject mo in moc)
                    {
                        codeString = mo.Properties["SerialNumber"].Value.ToString();
                        break;
                    }

                    moc.Dispose();

                    var adapters = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var adapter in adapters)
                    {
                        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            codeString += adapter.GetPhysicalAddress();
                        }
                    }

                    mc = new ManagementClass("Win32_Processor");
                    moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        codeString += mo.Properties["ProcessorId"].Value.ToString();
                        break;
                    }

                    moc.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                TbMachineCode.Text = Md5.GetStringMd5(codeString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnCopy_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetDataObject(TbMachineCode.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
