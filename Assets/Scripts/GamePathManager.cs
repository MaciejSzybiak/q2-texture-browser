/**
Copyright (C) 2020 Maciej Szybiak

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see https://www.gnu.org/licenses/.
*/

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

public class GamePathManager : MonoBehaviour
{
    #region unity properties
    [SerializeField]
    private TMP_InputField pathField = default;
    #endregion

    public static string GamePath { get; private set; } = "C:/Quake2";

    /// <summary>
    /// Stores colormap color data.
    /// </summary>
    public static List<Color> colormap = new List<Color>();

    /// <summary>
    /// Gamepath is valid if the folder can be found and if colormap file is present in the editor directory.
    /// </summary>
    public static bool IsGamePathCorrect { get; private set; } = false;

    private void Start()
    {
        //check if config exists and open it
        if (File.Exists("cfg.txt"))
        {
            using (FileStream stream = new FileStream("cfg.txt", FileMode.Open))
            {
                StreamReader reader = new StreamReader(stream);
                pathField.text = reader.ReadLine();
                //bugfix: broken alignment after inserting text
                pathField.textComponent.alignment = TextAlignmentOptions.Right;
                pathField.textComponent.alignment = TextAlignmentOptions.Left;
                reader.Close();
            }
            OnPathSet(pathField.text);
        }
        else
        {
            OnPathSet("");
        }
        LoadColorMap();
    }

    /// <summary>
    /// Sets game path and triggers directory loading.
    /// </summary>
    /// <param name="path">Path to set. If empty then default location will be used.</param>
    public void OnPathSet(string path)
    {
        if (path.Length == 0)
        {
            GamePath = "C:/Quake2";
        }
        else
        {
            GamePath = path;

            using (FileStream stream = new FileStream("cfg.txt", FileMode.Create))
            {
                StreamWriter writer = new StreamWriter(stream);
                writer.WriteLine(GamePath);
                writer.Close();
            }
        }

        if (Directory.Exists(GamePath))
        {
            FileList.Instance.OnGamePathSet(GamePath);
            IsGamePathCorrect = true;
        }
        else
        {
            IsGamePathCorrect = false;
        }
    }

    /// <summary>
    /// Parses the colormap file.
    /// </summary>
    private void LoadColorMap()
    {
        BinaryReader reader;

        colormap.Clear();

        try
        {
            reader = new BinaryReader(File.OpenRead("colormap.pcx"));
        }
        catch(System.Exception)
        {
            Logging.LogError("Failed to load colormap!");
            IsGamePathCorrect = false;
            return;
        }

        //prevent file stream out of range error
        if(reader.BaseStream.Length < 768)
        {
            Logging.LogError("Incorrect colormap file!");
            IsGamePathCorrect = false;
            return;
        }

        //parse colormap colors
        reader.BaseStream.Position = reader.BaseStream.Length - 768;
        byte[] qpalette = reader.ReadBytes(768);

        Color c;

        for (int i = 0; i < qpalette.Length; i += 3)
        {
            c = new Color(
                qpalette[i] / 255f,
                qpalette[i + 1] / 255f,
                qpalette[i + 2] / 255f);

            colormap.Add(c);
        }
    }
}
