using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Forms
{
    public partial class InstallerForm : Form
    {
        bool busy = false;

        public InstallerForm()
        {
            InitializeComponent();
        }

        private void InstallerForm_Load(object sender, EventArgs e)
        {
            PkgInstaller.installerForm = this;
            Print($"Welcome to the package manager.{Environment.NewLine}Here you can install packages by ticking or uninstall by unticking them.");
            RefreshGui();
            EnableRequired();
            if (pkgList.SelectedIndex < 0)
                pkgList.SelectedIndex = 0;
        }

        void SetBusy (bool state)
        {
            busy = state;
            downloadPackagesBtn.Enabled = !state;
            redownloadPkgsBtn.Enabled = !state;
            doneBtn.Enabled = !state;
        }

        private void downloadPackagesBtn_Click(object sender, EventArgs e)
        {
            UpdatePackages(false);
        }


        private async void redownloadPkgsBtn_Click(object sender, EventArgs e)
        {
            FlowPackage pkg = PkgUtils.GetPkg(pkgList.SelectedItem.ToString());
            if (PkgUtils.IsInstalled(pkg))     // Uninstall first if force = true, to ensure a clean reinstall
                PkgInstaller.Uninstall(pkg.fileName);
            await Task.Delay(10);
            await PkgInstaller.DownloadAndInstall(pkg.fileName);
        }

        async void UpdatePackages (bool force)
        {
            SetBusy(true);
            EnableRequired();
            for (int i = 0; i < pkgList.Items.Count; i++)
            {
                FlowPackage pkg = PkgUtils.GetPkg(pkgList.Items[i].ToString());

                if(force && PkgUtils.IsInstalled(pkg))     // Uninstall first if force = true, to ensure a clean reinstall
                    PkgInstaller.Uninstall(pkg.fileName);

                bool install = pkgList.GetItemChecked(i);

                if(install && !PkgUtils.IsInstalled(pkg))   // Install if not installed
                    await PkgInstaller.DownloadAndInstall(pkg.fileName);

                if (!install && PkgUtils.IsInstalled(pkg))   // Uninstall if installed
                    PkgInstaller.Uninstall(pkg.fileName);
            }
            Print("All tasks completed.");
            SetBusy(false);
        }

        void RefreshGui ()
        {
            pkgList.Items.Clear();
            foreach (FlowPackage pkg in PkgInstaller.packages)
                pkgList.Items.Add(pkg.friendlyName, PkgUtils.IsInstalled(pkg));
        }

        void EnableRequired ()
        {
            for (int i = 0; i < pkgList.Items.Count; i++)
            {
                if(pkgList.Items[i].ToString().ToLower().Contains("required"))
                    pkgList.SetItemChecked(i, true);
            }
        }

        public void Print(string s, bool replaceLastLine = false)
        {
            if (replaceLastLine)
            {
                try
                {
                    logBox.Text = logBox.Text.Remove(logBox.Text.LastIndexOf(Environment.NewLine));
                }
                catch { }
            }
            if (string.IsNullOrWhiteSpace(logBox.Text))
                logBox.Text += s;
            else
                logBox.Text += Environment.NewLine + s;
            logBox.SelectionStart = logBox.Text.Length;
            logBox.ScrollToCaret();
        }

        private void InstallerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(busy || !HasRequiredPkgs(true))
                e.Cancel = true;
        }

        bool HasRequiredPkgs (bool silent = false)
        {
            bool isOk = true;
            for (int i = 0; i < pkgList.Items.Count; i++)
            {
                if (pkgList.Items[i].ToString().ToLower().Contains("required"))
                {
                    string frName = pkgList.Items[i].ToString();
                    if (!PkgUtils.IsInstalled(PkgUtils.GetPkg(frName)))
                    {
                        if(!silent)
                            Print($"The package {pkgList.Items[i].ToString().Wrap()} is required but not installed!");
                        isOk = false;
                        EnableRequired();
                    }
                }
            }
            return isOk;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            if (HasRequiredPkgs())
                Close();
            else
                Print("Please click \"Download/Update Packages\".");
        }

        private void pkgList_SelectedIndexChanged(object sender, EventArgs e)
        {
            FlowPackage pkg = PkgUtils.GetPkg(PkgUtils.GetPkg(pkgList.SelectedItem.ToString()).friendlyName);
            GetPkgInfo(pkg);
        }

        void GetPkgInfo(FlowPackage pkg)
        {
            string nl = Environment.NewLine;
            pkgInfoTextbox.Text = $"Friendly Name: {pkg.friendlyName}";
            pkgInfoTextbox.Text += $"{nl}Package Name: {pkg.fileName}";
            pkgInfoTextbox.Text += $"{nl}Download Size: {pkg.downloadSizeMb} MB";
            pkgInfoTextbox.Text += $"{nl}{nl}Description:{nl}{pkg.desc}";
        }
    }
}
