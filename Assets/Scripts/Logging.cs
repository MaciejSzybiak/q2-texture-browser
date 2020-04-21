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

using System.IO;
using UnityEngine;

public static class Logging
{
    private static StreamWriter writer;

    private static void Application_quitting()
    {
        writer.Close();
    }

    static Logging()
    {
#if !UNITY_EDITOR
        Application.quitting += Application_quitting;

        if (!Directory.Exists("logs"))
        {
            Directory.CreateDirectory("logs");
        }

        writer = File.CreateText("logs/log_" + System.DateTime.Now.ToString().Replace(':', '_').Replace('.', '_') + ".txt");
#endif
    }

    public static void Log(object message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
#else
        writer.WriteLine("LOG     " + message.ToString());
#endif
    }

    public static void Log(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.Log(message, context);
#else
        writer.WriteLine("LOG     " + message.ToString() + " context: " + context.ToString());
#endif
    }

    public static void LogWarning(object message)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message);
#else
        writer.WriteLine("WARNING " + message.ToString());
#endif
    }

    public static void LogWarning(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.LogWarning(message, context);
#else
        writer.WriteLine("WARNING " + message.ToString() + " context: " + context.ToString());
#endif
    }

    public static void LogError(object message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#else
        writer.WriteLine("ERROR   " + message.ToString());
#endif
    }

    public static void LogError(object message, Object context)
    {
#if UNITY_EDITOR
        Debug.LogError(message, context);
#else
        writer.WriteLine("ERROR   " + message.ToString() + " context: " + context.ToString());
#endif
    }
}
