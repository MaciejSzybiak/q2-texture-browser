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

using UnityEngine;
using System;

/// <summary>
/// Stores texture information.
/// </summary>
public class Q2Texture
{
    public string Name { get; private set; }
    public string Path { get; private set; }
    public bool HasJPG { get; private set; }
    public bool HasPNG { get; private set; }

    //properties
    public Vector2Int Dimensions = new Vector2Int();
    public int FileSize;
    public DateTime ModificationDate;
    public uint flags;
    public uint contents;
    public uint value;

    public Q2Texture(string name, string path, bool hasJPG, bool hasPNG)
    {
        Name = name;
        Path = path;
        HasJPG = hasJPG;
        HasPNG = hasPNG;
    }
}
