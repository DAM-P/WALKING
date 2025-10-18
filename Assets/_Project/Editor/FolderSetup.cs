using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class FolderSetup
{
    // 定义菜单项的路径
    private const string MenuItemPath = "Tools/Project Setup/Create Default Folders";

    // 主项目文件夹名
    private const string ProjectFolderName = "_Project";

    [MenuItem(MenuItemPath)]
    public static void CreateDefaultFolders()
    {
        // 创建一个列表，包含所有需要创建的文件夹路径
        var folders = new List<string>
        {
            // 顶层文件夹
            "Art",
            "Audio",
            "Prefabs",
            "Scenes",
            "Scripts",
            "Settings",
        };

        // Scripts内部的功能模块文件夹
        var scriptSubFolders = new List<string>
        {
            "Core",
            "Player",
            "Enemies",
            "Weapons",
            "UI"
        };
        
        foreach (var subFolder in scriptSubFolders)
        {
            folders.Add($"Scripts/{subFolder}/Authoring");
            folders.Add($"Scripts/{subFolder}/Components");
            folders.Add($"Scripts/{subFolder}/Systems");
        }

        // Art内部的子文件夹
        folders.Add("Art/Materials");
        folders.Add("Art/Models");
        folders.Add("Art/Shaders");
        folders.Add("Art/Textures");

        // Audio内部的子文件夹
        folders.Add("Audio/Music");
        folders.Add("Audio/SFX");

        // 创建所有文件夹
        foreach (var folder in folders)
        {
            // 使用Path.Combine来安全地拼接路径
            string path = Path.Combine(Application.dataPath, ProjectFolderName, folder);

            // Directory.CreateDirectory会递归创建所有不存在的父文件夹
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                // 创建一个.gitkeep文件，以确保空文件夹能被Git追踪
                CreateGitKeepFile(path);
            }
        }

        // 刷新Asset数据库，让新创建的文件夹在Unity编辑器中显示出来
        AssetDatabase.Refresh();

        // 在控制台打印成功信息
        Debug.Log("Project folders created successfully under Assets/_Project/ !");
    }

    private static void CreateGitKeepFile(string path)
    {
        string gitKeepPath = Path.Combine(path, ".gitkeep");
        if (!File.Exists(gitKeepPath))
        {
            // File.Create会返回一个FileStream，需要立即关闭它以释放文件句柄
            File.Create(gitKeepPath).Close();
        }
    }
}