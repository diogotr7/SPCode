﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SPCode.Interop;
using SPCode.Utils;
using static SPCode.Interop.TranslationProvider;

namespace SPCode.UI.Windows
{
    public partial class ConfigWindow
    {
        private bool AllowChange;
        private bool NeedsSMDefInvalidation;

        private ICommand textBoxButtonFileCmd;
        private ICommand textBoxButtonFolderCmd;

        private static TextBox SelectedBox;

        private readonly string[] CompileMacros =
        {
            $"{{editordir}} {Translate("macro_editordir")}",
            $"{{scriptdir}} {Translate("macro_scriptdir")}",
            $"{{copydir}} {Translate("macro_copydir")}",
            $"{{scriptfile}} {Translate("macro_scriptfile")}",
            $"{{scriptname}} {Translate("macro_scriptname")}",
            $"{{pluginfile}} {Translate("macro_pluginfile")}",
            $"{{pluginname}} {Translate("macro_pluginname")}"
        };

        private readonly string[] CommandMacros =
        {
            $"{{plugins_reload}} {Translate("macro_plugins_reload")}",
            $"{{plugins_load}} {Translate("macro_plugins_load")}",
            $"{{plugins_unload}} {Translate("macro_plugins_unload")}"
        };

        public ConfigWindow()
        {
            InitializeComponent();
            Language_Translate();
            EvaluateRTL();
            if (Program.OptionsObject.Program_AccentColor != "Red" || Program.OptionsObject.Program_Theme != "BaseDark")
            {
                ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(Program.OptionsObject.Program_AccentColor),
                    ThemeManager.GetAppTheme(Program.OptionsObject.Program_Theme));
            }

            foreach (var config in Program.Configs)
            {
                ConfigListBox.Items.Add(new ListBoxItem { Content = config.Name });
            }

            SelectedBox = C_PreBuildCmd;

            ConfigListBox.SelectedIndex = Program.SelectedConfig;

            CompileMacros.ToList().ForEach(x => CMD_ItemC.Items.Add(x));
            CMD_ItemC.SelectionChanged += CompileMacros_OnClickedItem;
            CommandMacros.ToList().ForEach(x => Rcon_MenuC.Items.Add(x));
            Rcon_MenuC.SelectionChanged += CommandMacros_OnClickedItem;

            var item1 = new ComboBoxItem()
            {
                Visibility = Visibility.Collapsed,
                Content = Translate("Macros"),
            };
            var item2 = new ComboBoxItem()
            {
                Visibility = Visibility.Collapsed,
                Content = Translate("Macros"),
            };
            CMD_ItemC.Items.Insert(0, item1);
            Rcon_MenuC.Items.Insert(0, item2);
            CMD_ItemC.SelectedIndex = 0;
            Rcon_MenuC.SelectedIndex = 0;
        }

        public ICommand TextBoxButtonFolderCmd
        {
            set { }
            get
            {
                if (textBoxButtonFolderCmd == null)
                {
                    var cmd = new SimpleCommand
                    {
                        CanExecutePredicate = o => true,
                        ExecuteAction = o =>
                        {
                            if (o is TextBox box)
                            {
                                var dialog = new CommonOpenFileDialog
                                {
                                    IsFolderPicker = true
                                };
                                var result = dialog.ShowDialog();

                                if (result == CommonFileDialogResult.Ok)
                                {
                                    box.Text = dialog.FileName;
                                }
                            }
                        }
                    };
                    textBoxButtonFolderCmd = cmd;
                    return cmd;
                }

                return textBoxButtonFolderCmd;
            }
        }

        public ICommand TextBoxButtonFileCmd
        {
            set { }
            get
            {
                if (textBoxButtonFileCmd == null)
                {
                    var cmd = new SimpleCommand
                    {
                        CanExecutePredicate = o => true,
                        ExecuteAction = o =>
                        {
                            if (o is TextBox box)
                            {
                                var dialog = new OpenFileDialog
                                {
                                    Filter = Constants.ServerExecutableFilter,
                                    Multiselect = false,
                                    CheckFileExists = true,
                                    CheckPathExists = true,
                                    Title = Translate("SelectExe")
                                };
                                var result = dialog.ShowDialog();

                                Debug.Assert(result != null, nameof(result) + " != null");
                                if (result.Value)
                                {
                                    var fInfo = new FileInfo(dialog.FileName);
                                    if (fInfo.Exists)
                                    {
                                        box.Text = fInfo.FullName;
                                    }
                                }
                            }
                        }
                    };
                    textBoxButtonFileCmd = cmd;
                    return cmd;
                }

                return textBoxButtonFileCmd;
            }
        }

        private void ConfigListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadConfigToUI(ConfigListBox.SelectedIndex);
        }

        private void CompileMacros_OnClickedItem(object sender, SelectionChangedEventArgs e)
        {
            if (CMD_ItemC.SelectedItem is not string content)
            {
                return;
            }

            SelectedBox.AppendText(content.Substring(0, content.IndexOf('}') + 1));
            var item = new ComboBoxItem()
            {
                Visibility = Visibility.Collapsed,
                Content = Translate("Macros"),
            };
            CMD_ItemC.Items.Insert(0, item);
            CMD_ItemC.SelectedIndex = 0;
            SelectedBox.Focus();
            SelectedBox.Select(SelectedBox.Text.Length, 0);
        }

        private void CommandMacros_OnClickedItem(object sender, SelectionChangedEventArgs e)
        {
            if (Rcon_MenuC.SelectedItem is not string content)
            {
                return;
            }

            C_RConCmds.AppendText(content.Substring(0, content.IndexOf('}') + 1));
            var item = new ComboBoxItem()
            {
                Visibility = Visibility.Collapsed,
                Content = Translate("Macros"),
            };
            Rcon_MenuC.Items.Insert(0, item);
            Rcon_MenuC.SelectedIndex = 0;
            C_RConCmds.Focus();
            C_RConCmds.Select(C_RConCmds.Text.Length, 0);
        }

        private void BuildCommandsBoxes_OnFocus(object sender, RoutedEventArgs e)
        {
            SelectedBox = sender as TextBox;
        }

        private void LoadConfigToUI(int index)
        {
            if (index < 0 || index >= Program.Configs.Count)
            {
                return;
            }

            AllowChange = false;
            var c = Program.Configs[index];

            C_Name.Text = c.Name;
            C_SMDir.ItemsSource = c.SMDirectories;
            C_AutoCopy.IsChecked = c.AutoCopy;
            C_AutoUpload.IsChecked = c.AutoUpload;
            C_AutoRCON.IsChecked = c.AutoRCON;
            C_CopyDir.Text = c.CopyDirectory;
            C_ServerFile.Text = c.ServerFile;
            C_ServerArgs.Text = c.ServerArgs;
            C_PreBuildCmd.Text = c.PreCmd;
            C_PostBuildCmd.Text = c.PostCmd;
            C_OptimizationLevel.Value = c.OptimizeLevel;
            C_VerboseLevel.Value = c.VerboseLevel;
            C_DeleteAfterCopy.IsChecked = c.DeleteAfterCopy;
            C_FTPHost.Text = c.FTPHost;
            C_FTPUser.Text = c.FTPUser;
            C_FTPPW.Password = c.FTPPassword;
            C_FTPDir.Text = c.FTPDir;
            C_RConIP.Text = c.RConIP;
            C_RConPort.Text = c.RConPort.ToString();
            C_RConPW.Password = c.RConPassword;
            C_RConCmds.Text = c.RConCommands;
            AllowChange = true;
        }

        private void NewButton_Clicked(object sender, RoutedEventArgs e)
        {
            var c = new Config
            {
                Name = "New Config",
                Standard = false,
                OptimizeLevel = 2,
                VerboseLevel = 1,
                SMDirectories = new List<string>()
            };
            var configList = new List<Config>(Program.Configs) { c };
            Program.Configs = configList;
            ConfigListBox.Items.Add(new ListBoxItem { Content = Translate("NewConfig") });
        }

        private void DeleteButton_Clicked(object sender, RoutedEventArgs e)
        {
            var index = ConfigListBox.SelectedIndex;
            var c = Program.Configs[index];
            if (c.Standard)
            {
                this.ShowMessageAsync(Translate("CannotDelConf"),
                    Translate("YCannotDelConf"), MessageDialogStyle.Affirmative,
                    Program.MainWindow.MetroDialogOptions);
                return;
            }

            var configList = new List<Config>(Program.Configs);
            configList.RemoveAt(index);
            Program.Configs = configList;
            ConfigListBox.Items.RemoveAt(index);
            if (index == Program.SelectedConfig)
            {
                Program.SelectedConfig = 0;
            }

            ConfigListBox.SelectedIndex = 0;
        }

        private void AddSMDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var c = Program.Configs[ConfigListBox.SelectedIndex];

                if (c.SMDirectories.Contains(dialog.FileName))
                {
                    return;
                }

                try
                {
                    Directory.GetAccessControl(dialog.FileName);
                }
                catch (UnauthorizedAccessException)
                {
                    this.ShowMessageAsync(Translate("PermissionAccessError"),
                        Translate("PermissionAcessErrorMessage"),
                        MessageDialogStyle.Affirmative, Program.MainWindow.MetroDialogOptions);
                }

                c.SMDirectories.Add(dialog.FileName);
                C_SMDir.Items.Refresh();
                NeedsSMDefInvalidation = true;
            }
        }

        private void RemoveSMDirButton_Click(object sender, RoutedEventArgs e)
        {
            var c = Program.Configs[ConfigListBox.SelectedIndex];
            if (C_SMDir.SelectedItem == null)
            {
                return;
            }
            c.SMDirectories.Remove(C_SMDir.SelectedItem.ToString());
            C_SMDir.Items.Refresh();
            NeedsSMDefInvalidation = true;
        }

        private void C_Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            var Name = C_Name.Text;
            Program.Configs[ConfigListBox.SelectedIndex].Name = Name;
            ((ListBoxItem)ConfigListBox.SelectedItem).Content = Name;
        }

        private void C_CopyDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].CopyDirectory = C_CopyDir.Text;
        }

        private void C_ServerFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].ServerFile = C_ServerFile.Text;
        }

        private void C_ServerArgs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].ServerArgs = C_ServerArgs.Text;
        }

        private void C_PostBuildCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].PostCmd = C_PostBuildCmd.Text;
        }

        private void C_PreBuildCmd_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].PreCmd = C_PreBuildCmd.Text;
        }

        private void C_OptimizationLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].OptimizeLevel = (int)C_OptimizationLevel.Value;
        }

        private void C_VerboseLevel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].VerboseLevel = (int)C_VerboseLevel.Value;
        }

        private void C_AutoCopy_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoCopy.IsChecked != null, "C_AutoCopy.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoCopy = C_AutoCopy.IsChecked.Value;
        }

        public void C_AutoUpload_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoUpload.IsChecked != null, "C_AutoUpload.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoUpload = C_AutoUpload.IsChecked.Value;
        }

        public void C_AutoRCON_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_AutoUpload.IsChecked != null, "C_AutoUpload.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].AutoRCON = C_AutoRCON.IsChecked.Value;
        }

        private void C_DeleteAfterCopy_Changed(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Debug.Assert(C_DeleteAfterCopy.IsChecked != null, "C_DeleteAfterCopy.IsChecked != null");
            Program.Configs[ConfigListBox.SelectedIndex].DeleteAfterCopy = C_DeleteAfterCopy.IsChecked.Value;
        }

        private void C_FTPHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPHost = C_FTPHost.Text;
        }

        private void C_FTPUser_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPUser = C_FTPUser.Text;
        }

        private void C_FTPPW_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPPassword = C_FTPPW.Password;
        }

        private void C_FTPDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].FTPDir = C_FTPDir.Text;
        }

        private void C_RConIP_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConIP = C_RConIP.Text;
        }

        private void C_RConPort_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            if (!ushort.TryParse(C_RConPort.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var newPort))
            {
                newPort = 27015;
                C_RConPort.Text = "27015";
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConPort = newPort;
        }

        private void C_RConPW_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConPassword = C_RConPW.Password;
        }

        private void C_RConCmds_TextChanged(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
            {
                return;
            }

            Program.Configs[ConfigListBox.SelectedIndex].RConCommands = C_RConCmds.Text;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            if (NeedsSMDefInvalidation)
            {
                foreach (var config in Program.Configs)
                {
                    config.InvalidateSMDef();
                }
            }

            var configsList = new List<string>();
            foreach (ListBoxItem item in ConfigListBox.Items)
            {
                configsList.Add(item.Content.ToString());
            }

            // Check for empty named configs
            if (configsList.Any(x => string.IsNullOrEmpty(x)))
            {
                e.Cancel = true;
                this.ShowMessageAsync(Translate("ErrorSavingConfigs"),
                    Translate("EmptyConfigNames"), MessageDialogStyle.Affirmative,
                    Program.MainWindow.MetroDialogOptions);
                return;
            }

            // Check for duplicate names in the config list
            if (configsList.Count != configsList.Distinct().Count())
            {
                e.Cancel = true;
                this.ShowMessageAsync(Translate("ErrorSavingConfigs"),
                    Translate("DuplicateConfigNames"), MessageDialogStyle.Affirmative,
                    Program.MainWindow.MetroDialogOptions);
                return;
            }

            Program.MainWindow.FillConfigMenu();
            Program.MainWindow.ChangeConfig(Program.SelectedConfig);
            var outString = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true
            };
            using (var writer = XmlWriter.Create(outString, settings))
            {
                writer.WriteStartElement("Configurations");
                foreach (var c in Program.Configs)
                {
                    writer.WriteStartElement("Config");
                    writer.WriteAttributeString("Name", c.Name);
                    var SMDirOut = new StringBuilder();
                    foreach (var dir in c.SMDirectories)
                    {
                        SMDirOut.Append(dir.Trim() + ";");
                    }

                    writer.WriteAttributeString("SMDirectory", SMDirOut.ToString());
                    writer.WriteAttributeString("Standard", c.Standard ? "1" : "0");
                    writer.WriteAttributeString("CopyDirectory", c.CopyDirectory);
                    writer.WriteAttributeString("AutoCopy", c.AutoCopy ? "1" : "0");
                    writer.WriteAttributeString("AutoUpload", c.AutoUpload ? "1" : "0");
                    writer.WriteAttributeString("AutoRCON", c.AutoRCON ? "1" : "0");
                    writer.WriteAttributeString("ServerFile", c.ServerFile);
                    writer.WriteAttributeString("ServerArgs", c.ServerArgs);
                    writer.WriteAttributeString("PostCmd", c.PostCmd);
                    writer.WriteAttributeString("PreCmd", c.PreCmd);
                    writer.WriteAttributeString("OptimizationLevel", c.OptimizeLevel.ToString());
                    writer.WriteAttributeString("VerboseLevel", c.VerboseLevel.ToString());
                    writer.WriteAttributeString("DeleteAfterCopy", c.DeleteAfterCopy ? "1" : "0");
                    writer.WriteAttributeString("FTPHost", c.FTPHost);
                    writer.WriteAttributeString("FTPUser", c.FTPUser);
                    writer.WriteAttributeString("FTPPassword", ManagedAES.Encrypt(c.FTPPassword));
                    writer.WriteAttributeString("FTPDir", c.FTPDir);
                    writer.WriteAttributeString("RConIP", c.RConIP);
                    writer.WriteAttributeString("RConPort", c.RConPort.ToString());
                    writer.WriteAttributeString("RConPassword", ManagedAES.Encrypt(c.RConPassword));
                    writer.WriteAttributeString("RConCommands", c.RConCommands);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.Flush();
            }

            File.WriteAllText(PathsHelper.ConfigFilePath, outString.ToString());
            LoggingControl.LogAction($"Configs saved.", 2);
        }

        private void Language_Translate()
        {
            Title = Translate("Configs");
            AddSMDirButton.Content = Translate("Add");
            RemoveSMDirButton.Content = Translate("Remove");
            NewButton.Content = Translate("New");
            DeleteButton.Content = Translate("Delete");
            NameBlock.Text = Translate("Name");
            ScriptingDirBlock.Text = Translate("ScriptDir");
            CopyDirBlock.Text = Translate("CopyDir");
            ServerExeBlock.Text = Translate("ServerExe");
            ServerStartArgBlock.Text = Translate("serverStartArgs");
            PreBuildBlock.Text = Translate("PreBuildCom");
            PostBuildBlock.Text = Translate("PostBuildCom");
            OptimizeBlock.Text = Translate("OptimizeLvl");
            VerboseBlock.Text = Translate("VerboseLvl");
            C_AutoCopy.Content = Translate("AutoCopy");
            C_AutoUpload.Content = Translate("AutoUpload");
            C_AutoRCON.Content = Translate("AutoRCON");
            C_DeleteAfterCopy.Content = Translate("DeleteOldSMX");
            FTPHostBlock.Text = Translate("FTPHost");
            FTPUserBlock.Text = Translate("FTPUser");
            FTPPWBlock.Text = Translate("FTPPw");
            FTPDirBlock.Text = Translate("FTPDir");
            CMD_ItemC.Text = Translate("CMDLineCom");
            RConIPBlock.Text = Translate("RConIP");
            RConPortBlock.Text = Translate("RconPort");
            RConPWBlock.Text = Translate("RconPw");
            RConComBlock.Text = Translate("RconCom");
            Rcon_MenuC.Text = Translate("RConCMDLineCom");
            BackupConfigsButton.Content = Translate("BackupConfigs");
            LoadConfigsButton.Content = Translate("LoadConfigs");
        }

        private void EvaluateRTL()
        {
            ConfigsWindowMainGrid.FlowDirection = Program.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private class SimpleCommand : ICommand
        {
            public Predicate<object> CanExecutePredicate { get; set; }
            public Action<object> ExecuteAction { get; set; }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public void Execute(object parameter)
            {
                ExecuteAction?.Invoke(parameter);
            }
        }
    }
}