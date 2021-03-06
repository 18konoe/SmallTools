﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using UtilitiesLib;

namespace AutoHostsUpdateService
{
    public partial class AutoHostsUpdateService : ServiceBase
    {
        private IEnumerable<string> internalHostsSaveTargetCollection; // 内部Hostsファイルパス
        private IEnumerable<string> externalHostsSaveTargetCollection; // 外部Hostsファイルパス

        private bool IsApplyInExternalHosts { get; set; }   // 外部HostsファイルにIP情報を適用するか
        private bool IsUpdateInternalHosts { get; set; }    // ローカルのHostsファイルを更新するか
        private bool IsNetworkAuthentication { get; set; }  // ネットワーク認証をするか

        private string NetworkAuthenticationUsername { get; set; } // ネットワーク認証のユーザー名
        private string NetworkAuthenticationPassword { get; set; } // ネットワーク認証のパスワード

        private string HostName { get; set; }   // ホスト名
        private string IpAddress { get; set; }  // IP
        private string HostsLine { get; set; }  // Hostsのフォーマット

        private object LockObject { get; }                      // ロックオブジェクト
        private DateTime NextExecutionDateTime { get; set; }    // 次回ポーリング時刻
        private TimeSpan PollingInterval { get; }               // ポーリング間隔

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public AutoHostsUpdateService()
        {
            InitializeComponent();

            IsApplyInExternalHosts = false;
            IsUpdateInternalHosts = false;
            IsNetworkAuthentication = false;
            NetworkAuthenticationUsername = string.Empty;
            NetworkAuthenticationPassword = string.Empty;
            HostName = string.Empty;
            IpAddress = string.Empty;
            HostsLine = string.Empty;

            NextExecutionDateTime = DateTime.MinValue;
            PollingInterval = new TimeSpan(0, 30, 0);

            LockObject = new Object();
        }

        /// <summary>
        /// サービス開始時に実行される
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            timer.Enabled = true;
        }

        /// <summary>
        /// レジストリ情報を読み込む
        /// </summary>
        private void LoadRegistry()
        {
            string strInternalHostsSaveTarget = RegistryUtility.StrRegistryReadValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrInternalHostsTargetRegistryName);
            string strExternalHostsSaveTarget = RegistryUtility.StrRegistryReadValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrExternalHostsTargetRegistryName);

            internalHostsSaveTargetCollection = strInternalHostsSaveTarget.Split(new char[] { ';' }).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));
            externalHostsSaveTargetCollection = strExternalHostsSaveTarget.Split(new char[] { ';' }).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));

            NetworkAuthenticationUsername = RegistryUtility.StrRegistryReadValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrNetworkAuthenticationUsernameName);
            NetworkAuthenticationPassword = CryptUtility.DecryptString(RegistryUtility.RGBRegistryReadBinaryValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrNetworkAuthenticationPasswordName));

            int value = 0;

            if (RegistryUtility.FRegistryReadDwordValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrIsApplyInExternalHostsRegistryName, out value) == true)
            {
                IsApplyInExternalHosts = (value == 0) ? false : true;
            }

            if (RegistryUtility.FRegistryReadDwordValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrIsUpdateInternalHostsRegistryName, out value) == true)
            {
                IsUpdateInternalHosts = (value == 0) ? false : true;
            }

            if (RegistryUtility.FRegistryReadDwordValue(Microsoft.Win32.RegistryHive.LocalMachine, LocalResources.m_gstrAutoHostsUpdateServiceRegistryPath, LocalResources.m_gstrIsNetworkAuthenticationName, out value) == true)
            {
                IsNetworkAuthentication = (value == 0) ? false : true;
            }
        }

        /// <summary>
        /// サービス停止時に実行される
        /// </summary>
        protected override void OnStop()
        {
            timer.Enabled = false;
        }

        private void eventLog_EntryWritten(object sender, EntryWrittenEventArgs e)
        {
        }

        private string GetIpFromHost(string hostName)
        {
            string ipAddress = string.Empty;
            IPAddress[] ipAdrList = Dns.GetHostAddresses(hostName);

            foreach (IPAddress address in ipAdrList)
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddress = address.ToString();
                }
            }
            return ipAddress;
        }

        /// <summary>
        /// タイマー実行される関数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            LoadRegistry();

            lock (LockObject)
            {
                if (DateTime.Now > NextExecutionDateTime)
                {
                    bool fnetworkCapability = true;

                    try
                    {
                        // ホスト名取得
                        HostName = Dns.GetHostName();

                        // IP取得
                        IpAddress = GetIpFromHost(HostName);

                        // Hostsのフォーマットに変換
                        HostsLine = IpAddress + " " + HostName;
                    }
                    catch(Exception)
                    {
                        fnetworkCapability = false;
                        eventLog.WriteEntry("Cannot get the Hostname and IP.", EventLogEntryType.Error);
                    }

                    if (fnetworkCapability)
                    {
                        
                        foreach (string strExternalHostsSaveTarget in externalHostsSaveTargetCollection)
                        {
                            string externalHostsStrings = string.Empty;
                            // 外部Hostsがネットワーク上か調べる
                            string externalHostsSaveTargetFolderPath = Path.GetDirectoryName(strExternalHostsSaveTarget);
                            NetworkUtility networkUtility = new NetworkUtility(externalHostsSaveTargetFolderPath);
                            // 外部Hostsがネットワーク上だった場合
                            if (networkUtility.CheckNetworkPath())
                            {
                                eventLog.WriteEntry(externalHostsSaveTargetFolderPath + " is network path.", EventLogEntryType.Information);
                                // ユーザーを偽装する
                                using (NativeMethods.GetImpersonationContext("NetworkService", "NT AUTHORITY", ""))
                                {
                                    bool isSuccessNetworkConnect;
                                    // ユーザー情報に応じて接続を試みる
                                    if (IsNetworkAuthentication)
                                    {
                                        // ユーザー名を"\"で分割
                                        IEnumerable<string> UsernameCollection = NetworkAuthenticationUsername.Split(new char[] { '\\' }).Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));

                                        // ドメインが入力されていた場合
                                        if (UsernameCollection.Count() == 2)
                                        {
                                            isSuccessNetworkConnect = networkUtility.Connect(externalHostsSaveTargetFolderPath, NetworkAuthenticationUsername, NetworkAuthenticationPassword);
                                        }
                                        // ドメインが入力されていなかった場合
                                        else if (UsernameCollection.Count() == 1)
                                        {
                                            isSuccessNetworkConnect = networkUtility.Connect(externalHostsSaveTargetFolderPath, HostName+"\\"+NetworkAuthenticationUsername, NetworkAuthenticationPassword);
                                        }
                                        // それ以外の場合は中止
                                        else
                                        {
                                            isSuccessNetworkConnect = false;
                                        }
                                    }
                                    else
                                    {
                                        isSuccessNetworkConnect = networkUtility.Connect();
                                    }
                                    // 接続成功の場合
                                    if (isSuccessNetworkConnect)
                                    {
                                        eventLog.WriteEntry("Succeeded connecting to " + externalHostsSaveTargetFolderPath, EventLogEntryType.Information);
                                        // Hostsをアップデート
                                        externalHostsStrings = UpdateExternalHosts(strExternalHostsSaveTarget);

                                        // ネットワーク切断と破棄
                                        if (isSuccessNetworkConnect)
                                        {
                                            networkUtility.Disconnect();
                                            eventLog.WriteEntry("Disconnected to " + externalHostsSaveTargetFolderPath, EventLogEntryType.Information);
                                        }
                                        networkUtility = null;
                                    }
                                    // 接続失敗の場合
                                    else
                                    {
                                        eventLog.WriteEntry("Failed connecting to " + externalHostsSaveTargetFolderPath, EventLogEntryType.Information);
                                    }
                                }
                            }
                            // 外部Hostsがローカルだった場合
                            else
                            {
                                eventLog.WriteEntry(externalHostsSaveTargetFolderPath + " is local path.", EventLogEntryType.Information);
                                // 外部Hostsをアップデート
                                externalHostsStrings = UpdateExternalHosts(strExternalHostsSaveTarget);
                            }
                            // 外部Hostsから取得した文字列がNullか空でないか確認
                            if (!string.IsNullOrEmpty(externalHostsStrings))
                            {
                                // 内部Hostsをアップデート
                                UpdateInternalHosts(externalHostsStrings);
                            }
                        }
                    }
                    UpdateNextPoll();
                }
            }

        }

        private string UpdateExternalHosts(string strExternalHostsSaveTarget)
        {
            string result = string.Empty;

            // 外部Hostsファイルのインスタンス
            EditHosts externalHosts = new EditHosts(strExternalHostsSaveTarget, eventLog);

            // 外部ファイルへHosts情報を適用する場合
            if (IsApplyInExternalHosts)
            {
                eventLog.WriteEntry("externalHosts.MargeString(" + HostsLine + ", ' ', 1);", EventLogEntryType.Information);
                externalHosts.MargeString(HostsLine, ' ', 1);
            }

            result = externalHosts.readAllStrings();

            return result;
        }

        private void UpdateInternalHosts(string hostsStrings)
        {
            // 外部ファイルから内部のHostsを更新する場合
            if (IsUpdateInternalHosts)
            {
                eventLog.WriteEntry("Update Internal Hosts", EventLogEntryType.Information);
                foreach (string strInternalHostsSaveTarget in internalHostsSaveTargetCollection)
                {
                    // 内部Hostsファイルのインスタンス
                    EditHosts internalHosts = new EditHosts(strInternalHostsSaveTarget, eventLog);
                    eventLog.WriteEntry("EditHosts internalHosts = EditHosts.CreateEditHosts(strInternalHostsSaveTarget);", EventLogEntryType.Information);

                    eventLog.WriteEntry("internalHosts.MargeString(" + hostsStrings + ", ' ', 1);", EventLogEntryType.Information);
                    internalHosts.MargeString(hostsStrings, ' ', 1);

                }
            }
        }

        /// <summary>
        /// 次回実行時刻を更新します
        /// </summary>
        private void UpdateNextPoll()
        {
            NextExecutionDateTime = DateTime.Now.Add(PollingInterval);
        }
    }
}
