﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Implementation;
using Declarations;
using Declarations.Players;
using Declarations.Events;
using Declarations.Media;

namespace WatchItOnce
{
    public delegate void OnMediaEndedDelegate(MediaFile file);
    public delegate void OnMediaSkippedDelegate(MediaFile file, long lastPosition);

    public partial class PlayerWindow : Form
    {
        public PlayerWindow(IMediaFileIterator files)
        {
            mFiles = files;
            InitializeComponent();

            var playerPath = System.Windows.Forms.Application.StartupPath;
            mPlayerFactory = new MediaPlayerFactory(playerPath);
            mPlayer = mPlayerFactory.CreatePlayer<IVideoPlayer>();
            mPlayerController = new PlayerController(mPlayer, mPlayerFactory);

            mPlayer.Events.MediaEnded += new EventHandler(Events_MediaEnded);
            mPlayer.Events.PlayerStopped += new EventHandler(Events_PlayerStopped);

            mPlayer.WindowHandle = Handle;
            mPlayer.KeyInputEnabled = false;

            KeyDown += new System.Windows.Forms.KeyEventHandler(playerWindow_KeyDown);
        }

        public event OnMediaEndedDelegate OnMediaEnded;
        public event OnMediaSkippedDelegate OnMediaSkipped;

        IMediaFileIterator mFiles;

        IMediaPlayerFactory mPlayerFactory;
        IVideoPlayer mPlayer;
        PlayerController mPlayerController;
        int mLastWidth;
        int mLastHeight;
        int mLastTop;
        int mLastLeft;
        MediaFile mPlayingFile;

        private void EnterFullscreen()
        {
            mLastWidth = Width;
            mLastHeight = Height;
            mLastTop = Top;
            mLastLeft = Left;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }

        private void EscapeFullscreen()
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            Width = mLastWidth;
            Height = mLastHeight;
            Top = mLastTop;
            Left = mLastLeft;
        }

        void Open(MediaFile file)
        {
            mPlayingFile = file;

            string fullFileName = mPlayingFile.Path;
            if (mPlayerController.IsPlaying)
                mPlayerController.Stop();
            mPlayerController.Open(fullFileName);
            Text = System.IO.Path.GetFileNameWithoutExtension(fullFileName);
        }

        private bool playNextVideo()
        {
            MediaFile next = mFiles.GetNextFile();
            if (next == null)
            {
                mPlayingFile = null;
                return false;
            }
            if (mPlayingFile != null && OnMediaSkipped != null)
                OnMediaSkipped(mPlayingFile, (long)(mPlayer.Length * mPlayer.Position / 1000));

            this.BeginInvoke(new Action(delegate
            {
                int vol = mPlayer.Volume;
                Open(next);
                mPlayerController.Play();
                mPlayerController.Seek(next.PositionSeconds * 1000);
                mPlayer.Volume = vol;
            }));
            return true;
        }

        #region speed control
        float mSpeed = 1.0f;
        private void speedOnOff()
        {
            if (mPlayer.PlaybackRate == 1.0f)
                mPlayer.PlaybackRate = mSpeed;
            else
            {
                mSpeed = mPlayer.PlaybackRate;
                mPlayer.PlaybackRate = 1.0f;
            }
        }
        #endregion

        private void seekSeconds(int seconds)
        {
            mPlayer.Time = Math.Min(mPlayer.Length, Math.Max(0, mPlayer.Time + seconds * 1000));
        }

        private static int getSeekSeconds()
        {
            int seek = 5;
            if ((Control.ModifierKeys & Keys.Alt) != 0)
                seek *= 2;
            if ((Control.ModifierKeys & Keys.Control) != 0)
                seek *= 3;
            if ((Control.ModifierKeys & Keys.Shift) != 0)
                seek *= 4;
            return seek;
        }

        private static int getVolumeLeap()
        {
            int gain = 1;
            if ((Control.ModifierKeys & Keys.Alt) != 0)
                gain *= 2;
            if ((Control.ModifierKeys & Keys.Control) != 0)
                gain *= 3;
            if ((Control.ModifierKeys & Keys.Shift) != 0)
                gain *= 4;
            return gain;
        }

        void playerWindow_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (WindowState == FormWindowState.Maximized)
                        EscapeFullscreen();
                    else
                        EnterFullscreen();
                    break;
                case Keys.B:
                    if (mPlayerController.IsPlaying)
                    {
                        mPlayerController.Pause();
                        EscapeFullscreen();
                    }
                    e.Handled = true;
                    break;
                case Keys.N:
                    if (!playNextVideo())
                        mPlayerController.Pause();
                    break;
                case Keys.M:
                    if (System.Windows.Forms.MessageBox.Show("Are you sure?", "Mark as watched?", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.Yes)
                        break;
                    mPlayerController.Stop();
                    if (OnMediaEnded != null)
                    {
                        OnMediaEnded(mPlayingFile);
                        mPlayingFile = null;
                    }
                    playNextVideo();
                    break;
                case Keys.D0:
                    speedOnOff();
                    break;
                case Keys.D1:
                    mPlayer.PlaybackRate = 1.1f;
                    break;
                case Keys.D2:
                    mPlayer.PlaybackRate = 1.2f;
                    break;
                case Keys.D3:
                    mPlayer.PlaybackRate = 1.3f;
                    break;
                case Keys.D4:
                    mPlayer.PlaybackRate = 1.4f;
                    break;
                case Keys.D5:
                    mPlayer.PlaybackRate = 1.5f;
                    break;
                case Keys.D6:
                    mPlayer.PlaybackRate = 1.6f;
                    break;
                case Keys.D7:
                    mPlayer.PlaybackRate = 1.7f;
                    break;
                case Keys.D8:
                    mPlayer.PlaybackRate = 1.8f;
                    break;
                case Keys.D9:
                    mPlayer.PlaybackRate = 1.9f;
                    break;
                case Keys.Escape:
                    if (WindowState == FormWindowState.Maximized)
                        EscapeFullscreen();
                    break;
                case Keys.Space:
                    if (mPlayerController.IsPlaying)
                        mPlayerController.Pause();
                    else
                        mPlayerController.Play();
                    break;
                case Keys.Left:
                    {
                        int seek = getSeekSeconds();
                        seekSeconds(-seek);
                    }
                    e.Handled = true;
                    break;
                case Keys.Right:
                    {
                        int seek = getSeekSeconds();
                        seekSeconds(seek);
                    }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    {
                        int gain = getVolumeLeap();
                        mPlayer.Volume = Math.Min(100, mPlayer.Volume + gain);
                    }
                    e.Handled = true;
                    break;
                case Keys.Down:
                    {
                        int gain = getVolumeLeap();
                        mPlayer.Volume = Math.Max(0, mPlayer.Volume - gain);
                    }
                    e.Handled = true;
                    break;
            }
        }

        void Events_StateChanged(object sender, MediaStateChange e)
        {
            switch (e.NewState)
            {
                case MediaState.NothingSpecial:
                case MediaState.Buffering:
                    // do nothing
                    break;
                case MediaState.Opening:
                    mPlayer.Time = (long)Math.Round(mPlayingFile.PositionSeconds * 1000.0);
                    break;
            }
        }

        void Events_PlayerStopped(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(delegate
            {
                
            }));
        }

        void Events_MediaEnded(object sender, EventArgs e)
        {
            mPlayerController.Stop();
            if (OnMediaEnded != null)
            {
                OnMediaEnded(mPlayingFile);
                mPlayingFile = null;
            }
            if (!playNextVideo())
                mPlayerController.Pause();
        }

        private void PlayerWindow_Load(object sender, EventArgs e)
        {
            playNextVideo();
        }

        private void PlayerWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (OnMediaSkipped != null && mPlayingFile != null)
                OnMediaSkipped(mPlayingFile, (long)(mPlayer.Length * mPlayer.Position / 1000));
        }
    }
}
