﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UtilitiesLib;

namespace AutoHostsUpdateGUI
{
    partial class MainWindow
    {
        /// <summary>
        /// Clickイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveRegistry();
            ApplyButton.IsEnabled = false;
        }
        /// <summary>
        /// Clickイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serviceStartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_serviceController != null)
            {
                _serviceController.Dispose();
            }
            _serviceController = new ServiceController("AutoHostsUpdateService");

            switch (_serviceController.Status)
            {
                case ServiceControllerStatus.Stopped:
                    // サービスは実行されていません
                    _serviceController.Start();
                    ServiceStartStopButton.IsEnabled = false;
                    break;
                case ServiceControllerStatus.StartPending:
                    // サービスは開始中です
                    
                    break;
                case ServiceControllerStatus.StopPending:
                    // サービスは停止中です
                    
                    break;
                case ServiceControllerStatus.Running:
                    // サービスは実行中です
                    _serviceController.Stop();
                    ServiceStartStopButton.IsEnabled = false;
                    break;
                case ServiceControllerStatus.ContinuePending:
                    // サービスの継続は保留中です
                    
                    break;
                case ServiceControllerStatus.PausePending:
                    // サービスは実行されていません
                    
                    break;
                case ServiceControllerStatus.Paused:
                    // サービスの一時中断は保留中です

                    break;
                default:
                    // それ以外の状態
                    
                    break;
            }

        }
        /// <summary>
        /// Checkedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void internalHostsPathCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsUpdateInternalHosts = true;
            ApplyButton.IsEnabled = true;
        }

        /// <summary>
        /// TextChangedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void internalHostsPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (InternalHostsPathTextBox.Text != null)
            {
                InternalHostsSaveTarget = InternalHostsPathTextBox.Text;
            }

            if (IsUpdateInternalHosts && System.Text.RegularExpressions.Regex.IsMatch(InternalHostsSaveTarget, @"^[;]*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                IsEnoughRequiredInternalHosts = false;
                ApplyButton.IsEnabled = false;
            }
            else
            {
                IsEnoughRequiredInternalHosts = true;
            }

            if (CheckEnoughRequiredValue())
            {
                ApplyButton.IsEnabled = true;
            }
        }
        /// <summary>
        /// Checkedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void externalHostsPathCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsApplyInExternalHosts = true;
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// TextChangedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void externalHostsPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ExternalHostsPathTextBox.Text != null)
            {
                ExternalHostsSaveTarget = ExternalHostsPathTextBox.Text;
            }

            if (IsApplyInExternalHosts && System.Text.RegularExpressions.Regex.IsMatch(ExternalHostsSaveTarget, @"^[;]*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                IsEnoughRequiredExternalHosts = false;
                ApplyButton.IsEnabled = false;
            }
            else
            {
                IsEnoughRequiredExternalHosts = true;
            }

            if (CheckEnoughRequiredValue())
            {
                ApplyButton.IsEnabled = true;
            }
        }
        /// <summary>
        /// Checkedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void networkAuthenticationCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            IsNetworkAuthentication = true;
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// TextChangedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void networkAuthenticationUsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NetworkAuthenticationUsernameTextBox.Text != null)
            {
                NetworkAuthenticationUsername = NetworkAuthenticationUsernameTextBox.Text;
            }
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// Uncheckedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void internalHostsPathCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsUpdateInternalHosts = false;
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// Uncheckedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void externalHostsPathCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsApplyInExternalHosts = false;
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// Uncheckedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void networkAuthenticationCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            IsNetworkAuthentication = false;
            ApplyButton.IsEnabled = true;
        }
        /// <summary>
        /// PasswordChangedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void networkAuthenticationPasswordPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (NetworkAuthenticationPasswordPasswordBox.Password != null)
            {
                NetworkAuthenticationPassword = NetworkAuthenticationPasswordPasswordBox.Password;
            }

            ApplyButton.IsEnabled = true;

        }
        /// <summary>
        /// Clickイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFileInternalHostsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = Properties.Resources.OpenFileDialogFilter;
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                InternalHostsPathTextBox.Text = "";

                foreach (string strFilePath in openFileDialog.FileNames)
                {
                    InternalHostsPathTextBox.Text += strFilePath + ";";
                }
            }
        }
        /// <summary>
        /// Clickイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openFileExternalHostsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = Properties.Resources.OpenFileDialogFilter;
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                ExternalHostsPathTextBox.Text = "";

                foreach (string strFilePath in openFileDialog.FileNames)
                {
                    ExternalHostsPathTextBox.Text += strFilePath + ";";
                }
            }
        }
        private void MenuItem_Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.Filter = Properties.Resources.ImportFileDialogFilter;
            openFileDialog.Multiselect = false;
            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string fileName = openFileDialog.FileName;
                XmlResources xmlResources = XmlImportExportUtility.XmlDeserialize(fileName);
                if (xmlResources != null)
                {
                    SaveRegistryFromXmlResources(xmlResources);
                    LoadRegistry();
                    MessageBox.Show(Properties.Resources.ImportSuccessMessageBoxText, Properties.Resources.MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ImportErrorMessageBoxText, Properties.Resources.MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
        private void MenuItem_Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.Filter = Properties.Resources.ExportFileDialogFilter;
            saveFileDialog.AddExtension = true;
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.FileName = "AutoHostsUpdateConfig.xml";
            bool? result = saveFileDialog.ShowDialog();
            if (result == true)
            {
                string fileName = saveFileDialog.FileName;
                LoadRegistryToXmlResources();
                XmlResources xmlResources = LoadRegistryToXmlResources();
                if (XmlImportExportUtility.XmlSerialize(fileName, xmlResources))
                {
                    MessageBox.Show(Properties.Resources.ExportSuccessMessageBoxText, Properties.Resources.MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ExportErrorMessageBoxText, Properties.Resources.MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
