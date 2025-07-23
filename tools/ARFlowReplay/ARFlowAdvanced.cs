using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Google.Protobuf;
using Grpc.Net.Client;
using Newtonsoft.Json;

namespace ARFlowReplayGUI
{
    // Windows Forms GUI Application
    public class ARFlowReplayForm : Form
    {
        private TextBox txtDataPath;
        private TextBox txtServerUrl;
        private ListBox lstFrames;
        private Button btnBrowse;
        private Button btnConnect;
        private Button btnPlay;
        private Button btnPause;
        private Button btnStop;
        private TrackBar trkSpeed;
        private TrackBar trkProgress;
        private Label lblStatus;
        private Label lblSpeed;
        private Label lblFrameInfo;
        private CheckBox chkLoop;
        private CheckBox chkSkipFrames;
        private NumericUpDown numSkipInterval;
        private ProgressBar progressBar;
        private GroupBox grpMetadata;
        private TextBox txtMetadata;

        private ARFlowReplayEngine _replayEngine;
        private List<string> _frameFiles;
        private bool _isConnected = false;

        public ARFlowReplayForm()
        {
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "ARFlow Data Replay Tool";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Data Path Section
            var lblDataPath = new Label
            {
                Text = "Data Path:",
                Location = new Point(10, 15),
                Size = new Size(70, 20)
            };

            txtDataPath = new TextBox
            {
                Location = new Point(85, 12),
                Size = new Size(300, 20)
            };

            btnBrowse = new Button
            {
                Text = "Browse...",
                Location = new Point(390, 10),
                Size = new Size(75, 25)
            };
            btnBrowse.Click += BtnBrowse_Click;

            // Server URL Section
            var lblServerUrl = new Label
            {
                Text = "Server URL:",
                Location = new Point(10, 45),
                Size = new Size(70, 20)
            };

            txtServerUrl = new TextBox
            {
                Text = "http://192.168.1.219:8500",
                Location = new Point(85, 42),
                Size = new Size(300, 20)
            };

            btnConnect = new Button
            {
                Text = "Connect",
                Location = new Point(390, 40),
                Size = new Size(75, 25)
            };
            btnConnect.Click += BtnConnect_Click;

            // Frame List
            var lblFrames = new Label
            {
                Text = "Frames:",
                Location = new Point(10, 75),
                Size = new Size(50, 20)
            };

            lstFrames = new ListBox
            {
                Location = new Point(10, 95),
                Size = new Size(455, 200),
                SelectionMode = SelectionMode.MultiExtended
            };
            lstFrames.SelectedIndexChanged += LstFrames_SelectedIndexChanged;

            // Playback Controls
            var grpPlayback = new GroupBox
            {
                Text = "Playback Controls",
                Location = new Point(10, 305),
                Size = new Size(455, 120)
            };

            btnPlay = new Button
            {
                Text = "▶ Play",
                Location = new Point(10, 25),
                Size = new Size(60, 30),
                Enabled = false
            };
            btnPlay.Click += BtnPlay_Click;

            btnPause = new Button
            {
                Text = "⏸ Pause",
                Location = new Point(75, 25),
                Size = new Size(60, 30),
                Enabled = false
            };
            btnPause.Click += BtnPause_Click;

            btnStop = new Button
            {
                Text = "⏹ Stop",
                Location = new Point(140, 25),
                Size = new Size(60, 30),
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            lblSpeed = new Label
            {
                Text = "Speed: 1.0x",
                Location = new Point(220, 30),
                Size = new Size(80, 20)
            };

            trkSpeed = new TrackBar
            {
                Location = new Point(300, 25),
                Size = new Size(140, 30),
                Minimum = 1,
                Maximum = 50,
                Value = 10,
                TickFrequency = 5
            };
            trkSpeed.ValueChanged += TrkSpeed_ValueChanged;

            chkLoop = new CheckBox
            {
                Text = "Loop",
                Location = new Point(10, 65),
                Size = new Size(60, 20)
            };

            chkSkipFrames = new CheckBox
            {
                Text = "Skip frames:",
                Location = new Point(80, 65),
                Size = new Size(90, 20)
            };

            numSkipInterval = new NumericUpDown
            {
                Location = new Point(175, 63),
                Size = new Size(50, 20),
                Minimum = 1,
                Maximum = 100,
                Value = 1
            };

            grpPlayback.Controls.AddRange(new Control[] { 
                btnPlay, btnPause, btnStop, lblSpeed, trkSpeed, 
                chkLoop, chkSkipFrames, numSkipInterval 
            });

            // Progress
            trkProgress = new TrackBar
            {
                Location = new Point(10, 435),
                Size = new Size(455, 30),
                Enabled = false
            };
            trkProgress.ValueChanged += TrkProgress_ValueChanged;

            progressBar = new ProgressBar
            {
                Location = new Point(10, 470),
                Size = new Size(455, 20)
            };

            // Metadata Display
            grpMetadata = new GroupBox
            {
                Text = "Frame Metadata",
                Location = new Point(480, 10),
                Size = new Size(300, 280)
            };

            txtMetadata = new TextBox
            {
                Location = new Point(10, 20),
                Size = new Size(280, 250),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            grpMetadata.Controls.Add(txtMetadata);

            // Status
            lblStatus = new Label
            {
                Text = "Ready",
                Location = new Point(10, 500),
                Size = new Size(455, 20),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblFrameInfo = new Label
            {
                Text = "Frame: 0/0",
                Location = new Point(480, 305),
                Size = new Size(300, 20)
            };

            // Add all controls
            this.Controls.AddRange(new Control[] {
                lblDataPath, txtDataPath, btnBrowse,
                lblServerUrl, txtServerUrl, btnConnect,
                lblFrames, lstFrames, grpPlayback,
                trkProgress, progressBar, grpMetadata,
                lblStatus, lblFrameInfo
            });
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select ARFlow data folder";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtDataPath.Text = dialog.SelectedPath;
                    LoadFrameFiles();
                }
            }
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtDataPath.Text))
            {
                MessageBox.Show("Please select a data folder first.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblStatus.Text = "Connecting to server...";
                _replayEngine = new ARFlowReplayEngine(txtDataPath.Text, txtServerUrl.Text);
                await _replayEngine.ConnectAsync();
                
                _isConnected = true;
                btnConnect.Text = "Connected";
                btnConnect.Enabled = false;
                btnPlay.Enabled = true;
                lblStatus.Text = "Connected to server";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text 