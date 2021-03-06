﻿//
//  Watch It Once
//  Copyright (C) 2013 Victor Tereschenko
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//     
// ========================================================================

using System;
using System.Windows.Forms;
using WatchItOnce.MediaFileIterator;

namespace WatchItOnce
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Options options = new Options();
            try
            {
                options.Load(args);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            MediaFile[] files = MediaFileScanner.GetFromFolder(System.IO.Directory.GetCurrentDirectory(), options.Filter,
                options.Extensions.ToArray());
            if (files.Length == 0)
            {
                MessageBox.Show("No files to play");
                return;
            }

            IMediaFileIterator mediaFiles = createIterator(options, files);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            PlayerOptions playerOptions = new PlayerOptions(options.AutoNext);
            var playerWindow = new PlayerWindow(mediaFiles, playerOptions);
            playerWindow.OnMediaSkipped += new OnMediaSkippedDelegate(playerWindow_OnMediaSkipped);
            if (options.DeleteAfterWatch)
                playerWindow.OnMediaEnded += new OnMediaEndedDelegate(playerWindow_OnMediaEnded);
            Application.Run(playerWindow);
        }

        private static IMediaFileIterator createIterator(Options options, MediaFile[] files)
        {
            IMediaFileIterator mediaFiles;
            switch (options.SortOrder)
            {
                default:
                case SortOrder.Default:
                    mediaFiles = new OrderedIterator(files);
                    break;
                case SortOrder.Random:
                    mediaFiles = new RandomIterator(files, false);
                    break;
                case SortOrder.RandomContinue:
                    mediaFiles = new RandomIterator(files, true);
                    break;
                case SortOrder.ByName:
                    mediaFiles = new SoredByNameIterator(files);
                    break;
            }

            return mediaFiles;
        }

        static void playerWindow_OnMediaEnded(MediaFile file)
        {
            string infoPath = file.Path + ".info";
            if (System.IO.File.Exists(infoPath))
                System.IO.File.Delete(infoPath);

            System.IO.FileAttributes attrs = System.IO.File.GetAttributes(file.Path);
            if (attrs.HasFlag(System.IO.FileAttributes.ReadOnly))
                System.IO.File.SetAttributes(file.Path, System.IO.FileAttributes.Normal);
            System.IO.File.Delete(file.Path);
        }

        static void playerWindow_OnMediaSkipped(MediaFile file, long lastPosition)
        {
            string infoPath = file.Path + ".info";
            System.IO.File.WriteAllText(infoPath, lastPosition.ToString());
        }
    }
}
