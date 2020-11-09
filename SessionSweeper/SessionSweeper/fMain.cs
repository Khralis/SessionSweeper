using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SessionSweeper
{
    public partial class fMain : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public fMain()
        {
            InitializeComponent();
            //change arg3 to "0x4000" (MOD_NOREPEAT) to prevent hotkey firing multiple times when held 
            //https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
            RegisterHotKey(this.Handle, 0, 0x4000, Keys.Pause.GetHashCode());
            RegisterHotKey(this.Handle, 1, 0x4000, Keys.Scroll.GetHashCode());
            RegisterHotKey(this.Handle, 2, 0x4000, Keys.PrintScreen.GetHashCode());
            RegisterHotKey(this.Handle, 3, 0x4000, Keys.End.GetHashCode());
            DataStorage.FirewallControl.UnlockLobby();
            FormClosing += fMain_Closing;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x0312)
            {
                if (isGTAVRunning())
                {
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    int id = m.WParam.ToInt32();

                    if (key == Keys.Pause)
                    {
                        SweepSession();
                    }
                    else if (key == Keys.Scroll)
                    {
                        ToggleLockSession();
                    }
                    else if (key == Keys.PrintScreen)
                    {
                        ToggleNetwork();
                    }
                    else if (key == Keys.End)
                    {
                        ToggleAntiIdling();
                    }
                }
                else
                {
                    Activate();
                    MessageBox.Show("GTA V was not detected!", "SessionSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool isGTAVRunning()
        {
            bool isRunning = false;
            foreach (Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals("GTA5"))
                {
                    isRunning = true;
                    DataStorage.pGTAV = process;
                    break;
                }
            }
            return isRunning;
        }

        private void ToggleNetwork()
        {
            if (isGTAVRunning())
            {
                if (!tmrNetwork.Enabled)
                {
                    DataStorage.pPending.Play();
                    NetworkConnectionControl.Disconnect();
                    tmrNetwork.Start();
                }
            }
            else
            {
                Activate();
                MessageBox.Show("GTA V was not detected!", "SessionSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleAntiIdling()
        {
            DataStorage.AntiIdlingActive = !DataStorage.AntiIdlingActive;

            if (isGTAVRunning())
            {
                if (DataStorage.AntiIdlingActive)
                {
                    DataStorage.MouseEventControl.Start();
                    lblAfkStatus.BackColor = Color.Green;
                    lblAfkStatus.Text = "Auto mouse movement enabled!";
                }
                else
                {
                    DataStorage.MouseEventControl.Stop();
                    lblAfkStatus.BackColor = Color.Red;
                    lblAfkStatus.Text = "Auto mouse movement disabled!";
                }
            }
            else
            {
                Activate();
                MessageBox.Show("GTA V was not detected!", "SessionSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SweepSession()
        {
            if (isGTAVRunning())
            {
                if (!tmrResume.Enabled)
                {
                    DataStorage.pPending.Play();
                    Toolkit.SuspendProcess(DataStorage.pGTAV.Id);
                    tmrResume.Start();
                }
            }
            else
            {
                Activate();
                MessageBox.Show("GTA V was not detected!", "SessionSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleLockSession()
        {
            if (!DataStorage.HasAdministrativeRight) { return; }
            if (isGTAVRunning())
            {
                if (DataStorage.LobbyLocked)
                {
                    DataStorage.FirewallControl.UnlockLobby(true);
                    lblSessionStatus.BackColor = Color.Green;
                    lblSessionStatus.Text = "Session is unlocked!";
                }
                else
                {
                    DataStorage.FirewallControl.LockLobby(true);
                    lblSessionStatus.BackColor = Color.Red;
                    lblSessionStatus.Text = "Session is locked!";
                }
                DataStorage.LobbyLocked = !DataStorage.LobbyLocked;
            }
            else
            {
                Activate();
                MessageBox.Show("GTA V was not detected!", "SessionSweeper", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tmrNetwork_Tick(object sender, EventArgs e)
        {
            NetworkConnectionControl.Connect();
            DataStorage.pSweeped.Play();
            tmrNetwork.Stop();
        }

        private void tmrResume_Tick(object sender, EventArgs e)
        {
            Toolkit.ResumeProcess(DataStorage.pGTAV.Id);
            DataStorage.pSweeped.Play();
            tmrResume.Stop();
        }

        private void fMain_Load(object sender, EventArgs e)
        {
            if (!DataStorage.HasAdministrativeRight)
            {
                lblSessionStatus.Text = "Run as admin to lock sessions!";
                lblSessionStatus.BackColor = Color.Red;
                lblLockSessionInfo.Font = new Font(lblLockSessionInfo.Font, FontStyle.Strikeout);
                btnScrollLock.Enabled = false;
            }

            lblAfkStatus.Text = "Auto mouse movement disabled!";
            lblAfkStatus.BackColor = Color.Red;
        }

        private void fMain_Closing(object sender, CancelEventArgs e)
        {
            DataStorage.FirewallControl.UnlockLobby();
            UnregisterHotKey(this.Handle, 0);
            UnregisterHotKey(this.Handle, 1);
            UnregisterHotKey(this.Handle, 2);
            UnregisterHotKey(this.Handle, 3);
        }

        private void btnPauseBreak_Click(object sender, EventArgs e)
        {
            SweepSession();
        }

        private void btnScrollLock_Click(object sender, EventArgs e)
        {
            ToggleLockSession();
        }

        private void btnPrintScreen_Click(object sender, EventArgs e)
        {
            ToggleNetwork();
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            ToggleAntiIdling();
        }
    }
}
