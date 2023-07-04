﻿using System.IO;
using System.Text;
using DevToys.Api;
using DevToys.Windows.Helpers;
using Microsoft.Win32;

namespace DevToys.Windows.Core;

[Export(typeof(IFileStorage))]
internal sealed class FileStorage : IFileStorage
{
    public string AppCacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevToys");

    public bool FileExists(string relativeOrAbsoluteFilePath)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        return File.Exists(relativeOrAbsoluteFilePath);
    }

    public Stream OpenReadFile(string relativeOrAbsoluteFilePath)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        if (!File.Exists(relativeOrAbsoluteFilePath))
        {
            throw new FileNotFoundException("Unable to find the indicated file.", relativeOrAbsoluteFilePath);
        }

        return File.OpenRead(relativeOrAbsoluteFilePath);
    }

    public Stream OpenWriteFile(string relativeOrAbsoluteFilePath, bool replaceIfExist)
    {
        if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
        {
            relativeOrAbsoluteFilePath = Path.Combine(AppCacheDirectory, relativeOrAbsoluteFilePath);
        }

        if (File.Exists(relativeOrAbsoluteFilePath) && replaceIfExist)
        {
            File.Delete(relativeOrAbsoluteFilePath);
        }

        string parentDirectory = Path.GetDirectoryName(relativeOrAbsoluteFilePath)!;
        if (!Directory.Exists(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        return File.OpenWrite(relativeOrAbsoluteFilePath);
    }

    public async ValueTask<Stream?> PickSaveFileAsync(string[] fileTypes)
    {
        return await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var saveFileDialog = new SaveFileDialog
            {
                OverwritePrompt = true
            };

            if (fileTypes is not null)
            {
                saveFileDialog.Filter = GenerateFilter(fileTypes);
            }

            if (saveFileDialog.ShowDialog() == true)
            {
                if (File.Exists(saveFileDialog.FileName))
                {
                    // Clear the file.
                    using FileStream fileStream = File.Open(saveFileDialog.FileName, FileMode.Open);
                    fileStream.SetLength(0);
                    fileStream.Close();
                }

                return saveFileDialog.OpenFile();
            }

            return null;
        });
    }

    public async ValueTask<Stream?> PickOpenFileAsync(string[] fileTypes)
    {
        return await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                ShowReadOnly = true
            };

            if (fileTypes is not null)
            {
                openFileDialog.Filter = GenerateFilter(fileTypes);
            }

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.OpenFile();
            }

            return null;
        });
    }

    public ValueTask<string?> PickFolderAsync()
    {
        // TODO: prompt the user to type in the console a relative or absolute file path that has one of the file types indicated.
        throw new NotImplementedException();
    }

    private static string GenerateFilter(string[] fileTypes)
    {
        var filters = new StringBuilder();
        foreach (string fileType in fileTypes)
        {
            filters.Append($"{fileType}|{fileType}|");
        }

        if (filters.Length > 0)
        {
            filters.Remove(filters.Length - 1, 1);
        }
        return filters.ToString();
    }
}
