﻿using com.Lavaver.WorldBackup.Core;
using System.Xml.Linq;
using com.Lavaver.WorldBackup.Global;
using com.Lavaver.WorldBackup.Database.MySQL;

namespace com.Lavaver.WorldBackup.Database
{
    internal class RecoveryFile
    {

        public static void RestoreData()
        {
            if (SQLConfig.IsEnabled())
            {
                LogConsole.Log("备份数据库", "正在读数据库...", ConsoleColor.Green);
                string DbData = Tables.GetBackupTable();
                if (DbData == null)
                {
                    LogConsole.Log("备份数据库", "未找到备份记录", ConsoleColor.Yellow);
                    return;
                }
                SQL_RestoreData(DbData);
                return;
            }
            else
            {
                if (!File.Exists(GlobalString.DatabaseLocation()))
                {
                    LogConsole.Log("备份数据库", "未找到备份数据库文件。请先正常运行程序以自动备份并生成数据库信息后再试。", ConsoleColor.Red);
                    return;
                }
            }

            if (!File.Exists(GlobalString.SoftwareConfigLocation()))
            {
                LogConsole.Log("配置文件", "未找到配置文件。请检查配置文件路径。", ConsoleColor.Red);
                return;
            }

            try
            {
                // 加载 XML 文件
                XDocument doc = XDocument.Load(GlobalString.DatabaseLocation());
                XDocument configDoc = XDocument.Load(GlobalString.SoftwareConfigLocation());

                if (doc.Root == null || doc.Root.Elements("Backup").Count() == 0)
                {
                    LogConsole.Log("备份数据库", "未找到备份记录", ConsoleColor.Yellow);
                    return;
                }

                // 查询和解析 XML 内容
                var backups = doc.Descendants("Backup")
                                 .Select(backup => new
                                 {
                                     Element = backup,
                                     Identifier = backup.Element("Identifier")?.Value,
                                     Time = backup.Element("Time")?.Value,
                                     Path = backup.Element("Path")?.Value,
                                 })
                                 .ToList();

                int selectedIndex = 0;

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("{0,-40} {1,-20} {2}", "Identifier", "Time", "Path");
                    Console.WriteLine(new string('-', 100));

                    for (int i = 0; i < backups.Count; i++)
                    {
                        if (i == selectedIndex)
                        {
                            Console.BackgroundColor = ConsoleColor.Gray;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }

                        Console.WriteLine("{0,-40} {1,-20} {2}", backups[i].Identifier, backups[i].Time, backups[i].Path);

                        if (i == selectedIndex)
                        {
                            Console.ResetColor();
                        }
                    }

                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.UpArrow)
                    {
                        selectedIndex = (selectedIndex > 0) ? selectedIndex - 1 : backups.Count - 1;
                    }
                    else if (key == ConsoleKey.DownArrow)
                    {
                        selectedIndex = (selectedIndex + 1) % backups.Count;
                    }
                    else if (key == ConsoleKey.Enter)
                    {
                        Console.WriteLine("\n确认要恢复该备份吗？(Y/N)");
                        var confirmKey = Console.ReadKey(true).Key;
                        if (confirmKey == ConsoleKey.Y)
                        {
                            var backupPath = backups[selectedIndex].Path;
                            var sourceElement = configDoc.Descendants("source").FirstOrDefault();

                            if (sourceElement != null)
                            {
                                var targetPath = sourceElement.Value;

                                // 恢复备份：删除目标路径现有内容，然后复制备份内容
                                RestoreBackup(backupPath, targetPath);

                                LogConsole.Log("备份数据库", "恢复成功", ConsoleColor.Green);
                            }
                            else
                            {
                                LogConsole.Log("配置文件", "配置文件中未找到 source 元素。", ConsoleColor.Red);
                            }
                            break;
                        }
                        else if (confirmKey == ConsoleKey.N)
                        {
                            LogConsole.Log("备份数据库", "取消恢复", ConsoleColor.Yellow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogConsole.Log("备份数据库", $"读取数据库中存在的备份记录时出现问题：{ex.Message}", ConsoleColor.Red);
            }
        }

        public static void RestoreBackup(string backupPath, string targetPath)
        {
            // 确保目标路径存在
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            Directory.CreateDirectory(targetPath);

            // 假设 backupPath 是文件夹路径，将其内容复制到 targetPath
            foreach (var dirPath in Directory.GetDirectories(backupPath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(backupPath, targetPath));
            }

            foreach (var filePath in Directory.GetFiles(backupPath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(backupPath, targetPath), true);
            }
        }

        /// <summary>
        /// 恢复备份数据（MySQL）
        /// </summary>
        /// <param name="returnData">返回的备份数据（纯文本，XML 格式）</param>
        public static void SQL_RestoreData(string returnData)
        {
            // 将返回文本数据转换为 XML 文档
            XDocument doc = XDocument.Parse(returnData);

            // 解析 XML 内容
            var backups = doc.Descendants("Backup")
                             .Select(backup => new
                             {
                                 Element = backup,
                                 Identifier = backup.Element("Identifier")?.Value,
                                 Time = backup.Element("Time")?.Value,
                                 Path = backup.Element("Path")?.Value,
                             })
                             .ToList();

            int selectedIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("{0,-40} {1,-20} {2}", "Identifier", "Time", "Path");
                Console.WriteLine(new string('-', 100));

                for (int i = 0; i < backups.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }

                    Console.WriteLine("{0,-40} {1,-20} {2}", backups[i].Identifier, backups[i].Time, backups[i].Path);

                    if (i == selectedIndex)
                    {
                        Console.ResetColor();
                    }
                }

                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow)
                {
                    selectedIndex = (selectedIndex > 0) ? selectedIndex - 1 : backups.Count - 1;
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    selectedIndex = (selectedIndex + 1) % backups.Count;
                }
                else if (key == ConsoleKey.Enter)
                {
                    Console.WriteLine("\n确认要恢复该备份吗？(Y/N)");
                    var confirmKey = Console.ReadKey(true).Key;
                    if (confirmKey == ConsoleKey.Y)
                    {
                        var backupPath = backups[selectedIndex].Path;
                        var sourceElement = doc.Descendants("source").FirstOrDefault();

                        if (sourceElement != null)
                        {
                            var targetPath = sourceElement.Value;

                            // 恢复备份：删除目标路径现有内容，然后复制备份内容
                            RestoreBackup(backupPath, targetPath);

                            LogConsole.Log("备份数据库", "恢复成功", ConsoleColor.Green);
                        }
                        else
                        {
                            LogConsole.Log("配置文件", "配置文件中未找到 source 元素。", ConsoleColor.Red);
                        }
                        break;
                    }
                    else if (confirmKey == ConsoleKey.N)
                    {
                        LogConsole.Log("备份数据库", "取消恢复", ConsoleColor.Yellow);
                    }
                }
            }
        }
    }
}
